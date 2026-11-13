"""天赋界面参考图 —— 第 3 轮（v1 被否：太简陋太复古；v2 被否：不该拿我们自制的底图当图 1）。

v3 的根本改动：
  · **图 1 不再是我们做的界面**，直接就是刀锋技能页参考图原图
    → 版式与功能（顶部标题+细金线、三个页签、节点树区、底部大面板+通栏按钮）
      全部由参考图决定，模型只做重绘
  · 我们只改两样：**画风**（图 2 三国概念图锚定）与**内容物**（我们的 6 个天赋 + 中文 + 图标）
  · 节点牌：菱形 → 兵符（竖长 + 拱顶 + 顶部穿绳孔 + 直筒身 + 圆底）

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
# 图 1 = 刀锋参考图原图（版式与功能的底图）
SRC_BASE = os.path.join(ROOT, "_art_process/Authoring/blades_skill_ref/blades_skills_portrait_rebuild.png")
# 图 2 = 画风锚点（三国概念图，art-style-guide 的权威基准）
SRC_STYLE = r"C:\Users\steam\Downloads\微信图片_20260928101134_12_223.png"
OUT_DIR = r"C:\Users\steam\Pictures\gptGen\talent_ui_v3"
IMG_IN_1 = os.path.join(OUT_DIR, "blades_ref_input.png")
IMG_IN_2 = os.path.join(OUT_DIR, "sanguo_style_input.png")
IMG_OUT = os.path.join(OUT_DIR, "talent_ui_mock_v3.png")
ENDPOINT = "https://api.muskapis.com/v1/images/edits"
MODEL = "gpt-image-2.6-sunburst"
TIMEOUT = 900

PROMPT_EN = """Redraw image 1 as our own game's version of the very same screen. Image 1 is the screen to be redrawn; image 2 is the art-direction reference.

KEEP FROM IMAGE 1 (the structure, the function and the layout - do not redesign them):
- the overall screen composition and its vertical rhythm
- the header: a large title at the top left, ONE thin horizontal hairline rule running across beneath it, and a small emblem plus a number slot at the top right
- the row of three category tabs below the header; the active tab is brighter and carries a single thin underline, the other two are dimmer and have no underline
- the node tree area: node plates linked by thin lines, each plate carrying a pictogram, with a small label and a smaller price line underneath each plate
- the large bottom detail panel: its frame, a small node plate thumbnail on the left, a large name, one metadata line, one description paragraph, and a wide button pinned along the bottom edge of the panel
- the restrained, spacious, high-contrast feel of the whole screen

CHANGE ONLY THESE THREE THINGS:

1. NODE SHAPE. Every diamond node plate becomes a Chinese "bingfu" / tiger-tally command tablet: tall vertical proportion, height about 1.55x the width; a rounded arched crown; ONE small circular cord hole at the top centre of the crown; a straight-sided body; a rounded-off bottom. The pictogram sits in the lower half of the tablet, below the cord hole. Keep the plates flat, clean and MATTE exactly like image 1's diamonds, just with this new silhouette - NO glow, NO halo, NO bloom around the plates, and the cord hole is a plain hole, not a lamp.

2. CONTENT. The screen is a "Talent" tree for our Three Kingdoms action game. The node tree area keeps image 1's treatment (plates linked by thin lines, a small label and a smaller price line under each) but holds OUR six talent nodes instead of image 1's thirteen: one node at the top centre, two on the left and two on the right in the row below it, and one at the bottom centre; thin connector lines run upward and merge into the top node. The detail panel keeps image 1's structure but shows ONE of our talents: its Chinese name, one Chinese metadata line, one Chinese description sentence, a Chinese unlock-state line, a Chinese coin-cost line, and a Chinese unlock button. Drop image 1's data table entirely. Keep the six nodes visually distinguishable by state: the top node = unlocked, in the brightest polished brass; the two nodes on the right = available, in normal brass; the two nodes on the left and the bottom node = locked, in dull desaturated dark bronze. No glow on any of them.

3. ART DIRECTION. Render everything in the art direction of image 2: dense, chunky, high-contrast pixel art in the Three Kingdoms idiom - heavy battered metal, ink-black darks, warm earth and ochre, vermilion accents, thick readable shapes, strong black outlines, clustered hand-painted pixels. Keep image 1's thin gold hairlines and its clean spacing, but give every plate, frame and button the material and pixel texture of image 2. Everything is matte and heavy: no glow, no halo, no bloom, no light source effect.

DO NOT:
- do not keep any of image 1's English words or numbers; do not keep its data table
- do not render any text, letters, numbers or Chinese characters anywhere: leave every label area as clean empty space
- do not draw characters, soldiers, horses, weapons, banners, scenery or a battlefield background
- do not use smooth 3D rendering, chrome reflections, lens flares, neon, cartoon shading, ornate filigree, seal stamps, checkerboard, or a white background"""

PROMPT_ZH = """
【中文补充要求 —— 与上面英文同义，逐条对齐】
把【图 1】重绘成我们游戏里同一个界面的版本。图 1 = 要重绘的界面；图 2 = 画风参考。

【从图 1 保留（结构、功能、版式 —— 不要重新设计）】
· 整体构图与纵向节奏
· 顶部：左上一个大标题，标题下一条横贯的细金线，右上一个小徽记 + 数字位
· 标题下一行三个分类页签；选中那个更亮并带一条细下划线，另外两个更暗且没有下划线
· 节点树区：用细线连起来的节点牌，每块牌内一个图形，牌下方一行小标签和一行更小的价格
· 底部大详情面板：它的边框、左侧一个节点牌小缩略图、一个大标题、一行元信息、一段描述、
  以及贴在面板底边的一个通栏按钮
· 整屏克制、留白、高对比的气质

【只改这三处】

1. 节点造型。所有菱形节点牌换成中国古代「兵符／虎符」形铜牌：竖长比例，高约为宽的 1.55 倍；
   顶部圆拱冠；冠顶正中**一个**小圆形穿绳孔；牌身直筒；底部圆收。图形放在牌的下半部、
   穿绳孔之下。牌的画法保持图 1 菱形那种干净、扁平、哑光的质感，只是换了这个剪影 —— 不要发光、

2. 内容物。这个界面是我们三国动作游戏的「天赋」树。节点树区保留图 1 的做法（细线连起来的牌、
   牌下一行标签加一行更小的价格），但装的是**我们的 6 个天赋节点**，不是图 1 的 13 个：
   最上方正中 1 个，其下一行左 2 右 2 共 4 个，最底部正中 1 个；细连线由下往上汇聚到顶部节点。
   详情面板保留图 1 的结构，但展示我们其中一个天赋：中文名称、一行中文元信息、一句中文描述、
   一行中文解锁状态、一行中文铜钱花费、一个中文解锁按钮。图 1 的数据表整个删掉。六个节点要按状态分得出来：最上面那个 = 已解锁，用最亮的抛光黄铜；右侧两个 = 可解锁，用正常黄铜；左侧两个和最下面那个 = 锁定，用发暗、去饱和的暗铜。都不发光。

3. 画风。整屏按【图 2】的方向画：密实的、成块的高对比三国像素风 —— 厚重残破的金属、墨黑暗部、
   暖土与赭黄、朱红点缀、粗壮可读的形状、硬黑描边、成簇的手绘像素。保留图 1 的细金线与
   干净留白，但每块牌、每个边框、每个按钮都换上图 2 的材质与像素质感。全部哑光厚重：不要发光、
   不要光晕、不要辉光、不要任何光源特效。

【不要】
· 不要保留图 1 的任何英文词与数字；不要保留它的数据表
· 任何地方都不要画文字、字母、数字、汉字，所有标签位置保持干净的留白
· 不要人物、士兵、战马、兵器、旗幡、景物或战场背景
· 不要光滑 3D 渲染、镜面反射、镜头光晕、霓虹、卡通上色、繁复花纹、印章、棋盘格、白底
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

    with open(os.path.join(OUT_DIR, "talent_ui_mock_v3.prompt.txt"), "w",
              encoding="utf-8", newline="\n") as fh:
        fh.write(PROMPT)

    req_meta = {
        "version": "talent_ui_mock_v3",
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
             "role": "image 1: THE SCREEN TO REDRAW (Blades skill-page reference) -> keeps structure, function and layout verbatim",
             "sha256": sha256_file(IMG_IN_1), "bytes": os.path.getsize(IMG_IN_1)},
            {"order": 2, "field": "image[]", "filename": os.path.basename(IMG_IN_2),
             "path": IMG_IN_2,
             "role": "image 2: ART-DIRECTION ANCHOR (Three Kingdoms concept art) -> pixel-art material, palette and texture only",
             "sha256": sha256_file(IMG_IN_2), "bytes": os.path.getsize(IMG_IN_2)},
        ],
        "body_bytes": len(body),
        "automatic_retries": 0,
        "read_timeout_seconds": TIMEOUT,
        "submitted_at_local": time.strftime("%Y-%m-%d %H:%M:%S"),
    }
    with open(os.path.join(OUT_DIR, "talent_ui_mock_v3.request.json"), "w", encoding="utf-8") as fh:
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
        with open(os.path.join(OUT_DIR, "talent_ui_mock_v3.status.json"), "w", encoding="utf-8") as fh:
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
        with open(os.path.join(OUT_DIR, "talent_ui_mock_v3.status.json"), "w", encoding="utf-8") as fh:
            json.dump(status, fh, indent=2, ensure_ascii=False)
        raise SystemExit(status["error"])

    with open(IMG_OUT, "wb") as fh:
        fh.write(blob)
    with open(os.path.join(OUT_DIR, "talent_ui_mock_v3.response.json"), "w", encoding="utf-8") as fh:
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
    with open(os.path.join(OUT_DIR, "talent_ui_mock_v3.status.json"), "w", encoding="utf-8") as fh:
        json.dump(status, fh, indent=2, ensure_ascii=False)
    print(json.dumps(status, indent=2, ensure_ascii=False))


if __name__ == "__main__":
    main()
