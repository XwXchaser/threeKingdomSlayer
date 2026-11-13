"""v3 参考图合成（第 3 版）：坐标由读图确定（_tree_grid.png / _panel_grid.png），不再自动检测。

写出内容：
  顶部：标题「天赋」+ 右上数字槽 1280
  页签：三个中文页签，并把「亮块 + 下划线」从第 1 个挪到第 3 个（我们的默认页签是三选一解锁）
  树：6 块兵符，牌下两行写 名称 / 1 铜钱
  底部大面板：大标题 / 花费 / 状态 / 描述 / 元信息 / 前置 / 通栏按钮「解锁 1」
"""
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFont

SRC = r"C:\Users\steam\Pictures\gptGen\talent_ui_v3\talent_ui_mock_v3.png"
OUT_DIR = r"C:\Users\steam\Pictures\gptGen\talent_ui_v3"
OUT_NATIVE = os.path.join(OUT_DIR, "talent_ui_mock_v3_comp_1024x1536.png")
OUT_916 = os.path.join(OUT_DIR, "talent_ui_mock_v3_comp_1080x1920.png")

F_HEAVY = r"C:\threeKingdomSlayer\threeKingdomSlayer\Assets\Fonts\方正粗黑宋简体.ttf"
F_REG = "C:/Windows/Fonts/msyh.ttc"

CREAM = (234, 224, 200)
CREAM_DIM = (170, 160, 140)
GOLD = (232, 196, 124)
GREEN = (150, 206, 150)
COIN = (226, 190, 120)

TABS = ["基础数值", "招式解锁", "三选一解锁"]
ACTIVE_TAB = 2
TAB_BLOCKS = [(52, 131, 200, 174), (227, 131, 353, 166), (378, 131, 509, 166)]

# (名称, 状态, 名称条, 价格条)
LABELS = [
    ("旋风", "unlocked", (437, 368, 577, 394), (437, 404, 577, 428)),
    ("聚拢波", "locked", (210, 614, 357, 638), (210, 642, 357, 664)),
    ("回旋波", "available", (667, 614, 813, 638), (667, 642, 813, 664)),
    ("击退波", "locked", (210, 889, 357, 911), (210, 917, 357, 939)),
    ("箭矢齐射", "available", (667, 889, 813, 911), (667, 917, 813, 939)),
    ("海浪", "locked", (437, 1059, 577, 1081), (437, 1087, 577, 1109)),
]

PANEL_TITLE = (240, 1162, 930, 1205)
PANEL_COST = (240, 1207, 395, 1239)
PANEL_STATE = (415, 1207, 645, 1239)
PANEL_DESC = (240, 1242, 930, 1305)
PANEL_META = (240, 1307, 395, 1333)
PANEL_PRQ = (240, 1335, 395, 1361)
PANEL_BTN = (55, 1405, 960, 1458)
COIN_BOX = (898, 50, 967, 85)


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

    # 页签：压暗第 1 个、提亮第 3 个
    scale_box(a, TAB_BLOCKS[0], 0.5)
    scale_box(a, TAB_BLOCKS[ACTIVE_TAB], 1.55)
    img = Image.fromarray(np.clip(a, 0, 255).astype(np.uint8), "RGB")
    d = ImageDraw.Draw(img)

    # 顶部标题
    d.text((58, 28), "天赋", font=font(F_HEAVY, 50), fill=CREAM)
    ctext(d, COIN_BOX, "1280", font(F_HEAVY, 22), CREAM)

    # 页签文字 + 选中下划线（提亮块上用深色字，暗块上用奶油色字）
    f_tab = font(F_REG, 20)
    for i, t in enumerate(TABS):
        ctext(d, TAB_BLOCKS[i], t, f_tab,
              (34, 26, 16) if i == ACTIVE_TAB else CREAM_DIM)
    X0, Y0, X1, Y1 = TAB_BLOCKS[ACTIVE_TAB]
    d.rectangle([X0 + 10, Y1 + 5, X1 - 10, Y1 + 8], fill=GOLD)

    # 节点标签
    f_name = font(F_HEAVY, 21)
    f_price = font(F_REG, 16)
    for name, state, nb, pb in LABELS:
        ctext(d, nb, name, f_name, CREAM if state != "locked" else CREAM_DIM)
        ctext(d, pb, "1 铜钱", f_price, COIN if state != "locked" else (128, 120, 106))

    # 底部面板
    d.text((PANEL_TITLE[0] + 12, (PANEL_TITLE[1] + PANEL_TITLE[3]) // 2), "回旋波",
           font=font(F_HEAVY, 32), anchor="lm", fill=CREAM)
    ctext(d, PANEL_COST, "花费 1 铜钱", font(F_REG, 17), COIN)
    ctext(d, PANEL_STATE, "可以解锁", font(F_REG, 17), GREEN)
    d.text((PANEL_DESC[0] + 12, PANEL_DESC[1] + 10), "每 6 秒把周围敌人打飞一次",
           font=font(F_REG, 20), fill=CREAM_DIM)
    d.text((PANEL_DESC[0] + 12, PANEL_DESC[1] + 38), "持续伤害并把周围敌人击飞",
           font=font(F_REG, 18), fill=(138, 130, 116))
    ctext(d, PANEL_META, "定时被动", font(F_REG, 17), CREAM_DIM)
    ctext(d, PANEL_PRQ, "前置：箭矢齐射", font(F_REG, 17), CREAM_DIM)
    ctext(d, PANEL_BTN, "解锁 1", font(F_HEAVY, 28), (250, 240, 218))

    img.save(OUT_NATIVE)

    # 9:16 成品：等比放大到 1080x1620，上下各用背景中位色补 150px（不做边缘行拉伸，避免接缝）
    scaled = img.resize((1080, 1620), Image.LANCZOS)
    sa = np.asarray(scaled)
    margin = sa[200:1400, 0:45].reshape(-1, 3)
    band = tuple(int(v) for v in np.median(margin, axis=0))
    canvas = Image.new("RGB", (1080, 1920), band)
    canvas.paste(scaled, (0, 150))
    canvas.save(OUT_916)
    print("saved", OUT_NATIVE)
    print("saved", OUT_916)


if __name__ == "__main__":
    main()
