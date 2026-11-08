# -*- coding: utf-8 -*-
"""1012 身材比例测量器：把任意一张 1012 候选图与引擎基准 idle1 做同口径分段测量并判定是否达标。

为什么需要它
    横向尺寸必须**分段测量**：取整行跨度会把武器（刀）算进身体。
    A6b 的刀向左下伸出会污染下半身测量，idle1 举起的刀会污染上半身测量，
    只有「取每一行里属于身体的那一段」才是两边可比的量。

用法
    python Tools/visual_brief/measure_1012_proportion.py <候选图路径>
    python Tools/visual_brief/measure_1012_proportion.py <候选图.png> --json

输出
    逐部位实测值、与基准的差值、是否达标（判据见 --tolerance）
"""
import argparse
import json
import os
import sys
import numpy as np
from PIL import Image

PROJ = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
BASELINE = "Assets/Sprites/Enemy/Enemy1012/Enemy_1012_idle1.png"
# 权威基准：v11（1011 style-locked 全身设计，1012 的比例来源）
BASELINE_V11 = r"C:/Users/steam/Pictures/gptGen/enemy1011_style_locked_dao_v11/enemy1011_style_locked_dao_v11.png"
GREEN = (8, 145, 73)

# 基准值（在 605px 身高口径下实测，见 make_1012_proportion_sheet.py）
BASELINE_PCT = {
    "头盔 3-12%": 23.0,
    "肩/胸 20-30%": 41.0,
    "腰/胯 48-60%": 35.0,
    "腿 70-85%": 53.0,
    "脚/靴 90-100%": 67.0,
}
# 目标区间（占身高 %）。头/盔因兜鍪形制锁死，见方案 §2.2，放宽到 ±3.5
TOLERANCE = {
    "头盔 3-12%": (-3.5, 1.0),
    "肩/胸 20-30%": (-2.0, 2.0),
    "腰/胯 48-60%": (-2.0, 1.5),
    "腿 70-85%": (-2.0, 2.5),
    "脚/靴 90-100%": (-2.0, 2.0),
}

BANDS = [("头盔 3-12%", .03, .12, "one"),
         ("肩/胸 20-30%", .20, .30, "one"),
         ("腰/胯 48-60%", .48, .60, "one"),
         ("腿 70-85%", .70, .85, "one"),
         ("脚/靴 90-100%", .90, 1.0, "feet")]


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


def body_mask(path):
    """返回 (mask, 人物 bbox, 主体高)。绿幕图按绿幕色反选，精灵图按 alpha。"""
    im = Image.open(path)
    if im.mode == "RGBA":
        a = np.asarray(im.split()[-1])
        if a.min() < 250:                      # 有透明通道 → 按 alpha
            mask = a > 10
            rgb = im.convert("RGB")
            arr = np.asarray(rgb).astype(int)
            greenish = (np.abs(arr - np.array(GREEN)).sum(axis=2) < 90)
            if greenish.any() and (a > 10).sum() > greenish.sum():
                mask = (a > 10) & ~greenish
    else:
        rgb = im.convert("RGB")
        arr = np.asarray(rgb).astype(int)
        mask = np.abs(arr - np.array(GREEN)).sum(axis=2) > 90
    ys, xs = np.where(mask)
    if not len(ys):
        raise SystemExit("没有找到人物像素")
    box = (int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1)
    return mask, box, box[3] - box[1]


def band_measure(mask, f0, f1, mode="one"):
    ys, _ = np.where(mask)
    top, bot = int(ys.min()), int(ys.max())
    b0, b1 = int(top + (bot - top) * f0), int(top + (bot - top) * f1)
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


def measure(path, baseline_pct=None):
    mask, box, H = body_mask(path)
    if baseline_pct is None:
        baseline_pct = BASELINE_PCT
    rows = []
    for name, f0, f1, mode in BANDS:
        w = band_measure(mask, f0, f1, mode)
        pct = w / H * 100
        lo, hi = TOLERANCE[name]
        base = baseline_pct[name]
        ok = (base + lo) <= pct <= (base + hi)
        rows.append({"band": name, "px": w, "pct": round(pct, 1),
                     "baseline_pct": base, "diff_pp": round(pct - base, 1), "pass": bool(ok)})
    return {"file": path, "body_h": H, "bbox": box, "bands": rows}


def main(argv):
    ap = argparse.ArgumentParser(description="1012 身材比例测量（与引擎 idle1 同口径）")
    ap.add_argument("image", help="候选图路径")
    ap.add_argument("--json", action="store_true", help="只输出 JSON")
    ap.add_argument("--baseline", default="idle1", choices=["idle1", "v11"],
                    help="判定基准：idle1=引擎旧精灵；v11=权威设计源图")
    args = ap.parse_args(argv)

    path = args.image if os.path.isabs(args.image) else os.path.join(PROJ, args.image)
    if not os.path.isfile(path):
        print("错误：文件不存在 %s" % path, file=sys.stderr)
        return 2

    base_pct = None
    if args.baseline == "v11":
        if not os.path.isfile(BASELINE_V11):
            print("错误：找不到 v11 基准 %s" % BASELINE_V11, file=sys.stderr)
            return 2
        bmask, bbox, bH = body_mask(BASELINE_V11)
        base_pct = {}
        for name, f0, f1, mode in BANDS:
            w = band_measure(bmask, f0, f1, mode)
            base_pct[name] = round(w / bH * 100, 1)
    r = measure(path, base_pct)
    if args.json:
        print(json.dumps(r, ensure_ascii=False, indent=2))
        return 0

    print("== %s ==  主体高 %d px  bbox %s   基准=%s" % (
        os.path.basename(path), r["body_h"], r["bbox"], args.baseline))
    print("%-14s %8s %8s %8s %10s %6s" % ("部位", "实测px", "占比%", "基准%", "差pp", "判定"))
    allok = True
    for b in r["bands"]:
        flag = "PASS" if b["pass"] else "FAIL"
        allok = allok and b["pass"]
        print("%-14s %8d %7.1f%% %7.1f%% %+9.1f %6s"
              % (b["band"], b["px"], b["pct"], b["baseline_pct"], b["diff_pp"], flag))
    print("\n总体：%s" % ("全部达标" if allok else "存在未达标项"))
    return 0 if allok else 1


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
