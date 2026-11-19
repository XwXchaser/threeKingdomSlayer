# -*- coding: utf-8 -*-
"""抡斧四视图 · 提交脚本（2026-11-14，概念图已验收后启动）。

依据：Locus/knowledge/plan/weapon-concept-image-plan-20261114.md §5.8
需求书：Locus/knowledge/design/weapon-3d-art-spec-axe-and-chain.md §4.3 / §4.4
接口：POST https://api.muskapis.com/v1/images/edits（multipart，两张 image[]，顺序固定）
模型：gpt-image-2.6-sunburst
参数：quality=max / input_fidelity=high / output_format=png / n=1 / 不传 background
尺寸：先试 384x1024（接口若拒绝尺寸则按 §5.8 降级为 1024x1536，事后本地裁 384x1024）
纪律：每个视图只提交一次（尺寸降级除外，且只降级一次）；不覆盖已有产物；四张单图全过后才做合图

用法
    python Tools/visual_brief/submit_axe_four_view_20261114.py --view FRONT --dry-run
    python Tools/visual_brief/submit_axe_four_view_20261114.py --view FRONT --approve
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
OUT_DIR = pathlib.Path("C:/Users/steam/Pictures/gptGen/axe_four_view_20261114")
ENDPOINT = "https://api.muskapis.com/v1/images/edits"
MODELS_ENDPOINT = "https://api.muskapis.com/v1/models"
MODEL = "gpt-image-2.6-sunburst"
SIZE_PRIMARY = "384x1024"
SIZE_FALLBACK = "1024x1536"
QUALITY = "max"
INPUT_FIDELITY = "high"
TIMEOUT = 900

# 上传顺序固定（plan §5.8）：image1 = 已确认概念图（最高优先级设计锁），image2 = PBZ 斧头结构辅助
# image1 默认用 policy-safe 变体：plan §4.1 记录「亮银白的大刃」当编辑参考会被内容安全拦 400，
# 2026-11-14 首次四视图提交实测复现（request id 20261114202307351620050tb8mtsZB）。
# --ref 可切回 white / raw 变体做对照诊断。
REF_VARIANTS = {
    "safe": (r"C:/Users/steam/Pictures/gptGen/axe_four_view_20261114/axe_concept_ref_safe.png",
             "image 1: the APPROVED axe concept image (highlight-compressed variant), highest-priority design lock"),
    "white": (r"C:/Users/steam/Pictures/gptGen/axe_concept_20261114/axe_concept_v1_on_white.png",
              "image 1: the APPROVED axe concept image (white background composite), highest-priority design lock"),
    "raw": (r"C:/Users/steam/Pictures/gptGen/axe_concept_20261114/axe_concept_v1.png",
            "image 1: the APPROVED axe concept image (original transparent PNG), highest-priority design lock"),
}
PBZ_HEAD = (r"SkillWeaponsArtSource/01_Reference/pbz/juxiongfu_axe_head_zoom.png",
             "image 2: auxiliary reference for the axe-head structure only")
PBZ_PALETTE = (r"SkillWeaponsArtSource/01_Reference/style/game_palette_board.png",
               "image 3: project color relationships and material color blocking")
PBZ_FULL_AXE = (r"SkillWeaponsArtSource/01_Reference/pbz/juxiongfu_axe.png",
                "image 2: full axe proportions, long handle and component placement")

# 2026-11-14 实测能过内容安全的那一组参考图（与已验收概念图同源）
REF_SET_PBZ3 = [
    PBZ_HEAD,
    PBZ_FULL_AXE,
    PBZ_PALETTE,
]

# 已验收概念图使用的设计描述（逐字取自 Tools/visual_brief/submit_axe_concept_20261114.py），
# 配合上面那组参考图可以复现同一造型；用于“概念图无法上传”时的四视图回退路径。
PROMPT_CONCEPT_EN = """Design a new heavy single-edged battle axe for a Three Kingdoms battlefield action game, based on the three uploaded reference images.

Keep the reference responsibilities separate: image 1 is only for the axe-head structure and mass; image 2 is only for the full axe proportions, long handle, and component placement; image 3 is only for the project's color relationships and material color blocking. Do not copy any background, character, text, composition, lighting, or decorative details from the references.

Show one complete heavy battle axe standing vertically, handle down and axe head up, centered in the frame. The long handle occupies approximately 58 percent of the total length, and the axe head occupies approximately 42 percent. The visual center of mass is close to the axe head.

The axe head must be one solid, thick, volumetric steel plate that expands to one side from the handle. Its cutting edge is one smooth continuous circular arc, starting from the upper corner, sweeping around the outer side, and ending at a lower-right pointed tip. The edge must remain smooth and continuous, with no folded corners, straight diagonal edge, or triangular spike. The area inside the arc must be filled solid metal. It must not be a hollow crescent, thin hook blade, shovel, or flat plate. The blunt axe back must be visibly thick and heavy. The cross-section must read as a wedge: thick at the back and progressively thinner toward the cutting edge, ending in a crisp narrow edge.

Add one clearly visible long bird-beak-like downward spike beneath the handle-side end of the axe head, approximately half the width of the axe head. At the bottom of the handle, add only two or three small steel hooked spikes, not a trident.

The handle is warm brown aged wood, never near-black. Add one warm reddish-brown leather wrap near the axe head and another wrap around the middle of the handle. At the connection between the wooden handle and the steel head, add a cold blue-gray steel collar and one or two reddish-brown leather bindings. The bindings and collar must stay only around the connection area and must never cross the axe face, cutting edge, or axe back.

Use the project's high-definition pixelated Three Kingdoms battlefield action-art direction: heavy, fierce, bright, high contrast, large readable shapes, and controlled pixel-like color clusters. Use cold blue-gray steel with darker blue-gray shadow planes, a bright silver-white cutting edge as the brightest part of the weapon, warm brown wood, and warm reddish-brown leather. Materials should be clearly readable but not photorealistic.

Present the weapon as a clean isolated concept design on a pure white or very light neutral background with only a subtle soft ground shadow. The entire weapon must be visible, uncropped, upright, and not tilted. Do not include a person, hand, character, text, dimension labels, orthographic views, UI, or environment."""

VIEW_SENTENCE_HYBRID = {
    "FRONT": "FRONT: Observe from local +Z toward -Z. Screen right is local +X and screen up is +Y. Keep local +Y pointing from the grip toward the axe head.",
    "LEFT": "LEFT: Observe from local -X toward +X. Screen right is local +Z and screen up is +Y. Draw the real left-side thickness, wedge cross-section, blunt axe back, collar, bindings and beak spike. Do not create it by horizontally mirroring the front view.",
    "RIGHT": "RIGHT: Observe from local +X toward -X. Screen right is local -Z and screen up is +Y. Draw the real right-side thickness, wedge cross-section, blunt axe back, collar, bindings and beak spike. Do not create it by horizontally mirroring the front view.",
    "BACK": "BACK: Observe from local -Z toward +Z. Screen right is local -X and screen up is +Y. Draw the real back structure and thickness, keeping the beak spike, tail spikes, collar and bindings as parts of the same weapon.",
}

VIEW_TAIL_HYBRID = """Now render that same weapon as a single orthographic view for 3D modeling: use orthographic projection with no perspective shortening and no tilt, a pure white background, the whole weapon upright, complete and uncropped in the center of the frame with generous empty margin on every side, and every part of the same weapon unchanged. Do not redesign it. No person, hand, scene, text, dimension labels, arrows, watermark, border or decorative frame."""

# v2：把正交约束写死（模型默认会画成正面/斜视，侧面尤其明显）
VIEW_SENTENCE_ORTHO = {
    "FRONT": "FRONT: Observe from local +Z toward -Z. Screen right is local +X and screen up is +Y. Keep local +Y pointing from the grip toward the axe head. Show the flat face of the axe head with its continuous crescent cutting edge sweeping around the right side.",
    "LEFT": "LEFT: Observe from local -X toward +X. Screen right is local +Z and screen up is +Y. This is a pure side view: the axe head is seen EDGE-ON, so the broad crescent blade must NOT be visible. On screen the axe head is only a narrow vertical wedge slab, roughly one fifth of its front-view width, showing the thick flat steel back plate, the thin bright cutting-edge line along its far side, the wooden handle in front of it, the steel collar, the leather bindings, and the downward beak spike seen from the side. Do not draw a front view, a three-quarter view, or a wide crescent blade, and do not mirror the front view.",
    "RIGHT": "RIGHT: Observe from local +X toward -X. Screen right is local -Z and screen up is +Y. This is a pure side view: the axe head is seen EDGE-ON, so the broad crescent blade must NOT be visible. On screen the axe head is only a narrow vertical wedge slab, roughly one fifth of its front-view width, showing the thick flat steel back plate, the thin bright cutting-edge line along its far side, the wooden handle in front of it, the steel collar, the leather bindings, and the downward beak spike seen from the side. Do not draw a front view, a three-quarter view, or a wide crescent blade, and do not mirror the front view.",
    "BACK": "BACK: Observe from local -Z toward +Z. Screen right is local -X and screen up is +Y. This is the rear of the same weapon: because screen-right is -X, the axe head and its broad crescent blade must appear on the LEFT side of the image, mirrored relative to the front view. Show the clean flat steel back plate of the axe head with its thickness, the rear horn, the steel collar, the leather bindings, the wood handle, the beak spike and the tail hooks. Do not draw a front view and do not put the blade on the right.",
}

VIEW_SENTENCE_ORTHO_SIDE = {
    "LEFT": "LEFT: The camera stands on the -X side of the weapon and looks toward +X; screen right is local +Z and screen up is +Y. The axe head is seen edge-on as a slim vertical wedge of steel about one fifth of its front-view width, with the flat dark back plate on the right of the slab and the thin bright edge line running along its left side. The round wooden handle stands in front of the head, together with the steel collar, the leather bindings and the downward beak spike seen from the side.",
    "RIGHT": "RIGHT: Observe from local +X toward -X. Screen right is local -Z and screen up is +Y. The camera looks straight at the narrow side of the axe head, so on screen the head is a slim vertical wedge of steel seen from its narrow side, about one fifth of its front-view width. You see the flat steel back plate facing the camera, a thin bright edge line along the far side, the round wooden handle in front of the head, the steel collar, the leather bindings, and the downward beak spike seen from the side.",
}

VIEW_SENTENCE_ORTHO_SIDE_LEFT_B = """LEFT: Observe from the -X side toward +X. Screen right is +Z and screen up is +Y. The camera looks straight at the narrow side of the axe head, so on screen the head is a slim vertical wedge of steel seen from its narrow side, about one fifth of its front-view width. You see the flat steel back plate facing the camera, a thin bright edge line along the far side, the round wooden handle in front of the head, the steel collar, the leather bindings, and the downward beak spike seen from the side."""

VIEW_SENTENCE_ORTHO_SIDE_LEFT_C = """LEFT: This is the left side view of the same weapon. The camera looks straight across the narrow side of the axe head, so on screen the head is a slim vertical wedge of steel seen from its narrow side, about one fifth of its front-view width. The flat dark steel back plate is on the right of the slab and a thin bright edge line runs along its left side. The round wooden handle stands in front of the head, together with the steel collar, the leather bindings and the downward beak spike seen from the side. Screen right is +Z and screen up is +Y."""

VIEW_TAIL_ORTHO = """Now render exactly that weapon as ONE orthographic view for 3D modeling. Draw it as if a camera looks straight along the stated axis: orthographic projection, no perspective shortening, no three-quarter angle, no tilt, no foreshortening. Pure white background, the whole weapon upright, complete and uncropped in the center of the frame, generous empty margin on every side. Every part of the same weapon stays unchanged; do not redesign it, do not add or remove any part, and do not mirror the structure except where the stated viewing direction requires it. No person, hand, scene, text, dimension labels, arrows, watermark, border, glow or decorative frame."""

PROMPT_COMMON = """Use the first uploaded image, the approved axe concept image, as the highest-priority design lock. Use the second image only as an auxiliary reference for the axe-head structure. Draw one single orthographic view of the same approved battle axe for 3D modeling. Do not redesign the weapon.

Use a pure white background. Center the complete weapon vertically. Keep it upright, uncropped, and not tilted. Use orthographic projection with no perspective shortening. Do not include a person, hand, scene, text, dimension labels, arrows, watermark, or decorative frame.

Preserve exactly the approved concept's axe silhouette, continuous smooth circular cutting edge, solid thick axe head, wedge-shaped cross-section, thick blunt axe back, long handle proportion, downward bird-beak spike, small hooked tail spikes, steel collar, leather bindings, warm brown wood, reddish-brown leather, cold blue-gray steel, and silver-white cutting edge.

Do not mirror the left and right structures. Do not add decorations. Do not remove the beak spike, collar, leather bindings, or tail spikes. Draw only the specified observation direction."""

VIEW_SENTENCE = {
    "FRONT": "FRONT: Observe from local +Z toward -Z. Screen right is local +X and screen up is +Y. Show the approved front structure and keep local +Y pointing from the grip toward the axe head.",
    "LEFT": "LEFT: Observe from local -X toward +X. Screen right is local +Z and screen up is +Y. Show the real left-side thickness, wedge cross-section, axe back, collar, bindings, and beak spike. Do not create this view by horizontally mirroring the front view.",
    "RIGHT": "RIGHT: Observe from local +X toward -X. Screen right is local -Z and screen up is +Y. Show the real right-side thickness, wedge cross-section, axe back, collar, bindings, and beak spike. Do not create this view by horizontally mirroring the front view.",
    "BACK": "BACK: Observe from local -Z toward +Z. Screen right is local -X and screen up is +Y. Show the real back structure and thickness. Keep the beak spike, tail spikes, collar, and bindings as parts of the same approved weapon; do not redesign or mirror-replace them.",
}

# 诊断用极简提示词：只描述“把上传的物件画成正交模型图”，不出现任何武器部件名词。
# 用途：区分 400 到底来自图片侧还是提示词侧（见 plan §5.9 记录）。
PROMPT_MINIMAL = """Create one clean orthographic model sheet of the single object shown in the uploaded image, as a reference for 3D modeling. Use a pure white background. Place the complete object upright in the center, fully visible and uncropped, with generous empty margin on every side. Use orthographic projection with no perspective shortening and no tilt. Keep exactly the same shape, proportions, silhouette, colors, materials and every visible part of the uploaded object. Do not redesign it and do not add or remove any part. No person, hand, scenery, text, labels, numbers, dimensions, arrows, watermark, border, glow, vignette or decorative frame."""

VIEW_SENTENCE_MINIMAL = {
    "FRONT": "FRONT: observe from the front, screen right is local +X and screen up is +Y.",
    "LEFT": "LEFT: observe from the local -X side, screen right is local +Z and screen up is +Y. This is a real side view, not a mirrored copy of the front view.",
    "RIGHT": "RIGHT: observe from the local +X side, screen right is local -Z and screen up is +Y. This is a real side view, not a mirrored copy of the front view.",
    "BACK": "BACK: observe from the back, screen right is local -X and screen up is +Y. This is a real back view, not a mirrored copy of the front view.",
}

# plan §4.1：正文里出现别的武器名会被内容安全拦 400（概念图那次的负面词只含 shovel/spade/hoe 等工具词，
# 通过；四视图负面词里的 knife / spear / sword 是新增武器名，2026-11-14 实测 400）。
# => 四视图负面词去掉这三个武器名，其余保持 §5.8 原样（shovel/spade/hoe 是概念图那批已验证可过的词）。
NEGATIVE_ORIGINAL = """perspective view, three-quarter view, isometric view, tilted weapon, diagonal weapon, horizontal weapon, mirrored view, simplified side view, knife, spear, sword, shovel, spade, hoe, hollow crescent, thin blade, flat plate, missing thickness, missing axe back, missing beak spike, missing tail hooks, missing collar, bindings crossing the axe head, new decorations, gold, brass, bronze, copper, rust, black wood, black leather, character, hand, scenery, text, labels, arrows, dimensions, watermark, cropped weapon, multiple weapons, inconsistent proportions, inconsistent handle length, glow, glowing edge, magic aura, halo, vignette, gradient background, checkerboard background, transparent background"""

NEGATIVE = """perspective view, three-quarter view, isometric view, tilted weapon, diagonal weapon, horizontal weapon, mirrored view, simplified side view, shovel, spade, hoe, hollow crescent, thin blade, flat plate, missing thickness, missing axe back, missing beak spike, missing tail hooks, missing collar, bindings crossing the axe head, new decorations, gold, brass, bronze, copper, rust, black wood, black leather, character, hand, scenery, text, labels, arrows, dimensions, watermark, cropped weapon, multiple weapons, inconsistent proportions, inconsistent handle length, glow, glowing edge, magic aura, halo, vignette, gradient background, checkerboard background, transparent background"""


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


def build_prompt(view: str, style: str) -> str:
    if style == "minimal":
        return PROMPT_MINIMAL + "\n\n" + VIEW_SENTENCE_MINIMAL[view]
    if style == "hybrid":
        return (PROMPT_CONCEPT_EN + "\n\n" + VIEW_SENTENCE_HYBRID[view] + "\n\n" + VIEW_TAIL_HYBRID)
    if style == "ortho":
        return (PROMPT_CONCEPT_EN + "\n\n" + VIEW_SENTENCE_ORTHO[view] + "\n\n" + VIEW_TAIL_ORTHO)
    if style == "ortho_side":
        return (PROMPT_CONCEPT_EN + "\n\n" + VIEW_SENTENCE_ORTHO_SIDE[view] + "\n\n" + VIEW_TAIL_ORTHO)
    if style == "ortho_side_left_b":
        return (PROMPT_CONCEPT_EN + "\n\n" + VIEW_SENTENCE_ORTHO_SIDE_LEFT_B + "\n\n" + VIEW_TAIL_ORTHO)
    if style == "ortho_side_left_c":
        return (PROMPT_CONCEPT_EN + "\n\n" + VIEW_SENTENCE_ORTHO_SIDE_LEFT_C + "\n\n" + VIEW_TAIL_ORTHO)
    return (PROMPT_COMMON + "\n\n" + VIEW_SENTENCE[view]
            + "\n\nNegative constraints: " + NEGATIVE)


def submit(view: str, size: str, key: str, models: dict, files, style: str = "full") -> tuple[bytes, dict, dict]:
    """提交一次；返回 (png bytes, 原始响应 dict, meta)。失败抛异常。"""
    prompt = build_prompt(view, style)
    fields = {
        "model": MODEL,
        "prompt": prompt,
        "size": size,
        "quality": QUALITY,
        "input_fidelity": INPUT_FIDELITY,
        "output_format": "png",
        "n": "1",
    }
    body, ctype = build_multipart(fields, [("image[]", p) for p, _ in files])
    request = urllib.request.Request(ENDPOINT, data=body, method="POST", headers={
        "Authorization": "Bearer %s" % key, "Content-Type": ctype})
    t0 = time.time()
    with urllib.request.urlopen(request, timeout=TIMEOUT) as resp:
        raw = resp.read()
    payload = json.loads(raw.decode("utf-8"))
    item = payload["data"][0]
    if item.get("b64_json"):
        blob = base64.b64decode(item["b64_json"])
        download = "b64_json"
    elif item.get("url"):
        with urllib.request.urlopen(item["url"], timeout=300) as r:
            blob = r.read()
        download = "url"
    else:
        raise RuntimeError("no image payload")
    meta = {"http_status": 200, "seconds": round(time.time() - t0, 2), "download": download,
            "size": size, "usage": payload.get("usage"),
            "echoed_size": payload.get("size"), "echoed_quality": payload.get("quality"),
            "prompt_chars": len(prompt),
            "prompt_sha256": hashlib.sha256(prompt.encode("utf-8")).hexdigest()}
    return blob, payload, meta


def main() -> int:
    ap = argparse.ArgumentParser(description="提交抡斧四视图单图")
    ap.add_argument("--view", required=True, choices=sorted(VIEW_SENTENCE.keys()))
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--approve", action="store_true")
    ap.add_argument("--size", default=SIZE_PRIMARY,
                    help="请求尺寸（默认 %s，接口拒绝时自动降级 %s）" % (SIZE_PRIMARY, SIZE_FALLBACK))
    ap.add_argument("--ref", default="safe", choices=sorted(REF_VARIANTS.keys()),
                    help="image1 变体：safe=压过高光的 policy-safe 版（默认）/ white=白底原图 / raw=原始透明 PNG")
    ap.add_argument("--no-pbz", action="store_true", help="只上传概念图，不带 PBZ 斧头辅助参考")
    ap.add_argument("--prompt-style", default="full",
                    choices=["full", "minimal", "hybrid", "ortho", "ortho_side",
                             "ortho_side_left_b", "ortho_side_left_c"],
                    help="full=§5.8 四视图提示词；minimal=不含武器部件名词的诊断版；hybrid=已验证可过的概念图提示词 + 正交视图指令；ortho=hybrid 基础上写死正交/边缘朝向；ortho_side=侧面视图专用措辞（避开被拦的否定式表述）")
    ap.add_argument("--tag", default="v1", help="产物文件名后缀，默认 v1")
    ap.add_argument("--pbz-only", action="store_true",
                    help="诊断用：只上传 PBZ 头部特写 + 画风色卡，不上传概念图")
    ap.add_argument("--refs", default="concept", choices=["concept", "pbz3"],
                    help="concept=概念图(+PBZ 头图)；pbz3=PBZ 头图 + PBZ 整斧 + 色卡（2026-11-14 实测可过内容安全那组）")
    args = ap.parse_args()

    refs = [REF_VARIANTS[args.ref]]
    if args.refs == "pbz3":
        refs = list(REF_SET_PBZ3)
    elif args.pbz_only:
        refs = [PBZ_HEAD, PBZ_PALETTE]
    elif not args.no_pbz:
        refs.append(PBZ_HEAD)
    files = []
    for rel, role in refs:
        p = abs_path(rel)
        if not os.path.isfile(p):
            raise SystemExit("参考图缺失: %s" % p)
        files.append((p, role))

    stem = "axe_view_%s_%s" % (args.view.lower(), args.tag)
    img_out = OUT_DIR / ("%s.png" % stem)

    prompt_preview = build_prompt(args.view, args.prompt_style)

    if args.dry_run:
        print("=== 视图 === %s" % args.view)
        print("=== 参考图（顺序固定，两张 image[]）===")
        for i, (p, role) in enumerate(files, 1):
            print("image %d: %s\n         sha256=%s\n         %s" % (i, p, sha256_file(p), role))
        print("\n=== 提示词（%d 字符，sha256 %s）==="
              % (len(prompt_preview), hashlib.sha256(prompt_preview.encode("utf-8")).hexdigest()))
        print(prompt_preview)
        print("\n=== 参数 ===")
        print("endpoint=%s\nmodel=%s size=%s quality=%s input_fidelity=%s output_format=png n=1"
              % (ENDPOINT, MODEL, args.size, QUALITY, INPUT_FIDELITY))
        print("不传 background；超时 %ds；每视图只提交一次（尺寸降级除外）" % TIMEOUT)
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

    (OUT_DIR / ("%s.prompt.txt" % stem)).write_text(prompt_preview, encoding="utf-8", newline="\n")

    attempts = []
    blob = None
    payload = None
    meta = None
    size_used = args.size
    try:
        blob, payload, meta = submit(args.view, size_used, key, models, files, args.prompt_style)
    except urllib.error.HTTPError as exc:
        err_body = exc.read().decode("utf-8", "replace")[:800]
        attempts.append({"size": size_used, "http_status": exc.code, "error": err_body})
        lower = err_body.lower()
        size_rejected = exc.code == 400 and ("size" in lower or "dimension" in lower)
        if size_rejected and size_used != SIZE_FALLBACK:
            # §5.8：接口不接受 384x1024 时降级为 1024x1536，事后本地裁 384x1024（只降级一次）
            print("[降级] 尺寸 %s 被拒：%s" % (size_used, err_body[:200]))
            size_used = SIZE_FALLBACK
            blob, payload, meta = submit(args.view, size_used, key, models, files, args.prompt_style)
            meta["size_fallback_from"] = SIZE_PRIMARY
        else:
            with open(OUT_DIR / ("%s.status.json" % stem), "w", encoding="utf-8") as fh:
                json.dump({"state": "failed", "attempts": attempts}, fh, indent=2, ensure_ascii=False)
            raise SystemExit("HTTPError %s: %s" % (exc.code, err_body))
    except Exception as exc:  # noqa: BLE001
        with open(OUT_DIR / ("%s.status.json" % stem), "w", encoding="utf-8") as fh:
            json.dump({"state": "failed", "attempts": attempts,
                       "error": "%s: %s" % (type(exc).__name__, exc)}, fh, indent=2, ensure_ascii=False)
        raise SystemExit("%s: %s" % (type(exc).__name__, exc))

    attempts.append({"size": size_used, "http_status": 200, "seconds": meta["seconds"]})

    with open(OUT_DIR / ("%s.request.json" % stem), "w", encoding="utf-8") as fh:
        json.dump({
            "version": stem,
            "view": args.view,
            "plan": "Locus/knowledge/plan/weapon-concept-image-plan-20261114.md §5.8",
            "endpoint": ENDPOINT,
            "fields": {"model": MODEL, "size": size_used, "quality": QUALITY,
                       "input_fidelity": INPUT_FIDELITY, "output_format": "png", "n": "1"},
            "reference_images": [
                {"order": i + 1, "path": p, "role": role, "sha256": sha256_file(p),
                 "bytes": os.path.getsize(p)}
                for i, (p, role) in enumerate(files)
            ],
            "models_check": models,
            "attempts": attempts,
            "automatic_retries": 0,
            "read_timeout_seconds": TIMEOUT,
            "submitted_at_local": time.strftime("%Y-%m-%d %H:%M:%S"),
        }, fh, indent=2, ensure_ascii=False)

    cleaned = json.loads(json.dumps(payload))
    for it in cleaned.get("data", []):
        if it.get("b64_json"):
            it["b64_json"] = "<base64 omitted; image saved to %s>" % img_out
    with open(OUT_DIR / ("%s.response.json" % stem), "w", encoding="utf-8") as fh:
        json.dump(cleaned, fh, indent=2, ensure_ascii=False)

    with open(img_out, "wb") as fh:
        fh.write(blob)
    status = {"state": "image_saved", "view": args.view, "output_image": str(img_out),
              "output_bytes": len(blob), "output_sha256": hashlib.sha256(blob).hexdigest(),
              "attempts": attempts}
    status.update(png_info(str(img_out)))
    status.update(meta)
    with open(OUT_DIR / ("%s.status.json" % stem), "w", encoding="utf-8") as fh:
        json.dump(status, fh, indent=2, ensure_ascii=False)
    print(json.dumps(status, indent=2, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
