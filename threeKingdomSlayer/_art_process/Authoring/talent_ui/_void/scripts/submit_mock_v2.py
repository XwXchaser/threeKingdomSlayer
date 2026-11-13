"""天赋界面参考图 —— 第 2 轮（v1 被否：太简陋太复古）。

v2 相对 v1 的三个关键改动：
  1) 底图换材料语言：从「占位兵符两色平涂」改成刀锋参考图那种「暖奶油→蜜金渐层牌 + 细深棕描边 + 顶部发光孔 + 深色凹槽图标区」
  2) 底图整块改成近黑底 + 细暖金发丝线 + 明亮奶油色字体（v1 是大块暗铜底板）
  3) **喂两张图**：底图（版式）+ 刀锋竖屏参考图（风格锚点），提示词明确「image 2 只借视觉语言，不借版式」

参数沿用项目默认：gpt-image-2.6-sunburst / edits / quality=max / input_fidelity=high / 1024x1536 / png / n=1
"""
import base64
import hashlib
import json
import os
import shutil
import time
import urllib.request
import uuid

from PIL import Image

ROOT = r"C:\threeKingdomSlayer\threeKingdomSlayer"
SRC_BASE = os.path.join(ROOT, "Library/Locus/tmp/talent_ui/talent_mock_base2_notext.png")
SRC_STYLE = os.path.join(ROOT, "_art_process/Authoring/blades_skill_ref/blades_skills_portrait_rebuild.png")
OUT_DIR = r"C:\Users\steam\Pictures\gptGen\talent_ui_v2"
IMG_IN_1 = os.path.join(OUT_DIR, "talent_ui_base2_input.png")
IMG_IN_2 = os.path.join(OUT_DIR, "blades_style_ref_input.png")
IMG_OUT = os.path.join(OUT_DIR, "talent_ui_mock_v2.png")
ENDPOINT = "https://api.muskapis.com/v1/images/edits"
MODEL = "gpt-image-2.6-sunburst"
TIMEOUT = 900

PROMPT_EN = """Redraw the screen shown in image 1, keeping its layout, node count, node positions and the command-tally plaque shape EXACTLY as they are, but restyle it so it looks like image 2.

IMAGE ROLES
- image 1 = the screen to redraw. Keep its layout, its six node plates, their positions, the connector tree, the header rule, the three tabs, the bottom panel and the button.
- image 2 = STYLE REFERENCE ONLY. Take its visual language and nothing else.

TAKE FROM IMAGE 2 (the style reference)
- the pure near-black background with a faint warm dust grain
- the elegant restrained typography: a warm cream serif display title, small clean sans-serif labels, all in cream (#E8DCC4), dimmer cream for inactive items
- the single thin warm-gold hairline rule under the header
- the thin warm-gold hairline border on the bottom panel and on the button
- the node plate treatment: a warm cream-to-honey-gold plate, softly brighter toward its centre, with a thin dark brown outline, a soft warm bloom of light around it, and a small bright glowing point at its top
- the thin warm-tan connector lines that meet the glowing point of the plate below
- the dark recessed circular hollow in the lower half of each plate where the pictogram sits
- the generous empty space, the high contrast, the modern quiet feel

CHANGE ONLY THIS
- the node plates are Chinese "bingfu" / tiger-tally command tablets instead of diamonds: tall vertical proportion, height about 1.55x the width; a rounded arched crown; ONE small circular cord hole at the top centre of the crown that glows like a little lamp; a straight-sided body; a rounded-off bottom. Keep the plate material, the thin dark outline, the bloom, the glowing hole and the dark icon hollow exactly in image 2's language.
- the bottom panel keeps image 1's content layout (a small plaque thumbnail on the left, a name, one description line, a state line, a cost line, and a gold-outlined button on the right) but is rendered in image 2's language.
- keep image 1's three node states distinguishable:
  * the top plate = unlocked: brightest honey gold, strongest bloom, a warm glow inside the cord hole
  * the two plates on the right = available: normal honey gold
  * the two plates on the left and the bottom plate = locked: cold desaturated grey bronze, no bloom, dimmed

DO NOT
- do not copy image 2's layout, its diamond node shapes, its English words, its bottom data table, its header count badge, or its gold selection rectangle
- do not render any text, letters, numbers or Chinese characters: leave every label area as clean empty space
- do not add any new icons or symbols beyond the small pictograms already inside the plates
- no characters, no soldiers, no battlefield, no heavy dark 3D metal, no rivets, no filigree, no seal stamps, no ornate carving, no parchment scroll, no neon, no cartoon, no checkerboard, no white background"""

PROMPT_ZH = """
【中文补充要求 —— 与上面英文同义，逐条对齐】
把【图 1】这张界面**保持版式不动**地重绘，让它**看起来像【图 2】**。

【两张图的职责】
· 图 1 = 要重绘的界面。保留它的版式、6 块节点牌、节点位置、连线树、标题下的细线、三个页签、底部面板和按钮。
· 图 2 = **只作风格参考**。只取它的视觉语言，不取它的内容。

【从图 2 取的（视觉语言）】
· 纯近黑背景 + 极淡的暖色颗粒
· 雅致克制的字体：奶油色衬线大标题 + 干净的小号无衬线标签，统一奶油色（约 #E8DCC4），未选中项用更暗的奶油色
· 标题下一条细暖金发丝线
· 底部面板与按钮都是细暖金发丝线描边
· 节点牌的处理：暖奶油→蜜金渐层牌，越靠近中心越亮，细深棕描边，外围一圈柔和暖光晕，顶部有一个明亮的小光点
· 细暖棕连线，接在下方那块牌的光点上
· 每块牌下半部有一个深色圆形凹槽，图标放在里面
· 大量留空、高对比、现代而安静的整体气质

【只改这一处】
· 节点牌从图 2 的菱形换成中国古代「兵符／虎符」形铜牌：竖长比例，高约为宽的 1.55 倍；顶部圆拱冠；冠顶正中**一个**小圆形穿绳孔，像一盏小灯一样发光；牌身直筒；底部圆收。牌面的材质、细深棕描边、光晕、发光的孔、深色图标凹槽，都保持图 2 的语言。
· 底部面板保留图 1 的内容结构（左侧小兵符缩略图 + 名称 + 一行描述 + 状态行 + 价格行 + 右侧金线描边按钮），但用图 2 的语言渲染。
· 图 1 的三态必须能一眼分开：
  - 最上面那块 = 已解锁：最亮的蜜金色，光晕最强，孔内透暖光
  - 右侧两块 = 可解锁：正常蜜金色
  - 左侧两块和最下面那块 = 锁定：冷灰、去饱和的暗铜色，没有光晕，整体压暗

【不要】
· 不要照抄图 2 的版式、菱形节点、英文文字、底部数据表、标题右侧的数字徽记、节点外面的金色选中方框
· 不要画任何文字、字母、数字、汉字，所有标签位置保持干净的留白
· 不要新增任何图标或符号，牌内已有的小图形是占位，保持原样
· 不要人物、士兵、战场、厚重暗色 3D 金属、铆钉、花纹、印章、雕花、卷轴、霓虹、卡通、棋盘格、白底
"""

PROMPT = PROMPT_EN + "\n" + PROMPT_ZH


def sha256_file(path):
    with open(path, "rb") as fh:
        return hashlib.sha256(fh.read()).hexdigest()


def build_multipart(fields, files):
    boundary = "----locus" + uuid.uuid4().hex
    buf = bytearray()
    for name, value in fields:
        buf += f"--{boundary}\r\n".encode()
        buf += f'Content-Disposition: form-data; name="{name}"\r\n\r\n'.encode()
        buf += f"{value}\r\n".encode()
    for name, filename, path, ctype in files:
        buf += f"--{boundary}\r\n".encode()
        buf += (f'Content-Disposition: form-data; name="{name}"; filename="{filename}"\r\n').encode()
        buf += f"Content-Type: {ctype}\r\n\r\n".encode()
        with open(path, "rb") as fh:
            buf += fh.read()
        buf += b"\r\n"
    buf += f"--{boundary}--\r\n".encode()
    return bytes(buf), boundary


def main():
    key = os.environ.get("MUSK_API_KEY")
    if not key:
        raise SystemExit("MUSK_API_KEY missing")
    os.makedirs(OUT_DIR, exist_ok=True)
    if os.path.exists(IMG_OUT):
        raise SystemExit(f"refusing to overwrite existing output: {IMG_OUT}")
    shutil.copyfile(SRC_BASE, IMG_IN_1)
    shutil.copyfile(SRC_STYLE, IMG_IN_2)

    fields = [
        ("model", MODEL),
        ("prompt", PROMPT),
        ("size", "1024x1536"),
        ("quality", "max"),
        ("input_fidelity", "high"),
        ("output_format", "png"),
        ("n", "1"),
    ]
    files = [
        ("image[]", os.path.basename(IMG_IN_1), IMG_IN_1, "image/png"),
        ("image[]", os.path.basename(IMG_IN_2), IMG_IN_2, "image/png"),
    ]
    body, boundary = build_multipart(fields, files)

    with open(os.path.join(OUT_DIR, "talent_ui_mock_v2.prompt.txt"), "w",
              encoding="utf-8", newline="\n") as fh:
        fh.write(PROMPT)

    req_meta = {
        "version": "talent_ui_mock_v2",
        "endpoint": ENDPOINT,
        "method": "POST",
        "content_type": f"multipart/form-data; boundary=<{boundary}>",
        "fields": {"model": MODEL, "size": "1024x1536", "quality": "max",
                   "input_fidelity": "high", "output_format": "png", "n": "1",
                   "background": "omitted"},
        "prompt_sha256": hashlib.sha256(PROMPT.encode("utf-8")).hexdigest(),
        "prompt_chars": len(PROMPT),
        "negative_prompt": None,
        "images": [
            {"order": 1, "field": "image[]", "filename": os.path.basename(IMG_IN_1),
             "path": IMG_IN_1,
             "role": "image 1: the screen to redraw -> LAYOUT + plaque silhouette + node positions + material language",
             "sha256": sha256_file(IMG_IN_1), "bytes": os.path.getsize(IMG_IN_1)},
            {"order": 2, "field": "image[]", "filename": os.path.basename(IMG_IN_2),
             "path": IMG_IN_2,
             "role": "image 2: STYLE ANCHOR ONLY (The Elder Scrolls: Blades skill page rebuild) -> visual language, lighting, typography mood, restraint. NOT layout, NOT content.",
             "sha256": sha256_file(IMG_IN_2), "bytes": os.path.getsize(IMG_IN_2)},
        ],
        "body_bytes": len(body),
        "automatic_retries": 0,
        "read_timeout_seconds": TIMEOUT,
        "submitted_at_local": time.strftime("%Y-%m-%d %H:%M:%S"),
    }
    with open(os.path.join(OUT_DIR, "talent_ui_mock_v2.request.json"), "w", encoding="utf-8") as fh:
        json.dump(req_meta, fh, indent=2, ensure_ascii=False)

    request = urllib.request.Request(
        ENDPOINT, data=body, method="POST",
        headers={"Authorization": f"Bearer {key}",
                 "Content-Type": f"multipart/form-data; boundary={boundary}"})
    status = {"state": "submitted", "http_status": None, "seconds": None,
              "error": None, "timeout_seconds": TIMEOUT, "output_image": IMG_OUT}
    t0 = time.time()
    try:
        with urllib.request.urlopen(request, timeout=TIMEOUT) as resp:
            raw = resp.read()
            status["http_status"] = resp.status
    except Exception as exc:  # noqa: BLE001
        status["state"] = "failed"
        status["error"] = f"{type(exc).__name__}: {exc}"
        status["seconds"] = round(time.time() - t0, 2)
        with open(os.path.join(OUT_DIR, "talent_ui_mock_v2.status.json"), "w", encoding="utf-8") as fh:
            json.dump(status, fh, indent=2, ensure_ascii=False)
        raise SystemExit(status["error"])

    status["seconds"] = round(time.time() - t0, 2)
    payload = json.loads(raw.decode("utf-8"))
    item = payload["data"][0]
    b64 = item.get("b64_json")
    url = item.get("url")
    if b64:
        blob = base64.b64decode(b64)
    elif url:
        with urllib.request.urlopen(url, timeout=TIMEOUT) as r:
            blob = r.read()
    else:
        status["state"] = "failed"
        status["error"] = "no image payload in response"
        with open(os.path.join(OUT_DIR, "talent_ui_mock_v2.status.json"), "w", encoding="utf-8") as fh:
            json.dump(status, fh, indent=2, ensure_ascii=False)
        raise SystemExit(status["error"])

    with open(IMG_OUT, "wb") as fh:
        fh.write(blob)
    with open(os.path.join(OUT_DIR, "talent_ui_mock_v2.response.json"), "w", encoding="utf-8") as fh:
        json.dump(payload, fh, indent=2, ensure_ascii=False)

    out_bytes = open(IMG_OUT, "rb").read()
    with Image.open(IMG_OUT) as im:
        status["width"], status["height"] = im.size
        status["color_type"] = im.mode
    status["output_bytes"] = len(out_bytes)
    status["output_sha256"] = hashlib.sha256(out_bytes).hexdigest()
    status["png_signature"] = out_bytes[:8] == b"\x89PNG\r\n\x1a\n"
    status["usage"] = payload.get("usage")
    status["echoed_size"] = payload.get("size")
    status["echoed_quality"] = payload.get("quality")
    status["model"] = payload.get("model")
    status["created"] = payload.get("created")
    status["state"] = "image_saved"
    with open(os.path.join(OUT_DIR, "talent_ui_mock_v2.status.json"), "w", encoding="utf-8") as fh:
        json.dump(status, fh, indent=2, ensure_ascii=False)
    print(json.dumps(status, indent=2, ensure_ascii=False))


if __name__ == "__main__":
    main()
