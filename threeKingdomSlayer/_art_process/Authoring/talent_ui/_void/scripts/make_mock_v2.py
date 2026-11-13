"""天赋界面版式底图 —— v2：按刀锋参考图的材质语言重画。

与 v1 的差别（v1 被否：太简陋太复古）：
  · 兵符不再是「暗铜平涂」，改成参考图那种**暖奶油→蜜金渐层发光牌 + 细深棕描边**
  · 顶部穿绳孔变成**发光的亮点**（参考图里菱形顶点那个光点），连线接到这个光点上
  · 图标区是牌下半部的**暗色凹槽圆**（对应参考图节点中间的圆形镂空）
  · 底板改成纯近黑 + 细暖金发丝线（不再是大块暗铜底板）
  · 字体改成参考图的高对比奶油色，标题用衬线
  · 页签改成干净的细体 + 选中一条金下划线

输出（1024x1536，内容安全区 x in [96,928]）：
  talent_mock_base2_notext.png   喂给生图
  talent_mock_base2_text.png     给用户看版式
"""
import math
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = "C:/threeKingdomSlayer/threeKingdomSlayer"
OUT_DIR = os.path.join(ROOT, "Library/Locus/tmp/talent_ui")
os.makedirs(OUT_DIR, exist_ok=True)

W, H = 1024, 1536
CX = W // 2

BG        = (10, 10, 12)
PANEL     = (15, 15, 18)
GOLD_RULE = (146, 120, 78)
GOLD_TEXT = (214, 184, 122)
CREAM     = (234, 224, 200)
CREAM_DIM = (166, 155, 132)
STATE_PLATE = {
    #            描边            牌面边色       牌面高光      发光
    "locked":   ((52, 48, 46), (104, 99, 92), (146, 138, 124), False),
    "available": ((58, 40, 28), (196, 162, 112), (247, 228, 190), True),
    "unlocked": ((78, 54, 26), (224, 184, 122), (255, 242, 210), True),
}
RECESS     = (26, 22, 20)       # 图标凹槽

F_HEAVY = os.path.join(ROOT, "Assets/Fonts/方正粗黑宋简体.ttf")
F_SANS = "C:/Windows/Fonts/msyh.ttc"

TALLY_W, TALLY_H = 96, 150
HOLE_R = 7
CAP_H, BOT_R = 0.26, 0.09

NODES = [
    ("旋风", "unlocked", 0, 300, "Assets/Sprites/31Reward/icon/icon_31_cyclone.png"),
    ("聚拢波", "locked", -250, 560, None),
    ("回旋波", "available", 250, 560, "Assets/Sprites/31Reward/icon/icon_31_huixuanbiao.png"),
    ("击退波", "locked", -250, 820, None),
    ("箭矢齐射", "available", 250, 820, "Assets/Sprites/31Reward/icon/icon_31_tripleArrow.png"),
    ("海浪", "locked", 0, 1080, "Assets/Sprites/31Reward/icon/icon_31item_wave.png"),
]
LINKS = [("击退波", "聚拢波"), ("聚拢波", "旋风"), ("箭矢齐射", "回旋波"), ("回旋波", "旋风")]
STATE_CREAM = {"locked": (150, 142, 126), "available": CREAM, "unlocked": (246, 234, 206)}
STATE_GOLD = {"locked": (120, 108, 88), "available": GOLD_TEXT, "unlocked": (236, 208, 150)}

POS = {n[0]: (CX + n[2], n[3]) for n in NODES}
ICON = {n[0]: n[4] for n in NODES}
SELECTED = "回旋波"
TABS = ["基础数值", "招式解锁", "三选一解锁"]
ACTIVE_TAB = 2
TAB_X = [236, 512, 788]


def font(path, size):
    try:
        return ImageFont.truetype(path, size)
    except Exception:
        return ImageFont.truetype(F_SANS, size)


def half_w(t):
    return 0.5 + 0.5 * math.sqrt(max(0.0, 1.0 - (1.0 - t) ** 2))


def silhouette(cx, cy, w, h, inset=0.0, top_lift=0.0, bot_lift=0.0):
    hw = w * 0.5
    cap, bot = h * CAP_H, h * BOT_R
    top = cy - h * 0.5 + top_lift
    shoulder = top + cap
    bot_y = cy + h * 0.5 - bot_lift
    bottom_start = bot_y - bot
    right = []
    for i in range(25):
        t = i / 24
        right.append((cx + max(0.0, hw * half_w(t) - inset), top + cap * t))
    right.append((cx + hw - inset, bottom_start))
    for i in range(1, 25):
        a = (i / 24) * (math.pi * 0.5)
        right.append((cx + (hw - inset) * math.cos(a), bottom_start + bot * math.sin(a)))
    pts = list(right)
    pts.extend([(2 * cx - x, y) for (x, y) in reversed(right[:-1])])
    return pts


def draw_plate(img, cx, cy, w, h, icon_img=None, glyph=None, glyph_font=None,
               glyph_color=(240, 228, 200), glow=True, state="available"):
    """参考图风格的兵符牌：深棕描边 + 暖金渐层 + 顶部发光孔 + 下半暗色凹槽。"""
    plate_edge, plate_in, plate_core, glow_on = STATE_PLATE[state]
    glow = glow and glow_on
    pts = silhouette(cx, cy, w, h)

    # --- 牌体（带径向渐层）---
    mask = Image.new("L", (W, H), 0)
    ImageDraw.Draw(mask).polygon(pts, fill=255)
    m = np.asarray(mask).astype(np.float32) / 255.0
    hole_cy = cy - h * 0.5 + h * 0.125
    yy, xx = np.mgrid[0:H, 0:W]
    d = np.sqrt(((xx - cx) / (w * 0.62)) ** 2 + ((yy - (hole_cy + h * 0.18)) / (h * 0.70)) ** 2)
    t = np.clip(1.0 - d, 0.0, 1.0) ** 1.15
    rgb = (np.array(plate_in, np.float32)[None, None, :] * (1 - t[..., None])
           + np.array(plate_core, np.float32)[None, None, :] * t[..., None])
    base = np.asarray(img).astype(np.float32)
    a3 = m[..., None]
    base = base * (1 - a3) + rgb * a3
    img = Image.fromarray(np.clip(base, 0, 255).astype(np.uint8), "RGB")
    d = ImageDraw.Draw(img)

    # --- 外描边（深棕）---
    edge_mask = Image.new("L", (W, H), 0)
    ImageDraw.Draw(edge_mask).polygon(pts, fill=255)
    inner_mask = Image.new("L", (W, H), 0)
    ImageDraw.Draw(inner_mask).polygon(silhouette(cx, cy, w, h, inset=2.5, top_lift=2.5, bot_lift=2.5), fill=255)
    rim = np.clip(np.asarray(edge_mask, np.float32) / 255.0
                  - np.asarray(inner_mask, np.float32) / 255.0, 0, 1)[..., None]
    base = np.asarray(img).astype(np.float32)
    base = base * (1 - rim) + np.array(plate_edge, np.float32)[None, None, :] * rim
    img = Image.fromarray(np.clip(base, 0, 255).astype(np.uint8), "RGB")
    d = ImageDraw.Draw(img)

    # --- 顶部发光孔（参考图里节点顶点的光点）---
    if glow:
        halo = Image.new("RGB", (W, H), (0, 0, 0))
        ImageDraw.Draw(halo).ellipse([cx - 26, hole_cy - 26, cx + 26, hole_cy + 26], fill=(120, 96, 58))
        halo = halo.filter(ImageFilter.GaussianBlur(14))
        base = np.asarray(img).astype(np.float32) + np.asarray(halo).astype(np.float32)
        img = Image.fromarray(np.clip(base, 0, 255).astype(np.uint8), "RGB")
        d = ImageDraw.Draw(img)
    d.ellipse([cx - HOLE_R - 1.5, hole_cy - HOLE_R - 1.5, cx + HOLE_R + 1.5, hole_cy + HOLE_R + 1.5],
              fill=(40, 28, 20))
    d.ellipse([cx - HOLE_R, hole_cy - HOLE_R, cx + HOLE_R, hole_cy + HOLE_R], fill=(255, 246, 226))

    # --- 下半暗色凹槽（放图标）---
    rc_y = cy + h * 0.20
    rr = w * 0.30
    d.ellipse([cx - rr, rc_y - rr, cx + rr, rc_y + rr], fill=RECESS)
    d.ellipse([cx - rr, rc_y - rr, cx + rr, rc_y + rr], outline=(150, 124, 84), width=1)
    if icon_img is not None:
        s = min((rr * 1.5) / icon_img.width, (rr * 1.5) / icon_img.height)
        nw, nh = max(1, int(icon_img.width * s)), max(1, int(icon_img.height * s))
        im = icon_img.resize((nw, nh), Image.LANCZOS)
        img.paste(im, (int(cx - nw / 2), int(rc_y - nh / 2)), im)
    elif glyph:
        gf = glyph_font or font(30, True)
        bb = d.textbbox((0, 0), glyph, font=gf)
        d.text((cx - (bb[2] - bb[0]) / 2 - bb[0], rc_y - (bb[3] - bb[1]) / 2 - bb[1]),
               glyph, font=gf, fill=glyph_color)
    return img


def load_icon(rel):
    if not rel:
        return None
    p = os.path.join(ROOT, rel)
    return Image.open(p).convert("RGBA") if os.path.exists(p) else None


def build(with_text=True):
    img = Image.new("RGB", (W, H), BG)
    d = ImageDraw.Draw(img)

    # 顶部：标题 + 细金线 + 铜钱
    d.text((96, 40), "天赋", font=font(F_HEAVY, 50), fill=CREAM)
    d.rectangle([96, 116, 928, 117], fill=GOLD_RULE)
    d.ellipse([812, 48, 856, 92], outline=(206, 172, 108), width=3)
    d.rectangle([826, 62, 842, 78], fill=(206, 172, 108))
    if with_text:
        d.text((868, 54), "1280", font=font(F_SANS, 26), fill=CREAM)
    else:
        d.rounded_rectangle([866, 56, 934, 84], radius=3, fill=(38, 36, 34))

    # 页签
    for i, t in enumerate(TABS):
        f = font(F_SANS, 30)
        bb = d.textbbox((0, 0), t, font=f)
        d.text((TAB_X[i] - (bb[2] - bb[0]) / 2 - bb[0], 146), t, font=f,
               fill=CREAM if i == ACTIVE_TAB else CREAM_DIM)
        if i == ACTIVE_TAB:
            d.rectangle([TAB_X[i] - 78, 196, TAB_X[i] + 78, 198], fill=(198, 166, 110))

    # 连线：从下方牌的顶部光点连到上方牌的底缘
    hole_off = TALLY_H * 0.5 - TALLY_H * 0.125
    for a, b in LINKS:
        x1, y1 = POS[a]
        x2, y2 = POS[b]
        d.line([x1, y1 - hole_off, x2, y2 + TALLY_H * 0.5], fill=(150, 126, 92), width=2)

    # 节点
    for name, state, _ox, _oy, iconpath in NODES:
        nx, ny = POS[name]
        icon = load_icon(iconpath)
        img = draw_plate(img, nx, ny, TALLY_W, TALLY_H, icon_img=icon,
                         glyph=(None if icon is not None else name[0]),
                         glyph_font=font(F_HEAVY, 32),
                         glyph_color=(236, 224, 196), state=state)
        d = ImageDraw.Draw(img)
        # 名称（框下）
        ty = ny + TALLY_H // 2 + 16
        if with_text:
            f = font(F_SANS, 26)
            bb = d.textbbox((0, 0), name, font=f)
            d.text((nx - (bb[2] - bb[0]) / 2 - bb[0], ty), name, font=f, fill=STATE_CREAM[state])
        else:
            d.rounded_rectangle([nx - 48, ty + 6, nx + 48, ty + 24], radius=3, fill=(34, 32, 30))
        # 价格（名称下）
        py = ty + 34
        if with_text:
            f = font(F_SANS, 20)
            s = "1 铜钱"
            bb = d.textbbox((0, 0), s, font=f)
            d.text((nx - (bb[2] - bb[0]) / 2 - bb[0], py), s, font=f, fill=STATE_GOLD[state])
        else:
            d.rounded_rectangle([nx - 30, py + 6, nx + 30, py + 22], radius=3, fill=(28, 27, 26))

    # 底部详情面板（细金线框）
    PT, PB = 1240, 1500
    d.rectangle([96, PT, 928, PB], fill=PANEL, outline=GOLD_RULE, width=2)
    # 左侧缩略牌
    img = draw_plate(img, 148, PT + 112, 62, 96, icon_img=load_icon(ICON[SELECTED]),
                     glow=False, state="available")
    d = ImageDraw.Draw(img)
    if with_text:
        d.text((206, PT + 22), "回旋波", font=font(F_HEAVY, 38), fill=CREAM)
        d.text((206, PT + 80), "定时被动 · 每 6 秒把周围敌人打飞一次", font=font(F_SANS, 22), fill=CREAM_DIM)
        d.text((206, PT + 128), "可以解锁", font=font(F_SANS, 22), fill=(150, 200, 150))
        d.text((420, PT + 128), "花费 1 铜钱", font=font(F_SANS, 22), fill=GOLD_TEXT)
    else:
        d.rounded_rectangle([206, PT + 24, 330, PT + 62], radius=3, fill=(38, 36, 34))
        d.rounded_rectangle([206, PT + 84, 660, PT + 104], radius=3, fill=(30, 29, 28))
        d.rounded_rectangle([206, PT + 132, 320, PT + 152], radius=3, fill=(28, 32, 28))
        d.rounded_rectangle([420, PT + 132, 560, PT + 152], radius=3, fill=(36, 32, 26))
    # 解锁按钮（细金线框）
    d.rectangle([700, PT + 78, 900, PT + 168], fill=(24, 22, 22), outline=(198, 166, 110), width=2)
    if with_text:
        f = font(F_HEAVY, 28)
        bb = d.textbbox((0, 0), "解锁 1", font=f)
        d.text((800 - (bb[2] - bb[0]) / 2 - bb[0], PT + 104), "解锁 1", font=f, fill=CREAM)
    else:
        d.rounded_rectangle([748, PT + 110, 852, PT + 134], radius=3, fill=(40, 37, 34))

    return img


if __name__ == "__main__":
    for wt, name in ((True, "talent_mock_base2_text.png"), (False, "talent_mock_base2_notext.png")):
        im = build(wt)
        p = os.path.join(OUT_DIR, name)
        im.save(p)
        print("saved", p, im.size)
