"""v5 参考图合成：把真实中文内容写回 AI 生成图（坐标由 _v5_grid.png 读出）。

v5 的占位条是平面色块/描边框，直接往里写字。
"""
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFont

SRC = r"C:\Users\steam\Pictures\gptGen\talent_ui_v5\talent_ui_mock_v5.png"
OUT_DIR = r"C:\Users\steam\Pictures\gptGen\talent_ui_v5"
OUT_NATIVE = os.path.join(OUT_DIR, "talent_ui_mock_v5_comp_1024x1536.png")
OUT_916 = os.path.join(OUT_DIR, "talent_ui_mock_v5_comp_1080x1920.png")

F_HEAVY = r"C:\threeKingdomSlayer\threeKingdomSlayer\Assets\Fonts\方正粗黑宋简体.ttf"
F_REG = "C:/Windows/Fonts/msyh.ttc"

CREAM = (236, 226, 202)
CREAM_DIM = (150, 143, 130)
GOLD = (226, 190, 120)
GREEN = (150, 206, 150)
BG = (16, 16, 20)

TABS = ["基础数值", "招式解锁", "三选一解锁"]
ACTIVE_TAB = 2
TAB_BLOCKS = [(50, 118, 160, 140), (185, 118, 310, 140), (335, 118, 455, 140)]

TITLE_BOX = (50, 30, 335, 65)
COIN_BOX = (895, 30, 975, 60)

LABELS = [
    ("旋风", "unlocked", (455, 320, 570, 338), (475, 345, 550, 360)),
    ("聚拢波", "locked", (295, 555, 405, 570), (310, 575, 385, 590)),
    ("回旋波", "available", (615, 555, 730, 570), (630, 575, 705, 590)),
    ("击退波", "locked", (295, 815, 405, 830), (310, 835, 385, 850)),
    ("箭矢齐射", "available", (615, 815, 730, 830), (630, 835, 705, 850)),
    ("海浪", "locked", (455, 1075, 570, 1090), (475, 1100, 550, 1115)),
]

PANEL_TITLE = (190, 1230, 385, 1265)
PANEL_A = (190, 1275, 260, 1295)
PANEL_B = (275, 1275, 380, 1295)
PANEL_C = (410, 1275, 505, 1295)
PANEL_DESC = (190, 1315, 970, 1345)
PANEL_D = (190, 1375, 340, 1395)
PANEL_E = (420, 1375, 500, 1395)
PANEL_BTN = (360, 1440, 655, 1465)


def font(path, size):
    try:
        return ImageFont.truetype(path, size)
    except Exception:
        return ImageFont.truetype(F_REG, size)


def scale_box(a, box, k):
    X0, Y0, X1, Y1 = box
    a[Y0:Y1, X0:X1] = np.clip(a[Y0:Y1, X0:X1] * k, 0, 255)


def ctext(d, box, text, f, fill, anchor="mm"):
    X0, Y0, X1, Y1 = box
    d.text(((X0 + X1) // 2, (Y0 + Y1) // 2), text, font=f, anchor=anchor, fill=fill)


def main():
    img = Image.open(SRC).convert("RGB")
    a = np.asarray(img).astype(np.int16)

    # 页签：压暗第 1 个、提亮第 3 个，并把下划线从第 1 个挪到第 3 个
    scale_box(a, TAB_BLOCKS[0], 0.42)
    scale_box(a, TAB_BLOCKS[1], 0.42)
    scale_box(a, TAB_BLOCKS[ACTIVE_TAB], 1.9)
    a[142:162, 44:166] = BG          # 抹掉第 1 个下面那条下划线
    img = Image.fromarray(np.clip(a, 0, 255).astype(np.uint8), "RGB")
    d = ImageDraw.Draw(img)

    # 顶部
    d.text((TITLE_BOX[0] + 6, (TITLE_BOX[1] + TITLE_BOX[3]) // 2), "天赋",
           font=font(F_HEAVY, 40), anchor="lm", fill=(44, 34, 20))
    ctext(d, COIN_BOX, "1280", font(F_HEAVY, 22), CREAM)

    # 页签文字
    f_tab = font(F_REG, 22)
    for i, t in enumerate(TABS):
        ctext(d, TAB_BLOCKS[i], t, f_tab,
              (36, 28, 16) if i == ACTIVE_TAB else CREAM_DIM)
    X0, Y0, X1, Y1 = TAB_BLOCKS[ACTIVE_TAB]
    d.rectangle([X0 + 6, Y1 + 8, X1 - 6, Y1 + 11], fill=CREAM)

    # 节点标签
    f_name = font(F_HEAVY, 21)
    f_price = font(F_REG, 16)
    for name, state, nb, pb in LABELS:
        ctext(d, nb, name, f_name, CREAM if state != "locked" else CREAM_DIM)
        ctext(d, pb, "1 铜钱", f_price, GOLD if state != "locked" else (120, 114, 102))

    # 底部面板
    d.text((PANEL_TITLE[0] + 12, (PANEL_TITLE[1] + PANEL_TITLE[3]) // 2), "回旋波",
           font=font(F_HEAVY, 28), anchor="lm", fill=(44, 34, 20))
    ctext(d, PANEL_A, "花费 1 铜钱", font(F_REG, 15), GOLD)
    ctext(d, PANEL_B, "可以解锁", font(F_REG, 15), GREEN)
    ctext(d, PANEL_C, "定时被动", font(F_REG, 15), CREAM_DIM)
    d.text((PANEL_DESC[0] + 14, PANEL_DESC[1] + 8), "每 6 秒把周围敌人打飞一次",
           font=font(F_REG, 18), fill=CREAM_DIM)
    ctext(d, PANEL_D, "前置：箭矢齐射", font(F_REG, 15), CREAM_DIM)
    ctext(d, PANEL_E, "主动技能", font(F_REG, 15), CREAM_DIM)
    ctext(d, PANEL_BTN, "解锁 1", font(F_HEAVY, 26), (44, 32, 18))

    img.save(OUT_NATIVE)

    scaled = img.resize((1080, 1620), Image.LANCZOS)
    sa = np.asarray(scaled)
    band = tuple(int(v) for v in np.median(sa[200:1400, 0:45].reshape(-1, 3), axis=0))
    canvas = Image.new("RGB", (1080, 1920), band)
    canvas.paste(scaled, (0, 150))
    canvas.save(OUT_916)

    print("saved", OUT_NATIVE)
    print("saved", OUT_916)


if __name__ == "__main__":
    main()
