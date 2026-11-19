# -*- coding: utf-8 -*-
"""抡斧概念图 · 提交脚本（2026-11-14 新流程：先概念图，确认后才做四视图）。

依据：Locus/knowledge/plan/weapon-concept-image-plan-20261114.md §5
需求书：Locus/knowledge/design/weapon-3d-art-spec-axe-and-chain.md
接口：POST https://api.muskapis.com/v1/images/edits（multipart，三张 image[]，顺序固定）
模型：gpt-image-2.6-sunburst（发起前用 GET /v1/models 核对）
参数：size=1024x1536 / quality=max / input_fidelity=high / output_format=png / n=1 / 不传 background
纪律：最多提交一次，不自动重试，不覆盖已有产物；四视图闸门未开（等用户确认概念图）

用法
    python Tools/visual_brief/submit_axe_concept_20261114.py --dry-run
    python Tools/visual_brief/submit_axe_concept_20261114.py --approve
"""
from __future__ import annotations

import argparse
import base64
import hashlib
import json
import mimetypes
import os
import pathlib
import struct
import time
import urllib.error
import urllib.request
import uuid

PROJ = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
# 输出根目录：优先用本项目在本机一直用的 C:/Users/steam/Pictures/gptGen（见 plan §3 旧档案路径），
# 不存在时回落到当前运行用户的 <home>/Pictures/gptGen（见 skill/gpt-image-generation.md）。
def _out_root() -> pathlib.Path:
    steam_root = pathlib.Path("C:/Users/steam/Pictures/gptGen")
    if steam_root.is_dir():
        return steam_root
    return pathlib.Path.home() / "Pictures" / "gptGen"


OUT_DIR = _out_root() / "axe_concept_20261114"
ENDPOINT = "https://api.muskapis.com/v1/images/edits"
MODELS_ENDPOINT = "https://api.muskapis.com/v1/models"
MODEL = "gpt-image-2.6-sunburst"
SIZE = "1024x1536"
QUALITY = "max"
INPUT_FIDELITY = "high"
TIMEOUT = 900
STEM = "axe_concept_v1"

# 上传顺序固定（plan §5.3）：图1 只管斧头结构，图2 只管整斧比例，图3 只管配色
REFS = [
    (r"SkillWeaponsArtSource/01_Reference/pbz/juxiongfu_axe_head_zoom.png",
     "image 1: only the axe-head structure, mass, thickness, blunt axe back, continuous arc edge and the beak spike"),
    (r"SkillWeaponsArtSource/01_Reference/pbz/juxiongfu_axe.png",
     "image 2: only the full axe proportions, long handle and component placement"),
    (r"SkillWeaponsArtSource/01_Reference/style/game_palette_board.png",
     "image 3: only the project color relationships and material color blocking"),
]

PROMPT_CN = """请基于上传的三张参考图，设计一把全新的三国战场风格“抡斧”重型单刃大斧概念图。

参考图职责必须严格区分：第一张只负责斧头的结构与体量；第二张只负责整把大斧的长柄比例和部件关系；第三张只负责本项目的配色与材质色块。不要复制任何参考图的背景、人物、文字、构图、光照或装饰。

主体是一把竖直正立的重型单刃大斧，握把在下，斧头在上端，整把武器完整显示在画面中央。长柄约占全长 58%，斧头约占全长 42%，重心明显靠近斧头。

斧头必须是一整块实心、厚重、有真实体积的钢制斧头，从柄端向一侧张开。刃口是一条平滑连续的圆弧，从上方的角连续绕过外侧，一路弯到右下方的尖端，中途不能出现折角、直线斜边或尖三角。圆弧以内必须填满实心金属，不是空心月牙，不是薄钩刃，不是铲子。斧背厚重钝实，斧头断面表现为上厚下薄的楔形，刃缘收成清晰锋利的薄刃线。

斧头柄端下方单独垂下一根明显的长尖喙刺，形状像鸟喙或倒钩，长度约为斧头宽度的一半。握把尾端只有两到三个小型钢制倒钩尖刺，不要做成三叉戟。

长柄为暖棕色旧木，不能接近黑色。靠近斧头处和柄身中段各有一段暖棕偏红褐的旧皮革缠握。斧头与木柄连接处有冷蓝灰钢制环箍和一到两条红褐皮革绑带。绑带和环箍只能位于柄头连接区域，不能横跨斧面、刃面或斧背。

整体为项目现有的高清像素化三国战场动作美术：厚重、凶悍、明亮、高对比、清晰的大体块和像素色簇。钢材使用冷蓝灰和深蓝灰暗面，刃缘使用亮银白色作为整把武器最亮的部分；木柄使用暖棕，皮革使用偏红暖棕。材质表现清楚但不照片写实。

使用干净的单件武器概念展示构图，纯白或非常浅的中性背景，只有轻微柔和投影。武器必须完整、不出画、不倾斜、不与人物或手部组合。画面不需要文字、尺寸标注、四视图、UI 或场景。"""

PROMPT_EN = """Design a new heavy single-edged battle axe for a Three Kingdoms battlefield action game, based on the three uploaded reference images.

Keep the reference responsibilities separate: image 1 is only for the axe-head structure and mass; image 2 is only for the full axe proportions, long handle, and component placement; image 3 is only for the project's color relationships and material color blocking. Do not copy any background, character, text, composition, lighting, or decorative details from the references.

Show one complete heavy battle axe standing vertically, handle down and axe head up, centered in the frame. The long handle occupies approximately 58 percent of the total length, and the axe head occupies approximately 42 percent. The visual center of mass is close to the axe head.

The axe head must be one solid, thick, volumetric steel plate that expands to one side from the handle. Its cutting edge is one smooth continuous circular arc, starting from the upper corner, sweeping around the outer side, and ending at a lower-right pointed tip. The edge must remain smooth and continuous, with no folded corners, straight diagonal edge, or triangular spike. The area inside the arc must be filled solid metal. It must not be a hollow crescent, thin hook blade, shovel, or flat plate. The blunt axe back must be visibly thick and heavy. The cross-section must read as a wedge: thick at the back and progressively thinner toward the cutting edge, ending in a crisp narrow edge.

Add one clearly visible long bird-beak-like downward spike beneath the handle-side end of the axe head, approximately half the width of the axe head. At the bottom of the handle, add only two or three small steel hooked spikes, not a trident.

The handle is warm brown aged wood, never near-black. Add one warm reddish-brown leather wrap near the axe head and another wrap around the middle of the handle. At the connection between the wooden handle and the steel head, add a cold blue-gray steel collar and one or two reddish-brown leather bindings. The bindings and collar must stay only around the connection area and must never cross the axe face, cutting edge, or axe back.

Use the project's high-definition pixelated Three Kingdoms battlefield action-art direction: heavy, fierce, bright, high contrast, large readable shapes, and controlled pixel-like color clusters. Use cold blue-gray steel with darker blue-gray shadow planes, a bright silver-white cutting edge as the brightest part of the weapon, warm brown wood, and warm reddish-brown leather. Materials should be clearly readable but not photorealistic.

Present the weapon as a clean isolated concept design on a pure white or very light neutral background with only a subtle soft ground shadow. The entire weapon must be visible, uncropped, upright, and not tilted. Do not include a person, hand, character, text, dimension labels, orthographic views, UI, or environment."""

NEGATIVE = """hollow crescent axe, shovel, spade, hoe, thin crescent blade, thin flat plate, hook blade, sickle, pickaxe, double-headed axe, two-sided axe, straight diagonal cutting edge, folded edge, triangular blade, zigzag edge, serrated edge, ornate fantasy weapon, oversized decorations, engravings, emblem, crest, filigree, rows of rivets, decorative metal strips, gold, brass, bronze, copper, gilded metal, yellow metal, rust, heavy rust patches, dirty brown grime, black wood, black-brown leather, photorealistic weapon, sci-fi weapon, western medieval axe, Viking axe, cartoon toy, plastic material, glowing weapon, magic aura, flames, sparks, smoke, character, hand, person, battlefield, scenery, text, watermark, logo, cropped weapon, tilted weapon, multiple weapons, duplicate weapon, broken handle"""

PROMPT = PROMPT_EN + "\n\nNegative constraints: " + NEGATIVE


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


def main() -> int:
    ap = argparse.ArgumentParser(description="提交抡斧概念图")
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--approve", action="store_true")
    ap.add_argument("--model", default=MODEL)
    args = ap.parse_args()

    files = []
    for rel, role in REFS:
        p = abs_path(rel)
        if not os.path.isfile(p):
            raise SystemExit("参考图缺失: %s" % p)
        files.append((p, role))

    img_out = OUT_DIR / ("%s.png" % STEM)

    if args.dry_run:
        print("=== 模型 ===\n%s" % args.model)
        print("=== 参考图（顺序固定，三张 image[]）===")
        for i, (p, role) in enumerate(files, 1):
            print("image %d: %s\n         sha256=%s\n         %s" % (i, p, sha256_file(p), role))
        print("\n=== 提示词（实际提交用英文版 %d 字符，sha256 %s；中文版同存 .prompt_cn.txt）==="
              % (len(PROMPT), hashlib.sha256(PROMPT.encode("utf-8")).hexdigest()))
        print(PROMPT)
        print("\n=== 参数 ===")
        print("endpoint=%s\nmodel=%s size=%s quality=%s input_fidelity=%s output_format=png n=1"
              % (ENDPOINT, args.model, SIZE, QUALITY, INPUT_FIDELITY))
        print("不传 background；超时 %ds；自动重试 0；最多提交一次" % TIMEOUT)
        print("输出: %s" % img_out)
        return 0

    if not args.approve:
        raise SystemExit("未确认批准：加 --approve 才会真的提交（纪律：一次提交，不自动重试）")

    key = os.environ.get("MUSK_API_KEY")
    if not key:
        raise SystemExit("MUSK_API_KEY missing")
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    if img_out.exists():
        raise SystemExit("refusing to overwrite existing output: %s" % img_out)

    models = check_models(key)
    if args.model not in models["image_models"]:
        raise SystemExit("模型不可用: %s（当前图像模型 %s）" % (args.model, models["image_models"]))

    fields = {
        "model": args.model,
        "prompt": PROMPT,
        "size": SIZE,
        "quality": QUALITY,
        "input_fidelity": INPUT_FIDELITY,
        "output_format": "png",
        "n": "1",
    }
    body, ctype = build_multipart(fields, [("image[]", p) for p, _ in files])

    (OUT_DIR / ("%s.prompt.txt" % STEM)).write_text(PROMPT, encoding="utf-8", newline="\n")
    (OUT_DIR / ("%s.prompt_cn.txt" % STEM)).write_text(PROMPT_CN, encoding="utf-8", newline="\n")
    (OUT_DIR / ("%s.prompt_negative.txt" % STEM)).write_text(NEGATIVE, encoding="utf-8", newline="\n")

    with open(OUT_DIR / ("%s.request.json" % STEM), "w", encoding="utf-8") as fh:
        json.dump({
            "version": STEM,
            "plan": "Locus/knowledge/plan/weapon-concept-image-plan-20261114.md",
            "spec": "Locus/knowledge/design/weapon-3d-art-spec-axe-and-chain.md",
            "endpoint": ENDPOINT,
            "method": "POST",
            "content_type": ctype,
            "fields": fields,
            "reference_images": [
                {"order": i + 1, "path": p, "role": role, "sha256": sha256_file(p),
                 "bytes": os.path.getsize(p)}
                for i, (p, role) in enumerate(files)
            ],
            "models_check": models,
            "prompt_sha256": hashlib.sha256(PROMPT.encode("utf-8")).hexdigest(),
            "prompt_chars": len(PROMPT),
            "automatic_retries": 0,
            "read_timeout_seconds": TIMEOUT,
            "gate": "四视图闸门未开：概念图需用户确认后才能继续",
            "submitted_at_local": time.strftime("%Y-%m-%d %H:%M:%S"),
        }, fh, indent=2, ensure_ascii=False)

    request = urllib.request.Request(ENDPOINT, data=body, method="POST", headers={
        "Authorization": "Bearer %s" % key, "Content-Type": ctype})

    status = {"state": "submitted", "http_status": None, "seconds": None,
              "error": None, "output_image": str(img_out)}
    t0 = time.time()
    try:
        with urllib.request.urlopen(request, timeout=TIMEOUT) as resp:
            raw = resp.read()
            status["http_status"] = resp.status
    except urllib.error.HTTPError as exc:
        status["state"] = "failed"
        status["error"] = "HTTPError %s: %s" % (exc.code, exc.read().decode("utf-8", "replace")[:800])
        status["seconds"] = round(time.time() - t0, 2)
        with open(OUT_DIR / ("%s.status.json" % STEM), "w", encoding="utf-8") as fh:
            json.dump(status, fh, indent=2, ensure_ascii=False)
        raise SystemExit(status["error"])
    except Exception as exc:  # noqa: BLE001
        status["state"] = "failed"
        status["error"] = "%s: %s" % (type(exc).__name__, exc)
        status["seconds"] = round(time.time() - t0, 2)
        with open(OUT_DIR / ("%s.status.json" % STEM), "w", encoding="utf-8") as fh:
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
    with open(OUT_DIR / ("%s.response.json" % STEM), "w", encoding="utf-8") as fh:
        json.dump(cleaned, fh, indent=2, ensure_ascii=False)

    with open(img_out, "wb") as fh:
        fh.write(blob)
    status.update(png_info(str(img_out)))
    status["output_bytes"] = len(blob)
    status["output_sha256"] = hashlib.sha256(blob).hexdigest()
    status["usage"] = payload.get("usage")
    status["echoed_size"] = payload.get("size")
    status["echoed_quality"] = payload.get("quality")
    status["models_check"] = models
    status["state"] = "image_saved"
    with open(OUT_DIR / ("%s.status.json" % STEM), "w", encoding="utf-8") as fh:
        json.dump(status, fh, indent=2, ensure_ascii=False)
    print(json.dumps(status, indent=2, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
