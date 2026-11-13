# -*- coding: utf-8 -*-
"""Dead / 受击通用动作 · 并排组图参考图 · 提交脚本（1011 / 1012 / 1013 / 1051）。

依据：Locus/knowledge/plan/enemy-dead-group-reference-20261109.md §4（生成方案）
接口：POST https://api.muskapis.com/v1/images/edits（multipart，四张 image[]，顺序固定）
模型：gpt-image-2.6-sunburst（发起前用 GET /v1/models 核对）
参数：size=1536x1024 / quality=max / input_fidelity=high / output_format=png / n=1 / 不传 background
纪律：最多提交一次，不自动重试，不覆盖已有产物

本脚本是 submit_enemy_proportion_lineup.py（Idle 并排组图）的同构副本：只借用它的
「固定顺序 + 记录哈希 + 留档 + 一次提交不重试」流程，不改动它本身。

用法
    python Tools/visual_brief/submit_enemy_dead_group.py --dry-run
    python Tools/visual_brief/submit_enemy_dead_group.py --approve
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
OUT_DIR = r"C:/Users/steam/Pictures/gptGen/enemy_dead_group_v1"
ENDPOINT = "https://api.muskapis.com/v1/images/edits"
MODELS_ENDPOINT = "https://api.muskapis.com/v1/models"
MODEL = "gpt-image-2.6-sunburst"
SIZE = "1536x1024"
QUALITY = "max"
INPUT_FIDELITY = "high"
TIMEOUT = 1200

REFS = [
    (r"Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_dead3.png",
     "image 1: 1011 重甲刀兵 —— 动作与画风家族基准（腾空被打横那一拍），尽量照原样重画"),
    (r"Assets/Sprites/Enemy/Enemy1012/Enemy_1012_dead3.png",
     "image 2: 1012 山贼刀兵 —— 同一动作的第二例，确认家族一致性"),
    (r"Enemy1013ArtSource/02_IdentityImages/1013_cast_anchor_green_1080x1080_v6.png",
     "image 3: 1013 旗手形象唯一真源 —— 脸/头巾护额/络腮胡/布袍甲片/皮带/护腕/绑腿战靴/"
     "素木杆顶金属帽/暗赭红方旗+金回纹边框/比例；只换姿势"),
    (r"Enemy1051ArtSource/02_IdentityImages/1051_idle_identity_greenscreen_1024.png",
     "image 4: 1051 弓手形象唯一真源 —— 发髻/灰头带/蒙面布巾/怒视眉眼/麻衣/皮背心/护腕/"
     "带扣/暗红布条/绑腿/绑脚鞋/右肩箭袋三支白羽箭/左手木弓；只换姿势"),
]

PROMPT_EN = """Create one pose-comparison sheet for a high-definition pixel-art Three Kingdoms action game. Four separate full-body game sprites are drawn in a 2x2 arrangement on one flat chroma-green background (RGB 0,159,62), one sprite per quadrant, with clear empty green space between the four of them and none of them cropped.

ALL FOUR SPRITES SHOW THE SAME SINGLE MOMENT: the instant after a fatal hit, when the body has been knocked sideways through the air, already turned almost horizontal, feet off the ground, limbs thrown open by the force. This is one beat, not a sequence, not a storyboard, not four different stages.

Top left: the armored soldier of image 1. Reproduce him as he is in image 1: the same armor, the same helmet, the same red cloth, the same sword already separated from his hand and flying away at the upper LEFT of the frame with a clear gap between the blade and his body. His body lies almost horizontal, head toward the right side of the frame, feet toward the left, chest still turned toward the viewer, and the lowest point of his body is lifted clear of the ground.

Top right: the bandit swordsman of image 2, in that same beat and with the same read as image 1: body almost horizontal, head to the right, feet to the left, chest toward the viewer, body lifted clear of the ground, his sword already separated from his hand and flying away at the upper LEFT of the frame with a clear gap.

Bottom left: the standard-bearer of image 3 in that same beat. Keep him exactly as image 3: the same face, the same dark cloth headband and studded leather brow band, the same tied hair knot, the same square jaw and full beard, the same cream cross-collar cloth robe, the same few dark blue-grey shoulder and chest armour plates, the same dark brown leather belt with its square buckle, the same thick bracers, the same dark brown trousers, the same pale leg wraps, the same heavy boots, the same plain wooden pole with its small metal cap, the same dark vermilion square flag with its gold meander border, the same pixel-art rendering, the same colors and the same body proportions. Only his pose changes. His body is knocked sideways through the air and lies almost horizontal, head toward the right side of the frame, feet toward the left, chest still toward the viewer, his lowest point lifted clear of the ground, and both arms thrown open by the force.

His pole has left his hands and is tumbling through the air at the upper LEFT of the frame, the same way the two swords fly above him. The square flag is still tied to the top end of that same pole and travels with it: the cloth is never detached from the pole, never a separate flat piece lying on the ground, and it keeps the same side of the pole as in image 3.

Bottom right: the archer of image 4 in that same beat. Keep him exactly as image 4: the same topknot, the same grey cloth headband, the same grey scarf over the lower half of his face, the same angry narrowed eyes, the same coarse grey tunic with the torn hem, the same worn dark brown leather vest, the same leather bracers, the same square belt buckle, the same short dagger at his belt, the same red cloth strip, the same dark brown cropped trousers, the same pale leg wraps, the same strapped sandals, the same quiver with three white fletched arrows over his right shoulder, the same wooden bow with its wrappings and its red band on the grip, the same pixel-art rendering, the same colors and the same proportions. Only his pose changes. His body is knocked sideways through the air and lies almost horizontal, head toward the LEFT side of the frame, feet toward the right, chest still toward the viewer, his lowest point lifted clear of the ground.

His bow has just left his left hand and is tumbling through the air at the upper RIGHT of the frame, on the same side as the bow in image 4, with a clear gap between the bow and his body. There is no arrow in either hand and no loose fletching anywhere in the frame; the quiver on his shoulder still holds exactly the same three white fletched arrows.

All four bodies are drawn in the same silhouette language: the back arched into a curve rather than a straight line, the chest opened toward the viewer, the head thrown back with the chin up, the legs bent and lifted so the boots are clearly higher than the hips, the arms thrown open and asymmetric, one up and one down, and the hands relaxed, small and half closed, never splayed like claws and never reaching out toward the viewer.

Framing for all four: each figure fits inside its own quadrant with a clear green margin on every side, the whole body plus its weapon fully visible, nothing cropped and nothing touching the edge of the picture. Each body occupies the middle band of its quadrant, with the top of the head at about 30% of the canvas height and the lowest point of the body at about 75% of the canvas height, so the four bodies sit at the same height and can be compared directly. The four characters are drawn at a comparable body size so that the silhouettes can be compared.

Rendering: the same high-definition pixel-art rendering as the reference images, clean dark contours, clustered light and shadow planes, solid volumes in cloth, leather and wood. One flat chroma-green background (RGB 0,159,62) behind all four, no floor, no ground line, no shadow, no scenery, no text, no labels, no numbers, no UI, no watermark, no checkerboard."""

PROMPT_CN = """为高清像素化三国动作游戏制作一张姿态对照图。四个独立的全身游戏精灵以 2×2 排列画在同一张纯绿幕底（RGB 0,159,62）上，每格一个精灵，四者之间留出明显的绿色空隙，任何一个都不得出画。

四个精灵画的是同一个瞬间：致命一击刚打中、身体被横向打飞、已经几乎被打横、双脚离地、四肢被力量甩开的这一拍。这是一拍，不是一组连续动作，不是分镜，也不是四个不同阶段。

左上：图 1 的重甲兵。尽量照图 1 原样重画：同样的重甲、头盔、红布，他的剑已经脱手，飞在画面左上角，刀身与身体之间有明显空隙。他的身体几乎横躺，头在画面右侧、双脚在左侧，胸口仍朝向观众，身体最低点离开地面。

右上：图 2 的山贼刀兵，与图 1 同一拍、同一种读法：身体几乎横躺，头右脚左，胸口朝观众，身体离地，剑已脱手飞向画面左上角，与身体之间有明显空隙。

左下：图 3 的旗手，同一拍。人物完全照图 3：同一张脸、同一条深色布头巾与带铆钉皮质护额、同一束发髻、同一副方下颌与络腮胡、同一件米色交领布袍、同样那几块深蓝灰肩胸甲片、同一条带方形带扣的深棕皮带、同样厚实的护腕、同样的深棕裤、同样的浅色绑腿与厚底战靴、同一根杆顶带小金属帽的素木杆、同一面暗赭红方旗与金回纹边框、同样的像素画风、配色与身体比例。只有姿势改变：身体被横向打飞、几乎横躺，头在画面右侧、双脚在左侧，胸口仍朝观众，身体最低点离开地面，双臂被力量甩开。

旗杆已经脱手，在画面左上方翻飞，与上方两把剑的飞法一致。方旗仍然系在同一根杆的杆顶、随杆一起飞：旗布绝不与杆分离，绝不是单独平铺在地上的一块布，仍然在杆的同一侧。

右下：图 4 的弓手，同一拍。人物完全照图 4：同一发髻、灰头带、遮住下半脸的灰布巾、同一双怒视细眼、同一件粗麻短衣与破旧深棕皮背心、皮革护腕、方形带扣、腰间短刀、暗红布条、深棕束裤、浅色绑腿、绑脚鞋、右肩箭袋与三支白羽箭、同一把木弓与弓柄红带、同样的像素画风、配色与比例。只有姿势改变：身体被横向打飞、几乎横躺，头在画面左侧、双脚在右侧，胸口仍朝观众，身体最低点离开地面。

弓刚从他左手脱落，在画面右上角翻飞，与图 4 中弓所在的一侧相同，与身体之间有明显空隙。两只手里都没有箭，画面上也没有散落的箭羽；肩后箭袋仍是同样三支白羽箭。

四个身体用同一套剪影语言：背部弓成弧线而不是一条直线，胸口打开朝向观众，头后甩、下巴朝上，双腿屈起、鞋底明显高于臀部，双臂不对称地甩开（一上一下），手放松、半握、保持与身份图相同的小尺寸，绝不成张开的爪子，绝不朝镜头伸。

四个格子的取景一致：人物完整在自己的格子里，四周留明显绿色空隙，身体与武器全部可见，不出画、不贴边。身体都落在画布中间高度带：头顶约在画布 30% 高度、身体最低点约在 75% 高度，四个身体高度一致以便直接比较，四个角色画成大体相同的体量以便比较剪影。

渲染：与参考图一致的高清像素画风，干净的深色轮廓，成簇的明暗色块，布、皮革、木头有实打实的体积感。四个精灵共用一张纯绿幕底（RGB 0,159,62），没有地面、没有地平线、没有投影、没有场景、没有文字、没有编号、没有 UI、没有水印、没有棋盘格。"""

NEGATIVE = """fewer than four sprites, more than four sprites, extra person, duplicate character, two standard-bearers, two archers, second bow, second sword, second pole, second flag, flag detached from the pole, flag lying flat on the ground as a separate cloth, pole standing upright, spearhead on the pole tip, drum, shield on the archer, arrow in the hands, loose fletching, empty quiver, cropped head, cropped feet, missing hands, missing boots, extra limbs, extra fingers, splayed claw hands, hands reaching toward the viewer, straight horizontal body, body lying flat on the ground, standing pose, kneeling pose, crouching pose, feet touching the ground, body much smaller than the others, body much larger than the others, side view, three-quarter view, back view, perspective depth, different sizes per quadrant, text, labels, numbers, watermark, checkerboard, scenery, ground plane, cast shadow, motion blur, glow, spark, dust, blood, photorealism, 3D render, anime, cartoon, chibi"""

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
    ap = argparse.ArgumentParser(description="提交 Dead/受击通用动作并排组图参考图")
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

    img_out = os.path.join(OUT_DIR, "enemy_dead_group_v1.png")

    if args.dry_run:
        print("=== 模型 ===\n%s" % args.model)
        print("=== 参考图（顺序固定，四张 image[]）===")
        for i, (p, role) in enumerate(files, 1):
            print("image %d: %s\n         bytes=%s sha256=%s\n         %s"
                  % (i, p, os.path.getsize(p), sha256_file(p), role))
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

    with open(os.path.join(OUT_DIR, "enemy_dead_group_v1.prompt.txt"), "w",
              encoding="utf-8", newline="\n") as fh:
        fh.write(PROMPT)
    with open(os.path.join(OUT_DIR, "enemy_dead_group_v1.prompt_cn.txt"), "w",
              encoding="utf-8", newline="\n") as fh:
        fh.write(PROMPT_CN)
    with open(os.path.join(OUT_DIR, "enemy_dead_group_v1.prompt_negative.txt"), "w",
              encoding="utf-8", newline="\n") as fh:
        fh.write(NEGATIVE)

    with open(os.path.join(OUT_DIR, "enemy_dead_group_v1.request.json"), "w",
              encoding="utf-8") as fh:
        json.dump({
            "version": "enemy_dead_group_v1",
            "handoff": r"Locus/knowledge/plan/enemy-dead-group-reference-20261109.md",
            "handoff_section": "4",
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
            "prompt_en_sha256": hashlib.sha256(PROMPT_EN.encode("utf-8")).hexdigest(),
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
        with open(os.path.join(OUT_DIR, "enemy_dead_group_v1.status.json"), "w",
                  encoding="utf-8") as fh:
            json.dump(status, fh, indent=2, ensure_ascii=False)
        raise SystemExit(status["error"])
    except Exception as exc:  # noqa: BLE001
        status["state"] = "failed"
        status["error"] = "%s: %s" % (type(exc).__name__, exc)
        status["seconds"] = round(time.time() - t0, 2)
        with open(os.path.join(OUT_DIR, "enemy_dead_group_v1.status.json"), "w",
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
    with open(os.path.join(OUT_DIR, "enemy_dead_group_v1.response.json"), "w",
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
    with open(os.path.join(OUT_DIR, "enemy_dead_group_v1.status.json"), "w",
              encoding="utf-8") as fh:
        json.dump(status, fh, indent=2, ensure_ascii=False)
    print(json.dumps(status, indent=2, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
