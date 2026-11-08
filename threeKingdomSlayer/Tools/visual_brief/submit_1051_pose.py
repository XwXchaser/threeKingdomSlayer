"""1051 弓手 · 姿势图（射箭动作）GPT Image 双参考图融合提交脚本。

背景
    射箭动作的**姿势与弓箭几何**必须从工程内旧 105 弓手的动作帧继承
    （`Assets/Sprites/Enemy/Enemy5/enemy5_attack{1,2,3}.png`），
    不能靠文字转述——2026-11-08 实测：纯文字路线生成的视频把瞄准方向改到了画面右侧，验收不通过。

    做法：先用 `Tools/visual_brief/make_pose_reference.py` 把旧 105 的动作帧处理成
    **与新 1051 身份图同构图口径**的绿幕图，再作为第 2 张参考图交给生图模型：
        image 1 = 新 1051 身份图（形象唯一真源）
        image 2 = 旧 105 姿势参考图（姿势与弓箭几何唯一真源；脸/甲/红头巾不取）
    产出的姿势图再合成绿幕锚图，供视频工作流当首帧/末帧使用。

提示词 / 参考图职责
    Tools/visual_brief/prompts/1051_pose_<版本>.txt        提示词正文（唯一真源）
    Tools/visual_brief/prompts/refs_1051_pose_<版本>.json  参考图顺序与职责

接口（与 submit_1013_identity.py 同规格）
    POST https://api.muskapis.com/v1/images/edits
    multipart/form-data，图片字段名 `image[]`，多图顺序有意义
    model=gpt-image-2.6-sunburst / size=1024x1024 / quality=max / input_fidelity=high
    output_format=png / n=1 / 不传 background（要绿幕）
    超时 900s / 自动重试 0（一次提交，不自动重试）

纪律
    · 一次提交不自动重试（付费任务）；失败先报告再决定是否重提
    · 不覆盖已有产物

用法
    python Tools/visual_brief/submit_1051_pose.py --version draw --dry-run
    python Tools/visual_brief/submit_1051_pose.py --version draw --approve
"""

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
OUT_ROOT = r"C:/Users/steam/Pictures/gptGen/enemy1051_pose"
ASSET_DIR = os.path.join(PROJ, "Enemy1051ArtSource", "02_IdentityImages")

ENDPOINT = "https://api.muskapis.com/v1/images/edits"
MODEL = "gpt-image-2.6-sunburst"
SIZE = "1024x1024"
QUALITY = "max"
INPUT_FIDELITY = "high"
TIMEOUT = 900


def abs_path(p):
    return p if os.path.isabs(p) else os.path.join(PROJ, p.replace("/", os.sep))


def load_version(version):
    with open(os.path.join(PROMPT_DIR, "1051_pose_%s.txt" % version), "r", encoding="utf-8") as fh:
        prompt = fh.read().strip()
    with open(os.path.join(PROMPT_DIR, "refs_1051_pose_%s.json" % version), "r", encoding="utf-8") as fh:
        refs = json.load(fh)
    return prompt, refs


def png_info(path):
    with open(path, "rb") as fh:
        head = fh.read(33)
    if head[:8] != b"\x89PNG\r\n\x1a\n":
        raise ValueError("not a PNG")
    if head[12:16] != b"IHDR":
        raise ValueError("no IHDR")
    width, height = struct.unpack(">II", head[16:24])
    return {"width": width, "height": height, "bit_depth": head[24], "color_type": head[25]}


def save_payload(item, out_path):
    b64 = item.get("b64_json")
    url = item.get("url")
    if b64:
        blob = base64.b64decode(b64)
        with open(out_path, "wb") as fh:
            fh.write(blob)
        return {"download": "b64_json", "bytes": len(blob)}
    if url:
        with urllib.request.urlopen(url, timeout=300) as resp:
            blob = resp.read()
        with open(out_path, "wb") as fh:
            fh.write(blob)
        return {"download": "url", "url": url, "bytes": len(blob)}
    raise SystemExit("no image payload (neither b64_json nor url)")


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


def main():
    ap = argparse.ArgumentParser(description="提交 1051 弓手姿势图生成请求")
    ap.add_argument("--version", required=True, help="姿势版本：draw / release")
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--approve", action="store_true")
    args = ap.parse_args()

    prompt, refs = load_version(args.version)
    images = [(abs_path(r["path"]), r["role"]) for r in refs["references"]]
    for p, _ in images:
        if not os.path.isfile(p):
            raise SystemExit("参考图缺失: %s" % p)

    stem = "1051_pose_%s" % args.version
    img_out = os.path.join(ASSET_DIR, stem + "_raw.png")

    if args.dry_run:
        print("=== 版本 %s：%s ===" % (args.version, refs.get("title", "")))
        print("起点: %s" % refs.get("base_note", ""))
        print("\n=== 参考图（顺序）===")
        for i, (p, role) in enumerate(images, 1):
            print("image %d: %s\n         %s" % (i, p, role))
        print("\n=== 提示词（%d 字符，sha256 %s）===" % (
            len(prompt), hashlib.sha256(prompt.encode("utf-8")).hexdigest()[:16]))
        print(prompt)
        print("\n=== 参数 ===\nendpoint=%s\nmodel=%s size=%s quality=%s input_fidelity=%s output_format=png n=1"
              % (ENDPOINT, MODEL, SIZE, QUALITY, INPUT_FIDELITY))
        print("输出: %s" % img_out)
        return 0

    if not args.approve:
        raise SystemExit("未确认批准：加 --approve 才会真的提交（纪律：一次提交，不自动重试）")

    key = os.environ.get("MUSK_API_KEY")
    if not key:
        raise SystemExit("MUSK_API_KEY missing")
    os.makedirs(OUT_ROOT, exist_ok=True)
    os.makedirs(ASSET_DIR, exist_ok=True)
    if os.path.exists(img_out):
        raise SystemExit("refusing to overwrite existing output: %s" % img_out)

    fields = {
        "model": MODEL,
        "prompt": prompt,
        "size": SIZE,
        "quality": QUALITY,
        "input_fidelity": INPUT_FIDELITY,
        "output_format": "png",
        "n": "1",
    }
    files = [("image[]", p) for p, _ in images]
    body, ctype = build_multipart(fields, files)

    with open(os.path.join(OUT_ROOT, stem + ".prompt.txt"), "w", encoding="utf-8", newline="\n") as fh:
        fh.write(prompt)
    with open(os.path.join(OUT_ROOT, stem + ".request.json"), "w", encoding="utf-8") as fh:
        json.dump({
            "version": args.version,
            "endpoint": ENDPOINT,
            "method": "POST",
            "content_type": ctype,
            "fields": fields,
            "reference_images": [{"index": i + 1, "path": p, "role": role,
                                  "sha256": hashlib.sha256(open(p, "rb").read()).hexdigest()}
                                 for i, (p, role) in enumerate(images)],
            "prompt_sha256": hashlib.sha256(prompt.encode("utf-8")).hexdigest(),
            "prompt_chars": len(prompt),
            "automatic_retries": 0,
            "read_timeout_seconds": TIMEOUT,
            "submitted_at_local": time.strftime("%Y-%m-%d %H:%M:%S"),
        }, fh, indent=2, ensure_ascii=False)

    request = urllib.request.Request(
        ENDPOINT, data=body, method="POST",
        headers={"Authorization": "Bearer %s" % key, "Content-Type": ctype})

    status = {"state": "submitted", "http_status": None, "seconds": None,
              "error": None, "output_image": img_out}
    t0 = time.time()
    try:
        with urllib.request.urlopen(request, timeout=TIMEOUT) as resp:
            raw = resp.read()
            status["http_status"] = resp.status
    except urllib.error.HTTPError as exc:
        status["state"] = "failed"
        status["error"] = "HTTPError %s: %s" % (exc.code, exc.read().decode("utf-8", errors="replace")[:800])
        status["seconds"] = round(time.time() - t0, 2)
        with open(os.path.join(OUT_ROOT, stem + ".status.json"), "w", encoding="utf-8") as fh:
            json.dump(status, fh, indent=2, ensure_ascii=False)
        raise SystemExit(status["error"])
    except Exception as exc:  # noqa: BLE001
        status["state"] = "failed"
        status["error"] = "%s: %s" % (type(exc).__name__, exc)
        status["seconds"] = round(time.time() - t0, 2)
        with open(os.path.join(OUT_ROOT, stem + ".status.json"), "w", encoding="utf-8") as fh:
            json.dump(status, fh, indent=2, ensure_ascii=False)
        raise SystemExit(status["error"])

    status["seconds"] = round(time.time() - t0, 2)
    payload = json.loads(raw.decode("utf-8"))
    status.update(save_payload(payload["data"][0], img_out))

    cleaned = json.loads(raw.decode("utf-8"))
    for item in cleaned.get("data", []):
        if item.get("b64_json"):
            item["b64_json"] = "<base64 omitted; image saved to %s>" % img_out
    with open(os.path.join(OUT_ROOT, stem + ".response.json"), "w", encoding="utf-8") as fh:
        json.dump(cleaned, fh, indent=2, ensure_ascii=False)

    status.update(png_info(img_out))
    status["output_bytes"] = os.path.getsize(img_out)
    status["output_sha256"] = hashlib.sha256(open(img_out, "rb").read()).hexdigest()
    status["usage"] = payload.get("usage")
    status["state"] = "image_saved"
    with open(os.path.join(OUT_ROOT, stem + ".status.json"), "w", encoding="utf-8") as fh:
        json.dump(status, fh, indent=2, ensure_ascii=False)
    print(json.dumps(status, indent=2, ensure_ascii=False))
    print("\n下一步：先验收姿势与瞄准方向，再合成绿幕锚图")


if __name__ == "__main__":
    main()
