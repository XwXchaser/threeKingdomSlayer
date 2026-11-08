# -*- coding: utf-8 -*-
"""表情弧线闸门：量化"脸是否真的动起来"（沿用 v4 轮已验证的口径）。

口径（来自 enemy1012-bandit-idle-animation-handoff.md §0.4）
    · 指标：嘴部区域相对**首帧**的逐像素变化量（单像素变化 >60 记 1）
    · 全身版必须先做**头部垂直对齐**（否则头部点动会污染信号）
    · 闸门：峰值 − Q1 ≥ 60 ；Q3 < 峰值 × 0.6 ；末帧与首帧差异 ≈ 0（无缝）

用法
    python Tools/visual_brief/measure_expression_arc.py <视频.mp4> [--kind full|head]
    python Tools/visual_brief/measure_expression_arc.py <视频.mp4> --dump-frame0
"""
from __future__ import annotations

import argparse
import json
import os
import pathlib
import sys

import numpy as np
from PIL import Image

GREEN = (8, 145, 73)


def extract(video, workdir):
    import av
    workdir = pathlib.Path(workdir)
    raw = workdir / "raw_frames"
    raw.mkdir(parents=True, exist_ok=True)
    files = []
    with av.open(str(video)) as c:
        s = c.streams.video[0]
        for i, fr in enumerate(c.decode(s)):
            p = raw / ("f%03d.png" % i)
            if not p.exists():
                fr.to_image().convert("RGB").save(p)
            files.append(p)
    return files


def key_green(paths, outdir):
    """绿幕 → 掩膜（只用于取包围盒与对齐，不改交付素材）。"""
    outdir = pathlib.Path(outdir)
    outdir.mkdir(parents=True, exist_ok=True)
    masks = []
    for p in paths:
        a = np.asarray(Image.open(p).convert("RGB")).astype(int)
        m = np.abs(a - np.array(GREEN)).sum(axis=2) >= 90
        masks.append(m)
    return masks


def body_box(mask):
    ys, xs = np.where(mask)
    if not len(ys):
        return None
    return int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1


def align_heads(masks):
    """以首帧的身体顶端为基准，把每帧垂直对齐（全身版的头部点动会污染嘴部信号）。"""
    tops = []
    for m in masks:
        b = body_box(m)
        tops.append(b[1] if b else 0)
    base = tops[0]
    return [t - base for t in tops], tops


def mouth_band(kind, box, w, h):
    """嘴部区域：full = 按身体 bbox 比例推（沿用 v4 文档 E2 实测口径 x 0.40–0.60 / y 0.175–0.29）；
       head = 固定口径（v4 文档 E1 实测）。"""
    if kind == "head":
        return (455, 405, 625, 475)          # x0,y0,x1,y1 —— v4 E1 实测口径
    x0, y0, x1, y1 = box
    bh = y1 - y0
    bw = x1 - x0
    return (int(x0 + bw * 0.40), int(y0 + bh * 0.175),
            int(x0 + bw * 0.60), int(y0 + bh * 0.29))


def main() -> int:
    ap = argparse.ArgumentParser(description="表情弧线闸门")
    ap.add_argument("video")
    ap.add_argument("--kind", default="full", choices=["full", "head"])
    ap.add_argument("--work", default=None, help="中间目录（默认 Library/Locus/tmp/arc_measure）")
    ap.add_argument("--dump-frame0", action="store_true", help="导出首帧与嘴部区域图")
    ap.add_argument("--out", default=None, help="输出 json")
    args = ap.parse_args()

    proj = pathlib.Path(r"C:/threeKingdomSlayer/threeKingdomSlayer")
    work = pathlib.Path(args.work) if args.work else proj / "Library/Locus/tmp/arc_measure"
    work.mkdir(parents=True, exist_ok=True)
    stem = pathlib.Path(args.video).stem

    files = extract(args.video, work / stem)
    masks = key_green(files, work / stem / "keyed")
    offsets, tops = align_heads(masks)

    arrs = [np.asarray(Image.open(p).convert("RGB")).astype(int) for p in files]
    n = len(arrs)
    h, w = arrs[0].shape[:2]

    box0 = body_box(masks[0])
    band = mouth_band(args.kind, box0, w, h)
    x0, y0, x1, y1 = band

    base = arrs[0][y0:y1, x0:x1].copy()
    counts = []
    for i, a in enumerate(arrs):
        off = offsets[i]
        reg = a[max(0, y0 + off):min(h, y1 + off), x0:x1]
        b = base[:reg.shape[0], :reg.shape[1]]
        if reg.shape != b.shape:
            counts.append(0)
            continue
        d = np.abs(reg - b).sum(axis=2)
        counts.append(int((d > 60).sum()))

    counts = np.array(counts)
    peak = int(counts.max())
    peak_i = int(counts.argmax())
    q1 = int(counts[int(round(0.25 * (n - 1)))])
    tail = counts[int(round(0.90 * (n - 1))):]
    q3 = int(tail.mean()) if len(tail) else 0
    rise = peak - q1
    # 末帧与首帧比较：末帧也要按自己的头部对齐偏移取带
    off_last = offsets[-1]
    last_reg = arrs[-1][max(0, y0 + off_last):min(h, y1 + off_last), x0:x1]
    last_b = base[:last_reg.shape[0], :last_reg.shape[1]]
    end_diff = int((np.abs(last_reg - last_b).sum(axis=2) > 60).sum()) if last_reg.shape == last_b.shape else -1

    g_rise = rise >= 60
    g_fall = q3 < peak * 0.6
    g_end = end_diff <= max(5, peak * 0.05)
    passed = g_rise and g_fall and g_end

    print("== %s ==" % pathlib.Path(args.video).name)
    print("  帧数 %d   嘴部带 %s   头部垂直偏移 %d..%d" % (n, band, min(offsets), max(offsets)))
    print("  序列 %s" % " ".join(str(c) for c in counts[::4]))
    print("  Q1=%d  峰值=%d @f%d  Q3=%d  末帧与首帧差=%d" % (q1, peak, peak_i, q3, end_diff))
    print("  闸门 rise %d ≥ 60 → %s ｜ fall Q3 %d < 峰值×0.6=%d → %s ｜ 末帧≈首帧 → %s"
          % (rise, "PASS" if g_rise else "FAIL", q3, int(peak * 0.6), "PASS" if g_fall else "FAIL",
             "PASS" if g_end else "FAIL"))
    print("  总体：%s" % ("通过" if passed else "不通过"))

    if args.dump_frame0:
        vis = arrs[0].copy()
        vis[max(0, y0):min(h, y1), x0:x1] = [255, 0, 255]
        Image.fromarray(vis.astype("uint8")).save(str(work / "frame0_mouth_band.png"))
        print("  嘴部区域图：%s" % (work / "frame0_mouth_band.png"))

    if args.out:
        json.dump({"video": str(args.video), "kind": args.kind, "frames": n, "band": band,
                   "series": counts.tolist(), "q1": q1, "peak": peak, "peak_frame": peak_i,
                   "q3": q3, "end_diff": end_diff, "rise": rise,
                   "gate": {"rise": g_rise, "fall": g_fall, "end_match": g_end, "passed": passed}},
                  open(args.out, "w", encoding="utf-8"), ensure_ascii=False, indent=2)
    return 0 if passed else 1


if __name__ == "__main__":
    sys.exit(main())
