"""技能武器（抡斧）正交视图生成 · GPT Image 参考图编辑提交脚本。

用途
    把「已定稿的概念图」转成 **3D 建模用的正交视图**。
    沿用长枪那一轮的成例（StabWeaponArtSource/11_AssembledFiveViewV05、12_AssembledFourViewV06）：
      mode=five  → 一次生成 **1536×1024 五视图合板**（左三格 正/右/后 + 右上顶视 + 右下底视）
      mode=left  → 一次生成 **1024×1536 左侧视图**（参考 = 五视图合板，用于补齐四视图）

提示词 / 参考图职责
    Tools/visual_brief/prompts/axe_five_view_v1.txt            五视图合板提示词正文（唯一真源）
    Tools/visual_brief/prompts/axe_five_view_negative_v1.txt   负面词
    Tools/visual_brief/prompts/refs_axe_five_view_v1.json       参考图与职责
    Tools/visual_brief/prompts/axe_left_view_v1.txt             左侧视图提示词正文
    Tools/visual_brief/prompts/refs_axe_left_view_v1.json       左侧视图的参考图与职责

接口（沿用长枪那一轮同规格）
    POST https://api.muskapis.com/v1/images/edits
    multipart/form-data，图片字段名必须是 `image[]`，多图顺序有意义
    model=gpt-image-2.6-sunburst / quality=max / input_fidelity=high / output_format=png / n=1
    five: size=1536x1024 ; left: size=1024x1536 ; background=opaque（要白底不透明）
    ⚠ 长枪那一轮用的是 gpt-image-2.5-sunburst；本机 MUSK_API_KEY **只对 2.6 有权限**（2.5 会 403）⇒ 用 2.6。
    超时 900s / 自动重试 0（一次提交，不自动重试）

纪律
    · 一次提交不自动重试（付费任务）；失败先报告再决定是否重提
    · 不覆盖已有产物
    · 成图落 03_FourView/（本项目自产，可入库）；过程留痕在外部归档目录
    · 第三方参考图（PBZ 等）只留本地，不进仓库（见 .gitignore）

用法
    python Tools/visual_brief/submit_weapon_view.py --mode five --version v1 --dry-run
    python Tools/visual_brief/submit_weapon_view.py --mode five --version v1 --approve
    python Tools/visual_brief/submit_weapon_view.py --mode left --version v1 --approve
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
OUT_ROOT = r"C:/Users/steam/Pictures/gptGen/skill_weapon_view"
ASSET_DIR = os.path.join(PROJ, "SkillWeaponsArtSource", "03_FourView")

ENDPOINT = "https://api.muskapis.com/v1/images/edits"
MODEL = "gpt-image-2.6-sunburst"
QUALITY = "max"
INPUT_FIDELITY = "high"
TIMEOUT = 900

MODES = {
    # mode -> (提示词基名, 尺寸)
    "five": ("axe_five_view", "1536x1024"),
    "left": ("axe_left_view", "1024x1536"),
    # single：单视图逐张生成（提示词基名与尺寸由命令行覆盖）
    "single": ("axe_view_front", "1024x1536"),
}


def abs_path(p):
    return p if os.path.isabs(p) else os.path.join(PROJ, p.replace("/", os.sep))


def load_version(mode, version, prompt_base=None, size=None):
    stem = prompt_base or MODES[mode][0]
    size = size or MODES[mode][1]
    prompt_file = os.path.join(PROMPT_DIR, "%s_%s.txt" % (stem, version))
    refs_file = os.path.join(PROMPT_DIR, "refs_%s_%s.json" % (stem, version))
    neg_file = os.path.join(PROMPT_DIR, "%s_negative_%s.txt" % (stem, version))
    with open(prompt_file, "r", encoding="utf-8") as fh:
        prompt = fh.read().strip()
    if os.path.isfile(neg_file):
        with open(neg_file, "r", encoding="utf-8") as fh:
            neg = fh.read().strip()
        prompt = prompt + "\n\n不要：" + neg
    with open(refs_file, "r", encoding="utf-8") as fh:
        refs = json.load(fh)
    return prompt, refs, size


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
    ap = argparse.ArgumentParser(description="提交技能武器正交视图生成请求")
    ap.add_argument("--mode", required=True, choices=sorted(MODES),
                    help="five=五视图合板 / left=左侧视图 / single=单视图（配合 --prompt-base）")
    ap.add_argument("--version", default="v1")
    ap.add_argument("--prompt-base", default="", help="覆盖提示词基名（single 模式用）")
    ap.add_argument("--size", default="", help="覆盖出图尺寸，如 1024x1536")
    ap.add_argument("--stem", default="", help="产物名前缀")
    ap.add_argument("--out", default="", help="成图输出路径（默认落 03_FourView/）")
    ap.add_argument("--dry-run", action="store_true", help="只打印请求内容，不提交")
    ap.add_argument("--approve", action="store_true", help="确认已获用户批准，实际提交")
    args = ap.parse_args()

    stem_base = MODES[args.mode][0]
    prompt, refs, size = load_version(args.mode, args.version, args.prompt_base, args.size)
    images = [(abs_path(r["path"]), r["role"]) for r in refs["references"]]
    for p, _ in images:
        if not os.path.isfile(p):
            raise SystemExit("参考图缺失: %s" % p)

    stem = args.stem or ("%s_%s" % (stem_base, args.version))
    img_out = args.out or os.path.join(ASSET_DIR, stem + "_raw.png")

    if args.dry_run:
        print("=== mode=%s · 版本 %s ===" % (args.mode, args.version))
        print("%s" % refs.get("title", ""))
        print("起点: %s" % refs.get("base_note", ""))
        print("\n=== 参考图（顺序）===")
        for i, (p, role) in enumerate(images, 1):
            print("image %d: %s\n         %s" % (i, p, role))
        print("\n=== 提示词（%d 字符，sha256 %s）===" % (
            len(prompt), hashlib.sha256(prompt.encode("utf-8")).hexdigest()[:16]))
        print(prompt)
        print("\n=== 参数 ===")
        print("endpoint=%s\nmodel=%s size=%s quality=%s input_fidelity=%s output_format=png n=1（不传 background）"
              % (ENDPOINT, MODEL, size, QUALITY, INPUT_FIDELITY))
        print("超时 %ds；自动重试 0" % TIMEOUT)
        print("输出: %s" % img_out)
        return 0

    if not args.approve:
        raise SystemExit("未确认批准：加 --approve 才会真的提交（纪律：一次提交，不自动重试）")

    key = os.environ.get("MUSK_API_KEY")
    if not key:
        raise SystemExit("MUSK_API_KEY missing")
    os.makedirs(OUT_ROOT, exist_ok=True)
    os.makedirs(ASSET_DIR, exist_ok=True)
    parent = os.path.dirname(os.path.abspath(img_out))
    if parent:
        os.makedirs(parent, exist_ok=True)
    if os.path.exists(img_out):
        raise SystemExit("refusing to overwrite existing output: %s" % img_out)

    fields = {
        "model": MODEL,
        "prompt": prompt,
        "size": size,
        "quality": QUALITY,
        "input_fidelity": INPUT_FIDELITY,
        # ⚠ 不传 background：长枪那一轮传了 background=opaque，但本机这条链路
        #   （概念图 v1~v6 全部成功）从不传这个参数；传了会 400 内容策略拦截。
        "output_format": "png",
        "n": "1",
    }
    files = [("image[]", p) for p, _ in images]
    body, ctype = build_multipart(fields, files)

    prompt_txt = os.path.join(OUT_ROOT, stem + ".prompt.txt")
    req_json = os.path.join(OUT_ROOT, stem + ".request.json")
    resp_json = os.path.join(OUT_ROOT, stem + ".response.json")
    status_json = os.path.join(OUT_ROOT, stem + ".status.json")

    with open(prompt_txt, "w", encoding="utf-8", newline="\n") as fh:
        fh.write(prompt)
    with open(req_json, "w", encoding="utf-8") as fh:
        json.dump({
            "mode": args.mode,
            "version": args.version,
            "endpoint": ENDPOINT,
            "method": "POST",
            "content_type": ctype,
            "fields": {k: v for k, v in fields.items() if k != "prompt"},
            "prompt_sha256": hashlib.sha256(prompt.encode("utf-8")).hexdigest(),
            "prompt_chars": len(prompt),
            "reference_images": [{"index": i + 1, "path": p, "role": role}
                                 for i, (p, role) in enumerate(images)],
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
        with open(status_json, "w", encoding="utf-8") as fh:
            json.dump(status, fh, indent=2, ensure_ascii=False)
        raise SystemExit(status["error"])
    except Exception as exc:  # noqa: BLE001
        status["state"] = "failed"
        status["error"] = "%s: %s" % (type(exc).__name__, exc)
        status["seconds"] = round(time.time() - t0, 2)
        with open(status_json, "w", encoding="utf-8") as fh:
            json.dump(status, fh, indent=2, ensure_ascii=False)
        raise SystemExit(status["error"])

    status["seconds"] = round(time.time() - t0, 2)
    raw_resp_path = os.path.join(OUT_ROOT, stem + ".response.raw.json")
    with open(raw_resp_path, "wb") as fh:
        fh.write(raw)
    payload = json.loads(raw.decode("utf-8"))
    status.update(save_payload(payload["data"][0], img_out))

    cleaned = json.loads(raw.decode("utf-8"))
    for item in cleaned.get("data", []):
        if item.get("b64_json"):
            item["b64_json"] = "<base64 omitted; image saved to %s>" % img_out
    with open(resp_json, "w", encoding="utf-8") as fh:
        json.dump(cleaned, fh, indent=2, ensure_ascii=False)

    status.update(png_info(img_out))
    status["output_bytes"] = os.path.getsize(img_out)
    status["output_sha256"] = hashlib.sha256(open(img_out, "rb").read()).hexdigest()
    status["usage"] = payload.get("usage")
    status["echoed_size"] = payload.get("size")
    status["echoed_quality"] = payload.get("quality")
    status["state"] = "image_saved"
    with open(status_json, "w", encoding="utf-8") as fh:
        json.dump(status, fh, indent=2, ensure_ascii=False)
    print(json.dumps(status, indent=2, ensure_ascii=False))
    print("\n下一步：本地切格 / 对齐 / 合成看板；出图后交用户验收。")


if __name__ == "__main__":
    main()
