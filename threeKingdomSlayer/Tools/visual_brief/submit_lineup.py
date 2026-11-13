# -*- coding: utf-8 -*-
"""并排比例研究图 · 通用提交脚本（按 --version 驱动）。

取代逐版复制脚本的写法：提示词、负面词、参考图清单都在 Tools/visual_brief/prompts/ 下，
本脚本只负责组装请求、核对参考图哈希、发一次请求、留档。

版本约定：版本号 v 同时决定
  · 提示词  : prompts/enemy_proportion_lineup_<v>.txt            （英文正文，实际提交）
               prompts/enemy_proportion_lineup_<v>.cn.txt         （中文存档）
               prompts/enemy_proportion_lineup_<v>.negative.txt   （英文负面词）
  · 参考图  : prompts/refs_enemy_proportion_lineup_<v>.json
  · 输出目录: C:/Users/steam/Pictures/gptGen/enemy_proportion_lineup_<v>/

接口：POST https://api.muskapis.com/v1/images/edits（multipart，多张 image[]，顺序固定）
模型：gpt-image-2.6-sunburst（发起前用 GET /v1/models 核对）
参数：size / quality=max / input_fidelity=high / output_format=png / n=1 / 不传 background
纪律：最多提交一次，不自动重试，不覆盖已有产物

用法
    python Tools/visual_brief/submit_lineup.py --version v3_1021 --dry-run
    python Tools/visual_brief/submit_lineup.py --version v3_1021 --approve
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
GG_ROOT = r"C:/Users/steam/Pictures/gptGen"

ENDPOINT = "https://api.muskapis.com/v1/images/edits"
MODELS_ENDPOINT = "https://api.muskapis.com/v1/models"
MODEL = "gpt-image-2.6-sunburst"
SIZE = "1536x1024"
QUALITY = "max"
INPUT_FIDELITY = "high"
TIMEOUT = 900


def abs_path(p: str) -> str:
    return p if os.path.isabs(p) else os.path.join(PROJ, p.replace("/", os.sep))


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


def load(version: str):
    stem = "enemy_proportion_lineup_%s" % version
    with open(os.path.join(PROMPT_DIR, stem + ".txt"), "r", encoding="utf-8") as fh:
        prompt_en = fh.read().strip()
    with open(os.path.join(PROMPT_DIR, stem + ".cn.txt"), "r", encoding="utf-8") as fh:
        prompt_cn = fh.read().strip()
    with open(os.path.join(PROMPT_DIR, stem + ".negative.txt"), "r", encoding="utf-8") as fh:
        negative = fh.read().strip()
    with open(os.path.join(PROMPT_DIR, "refs_%s.json" % stem), "r", encoding="utf-8") as fh:
        refs_json = json.load(fh)

    refs = []
    for ref in sorted(refs_json["references"], key=lambda r: r["order"]):
        p = abs_path(ref["path"])
        if not os.path.isfile(p):
            raise SystemExit("参考图缺失: %s" % p)
        digest = sha256_file(p)
        prefix = ref.get("expected_sha256_prefix")
        if prefix and not digest.startswith(prefix):
            raise SystemExit("参考图哈希与清单不符: %s\n  期望前缀 %s\n  实际 %s"
                             % (p, prefix, digest))
        refs.append((p, ref["role"], digest))

    prompt = prompt_en + "\n\nNegative constraints: " + negative
    return stem, prompt, prompt_cn, negative, refs, refs_json


def main() -> int:
    ap = argparse.ArgumentParser(description="提交并排比例研究图（通用）")
    ap.add_argument("--version", required=True, help="如 v3_1021")
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--approve", action="store_true")
    ap.add_argument("--model", default=MODEL)
    ap.add_argument("--size", default=SIZE)
    ap.add_argument("--timeout", type=int, default=TIMEOUT,
                    help="读超时秒数（默认 %d）" % TIMEOUT)
    args = ap.parse_args()

    stem, prompt, prompt_cn, negative, refs, refs_json = load(args.version)
    out_dir = os.path.join(GG_ROOT, stem)
    img_out = os.path.join(out_dir, stem + ".png")

    if args.dry_run:
        print("=== 模型 ===\n%s" % args.model)
        print("=== 参考图（顺序固定，%d 张 image[]）===" % len(refs))
        for i, (p, role, digest) in enumerate(refs, 1):
            info = png_info(p)
            print("image %d: %s\n         %dx%d ct=%d  sha256=%s\n         %s"
                  % (i, p, info["width"], info["height"], info["color_type"], digest, role))
        print("\n=== 提交用提示词（英文正文 + 英文负面词，%d 字符，sha256 %s）==="
              % (len(prompt), hashlib.sha256(prompt.encode("utf-8")).hexdigest()))
        print(prompt)
        print("\n=== 参数 ===")
        print("endpoint=%s\nmodel=%s size=%s quality=%s input_fidelity=%s output_format=png n=1"
              % (ENDPOINT, args.model, args.size, QUALITY, INPUT_FIDELITY))
        print("不传 background；读超时 %ds；自动重试 0；最多提交一次" % args.timeout)
        print("输出: %s" % img_out)
        print("中文存档字符数: %d" % len(prompt_cn))
        return 0

    if not args.approve:
        raise SystemExit("未确认批准：加 --approve 才会真的提交（纪律：一次提交，不自动重试）")

    key = os.environ.get("MUSK_API_KEY")
    if not key:
        raise SystemExit("MUSK_API_KEY missing")
    os.makedirs(out_dir, exist_ok=True)
    if os.path.exists(img_out):
        raise SystemExit("refusing to overwrite existing output: %s" % img_out)

    models = check_models(key)
    if args.model not in models["image_models"]:
        raise SystemExit("模型不可用: %s（当前图像模型 %s）" % (args.model, models["image_models"]))

    fields = {
        "model": args.model,
        "prompt": prompt,
        "size": args.size,
        "quality": QUALITY,
        "input_fidelity": INPUT_FIDELITY,
        "output_format": "png",
        "n": "1",
    }
    body, ctype = build_multipart(fields, [("image[]", p) for p, _r, _d in refs])
    with open(os.path.join(out_dir, stem + ".prompt.txt"), "w",
              encoding="utf-8", newline="\n") as fh:
        fh.write(prompt)
    with open(os.path.join(out_dir, stem + ".prompt_cn.txt"), "w",
              encoding="utf-8", newline="\n") as fh:
        fh.write(prompt_cn)
    with open(os.path.join(out_dir, stem + ".prompt_negative.txt"), "w",
              encoding="utf-8", newline="\n") as fh:
        fh.write(negative)

    with open(os.path.join(out_dir, stem + ".request.json"), "w", encoding="utf-8") as fh:
        json.dump({
            "version": stem,
            "handoff": r"C:\threeKingdomSlayer\threeKingdomSlayer\Locus\knowledge\plan\enemy1021-art-production-handoff.md",
            "handoff_section": "0.21",
            "endpoint": ENDPOINT,
            "method": "POST",
            "content_type": ctype,
            "fields": fields,
            "reference_images": [
                {"order": i + 1, "path": p, "role": role, "sha256": digest,
                 "bytes": os.path.getsize(p)}
                for i, (p, role, digest) in enumerate(refs)
            ],
            "refs_manifest": refs_json,
            "models_check": models,
            "prompt_sha256": hashlib.sha256(prompt.encode("utf-8")).hexdigest(),
            "prompt_chars": len(prompt),
            "automatic_retries": 0,
            "attempt": "manual single retry after a lost response (first attempt: 901s read timeout, no artifact)",
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
        with open(os.path.join(out_dir, stem + ".status.json"), "w", encoding="utf-8") as fh:
            json.dump(status, fh, indent=2, ensure_ascii=False)
        raise SystemExit(status["error"])
    except Exception as exc:  # noqa: BLE001
        status["state"] = "failed"
        status["error"] = "%s: %s" % (type(exc).__name__, exc)
        status["seconds"] = round(time.time() - t0, 2)
        with open(os.path.join(out_dir, stem + ".status.json"), "w", encoding="utf-8") as fh:
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
    with open(os.path.join(out_dir, stem + ".response.json"), "w", encoding="utf-8") as fh:
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
    with open(os.path.join(out_dir, stem + ".status.json"), "w", encoding="utf-8") as fh:
        json.dump(status, fh, indent=2, ensure_ascii=False)
    print(json.dumps(status, indent=2, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
