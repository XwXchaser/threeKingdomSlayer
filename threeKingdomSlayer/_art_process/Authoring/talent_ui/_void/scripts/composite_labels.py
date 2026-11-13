"""把真实中文标签 / 铜钱数合成回 AI 生成的天赋界面参考图（第 2 版：定向窗口检测）。

输入：C:/Users/steam/Pictures/gptGen/talent_ui_v1/talent_ui_mock_v1.png (1024x1536)
输出：
  talent_ui_mock_v1_comp_1024x1536.png   原生尺寸合成版
  talent_ui_mock_v1_comp_1080x1920.png   9:16 成品（上下背景延展，不裁切不拉伸）

要点：
  · 占位条用「窗口内逐行游程 -> 连续行分带」定向找，不做全图连通域（相邻条会被并到一起）
  · 抹除时取四周背景像素随机回填（近黑底，肉眼无缝）
  · 铜牌 / 页签 / 按钮是 AI 画出来的实体，直接在上面写字，不抹
"""
import os
import random

import numpy as np
from PIL import Image, ImageDraw, ImageFont

SRC = r"C:\Users\steam\Pictures\gptGen\talent_ui_v1\talent_ui_mock_v1.png"
OUT_DIR = r"C:\Users\steam\Pictures\gptGen\talent_ui_v1"
OUT_NATIVE = os.path.join(OUT_DIR, "talent_ui_mock_v1_comp_1024x1536.png")
OUT_916 = os.path.join(OUT_DIR, "talent_ui_mock_v1_comp_1080x1920.png")

F_HEAVY = r"C:\threeKingdomSlayer\threeKingdomSlayer\Assets\Fonts\方正粗黑宋简体.ttf"
F_REG = "C:/Windows/Fonts/msyh.ttc"

BRASS = (216, 178, 112)
BRASS_HI = (240, 210, 158)
TEXT = (228, 220, 204)
TEXT_DIM = (150, 142, 128)

# 真实节点数据：名称 / 状态 / 价格 / 底图 x / 底图 y
NODES = [
    ("旋风", "unlocked", "1 铜钱", 512, 300),
    ("聚拢波", "locked", "1 铜钱", 262, 560),
    ("回旋波", "available", "1 铜钱", 762, 560),
    ("击退波", "locked", "1 铜钱", 262, 820),
    ("箭矢齐射", "available", "1 铜钱", 762, 820),
    ("海浪", "locked", "1 铜钱", 512, 1080),
]
TABS = ["基础数值", "招式解锁", "三选一解锁"]
ACTIVE_TAB = 2
TAB_CENTERS = [236, 512, 788]


def font(path, size):
    try:
        return ImageFont.truetype(path, size)
    except Exception:
        return ImageFont.truetype(F_REG, size)


def bands(mask, x0, x1, y0, y1, min_w=24, max_h=44, min_area=260, min_run=0):
    """在窗口里找「扁平的实心横条」：逐行取最长游程 -> 连续行合并成带。

    min_run > 0 时只认长度 >= min_run 的游程（用来把细连线排除掉）。
    """
    sub = mask[y0:y1, x0:x1]
    out = []
    cur = None
    for y in range(sub.shape[0]):
        line = sub[y]
        sx = ex = -1
        if line.any():
            idx = np.flatnonzero(line)
            br = np.flatnonzero(np.diff(idx) > 1)
            starts = np.concatenate(([idx[0]], idx[br + 1]))
            ends = np.concatenate((idx[br], [idx[-1]]))
            best = max(zip(starts, ends), key=lambda se: se[1] - se[0])
            if best[1] - best[0] + 1 >= max(1, min_run):
                sx, ex = int(best[0]), int(best[1]) + 1
        if sx < 0:
            if cur is not None:
                out.append(cur)
                cur = None
            continue
        if cur is not None and y - cur["y1"] <= 3 and sx <= cur["x1"] + 3 and ex >= cur["x0"] - 3:
            cur["y1"] = y + 1
            cur["x0"] = min(cur["x0"], sx)
            cur["x1"] = max(cur["x1"], ex)
        else:
            if cur is not None:
                out.append(cur)
            cur = {"y0": y, "y1": y + 1, "x0": sx, "x1": ex}
    if cur is not None:
        out.append(cur)

    res = []
    for b in out:
        x0b, y0b = b["x0"] + x0, b["y0"] + y0
        x1b, y1b = b["x1"] + x0, b["y1"] + y0
        w, h = x1b - x0b, y1b - y0b
        if w < min_w or h > max_h or w * h < min_area:
            continue
        res.append((x0b, y0b, x1b, y1b))
    return res


def plaque_bottom(fg, bx, by, half=45, span=170):
    """用「中心竖条被填满的比例」量出牌体下缘（连线只有几像素，占比很低，会被排除）。"""
    x0, x1 = max(0, bx - half), min(fg.shape[1], bx + half)
    y0, y1 = max(0, by - span), min(fg.shape[0], by + span)
    frac = fg[y0:y1, x0:x1].mean(axis=1)
    rows = frac > 0.45
    start = None
    for i, v in enumerate(rows):
        if v and start is None:
            start = i
        elif not v and start is not None:
            if i - start > 40:          # 太短的是图标/连线碎片
                return y0 + i - 1
            start = None
    if start is not None:
        return y0 + len(rows) - 1
    return by + 80


def erase_with_ring(img, bbox, rng, pad=3, grow=0):
    """用四周背景像素的中位色回填（近黑底，填平无痕）。"""
    x0, y0, x1, y1 = bbox
    x0 = max(0, x0 - grow); y0 = max(0, y0 - grow)
    x1 = min(img.width, x1 + grow); y1 = min(img.height, y1 + grow)
    px = img.load()
    ring = []
    for yy in range(max(0, y0 - pad), min(img.height, y1 + pad)):
        for xx in range(max(0, x0 - pad), min(img.width, x1 + pad)):
            if xx < x0 or xx >= x1 or yy < y0 or yy >= y1:
                ring.append(px[xx, yy])
    if not ring:
        return
    cols = sorted(c[ch] for c in ring for ch in ())
    med = tuple(sorted(int(c[ch]) for c in ring)[len(ring) // 2] for ch in range(3))
    for yy in range(y0, y1):
        for xx in range(x0, x1):
            px[xx, yy] = med


def main():
    rng = random.Random(20261102)
    img = Image.open(SRC).convert("RGB")
    d = ImageDraw.Draw(img)
    a = np.asarray(img).astype(np.int16)

    mx = a.max(axis=2)
    mn = a.min(axis=2)
    neutral = (mx - mn <= 18) & (mx >= 26) & (mx <= 100)
    # 标签条允许略带色偏（AI 可能给价格条加了铜色），只要比背景亮就算
    label_mask = (mx >= 24) & ((mx - mn) <= 45)
    fg = mx >= 24
    brass = (a[:, :, 0] > 95) & ((a[:, :, 0] - a[:, :, 2]) > 34)

    # ---------- 1. 标题铜牌 / 铜钱数槽 ----------
    reg = np.zeros_like(neutral); reg[0:120, 0:420] = True
    ys, xs = np.where(brass & reg)
    if len(xs):
        d.text(((xs.min() + xs.max()) // 2, (ys.min() + ys.max()) // 2), "天赋",
               font=font(F_HEAVY, 44), fill=BRASS_HI, anchor="mm")
    reg = np.zeros_like(neutral); reg[0:120, 760:1024] = True
    ys, xs = np.where(brass & reg)
    if len(xs):
        xs2, ys2 = xs[xs > 880], ys[xs > 880]
        if len(xs2):
            d.text(((xs2.min() + xs2.max()) // 2, (ys2.min() + ys2.max()) // 2), "1280",
                   font=font(F_HEAVY, 28), fill=TEXT, anchor="mm")

    # ---------- 2. 三个页签 ----------
    f_tab = font(F_HEAVY, 34)
    for i, cx in enumerate(TAB_CENTERS):
        x0, x1 = max(0, cx - 150), min(1024, cx + 150)
        reg = np.zeros_like(neutral); reg[104:196, x0:x1] = True
        ys, xs = np.where((neutral | brass) & reg)
        if not len(xs):
            continue
        d.text(((xs.min() + xs.max()) // 2, (ys.min() + ys.max()) // 2), TABS[i],
               font=f_tab, anchor="mm", fill=BRASS_HI if i == ACTIVE_TAB else TEXT_DIM)

    # ---------- 3. 节点下的名称条 / 价格条 ----------
    f_name = font(F_HEAVY, 30)
    f_price = font(F_REG, 23)
    report = []
    for name, state, price, bx, by in NODES:
        wx0, wx1 = max(0, bx - 120), min(1024, bx + 120)
        pb = plaque_bottom(fg, bx, by)
        wy0, wy1 = pb + 6, pb + 100
        bs = bands(label_mask, wx0, wx1, wy0, wy1, min_w=26, max_h=46, min_area=150, min_run=30)
        bs.sort(key=lambda r: r[1])
        # 底图口径：名称条在上（较宽），价格条在下（较窄）
        name_bar = bs[-2] if len(bs) >= 2 else (bs[-1] if bs else None)
        price_bar = bs[-1] if len(bs) >= 2 else None
        if name_bar is None:
            report.append(f"{name}: NOT FOUND  (window y {wy0}..{wy1})")
            continue
        erase_with_ring(img, name_bar, rng, grow=3)
        if price_bar:
            erase_with_ring(img, price_bar, rng, grow=3)
        cx = (name_bar[0] + name_bar[2]) // 2
        d.text((cx, name_bar[1] + 1), name, font=f_name, anchor="ma",
               fill=TEXT if state != "locked" else TEXT_DIM)
        if price_bar:
            pcx = (price_bar[0] + price_bar[2]) // 2
            d.text((pcx, price_bar[1] + 1), price, font=f_price, anchor="ma",
                   fill=BRASS if state != "locked" else (112, 106, 96))
        report.append(f"{name}: name={name_bar} price={price_bar}")
    print("\n".join(report))

    # ---------- 4. 底部详情卡 ----------
    cardreg = np.zeros_like(neutral); cardreg[1230:1536, :] = True
    ys, xs = np.where(brass & cardreg)
    if len(xs):
        cx0, cy0, cx1, cy1 = int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())
        sample = np.asarray(img.crop((cx0 + 30, cy0 + 15, cx1 - 30, cy1 - 15))).reshape(-1, 3)
        modal = np.median(sample, axis=0)
        diff = np.abs(a - modal).max(axis=2)
        inner = (diff > 14)
        windows = {
            "title": (cx0 + 95, cx0 + 380, cy0 + 2, cy0 + 62),
            "desc": (cx0 + 95, cx1 - 240, cy0 + 58, cy0 + 108),
            "state": (cx0 + 95, cx0 + 350, cy0 + 102, cy0 + 152),
            "cost": (cx0 + 300, cx0 + 620, cy0 + 102, cy0 + 152),
        }
        for key, (x0, x1, y0, y1) in windows.items():
            bs = bands(inner, max(0, x0), min(1024, x1), max(0, y0), min(1536, y1),
                       min_w=30, max_h=46, min_area=200)
            if not bs:
                print(f"card {key}: NOT FOUND")
                continue
            b = max(bs, key=lambda r: (r[2] - r[0]) * (r[3] - r[1]))
            erase_with_ring(img, b, rng, grow=3)
            print(f"card {key}: {b}")

        f_big = font(F_HEAVY, 34)
        f_mid = font(F_REG, 23)
        d.text((cx0 + 108, cy0 + 12), "回旋波", font=f_big, fill=BRASS_HI)
        d.text((cx0 + 108, cy0 + 64), "定时被动 · 每 6 秒把周围敌人打飞一次", font=f_mid, fill=TEXT_DIM)
        d.text((cx0 + 108, cy0 + 110), "可以解锁", font=f_mid, fill=(132, 214, 132))
        d.text((cx0 + 322, cy0 + 110), "花费 1 铜钱", font=f_mid, fill=BRASS)
        breg = np.zeros_like(neutral); breg[cy0:cy1, cx1 - 270:cx1] = True
        ys, xs = np.where(brass & breg)
        if len(xs):
            d.text(((xs.min() + xs.max()) // 2, (ys.min() + ys.max()) // 2), "解锁 1",
                   font=font(F_HEAVY, 32), fill=(246, 236, 214), anchor="mm")

    img.save(OUT_NATIVE)

    # ---------- 5. 9:16 成品 ----------
    w2, h2 = 1080, 1620
    scaled = img.resize((w2, h2), Image.LANCZOS)
    canvas = Image.new("RGB", (1080, 1920))
    canvas.paste(scaled.crop((0, 0, w2, 1)).resize((w2, 150), Image.NEAREST), (0, 0))
    canvas.paste(scaled, (0, 150))
    canvas.paste(scaled.crop((0, h2 - 1, w2, h2)).resize((w2, 150), Image.NEAREST), (0, 150 + h2))
    canvas.save(OUT_916)

    print("saved", OUT_NATIVE)
    print("saved", OUT_916)


if __name__ == "__main__":
    main()
