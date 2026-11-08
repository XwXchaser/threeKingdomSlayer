# -*- coding: utf-8 -*-
"""四兵种并排比例参考图 · 提交脚本（1011 重甲 / 1012 山贼刀兵 / 1013 旗手 / 105 弓手）。

依据：Locus/knowledge/plan/enemy-commander-buff-unit-plan.md §2.15
接口：POST https://api.muskapis.com/v1/images/edits（multipart，四张 image[]，顺序固定）
模型：gpt-image-2.6-sunburst（发起前用 GET /v1/models 核对）
参数：size=1536x1024 / quality=max / input_fidelity=high / output_format=png / n=1 / 不传 background
纪律：最多提交一次，不自动重试，不覆盖已有产物

用法
    python Tools/visual_brief/submit_enemy_proportion_lineup.py --dry-run
    python Tools/visual_brief/submit_enemy_proportion_lineup.py --approve
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
OUT_DIR = r"C:/Users/steam/Pictures/gptGen/enemy_proportion_lineup_v1"
ENDPOINT = "https://api.muskapis.com/v1/images/edits"
MODELS_ENDPOINT = "https://api.muskapis.com/v1/models"
MODEL = "gpt-image-2.6-sunburst"
SIZE = "1536x1024"
QUALITY = "max"
INPUT_FIDELITY = "high"
TIMEOUT = 900

REFS = [
    (r"Enemy1013ArtSource/02_IdentityImages/1011_green_1012_v6_reference.png",
     "image 1: 1011 重甲兵 —— 唯一共同体型 / 画风基准，同时是第一位重甲兵的造型参考"),
    (r"Enemy1012ArtSource/02_IdentityImages/input_identity_v12_bandit_idle_v6_1024.png",
     "image 2: 1012 山贼刀兵 —— 脸 / 汉末布衣 / 皮革 / 绑腿 / 刀兵身份参考，不作比例基准"),
    (r"Enemy1013ArtSource/02_IdentityImages/1013_identity_v2_stageA_2_6.png",
     "image 3: 1013 旗手候选 —— 可见面容 / 布甲 / 双手持杆；尚无旗布，非定稿"),
    (r"C:/Users/steam/Pictures/gptGen/enemy1051_redesign/T7_1051_identity_green_v1.png",
     "image 4: 1051 弓手候选 —— 木弓 / 箭袋 / 三国布革题材方向参考，非正式 105 定稿"),
]

PROMPT_CN = """为一款高清像素化三国战场动作游戏制作一张角色比例研究图。画面中只有四名曹军敌兵，分别是重甲兵、山贼刀兵、旗手和弓手。从左到右一人一个位置，四个人完整正面站立，排成同一行；这是用于比较体型的设计参考图，不是战斗场景，不是角色精灵图集。

参考图一是已验收的1011重甲兵，它是四名人物共同的身体比例和体量基准：头部相对身体有分量、肩膀宽、躯干紧凑、腿短而结实、手和脚厚实，站姿稳定。四个人都属于这一短壮、严肃、具有战斗重量的人体家族；保持共同的头身关系和身体重心，但不要把四人画成同一个脸、同一套衣服或同一个剪影。重甲、布衣、持旗和持弓可以造成合理的装备和姿态区别。

第一位：参考图一的重甲兵，保留其重甲、头盔、红色布料和持剑职业特征；他只作为比例和画风对照，不要重新设计成新武将。
第二位：参考图二的山贼刀兵，露出凶悍人脸，以汉末布衣、皮革、少量护甲、绑腿与刀表现兵种；人物比例与图一同族，保留轻装与重甲的区别。
第三位：参考图三的旗手候选，保留中国古代军人的可见面容、布甲和持杆概念；一个人双手持长木杆，杆顶有一面暗红色方形军旗，不带鼓和鼓槌。旗手的人物本体与图一同族，旗杆和旗布不是人体身高的一部分；图三只是候选，不把它尚未定稿的脸和服装当成必须原样复制的最终设计。
第四位：参考图四的弓手候选，以木弓、箭袋和三国布革军人装束识别远程兵种；这只是105弓手的方向参考，不把该候选脸部或服装当成已批准的最终造型。弓手的人体与图一同族，但装备轻于重甲兵。

四个人物本体的脚底位于同一视觉基线，人物本体高度大体相当；红缨、旗杆、旗布、刀、弓和箭袋不用于比较人物身高。人物之间留出空隙，四人的头肩、身体、双手和双脚都能单独看清，旗和武器不遮挡其他人。所有角色正面朝向玩家，不要透视远近造成的大小差，也不以整个人物拉伸或缩小来掩饰身体比例差异。

高清像素化三国动作游戏美术：清楚的深色轮廓、成簇的明暗色块，以及有体积的冷铁、布料和暖棕皮革；短壮但严肃凶悍，不做软萌卡通。横向画幅，单一中性纯色背景，只展示四个人物，不要环境、场景、战斗特效、地面透视、UI、文字、编号、水印或棋盘格。"""

PROMPT_EN = """Create one character-proportion study for a high-definition pixel-art Three Kingdoms battlefield action game. Show exactly four Cao army enemy soldiers, full-body and directly front-facing, arranged in one horizontal row from left to right: an armored infantryman, a bandit swordsman, a standard-bearer, and an archer. This is a visual design comparison, not a battle scene and not a production sprite atlas.

Image 1 is the approved Enemy 1011 and controls the shared body-proportion and mass language for all four: a head with substantial visual weight, broad shoulders, compact torso and waist, short sturdy legs, substantial hands and feet, and a stable stance. All four belong to the same short, solid, serious combat-body family, with related head-to-body proportions and center of mass. Do not make them clones with identical faces, clothing or silhouettes: heavy armor, cloth, a flag and a bow can naturally give the four soldiers distinct equipment and poses.

First, use image 1 for the armored infantryman's established armor, helmet, red cloth and sword. He is the comparison baseline; do not redesign him as a new general.
Second, use image 2 for the bandit swordsman's visible fierce face, Han-era cloth tunic, leather, limited armor, leg wraps and sword-bearing role. Give his body the same family proportions as image 1 while retaining the difference between light clothing and heavy armor.
Third, use image 3 for the standard-bearer's candidate visible face, cloth-and-armor clothing and two-handed pole-holding concept. Show one man holding a long wooden pole with a dark-red square military flag; no drum or drumsticks. His body belongs to the same family as image 1; the pole and flag are not part of his body height. Image 3 is only a candidate, not a finalized face or costume that must be copied exactly.
Fourth, use image 4 as a candidate reference for the archer's wooden bow, quiver and Han-era cloth-and-leather military theme. This is a possible direction for Enemy 105, not an approved final face or costume. His body belongs to the same family as image 1, while his equipment remains lighter than the armored infantryman's.

Place the four pairs of feet on a shared visual baseline and make their body heights broadly comparable. Do not count the plume, flagpole, flag, sword, bow or quiver as body height. Leave visible space between figures so every head, shoulder, torso, pair of hands and pair of feet can be judged separately; flags and weapons must not cover another soldier. All face the player directly. Do not use perspective depth to make one soldier larger or smaller, and do not stretch or shrink an entire body to disguise incorrect internal proportions.

Use high-definition pixel-art Three Kingdoms action-game rendering: clean dark contours, clustered light and shadow planes, solid cool iron and warm cloth / leather volumes. Compact and fierce, not cute. Landscape composition, a single plain neutral-color background, only the four characters. No environment, battle effects, perspective ground, interface, text, labels, numbers, watermark or checkerboard."""

NEGATIVE = """fewer than four people, extra person, duplicate soldiers, overlapping characters, hidden face, cropped head, missing hands, missing feet, extra limbs, swapped weapons, archer without bow, flag-bearer with drum, giant boss, identical clone faces, identical armor on all characters, long-legged realistic anatomy, tiny heads, extreme toy-like chibi, side view, three-quarter view, depth perspective lineup, flag or weapon covering another character, text, labels, numbers, emblem text, logo, UI, watermark, checkerboard, scenery, ground plane, battle effects, spell circle, photorealism, 3D render, western fantasy knight, anime moe, watercolor, blur, random pixel noise"""

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
    ap = argparse.ArgumentParser(description="提交四兵种并排比例参考图")
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

    img_out = os.path.join(OUT_DIR, "enemy_proportion_lineup_v1.png")

    if args.dry_run:
        print("=== 模型 ===\n%s" % args.model)
        print("=== 参考图（顺序固定，四张 image[]）===")
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
    os.makedirs(OUT_DIR, exist_ok=True)
    if os.path.exists(img_out):
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

    with open(os.path.join(OUT_DIR, "enemy_proportion_lineup_v1.prompt.txt"), "w",
              encoding="utf-8", newline="\n") as fh:
        fh.write(PROMPT)
    with open(os.path.join(OUT_DIR, "enemy_proportion_lineup_v1.prompt_cn.txt"), "w",
              encoding="utf-8", newline="\n") as fh:
        fh.write(PROMPT_CN)
    with open(os.path.join(OUT_DIR, "enemy_proportion_lineup_v1.prompt_negative.txt"), "w",
              encoding="utf-8", newline="\n") as fh:
        fh.write(NEGATIVE)

    with open(os.path.join(OUT_DIR, "enemy_proportion_lineup_v1.request.json"), "w",
              encoding="utf-8") as fh:
        json.dump({
            "version": "enemy_proportion_lineup_v1",
            "handoff": r"C:\threeKingdomSlayer\threeKingdomSlayer\Locus\knowledge\plan\enemy-commander-buff-unit-plan.md",
            "handoff_section": "2.15",
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
            "submitted_at_local": time.strftime("%Y-%m-%d %H:%M:%S"),
        }, fh, indent=2, ensure_ascii=False)

    request = urllib.request.Request(ENDPOINT, data=body, method="POST", headers={
        "Authorization": "Bearer %s" % key, "Content-Type": ctype})

    status = {"state": "submitted", "http_status": None, "seconds": None,
              "error": None, "output_image": img_out}
    t0 = time.time()
    try:
        with urllib.request.urlopen(request, timeout=TIMEOUT) as resp:
            raw = resp.read()
            status["http_status"] = resp.status
    except urllib.error.HTTPError as exc:
        status["state"] = "failed"
        status["error"] = "HTTPError %s: %s" % (exc.code, exc.read().decode("utf-8", "replace")[:800])
        status["seconds"] = round(time.time() - t0, 2)
        with open(os.path.join(OUT_DIR, "enemy_proportion_lineup_v1.status.json"), "w",
                  encoding="utf-8") as fh:
            json.dump(status, fh, indent=2, ensure_ascii=False)
        raise SystemExit(status["error"])
    except Exception as exc:  # noqa: BLE001
        status["state"] = "failed"
        status["error"] = "%s: %s" % (type(exc).__name__, exc)
        status["seconds"] = round(time.time() - t0, 2)
        with open(os.path.join(OUT_DIR, "enemy_proportion_lineup_v1.status.json"), "w",
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
    with open(os.path.join(OUT_DIR, "enemy_proportion_lineup_v1.response.json"), "w",
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
    with open(os.path.join(OUT_DIR, "enemy_proportion_lineup_v1.status.json"), "w",
              encoding="utf-8") as fh:
        json.dump(status, fh, indent=2, ensure_ascii=False)
    print(json.dumps(status, indent=2, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
