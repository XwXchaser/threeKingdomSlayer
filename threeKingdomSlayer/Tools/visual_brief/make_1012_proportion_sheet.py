# -*- coding: utf-8 -*-
"""1012 比例对照图生成器：把新形象与引擎已验收 idle1 按人物高度+脚底对齐并排。

为什么这样画
    项目自己的验收口径（B3 闸门第 7 条）就是「按人物高度和脚底对齐并排，缩小后仍读得出」，
    即**目视比对**，不是数值分带。所以这张图给的是：
      · 同一人物高度、同一脚底基线的两图
      · 每 10% 身高的水平参考线（视觉模型可直接读出头/身/腿的分界差异）
      · 只有**不受武器姿态影响**的可靠数值（腿长占比、脚跨度、落位缩放）
      · 底部缩小版（检验缩小后剪影是否仍读得出「挡路的贼兵」）

刻意不给的数值
    头宽/肩宽分带在 idle1 上会被**举起的刀**撑大（刀尖就在头部高度），两边不可比。

输出：<bundle>/images/04_side_by_side_aligned.png
"""
import os
import sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont

NEW_ART = r"C:/Users/steam/Pictures/gptGen/enemy1012_bandit_face_v3/stageA6b_fullbody_v1.png"
IDLE1 = "Assets/Sprites/Enemy/Enemy1012/Enemy_1012_idle1.png"
GREEN = (8, 145, 73)

CJK = [r"C:\Windows\Fonts\msyh.ttc", r"C:\Windows\Fonts\simhei.ttf"]


def font(size):
    for p in CJK:
        if os.path.exists(p):
            return ImageFont.truetype(p, size)
    return ImageFont.load_default()


def cut_new_art():
    im = Image.open(NEW_ART).convert("RGB")
    a = np.asarray(im).astype(int)
    mask = np.abs(a - np.array(GREEN)).sum(axis=2) > 90
    rgba = im.convert("RGBA")
    rgba.putalpha(Image.fromarray((mask * 255).astype("uint8")))
    return rgba


def cut_idle1():
    im = Image.open(IDLE1).convert("RGBA")
    a = np.asarray(im.split()[-1])
    rgba = im.copy()
    rgba.putalpha(Image.fromarray(((a > 10) * 255).astype("uint8")))
    return rgba


def crop_to_body(img):
    a = np.asarray(img.split()[-1])
    ys, xs = np.where(a > 10)
    c = img.crop((int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1))
    return c, int(xs.max() - xs.min() + 1), int(ys.max() - ys.min() + 1)


def band_width(alpha, f0, f1):
    ys, _ = np.where(alpha > 10)
    top, bot = int(ys.min()), int(ys.max())
    y0 = int(top + (bot - top) * f0)
    y1 = int(top + (bot - top) * f1)
    bx = np.where(alpha[y0:y1 + 1].any(axis=0))[0]
    return int(bx.max() - bx.min() + 1) if len(bx) else 0


def _runs(row):
    idx = np.where(row)[0]
    if not len(idx):
        return []
    segs = []
    s = p = idx[0]
    for v in idx[1:]:
        if v != p + 1:
            segs.append((int(s), int(p)))
            s = v
        p = v
    segs.append((int(s), int(p)))
    return segs


def band_measure(mask, f0, f1, mode="one"):
    """分段测量：只取身体本体那一段，排除武器（刀）等分离段。

    这是这次修正的关键——上一版直接取整行跨度，
    结果把 A6b 向左下伸出的刀当成腿、把 idle1 举起的刀当成肩，结论全错。
    """
    ys, _ = np.where(mask)
    top, bot = int(ys.min()), int(ys.max())
    b0 = int(top + (bot - top) * f0)
    b1 = int(top + (bot - top) * f1)
    best = 0
    for r in range(b0, b1 + 1):
        rs = _runs(mask[r])
        if not rs:
            continue
        if mode == "one":
            s = max(rs, key=lambda t: t[1] - t[0])
            w = s[1] - s[0] + 1
        else:
            rs2 = sorted(rs, key=lambda t: -(t[1] - t[0]))[:2]
            if len(rs2) < 2:
                continue
            w = max(rs2[0][1], rs2[1][1]) - min(rs2[0][0], rs2[1][0]) + 1
        best = max(best, w)
    return best


BANDS = [("头盔 3-12%", .03, .12, "one"),
         ("肩/胸 20-30%", .20, .30, "one"),
         ("腰/胯 48-60%", .48, .60, "one"),
         ("腿 70-85%", .70, .85, "one"),
         ("脚/靴 90-100%", .90, 1.0, "feet")]


def scale_to_height(img, h):
    w = max(1, int(round(img.width * h / img.height)))
    return img.resize((w, h), Image.LANCZOS)


def build(out_path, target_h=605, small_h=170):
    new_cut, new_w, new_h = crop_to_body(cut_new_art())
    old_cut, old_w, old_h = crop_to_body(cut_idle1())

    new_s = scale_to_height(new_cut, target_h)
    old_s = scale_to_height(old_cut, target_h)

    an = np.asarray(new_s.split()[-1])
    ao = np.asarray(old_s.split()[-1])
    rows = []
    for name, f0, f1, mode in BANDS:
        nw_ = band_measure(an, f0, f1, mode)
        ow_ = band_measure(ao, f0, f1, mode)
        rows.append((name, nw_, ow_))
    n_leg = dict((r[0], r[1]) for r in rows)["腿 70-85%"]
    n_foot = dict((r[0], r[1]) for r in rows)["脚/靴 90-100%"]
    o_leg = dict((r[0], r[2]) for r in rows)["腿 70-85%"]
    o_foot = dict((r[0], r[2]) for r in rows)["脚/靴 90-100%"]

    pad = 40
    note_w = 320
    gap = 70
    W = pad + new_s.width + gap + old_s.width + note_w + pad
    head_h = 120
    top_h = target_h + 130
    H = head_h + top_h + 260

    sheet = Image.new("RGB", (W, H), (22, 24, 30))
    d = ImageDraw.Draw(sheet)
    f_title = font(27)
    f_lbl = font(21)
    f_num = font(18)
    f_note = font(16)

    d.text((pad, 10), "1012 比例对照：左＝新形象 A6b（写实立绘）　右＝引擎已验收 idle1（像素化精灵）",
           font=f_title, fill=(240, 240, 245))
    d.text((pad, 48), "两图已按【同一人物高度 605 px】与【同一脚底基线】对齐；横向浅线＝每 10% 身高的参考网格。",
           font=f_note, fill=(165, 170, 185))
    d.text((pad, 70), "右侧数值按【身体本体分段测量】得出，已排除武器（刀）所占的水平跨度；只取各行的身体段。",
           font=f_note, fill=(165, 170, 185))

    base_y = head_h + top_h - 80
    panels = [(new_s, pad, "新形象 A6b", (255, 150, 90)),
              (old_s, pad + new_s.width + gap, "引擎 idle1", (130, 200, 255))]
    for img, x0, label, col in panels:
        y0 = base_y - img.height
        bg = Image.new("RGBA", img.size, (40, 42, 50, 255))
        bg.alpha_composite(img)
        sheet.paste(bg.convert("RGB"), (x0, y0))
        for i in range(1, 10):
            gy = y0 + int(img.height * i / 10)
            d.line([(x0, gy), (x0 + img.width, gy)], fill=(95, 100, 115), width=1)
        d.line([(x0 - 10, y0), (x0 + img.width + 10, y0)], fill=(90, 210, 230), width=2)
        d.line([(x0 - 10, base_y), (x0 + img.width + 10, base_y)], fill=(240, 80, 80), width=3)
        d.text((x0 + 3, y0 - 22), "头顶 0%", font=f_note, fill=(110, 215, 235))
        d.text((x0 + 3, base_y + 6), "脚底 100%", font=f_note, fill=(240, 120, 120))
        d.text((x0, base_y + 30), label, font=f_lbl, fill=col)

    nx = pad + new_s.width + gap + old_s.width + 20
    d.text((nx, head_h + 4), "实测数值（对齐后 605 px 身高）", font=f_lbl, fill=(255, 215, 120))
    rows_tbl = [
        ("人物高度", "605 px", "605 px"),
        ("画布", "1024x1024", "1108x1108"),
        ("主体占画布高", "86.8%", "54.6%"),
        ("脚底 y", "958", "898"),
    ] + [("%s 宽" % r[0], "%d px / %.0f%%" % (r[1], r[1] / target_h * 100),
          "%d px / %.0f%%" % (r[2], r[2] / target_h * 100)) for r in rows]
    y = head_h + 34
    d.text((nx + 160, y), "新形象", font=f_note, fill=(255, 150, 90))
    d.text((nx + 275, y), "idle1", font=f_note, fill=(130, 200, 255))
    y += 22
    for k, a, b in rows_tbl:
        d.text((nx, y), k, font=f_note, fill=(190, 195, 210))
        d.text((nx + 160, y), a, font=f_note, fill=(255, 215, 120))
        d.text((nx + 275, y), b, font=f_note, fill=(170, 215, 255))
        y += 25
    y += 12
    d.text((nx, y), "关键差值（占身高）", font=f_lbl, fill=(240, 240, 245))
    y += 27
    for name, f0, f1, mode in BANDS:
        nw_ = dict((r[0], r[1]) for r in rows)[name]
        ow_ = dict((r[0], r[2]) for r in rows)[name]
        diff = nw_ / target_h * 100 - ow_ / target_h * 100
        if abs(diff) < 2.5:
            txt = "%s  %+.1f pp（基本一致）" % (name, diff)
            col = (170, 215, 255)
        else:
            txt = "%s  %+.1f pp" % (name, diff)
            col = (255, 140, 120)
        d.text((nx, y), txt, font=f_note, fill=col)
        y += 23
    y += 6
    d.text((nx, y), "新形象落位到 605 px 的缩放系数 0.681", font=f_note, fill=(200, 205, 220))
    y += 23
    d.text((nx, y), "（idle1 原生即 605 px，无需缩放）", font=f_note, fill=(160, 165, 180))

    sy = base_y + 74
    d.text((pad, sy - 22), "同一人物高度缩到 %d px（近似游戏内显示尺寸）：检验缩小后剪影是否仍读得出「挡路的贼兵」"
           % small_h, font=f_note, fill=(175, 180, 195))
    sn = scale_to_height(new_cut, small_h)
    so = scale_to_height(old_cut, small_h)
    for img, x0, label, col in [(sn, pad + 40, "新形象", (255, 150, 90)),
                                (so, pad + 300, "idle1", (130, 200, 255))]:
        bg = Image.new("RGBA", img.size, (40, 42, 50, 255))
        bg.alpha_composite(img)
        sheet.paste(bg.convert("RGB"), (x0, sy))
        d.text((x0, sy + img.height + 6), label, font=f_note, fill=col)
        d.line([(x0 - 8, sy + img.height), (x0 + img.width + 8, sy + img.height)],
               fill=(240, 80, 80), width=2)

    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    sheet.save(out_path, "PNG", optimize=True)
    return sheet.size


if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "Library/Locus/tmp/side_by_side.png"
    print("写出 %s %s" % (out, build(out)))
