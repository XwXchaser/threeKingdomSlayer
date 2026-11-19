# -*- coding: utf-8 -*-
"""抡斧「砸地裂纹特效」贴图 · 提交脚本（2026-11-16）。

依据：Locus/knowledge/design/axe-ground-crack-vfx-spec-20261115.md
提示词（唯一真源）：Tools/visual_brief/prompts/axe_ground_crack_vfx_v1.txt
接口：POST https://api.muskapis.com/v1/images/generations（文生图，JSON）
模型：gpt-image-2.6-sunburst
参数：quality=max / size=1024x1024 / output_format=png / background=transparent / n=1
通道选择理由：skill/gpt-image-generation.md 第十轮实测——edits（带参考图）通道在某些时段是
  “家族级收紧”状态，纯文生图 generations 反而更易放行；本任务不需要造型锁，只需配色与结构描述。
纪律：400 不计费 ⇒ 被拦时先改写措辞再试一次，仍被拦再换通道（不要盲目连发）。

用法
    python Tools/visual_brief/submit_axe_ground_crack_20261116.py --dry-run
    python Tools/visual_brief/submit_axe_ground_crack_20261116.py --approve
"""
from __future__ import annotations

import argparse
import base64
import hashlib
import json
import os
import pathlib
import struct
import time
import urllib.error
import urllib.request

PROJ = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
PROMPT_FILE = os.path.join(PROJ, "Tools", "visual_brief", "prompts", "axe_ground_crack_vfx_v1.txt")
OUT_DIR = pathlib.Path("C:/Users/steam/Pictures/gptGen/axe_ground_crack_20261116")
ENDPOINT = "https://api.muskapis.com/v1/images/generations"
MODELS_ENDPOINT = "https://api.muskapis.com/v1/models"
MODEL = "gpt-image-2.6-sunburst"
SIZE = "1024x1024"
QUALITY = "max"
TIMEOUT = 900


def png_info(path) -> dict:
    with open(path, "rb") as fh:
        raw = fh.read()
    if raw[:8] != b"\x89PNG\r\n\x1a\n":
        raise ValueError("not a PNG")
    w, h = struct.unpack(">II", raw[16:24])
    color_type = raw[25]
    return {"width": w, "height": h, "bit_depth": raw[24], "color_type": color_type,
            "color_type_meaning": {0: "gray", 2: "RGB(no alpha)", 3: "palette", 4: "gray+alpha",
                                   6: "RGBA"}.get(color_type, "?")}


def alpha_histogram(path) -> dict:
    """统计 alpha 分布（用 zlib 解 IDAT 太重 ⇒ 用 PIL，缺 PIL 时跳过）。"""
    try:
        from PIL import Image
    except Exception:
        return {"available": False}
    im = Image.open(path).convert("RGBA")
    hist = im.getchannel("A").histogram()
    nonzero = sum(c for i, c in enumerate(hist) if i > 0)
    return {"available": True, "alpha_max": max(i for i, c in enumerate(hist) if c > 0) if nonzero else 0,
            "alpha_zero_ratio": round(hist[0] / float(im.width * im.height), 4),
            "corner_alpha": [im.getpixel(p)[3] for p in
                             ((0, 0), (im.width - 1, 0), (0, im.height - 1), (im.width - 1, im.height - 1))]}


def check_models(key: str) -> dict:
    req = urllib.request.Request(MODELS_ENDPOINT, headers={"Authorization": "Bearer %s" % key})
    with urllib.request.urlopen(req, timeout=120) as resp:
        payload = json.loads(resp.read().decode("utf-8"))
    ids = sorted(item.get("id", "") for item in payload.get("data", []))
    return {"http_status": resp.status, "image_models": [i for i in ids if "image" in i]}


def submit(prompt: str, key: str, background: str | None) -> tuple[bytes, dict, dict]:
    fields = {
        "model": MODEL,
        "prompt": prompt,
        "size": SIZE,
        "quality": QUALITY,
        "output_format": "png",
        "n": 1,
    }
    if background:
        fields["background"] = background
    request = urllib.request.Request(
        ENDPOINT,
        data=json.dumps(fields).encode("utf-8"),
        headers={"Authorization": "Bearer %s" % key, "Content-Type": "application/json"},
        method="POST")
    t0 = time.time()
    with urllib.request.urlopen(request, timeout=TIMEOUT) as resp:
        raw = resp.read()
    payload = json.loads(raw.decode("utf-8"))
    item = payload["data"][0]
    if item.get("b64_json"):
        blob = base64.b64decode(item["b64_json"])
        how = "b64_json"
    elif item.get("url"):
        with urllib.request.urlopen(item["url"], timeout=300) as r:
            blob = r.read()
        how = "url"
    else:
        raise RuntimeError("no image payload")
    meta = {"http_status": 200, "seconds": round(time.time() - t0, 2), "download": how,
            "usage": payload.get("usage"), "echoed_size": payload.get("size"),
            "echoed_quality": payload.get("quality"), "echoed_background": payload.get("background")}
    return blob, payload, meta


def main() -> int:
    ap = argparse.ArgumentParser(description="提交抡斧砸地裂纹特效贴图")
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--approve", action="store_true")
    ap.add_argument("--tag", default="v1")
    ap.add_argument("--prompt", default=None,
                    help="提示词文件路径（默认 Tools/visual_brief/prompts/axe_ground_crack_vfx_v1.txt）")
    ap.add_argument("--no-transparent", action="store_true",
                    help="不传 background=transparent（走纯黑底，事后本地抠）")
    args = ap.parse_args()

    prompt_path = args.prompt or PROMPT_FILE
    if not os.path.isfile(prompt_path):
        raise SystemExit("提示词文件不存在: %s（用 --prompt 指定）" % prompt_path)
    prompt = pathlib.Path(prompt_path).read_text(encoding="utf-8").strip()
    stem = "axe_ground_crack_%s" % args.tag
    img_out = OUT_DIR / ("%s.png" % stem)
    background = None if args.no_transparent else "transparent"

    if args.dry_run:
        print("=== 提示词（%d 字符，sha256 %s）===" % (len(prompt), hashlib.sha256(prompt.encode("utf-8")).hexdigest()))
        print(prompt)
        print("\n=== 参数 ===")
        print("endpoint=%s\nmodel=%s size=%s quality=%s output_format=png n=1 background=%s"
              % (ENDPOINT, MODEL, SIZE, QUALITY, background))
        print("输出: %s" % img_out)
        return 0

    if not args.approve:
        raise SystemExit("未确认批准：加 --approve 才会真的提交")

    key = os.environ.get("MUSK_API_KEY")
    if not key:
        raise SystemExit("MUSK_API_KEY missing")
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    if img_out.exists():
        raise SystemExit("refusing to overwrite existing output: %s" % img_out)

    models = check_models(key)
    if MODEL not in models["image_models"]:
        raise SystemExit("模型不可用: %s（当前图像模型 %s）" % (MODEL, models["image_models"]))

    (OUT_DIR / ("%s.prompt.txt" % stem)).write_text(prompt, encoding="utf-8", newline="\n")

    attempts = []
    try:
        blob, payload, meta = submit(prompt, key, background)
    except urllib.error.HTTPError as exc:
        body = exc.read().decode("utf-8", "replace")[:800]
        attempts.append({"http_status": exc.code, "error": body})
        with open(OUT_DIR / ("%s.status.json" % stem), "w", encoding="utf-8") as fh:
            json.dump({"state": "failed", "attempts": attempts}, fh, indent=2, ensure_ascii=False)
        raise SystemExit("HTTPError %s: %s" % (exc.code, body))
    attempts.append({"http_status": 200, "seconds": meta["seconds"]})

    # ★ 先落图，再清洗响应里的 base64（否则图片数据被自己覆盖）
    with open(img_out, "wb") as fh:
        fh.write(blob)
    cleaned = json.loads(json.dumps(payload))
    for it in cleaned.get("data", []):
        if it.get("b64_json"):
            it["b64_json"] = "<base64 omitted; image saved to %s>" % img_out
    with open(OUT_DIR / ("%s.response.json" % stem), "w", encoding="utf-8") as fh:
        json.dump(cleaned, fh, indent=2, ensure_ascii=False)

    with open(OUT_DIR / ("%s.request.json" % stem), "w", encoding="utf-8") as fh:
        json.dump({
            "version": stem,
            "plan": "Locus/knowledge/design/axe-ground-crack-vfx-spec-20261115.md §5",
            "prompt_file": prompt_path,
            "endpoint": ENDPOINT,
            "fields": {"model": MODEL, "size": SIZE, "quality": QUALITY,
                       "output_format": "png", "n": 1, "background": background},
            "reference_images": [],
            "models_check": models,
            "attempts": attempts,
            "submitted_at_local": time.strftime("%Y-%m-%d %H:%M:%S"),
        }, fh, indent=2, ensure_ascii=False)

    status = {"state": "image_saved", "output_image": str(img_out),
              "output_bytes": len(blob), "output_sha256": hashlib.sha256(blob).hexdigest()}
    status.update(png_info(img_out))
    status["alpha"] = alpha_histogram(img_out)
    status.update(meta)
    status["prompt_sha256"] = hashlib.sha256(prompt.encode("utf-8")).hexdigest()
    with open(OUT_DIR / ("%s.status.json" % stem), "w", encoding="utf-8") as fh:
        json.dump(status, fh, indent=2, ensure_ascii=False)
    print(json.dumps(status, indent=2, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
