"""天赋界面参考图 —— 第 4 轮（v3 被否：与当前主界面的画风与内容不相符）。

v4 相对 v3 的改动（依据「实机画风实测」，不是概念图推断）：
  · 第 3 张输入**从战场概念图换成实机兵符材质板**
    （Library/Locus/tmp/talent_ui/hud_token_material_ref.png，由已部署的
      Assets/Sprites/UI/Combat/hud_slot_token*.png 拼出）
    —— 战场概念图满屏厚重金属，正是要把「简洁白边」冲掉的东西
  · 兵符材质锚定到实物：面 #1A1A1E，描边 #F2EDE0，描边宽约牌宽 3%
  · 面板 = 平面近黑 + 一圈米白细发丝边框（无倒角无渐变）
  · 页签 = 纯文字 + 选中一条细下划线
  · 底部 = 通栏大按钮；全屏唯一饱和色留给它（深朱红 #9A0101 + 奶油字）
  · 背景 = 平面近黑，无笔刷噪点、无纹样、无暗角
  · 三态只改描边色，牌面始终是黑的

保留（用户已确认）：图 1 = 刀锋参考图原图（版式与功能照它）；图 2 = 战斗界面概念图（只取安静气质与奶油色字体）；
节点造型 = 兵符；内容 = 我们的 6 个天赋；页签纯文字；通栏大按钮。

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
# 图 1 = 刀锋参考图原图（版式与功能）
SRC_BASE = os.path.join(ROOT, "_art_process/Authoring/blades_skill_ref/blades_skills_portrait_rebuild.png")
# 图 2 = 战斗界面概念图（只取安静气质与奶油色字体）
SRC_UI = r"C:\Users\steam\Downloads\微信图片_20260928101134_11_223.png"
# 图 3 = 实机兵符材质板（已部署 HUD 令牌实物，兵符画法的唯一权威）
SRC_TOKEN = os.path.join(ROOT, "Library/Locus/tmp/talent_ui/hud_token_material_ref.png")
OUT_DIR = r"C:\Users\steam\Pictures\gptGen\talent_ui_v4"
IMG_OUT = os.path.join(OUT_DIR, "talent_ui_mock_v4.png")
ENDPOINT = "https://api.muskapis.com/v1/images/edits"
MODEL = "gpt-image-2.6-sunburst"
TIMEOUT = 900

PROMPT_EN = """Redraw the screen in image 1 as our own game's version of the same screen.

IMAGE ROLES
- image 1 = the screen to redraw. Keep its structure and function exactly: the header with a large title and one thin rule across, the row of three category tabs, the node tree area, and the large bottom detail panel with a full-width button pinned along its bottom edge.
- image 2 = the screen's overall quietness and its typography. Take from it ONLY: a very dark calm field, cream coloured type, restrained contrast, generous empty space. Do NOT take its brush strokes, its gold edging, its vermilion areas or its painted texture.
- image 3 = THE AUTHORITY on what a node looks like. Copy the way those three tablets are drawn, exactly: a flat near-black face, a thin cream hairline outline, a plain cream ring hole near the top, hard clean edges, no material, no shading, no texture, no bevel.

LAYOUT AND FUNCTION - KEEP FROM IMAGE 1
- the header: a large title at the top left, ONE thin horizontal hairline rule running across beneath it, and a small emblem plus a number slot at the top right
- the row of three category tabs below the header: PLAIN TEXT ONLY - no plates, no boxes, no metal, no frames. The active tab is brighter cream with a single thin underline beneath it; the other two are dimmer and have no underline.
- the node tree area: node plates linked by thin lines, each plate carrying a pictogram, with a small label and a smaller price line underneath each plate
- the large bottom detail panel: its frame, a small node plate thumbnail on the left, a large name, one metadata line, one description paragraph, a state line, a cost line, and a FULL-WIDTH button pinned along the bottom edge of the panel
- the restrained, spacious, high-contrast feel of the whole screen

NODE SHAPE AND MATERIAL - THE MOST IMPORTANT PART
Every diamond node plate becomes the same kind of tablet as image 3, only taller and with the pictogram inside:
- tall vertical proportion, height about 1.55x the width
- a rounded arched crown; ONE small circular cord hole at the top centre of the crown, drawn as a plain cream ring with the dark field showing through, exactly like image 3
- a straight-sided body and a rounded-off bottom
- the face is FLAT near-black ink, about #1A1A1E, with absolutely no gradient, no sheen, no texture, no bevel
- the outline is a thin cream hairline, about #F2EDE0, roughly 3% of the tablet's width, closed on all four sides - the same weight and brightness as image 3's outline
- the pictogram sits in the lower half of the tablet, on a slightly darker black circle
- the three states differ ONLY in the outline colour - the face stays black in all of them:
  * the top tablet = unlocked: bright cream-gold outline (#F0DFB4)
  * the two tablets on the right = available: warm bronze outline (#C9A97A)
  * the two tablets on the left and the bottom tablet = locked: cold grey outline (#8C8A86)
- no glow, no halo, no bloom, no light-source effect on any element

SCREEN MATERIAL
- the background is a flat near-black, about #101014 to #16161A, with faint grain only. No brush noise, no ornament, no pattern, no vignette, no texture.
- the bottom panel is a flat near-black field with a single thin cream hairline border (about #F2EDE0, 1-2 px) - like a hairline frame, no bevel, no metal edge, no gradient
- every rule, connector line and tab underline is that same thin cream hairline
- all typography is cream (#E8DCC4), clean and bold, with dimmed cream (#8C8A86) for inactive items
- the FULL-WIDTH unlock button is the only saturated element on the screen: a deep vermilion (#9A0101) flat fill with cream lettering. Nothing else on the screen is saturated.
- the whole screen must feel quiet, flat and legible - like our battle HUD, which is built from near-black shapes with cream hairlines

DO NOT
- do not keep any of image 1's English words or numbers; do not keep its data table
- do not render any text, letters, numbers or Chinese characters: leave every label area as clean empty space
- do not add any new icons or symbols beyond the small pictograms already inside the plates
- no characters, no soldiers, no battlefield scenery, no banners, no terrain
- no brass, no gold, no bronze, no battered metal, no heavy forged panels, no bevels, no rivets, no filigree, no carved ornament, no parchment scroll
- no brush strokes, no painted texture, no noise clusters, no mosaic filter
- no smooth 3D rendering, no chrome reflections, no lens flares, no neon, no cartoon shading, no checkerboard, no white background"""

PROMPT_ZH = """
【中文补充要求 —— 与上面英文同义，逐条对齐】
把【图 1】重绘成我们游戏里同一个界面的版本。

【三张图的职责】
· 图 1 = 要重绘的界面。**结构与功能原样保留**：顶部大标题 + 一条横贯细线、三个分类页签、
  节点树区、底部大详情面板（面板底边贴一个通栏按钮）。
· 图 2 = 只取**整屏的安静气质与字体**。只取：很暗很静的底、奶油色的字、克制的对比、
  大片留白。**不要**取它的笔刷笔触、描金边、朱红色块、绘画质感。
· 图 3 = **节点长什么样的唯一权威**。直接照抄那三块令牌的画法：平面近黑的牌面、
  一圈米白细描边、顶部一个素圈孔、硬边干净、**没有材质、没有明暗、没有纹理、没有倒角**。

【版式与功能 —— 照图 1 保留】
· 顶部：左上一个大标题，标题下一条横贯的细线，右上一个小徽记 + 数字位
· 标题下一行三个分类页签：**纯文字**——不要底板、不要牌子、不要金属、不要框。
  选中的那个是更亮的奶油色并带一条细下划线，另外两个更暗、没有下划线
· 节点树区：用细线连起来的节点牌，每块牌内一个图形，牌下方一行小标签和一行更小的价格
· 底部大详情面板：边框、左侧一个节点牌小缩略图、一个大标题、一行元信息、一段描述、
  一行状态、一行花费，以及**贴在面板底边的通栏大按钮**
· 整屏克制、留白、高对比的气质

【节点造型与材质 —— 这条最关键】
所有菱形节点牌，换成和【图 3】里那三块令牌一模一样的东西，只是更高一些、里面放上图形：
· 竖长比例，高约为宽的 1.55 倍
· 顶部圆拱冠；冠顶正中**一个**小圆形穿绳孔，画成一圈素米白圆环、透出后面的暗底，
  和图 3 完全一样
· 牌身直筒，底部圆收
· 牌面是**平面**近黑墨色（约 #1A1A1E），**绝对不要**渐变、反光、纹理、倒角
· 外圈是一道米白细描边（约 #F2EDE0），厚度约牌宽的 3%，四边闭合 ——
  粗细和亮度就和图 3 的描边一样
· 图形放在牌的下半部，一个稍深一点的黑色圆底上
· 三态**只改描边颜色**，牌面始终是黑的：
  - 最上面那块 = 已解锁：亮奶金描边（#F0DFB4）
  - 右侧两块 = 可解锁：暖铜描边（#C9A97A）
  - 左侧两块和最下面那块 = 锁定：冷灰描边（#8C8A86）
· 任何元素都**不要发光、不要光晕、不要辉光、不要光源特效**

【整屏材质】
· 背景是**平面**近黑（约 #101014 ~ #16161A），只允许极轻的颗粒。
  不要笔刷噪点、不要纹样、不要花纹、不要暗角、不要任何纹理
· 底部面板是**平面**近黑底 + 一圈米白细发丝边框（约 #F2EDE0，1–2px 的观感）——
  像一条发丝框线，不要倒角、不要金属边、不要渐变
· 所有分隔线、连线、页签下划线都是同一道米白细发丝线
· 所有文字是奶油色（#E8DCC4），干净有力的粗体；未选中项用更暗的奶油色（#8C8A86）
· 通栏的解锁按钮是**全屏唯一一处饱和色**：深朱红（#9A0101）平涂 + 奶油色字。
  除此之外屏幕上没有任何饱和色
· 整屏必须安静、平、易读 —— 像我们的战斗 HUD：近黑的块面 + 奶白的发丝线

【不要】
· 不要保留图 1 的任何英文词与数字；不要保留它的数据表
· 任何地方都不要画文字、字母、数字、汉字，所有标签位置保持干净的留白
· 不要新增任何图标或符号，牌内已有的小图形是占位，保持原样
· 不要人物、士兵、战场景物、旗幡、地形
· 不要黄铜、黄金、铜绿、残破金属、厚重锻铁板、倒角、铆钉、花纹、雕花、卷轴
· 不要笔刷笔触、绘画质感、噪点堆叠、马赛克滤镜
· 不要光滑 3D 渲染、镜面反射、镜头光晕、霓虹、卡通上色、棋盘格、白底
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


def fetch_with_retry(url, out_dir, dest, attempts=6, per_try=20):
    """图片 CDN 可能瞬时不可达：退避重试 + curl 兜底。失败返回 None（响应已落盘，可事后补下）。"""
    import subprocess
    last = None
    for i in range(attempts):
        try:
            with urllib.request.urlopen(url, timeout=per_try) as r:
                blob = r.read()
            if blob[:8] == b"\x89PNG\r\n\x1a\n":
                return blob
            last = "not a png"
        except Exception as exc:  # noqa: BLE001
            last = f"{type(exc).__name__}: {exc}"
        print(f"  download attempt {i + 1}/{attempts} failed: {last}")
        time.sleep(8)
    # curl 兜底（项目旧脚本用过 --noproxy "*"）
    try:
        tmp = os.path.join(out_dir, "_curl_dl.png")
        r = subprocess.run(
            ["curl", "--noproxy", "*", "--connect-timeout", "20", "--max-time", "120",
             "-sS", "-o", tmp, url],
            capture_output=True, text=True)
        if r.returncode == 0 and os.path.exists(tmp):
            blob = open(tmp, "rb").read()
            if blob[:8] == b"\x89PNG\r\n\x1a\n":
                return blob
    except Exception as exc:  # noqa: BLE001
        last = f"curl: {exc}"
    print("  curl fallback failed:", last)
    return None


def main():
    key = os.environ.get("MUSK_API_KEY")
    if not key:
        raise SystemExit("MUSK_API_KEY missing")
    os.makedirs(OUT_DIR, exist_ok=True)
    if os.path.exists(IMG_OUT):
        raise SystemExit(f"refusing to overwrite existing output: {IMG_OUT}")

    inputs = [
        (SRC_BASE, "blades_ref_input.png",
         "image 1: THE SCREEN TO REDRAW (Blades skill-page reference) -> structure + function verbatim"),
        (SRC_UI, "sanguo_combat_ui_input.png",
         "image 2: overall quietness + cream typography ONLY (our Three Kingdoms combat-interface concept). NOT brush, NOT gold edging, NOT vermilion."),
        (SRC_TOKEN, "hud_token_material_ref.png",
         "image 3: THE AUTHORITY on node rendering - our DEPLOYED battle-HUD tokens (flat #1A1A1E face, #F2EDE0 hairline outline, plain ring hole, no material)"),
    ]
    files = []
    for src, name, _role in inputs:
        dst = os.path.join(OUT_DIR, name)
        shutil.copyfile(src, dst)
        files.append(("image[]", name, dst, "image/png"))

    fields = [
        ("model", MODEL),
        ("prompt", PROMPT),
        ("size", "1024x1536"),
        ("quality", "max"),
        ("input_fidelity", "high"),
        ("output_format", "png"),
        ("n", "1"),
        # 图片 CDN (img.huaruics.com) 当前从本机不可达；要求直接回 base64 绕开它
        ("response_format", "b64_json"),
    ]
    body, boundary = build_multipart(fields, files)

    with open(os.path.join(OUT_DIR, "talent_ui_mock_v4.prompt.txt"), "w",
              encoding="utf-8", newline="\n") as fh:
        fh.write(PROMPT)

    req_meta = {
        "version": "talent_ui_mock_v4",
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
            {"order": i, "field": "image[]", "filename": files[i - 1][1],
             "path": files[i - 1][2], "role": inputs[i - 1][2],
             "sha256": sha256_file(files[i - 1][2]),
             "bytes": os.path.getsize(files[i - 1][2])}
            for i in range(1, len(inputs) + 1)
        ],
        "body_bytes": len(body),
        "automatic_retries": 0,
        "read_timeout_seconds": TIMEOUT,
        "submitted_at_local": time.strftime("%Y-%m-%d %H:%M:%S"),
    }
    req_meta["fields"]["response_format"] = "b64_json (workaround: image CDN unreachable from this host)"
    req_meta["fields"]["background"] = "omitted"
    with open(os.path.join(OUT_DIR, "talent_ui_mock_v4.request.json"), "w", encoding="utf-8") as fh:
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
        with open(os.path.join(OUT_DIR, "talent_ui_mock_v4.status.json"), "w", encoding="utf-8") as fh:
            json.dump(status, fh, indent=2, ensure_ascii=False)
        raise SystemExit(status["error"])

    status["seconds"] = round(time.time() - t0, 2)
    payload = json.loads(raw.decode("utf-8"))

    # 先落盘响应，再下载图片 —— 顺序反了会在下载失败时把 url 一起丢掉（2026-11-02 踩过）
    with open(os.path.join(OUT_DIR, "talent_ui_mock_v4.response.json"), "w", encoding="utf-8") as fh:
        json.dump(payload, fh, indent=2, ensure_ascii=False)

    item = payload["data"][0]
    b64 = item.get("b64_json")
    url = item.get("url")
    blob = None
    if b64:
        blob = base64.b64decode(b64)
        status["download"] = "b64_json"
    elif url:
        status["download"] = "url"
        status["image_url"] = url
        blob = fetch_with_retry(url, OUT_DIR, IMG_OUT, attempts=3, per_try=15)
        if blob is None:
            status["state"] = "response_saved_but_download_failed"
            status["error"] = "image CDN unreachable; url saved in response.json, retry download later"
            with open(os.path.join(OUT_DIR, "talent_ui_mock_v4.status.json"), "w", encoding="utf-8") as fh:
                json.dump(status, fh, indent=2, ensure_ascii=False)
            print(json.dumps(status, indent=2, ensure_ascii=False))
            raise SystemExit(status["error"])
    else:
        status["state"] = "failed"
        status["error"] = "no image payload in response"
        with open(os.path.join(OUT_DIR, "talent_ui_mock_v4.status.json"), "w", encoding="utf-8") as fh:
            json.dump(status, fh, indent=2, ensure_ascii=False)
        raise SystemExit(status["error"])

    with open(IMG_OUT, "wb") as fh:
        fh.write(blob)

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
    with open(os.path.join(OUT_DIR, "talent_ui_mock_v4.status.json"), "w", encoding="utf-8") as fh:
        json.dump(status, fh, indent=2, ensure_ascii=False)
    print(json.dumps(status, indent=2, ensure_ascii=False))


if __name__ == "__main__":
    main()
