"""天赋界面「版式底图」生成器（本地草稿，不调用任何付费接口）。

用途：
  1) 给用户看真实版式 + 兵符剪影 + 文字位置的草图；
  2) 作为 GPT Image 图生图的底图，保证参考图版式与实际实现一致。

数据源（真实资产）：
  Assets/ScriptableObjects/Talents/TalentTree_*.asset 的 treePosition / 前置关系
  Assets/Sprites/31Reward/icon/ 与 Assets/Sprites/BatlleHUD/ 的真实图标

输出（1024x1536，内容安全区 x in [96,928]，便于后续裁成 9:16）：
  talent_mock_base_text.png    带真实中文标签（给人看的草图）
  talent_mock_base_notext.png  标签换成占位横条（喂给图生图，避免 AI 画乱码字）
"""

import math
import os
from PIL import Image, ImageDraw, ImageFont

ROOT = "C:/threeKingdomSlayer/threeKingdomSlayer"
OUT_DIR = os.path.join(ROOT, "Library/Locus/tmp/talent_ui")
os.makedirs(OUT_DIR, exist_ok=True)

W, H = 1024, 1536
CX = W // 2

# ---- 配色 ----
BG        = (19, 18, 22)
CARD      = (27, 25, 29)
BODY      = (30, 29, 32)      # 穿绳孔透出的底色
HAIRLINE  = (92, 79, 54)
BRASS     = (216, 178, 112)
BRASS_HI  = (234, 202, 146)
TEXT      = (226, 218, 202)
TEXT_DIM  = (148, 140, 126)
GREEN     = (126, 206, 126)
LINK      = (104, 88, 56)

# 兵符三态（草稿用中性色，金属质感交给 AI 定）
TALLY_LOCK   = ((40, 38, 40), (104, 100, 94))
TALLY_AVAIL  = ((74, 58, 38), (188, 152, 98))
TALLY_UNLOCK = ((110, 86, 48), (240, 206, 148))
STATE_COLORS = {"locked": TALLY_LOCK, "available": TALLY_AVAIL, "unlocked": TALLY_UNLOCK}

FONT      = "C:/Windows/Fonts/msyh.ttc"
FONT_BOLD = "C:/Windows/Fonts/msyhbd.ttc"


def font(size, bold=False):
    return ImageFont.truetype(FONT_BOLD if bold else FONT, size)


# ---- 兵符造型（照 Assets/Sprites/UI/Combat/hud_slot_token.png 实测比例）----
# 实测 124x192：顶冠高约 25%、冠宽约半宽、直筒身、底部 8% 大圆收、穿绳孔在顶部 12.5%
TALLY_W, TALLY_H = 104, 160
HOLE_D = 16
CAP_H = 0.26
BOT_R = 0.09


def _half_width(t):
    """t: 0=顶冠, 1=肩部。返回半宽比例 0.5..1。"""
    return 0.5 + 0.5 * math.sqrt(max(0.0, 1.0 - (1.0 - t) ** 2))


def tally_silhouette(cx, cy, w, h, inset=0.0, top_lift=0.0, bot_lift=0.0):
    """兵符轮廓多边形（PIL 坐标，y 向下）。inset=左右内缩，top_lift/bot_lift=上下内缩。"""
    hw = w * 0.5
    cap = h * CAP_H
    bot = h * BOT_R
    top = cy - h * 0.5 + top_lift
    shoulder = top + cap
    bot_y = cy + h * 0.5 - bot_lift
    bottom_start = bot_y - bot
    steps = 24
    right = []
    for i in range(steps + 1):                       # 顶冠：窄冠 -> 肩部
        t = i / steps
        r = hw * _half_width(t) - inset
        right.append((cx + max(0.0, r), top + cap * t))
    right.append((cx + hw - inset, bottom_start))    # 直筒身
    for i in range(1, steps + 1):                     # 底部圆收：肩部 -> 底中
        a = (i / steps) * (math.pi * 0.5)
        right.append((cx + (hw - inset) * math.cos(a), bottom_start + bot * math.sin(a)))
    pts = list(right)
    pts.extend([(2 * cx - x, y) for (x, y) in reversed(right[:-1])])
    return pts


def draw_tally(draw, cx, cy, w, h, body, edge, hole_bg, ow=4, hole_d=HOLE_D,
               glyph=None, glyph_font=None, glyph_color=TEXT):
    """画一个兵符：外描边 + 内身 + 顶部穿绳孔 + 可选刻字。"""
    draw.polygon(tally_silhouette(cx, cy, w, h), fill=edge)
    draw.polygon(tally_silhouette(cx, cy, w, h, inset=ow, top_lift=ow, bot_lift=ow), fill=body)
    hole_cy = cy - h * 0.5 + h * 0.125               # 孔心在顶部 12.5%
    r = hole_d * 0.5
    draw.ellipse([cx - r - 2, hole_cy - r - 2, cx + r + 2, hole_cy + r + 2], fill=edge)
    draw.ellipse([cx - r, hole_cy - r, cx + r, hole_cy + r], fill=hole_bg)
    if glyph:
        gf = glyph_font or font(34, True)
        bb = draw.textbbox((0, 0), glyph, font=gf)
        draw.text((cx - (bb[2] - bb[0]) / 2 - bb[0], cy + h * 0.14 - (bb[3] - bb[1]) / 2 - bb[1]),
                  glyph, font=gf, fill=glyph_color)


def load_icon(rel):
    if not rel:
        return None
    path = os.path.join(ROOT, rel)
    if not os.path.exists(path):
        return None
    return Image.open(path).convert("RGBA")


def paste_icon(img, icon, cx, cy, box):
    if icon is None:
        return
    bw, bh = box
    s = min(bw / icon.width, bh / icon.height)
    nw, nh = max(1, int(icon.width * s)), max(1, int(icon.height * s))
    im = icon.resize((nw, nh), Image.LANCZOS)
    img.paste(im, (int(cx - nw / 2), int(cy - nh / 2)), im)


# ---- 真实数据（三选一解锁 = 第 3 个页签，本稿展示它）----
# (id, 显示名, 相对中心的 x, 画布 y, 图标路径或None, 状态)
NODES = [
    ("talent_pool_wave",        "海浪",      0,  1080, "Assets/Sprites/31Reward/icon/icon_31item_wave.png",   "locked"),
    ("talent_pool_push",        "击退波", -250,   820, None,                                                "locked"),
    ("talent_pool_arrowvolley", "箭矢齐射", 250,   820, "Assets/Sprites/31Reward/icon/icon_31_tripleArrow.png", "available"),
    ("talent_pool_convergence", "聚拢波", -250,   560, None,                                                "locked"),
    ("talent_pool_return",      "回旋波",   250,   560, "Assets/Sprites/31Reward/icon/icon_31_huixuanbiao.png", "available"),
    ("talent_pool_cyclone",     "旋风",      0,   300, "Assets/Sprites/31Reward/icon/icon_31_cyclone.png",     "unlocked"),
]
LINKS = [("talent_pool_push", "talent_pool_convergence"),
         ("talent_pool_convergence", "talent_pool_cyclone"),
         ("talent_pool_arrowvolley", "talent_pool_return"),
         ("talent_pool_return", "talent_pool_cyclone")]

SELECTED = "talent_pool_return"
TABS = ["基础数值", "招式解锁", "三选一解锁"]
ACTIVE_TAB = 2

POS = {n[0]: (CX + n[2], n[3]) for n in NODES}
NAME = {n[0]: n[1] for n in NODES}
ICON = {n[0]: n[4] for n in NODES}

CARD_TOP, CARD_BOT = 1270, 1500


def build(with_text=True):
    img = Image.new("RGB", (W, H), BG)
    d = ImageDraw.Draw(img)

    # 顶部标题 + 铜钱
    if with_text:
        d.text((100, 40), "天赋", font=font(58, True), fill=BRASS_HI)
    else:
        d.rounded_rectangle([100, 44, 236, 92], radius=5, fill=(52, 48, 42))
    d.ellipse([820, 46, 872, 98], outline=BRASS, width=4)
    d.rectangle([838, 64, 854, 80], fill=BRASS)
    if with_text:
        d.text((888, 52), "1280", font=font(34, True), fill=TEXT)
    else:
        d.rounded_rectangle([888, 56, 962, 88], radius=4, fill=(50, 47, 42))

    # 页签行（纯文字 + 选中下划线，照刀锋做法）
    tab_w, gap = 236, 40
    x0 = (W - (tab_w * 3 + gap * 2)) // 2
    for i, t in enumerate(TABS):
        tx = x0 + i * (tab_w + gap) + tab_w // 2
        if with_text:
            f = font(38, True)
            bb = d.textbbox((0, 0), t, font=f)
            d.text((tx - (bb[2] - bb[0]) / 2 - bb[0], 124), t, font=f,
                   fill=BRASS_HI if i == ACTIVE_TAB else TEXT_DIM)
        else:
            f = font(38, True)
            bb = d.textbbox((0, 0), t, font=f)
            wpx = bb[2] - bb[0]
            d.rounded_rectangle([tx - wpx / 2, 132, tx + wpx / 2, 164], radius=4,
                                fill=(58, 54, 48) if i == ACTIVE_TAB else (40, 38, 42))
        if i == ACTIVE_TAB:
            d.rectangle([tx - tab_w // 2 + 14, 180, tx + tab_w // 2 - 14, 184], fill=BRASS)

    # 连线（先画，节点覆盖）
    for a, b in LINKS:
        x1, y1 = POS[a]
        x2, y2 = POS[b]
        d.line([x1, y1, x2, y2], fill=LINK, width=5)

    # 节点
    for nid, name, _ox, _oy, iconpath, state in NODES:
        nx, ny = POS[nid]
        body, edge = STATE_COLORS[state]
        sel = nid == SELECTED
        if sel:
            edge = BRASS_HI
        ow = 5 if sel else 4
        icon = load_icon(iconpath)
        draw_tally(d, nx, ny, TALLY_W, TALLY_H, body, edge, BODY, ow=ow,
                   glyph=(None if icon is not None else name[0]),
                   glyph_font=font(40, True),
                   glyph_color=BRASS_HI if sel else TEXT_DIM)
        if icon is not None:
            paste_icon(img, icon, nx, ny + TALLY_H * 0.17, (66, 66))
        # 名称（框下方）
        ty = ny + TALLY_H // 2 + 24
        if with_text:
            f = font(30)
            bb = d.textbbox((0, 0), name, font=f)
            d.text((nx - (bb[2] - bb[0]) / 2 - bb[0], ty), name, font=f,
                   fill=TEXT if state != "locked" else TEXT_DIM)
        else:
            bw = 78 if len(name) <= 3 else 104
            d.rounded_rectangle([nx - bw // 2, ty + 4, nx + bw // 2, ty + 24], radius=4, fill=(48, 46, 50))
        # 价格（名称下方）
        py = ty + 36
        if with_text:
            f = font(24)
            s = "1 铜钱"
            bb = d.textbbox((0, 0), s, font=f)
            d.text((nx - (bb[2] - bb[0]) / 2 - bb[0], py), s, font=f,
                   fill=BRASS if state != "locked" else (108, 102, 92))
        else:
            d.rounded_rectangle([nx - 34, py + 4, nx + 34, py + 20], radius=3, fill=(42, 40, 44))

    # 底部详情卡
    d.rounded_rectangle([96, CARD_TOP, 928, CARD_BOT], radius=18, fill=CARD, outline=HAIRLINE, width=3)
    draw_tally(d, 148, CARD_TOP + 108, 70, 108, TALLY_AVAIL[0], BRASS_HI, CARD, ow=3)
    icon = load_icon(ICON[SELECTED])
    if icon is not None:
        paste_icon(img, icon, 148, CARD_TOP + 108 + 15, (40, 40))
    if with_text:
        d.text((206, CARD_TOP + 14), NAME[SELECTED], font=font(40, True), fill=BRASS_HI)
        d.text((206, CARD_TOP + 70), "定时被动 · 每 6 秒把周围敌人打飞一次", font=font(25), fill=TEXT_DIM)
        d.text((206, CARD_TOP + 118), "可以解锁", font=font(25), fill=GREEN)
        d.text((420, CARD_TOP + 118), "花费 1 铜钱", font=font(25), fill=BRASS)
    else:
        d.rounded_rectangle([206, CARD_TOP + 16, 340, CARD_TOP + 56], radius=5, fill=(60, 54, 42))
        d.rounded_rectangle([206, CARD_TOP + 74, 690, CARD_TOP + 100], radius=4, fill=(46, 44, 48))
        d.rounded_rectangle([206, CARD_TOP + 122, 330, CARD_TOP + 146], radius=4, fill=(40, 48, 40))
        d.rounded_rectangle([420, CARD_TOP + 122, 570, CARD_TOP + 146], radius=4, fill=(48, 44, 36))
    # 解锁按钮
    bx0, by0, bx1, by1 = 700, CARD_TOP + 62, 900, CARD_TOP + 142
    d.rounded_rectangle([bx0, by0, bx1, by1], radius=10, fill=(88, 68, 34), outline=BRASS, width=3)
    if with_text:
        f = font(34, True)
        bb = d.textbbox((0, 0), "解锁 1", font=f)
        d.text(((bx0 + bx1) / 2 - (bb[2] - bb[0]) / 2 - bb[0], by0 + 22), "解锁 1", font=f, fill=(240, 228, 204))
    else:
        d.rounded_rectangle([bx0 + 48, by0 + 26, bx1 - 48, by0 + 52], radius=4, fill=(96, 84, 56))

    return img


if __name__ == "__main__":
    for with_text, name in ((True, "talent_mock_base_text.png"), (False, "talent_mock_base_notext.png")):
        im = build(with_text)
        p = os.path.join(OUT_DIR, name)
        im.save(p)
        print("saved", p, im.size)
