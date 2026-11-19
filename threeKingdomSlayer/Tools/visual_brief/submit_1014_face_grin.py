# -*- coding: utf-8 -*-
"""1014 投壶兵 · 单图编辑：只改脸部表情（改成 1012 山贼式的奸诈咧嘴笑）。

底件：Enemy1014ArtSource/02_IdentityImages/1014_v2_base_greenscreen_1024.png
      （= v2 组图裁切件放大重贴的 1024² 纯绿幕底件，主体高 696 / 脚底 895 / 中心 474.0）
依据：Locus/knowledge/design/enemy-1014-art-brief.md §5.5（用户 2026-11-16 选方案 C）
     Locus/knowledge/memory/enemy-proportion-lineup-reference.md（教训五/六：单图编辑、只改一处、从底件重做）
提示词唯一真源：Tools/visual_brief/prompts/1014_face_grin_v1.*
接口：POST https://api.muskapis.com/v1/images/edits（multipart，一张 image[]）
模型：gpt-image-2.6-sunburst
参数：size=1024x1024 / quality=max / input_fidelity=high / output_format=png / n=1 / 不传 background
纪律：最多提交一次，不自动重试，不覆盖已有产物；400 不计费，改措辞再试但不降级输入图

用法
    python Tools/visual_brief/submit_1014_face_grin.py --dry-run
    python Tools/visual_brief/submit_1014_face_grin.py --approve
"""
from __future__ import annotations

import argparse
import base64
import hashlib
import json
import mimetypes
import os
import struct
import time
import urllib.error
import urllib.request
import uuid

PROJ = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
PROMPT_DIR = os.path.join(PROJ, "Tools", "visual_brief", "prompts")
PROMPT_EN_FILE = "1014_face_grin_v1.txt"
PROMPT_CN_FILE = "1014_face_grin_v1.cn.txt"
NEG_FILE = "1014_face_grin_v1.negative.txt"
BASE_IMAGE = "Enemy1014ArtSource/02_IdentityImages/1014_v2_base_greenscreen_1024.png"

OUT_DIR = r"C:/Users/steam/Pictures/gptGen/enemy1014_face_grin_v1"
OUT_NAME = "1014_face_grin_v1.png"
ENDPOINT = "https://api.muskapis.com/v1/images/edits"
MODELS_ENDPOINT = "https://api.muskapis.com/v1/models"
MODEL = "gpt-image-2.6-sunburst"
SIZE = "1024x1024"
QUALITY = "max"
INPUT_FIDELITY = "high"
TIMEOUT = 600


def abs_path(p: str) -> str:
    return p if os.path.isabs(p) else os.path.join(PROJ, p.replace("/", os.sep))


def read_text(name: str) -> str:
    with open(os.path.join(PROMPT_DIR, name), "r", encoding="utf-8") as fh:
        return fh.read().strip()


def sha256_file(path: str) -> str:
    with open(path, "rb") as fh:
        return hashlib.sha256(fh.read()).hexdigest()


def png_info(path: str) -> dict:
    with open(path, "rb") as fh:
        head = fh.read(33)
    if head[:8] != b"\x89PNG\r\n\x1a\n":
        raise ValueError("not a PNG")
    w, h = struct.unpack(">II", head[16:24])
    return {"width": w, "height": h, "bit_depth": head[24], "color_type": head[25]}


def build_multipart(fields, files):
    boundary = "----locus%s" % uuid.uuid4().hex
    out = bytearray()
    for k, v in fields.items():
        out += ("--%s\r\n" % boundary).encode()
        out += ('Content-Disposition: form-data; name="%s"\r\n\r\n' % k).encode()
        out += (v + "\r\n").encode()
    for name, path in files:
        ctype = mimetypes.guess_type(path)[0] or "application/octet-stream"
        with open(path, "rb") as fh:
            blob = fh.read()
        out += ("--%s\r\n" % boundary).encode()
        out += ('Content-Disposition: form-data; name="%s"; filename="%s"\r\n'
                % (name, os.path.basename(path))).encode()
        out += ("Content-Type: %s\r\n\r\n" % ctype).encode()
        out += blob + b"\r\n"
    out += ("--%s--\r\n" % boundary).encode()
    return bytes(out), "multipart/form-data; boundary=%s" % boundary


def check_models(key: str) -> dict:
    req = urllib.request.Request(MODELS_ENDPOINT, headers={"Authorization": "Bearer %s" % key})
    with urllib.request.urlopen(req, timeout=120) as resp:
        payload = json.loads(resp.read().decode("utf-8"))
    ids = sorted(item.get("id", "") for item in payload.get("data", []))
    return {"http_status": resp.status, "models": ids,
            "image_models": [i for i in ids if "image" in i]}


def main() -> int:
    ap = argparse.ArgumentParser(description="提交 1014 脸部表情修改（单图编辑）")
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--approve", action="store_true")
    ap.add_argument("--model", default=MODEL)
    ap.add_argument("--timeout", type=int, default=TIMEOUT)
    args = ap.parse_args()

    prompt_en = read_text(PROMPT_EN_FILE)
    prompt_cn = read_text(PROMPT_CN_FILE)
    negative = read_text(NEG_FILE)
    prompt = prompt_en + "\n\nNegative constraints: " + negative

    base = abs_path(BASE_IMAGE)
    if not os.path.isfile(base):
        raise SystemExit("底件缺失: %s" % base)
    base_sha = sha256_file(base)
    info = png_info(base)
    img_out = os.path.join(OUT_DIR, OUT_NAME)

    if args.dry_run:
        print("=== 模型 ===\n%s" % args.model)
        print("=== 底件（唯一 image[]）===\n%s\n         %dx%d ct=%d sha256=%s"
              % (base, info["width"], info["height"], info["color_type"], base_sha))
        print("\n=== 提交用提示词（英文正文 + 英文负面词，%d 字符，sha256 %s）==="
              % (len(prompt), hashlib.sha256(prompt.encode("utf-8")).hexdigest()))
        print(prompt)
        print("\n=== 参数 ===")
        print("endpoint=%s\nmodel=%s size=%s quality=%s input_fidelity=%s output_format=png n=1"
              % (ENDPOINT, args.model, SIZE, QUALITY, INPUT_FIDELITY))
        print("不传 background；超时 %ds；自动重试 0；最多提交一次" % args.timeout)
        print("输出: %s" % img_out)
        return 0

    if not args.approve:
        raise SystemExit("未确认批准：加 --approve 才会真的提交（纪律：一次提交，不自动重试）")

    key = os.environ.get("MUSK_API_KEY")
    if not key:
        raise SystemExit("MUSK_API_KEY missing")
    os.makedirs(OUT_DIR, exist_ok=True)
    if os.path.exists(img_out):
        raise SystemExit("refusing to overwrite existing output: %s" % img_out)

    models = check_models(key)
    if args.model not in models["image_models"]:
        raise SystemExit("模型不可用: %s（当前图像模型 %s）" % (args.model, models["image_models"]))

    fields = {
        "model": args.model,
        "prompt": prompt,
        "size": SIZE,
        "quality": QUALITY,
        "input_fidelity": INPUT_FIDELITY,
        "output_format": "png",
        "n": "1",
    }
    body, ctype = build_multipart(fields, [("image[]", base)])

    stem = OUT_NAME.replace(".png", "")
    with open(os.path.join(OUT_DIR, stem + ".prompt.txt"), "w",
              encoding="utf-8", newline="\n") as fh:
        fh.write(prompt)
    with open(os.path.join(OUT_DIR, stem + ".prompt_cn.txt"), "w",
              encoding="utf-8", newline="\n") as fh:
        fh.write(prompt_cn)
    with open(os.path.join(OUT_DIR, stem + ".prompt_negative.txt"), "w",
              encoding="utf-8", newline="\n") as fh:
        fh.write(negative)

    with open(os.path.join(OUT_DIR, stem + ".request.json"), "w",
              encoding="utf-8") as fh:
        json.dump({
            "version": "1014_face_grin_v1",
            "handoff": r"Locus\knowledge\plan\session-handoff-20261114.md §7.10",
            "brief": r"Locus\knowledge\design\enemy-1014-art-brief.md §5.5（方案 C）",
            "endpoint": ENDPOINT,
            "method": "POST",
            "content_type": ctype,
            "fields": fields,
            "base_image": {"path": base, "sha256": base_sha,
                           "bytes": os.path.getsize(base), "size": [info["width"], info["height"]]},
            "models_check": models,
            "prompt_sha256": hashlib.sha256(prompt.encode("utf-8")).hexdigest(),
            "prompt_chars": len(prompt),
            "automatic_retries": 0,
            "read_timeout_seconds": args.timeout,
            "submitted_at_local": time.strftime("%Y-%m-%d %H:%M:%S"),
        }, fh, indent=2, ensure_ascii=False)

    request = urllib.request.Request(ENDPOINT, data=body, method="POST", headers={
        "Authorization": "Bearer %s" % key, "Content-Type": ctype})

    status = {"state": "submitted", "http_status": None, "seconds": None,
              "error": None, "output_image": img_out}
    t0 = time.time()
    try:
        with urllib.request.urlopen(request, timeout=args.timeout) as resp:
            raw = resp.read()
            status["http_status"] = resp.status
    except urllib.error.HTTPError as exc:
        status["state"] = "failed"
        status["error"] = "HTTPError %s: %s" % (exc.code, exc.read().decode("utf-8", "replace")[:800])
        status["seconds"] = round(time.time() - t0, 2)
        with open(os.path.join(OUT_DIR, stem + ".status.json"), "w",
                  encoding="utf-8") as fh:
            json.dump(status, fh, indent=2, ensure_ascii=False)
        raise SystemExit(status["error"])
    except Exception as exc:  # noqa: BLE001
        status["state"] = "failed"
        status["error"] = "%s: %s" % (type(exc).__name__, exc)
        status["seconds"] = round(time.time() - t0, 2)
        with open(os.path.join(OUT_DIR, stem + ".status.json"), "w",
              encoding="utf-8") as fh:
            json.dump(status, fh, indent=2, ensure_ascii=False)
        raise SystemExit(status["error"])

    status["seconds"] = round(time.time() - t0, 2)
    payload = json.loads(raw.decode("utf-8"))
    item = payload["data"][0]
    if item.get("b64_json"):
        blob = base64.b64decode(item["b64_json"])
        status["download"] = "b64_json"
    elif item.get("url"):
        with urllib.request.urlopen(item["url"], timeout=300) as r:
            blob = r.read()
        status["download"] = "url"
    else:
        raise SystemExit("no image payload")

    cleaned = json.loads(raw.decode("utf-8"))
    for it in cleaned.get("data", []):
        if it.get("b64_json"):
            it["b64_json"] = "<base64 omitted; image saved to %s>" % img_out
    with open(os.path.join(OUT_DIR, stem + ".response.json"), "w",
              encoding="utf-8") as fh:
        json.dump(cleaned, fh, indent=2, ensure_ascii=False)

    with open(img_out, "wb") as fh:
        fh.write(blob)
    status.update(png_info(img_out))
    status["output_bytes"] = len(blob)
    status["output_sha256"] = hashlib.sha256(blob).hexdigest()
    status["usage"] = payload.get("usage")
    status["echoed_size"] = payload.get("size")
    status["echoed_quality"] = payload.get("quality")
    status["models_check"] = models
    status["state"] = "image_saved"
    with open(os.path.join(OUT_DIR, stem + ".status.json"), "w",
              encoding="utf-8") as fh:
        json.dump(status, fh, indent=2, ensure_ascii=False)
    print(json.dumps(status, indent=2, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
