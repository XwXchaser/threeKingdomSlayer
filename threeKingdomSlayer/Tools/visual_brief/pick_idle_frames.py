# -*- coding: utf-8 -*-
"""待机视频 → 6 帧 idle 精灵（剪影配对距离找周期 + 周期内等距取帧）。

做法（沿用 1012 步态那套：memory/enemy1012-art-replacement-pipeline.md §9.1）
    · 每帧按 bbox 归一化剪影，算 1-IoU；
    · 对所有 (i, i+P) 配对取最小值 → 使该值最小的 P 就是呼吸周期；
    · 在周期起点取相位 i，周期内等距取 N 帧（偏移 = round(k·P/N)），
      末帧与下一周期首帧同姿态 → 天然无缝。

用法
    python Tools/visual_brief/pick_idle_frames.py \
        --video Enemy1051ArtSource/01_SourceVideos/1051_idle_v1_4s.mp4 \
        --count 6 --json Library/Locus/tmp/1051_idle_picks.json
"""
from __future__ import annotations

import argparse
import json
import pathlib

import av
import numpy as np
from PIL import Image

GREEN = np.array([0., 159., 62.])
NORM = 96


def decode(path):
    out = []
    with av.open(str(path)) as c:
        s = c.streams.video[0]
        s.thread_type = "AUTO"
        for fr in c.decode(s):
            out.append(fr.to_ndarray(format="rgb24"))
    return out


def norm_mask(frame):
    a = frame.astype(np.float32)
    m = np.linalg.norm(a - GREEN, axis=2) > 79
    if not m.any():
        return None
    ys, xs = np.nonzero(m)
    crop = m[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    img = Image.fromarray((crop * 255).astype(np.uint8)).resize((NORM, NORM), Image.BILINEAR)
    return np.asarray(img) > 127


def main():
    ap = argparse.ArgumentParser(description="idle 关键帧挑选")
    ap.add_argument("--video", required=True)
    ap.add_argument("--count", type=int, default=6)
    ap.add_argument("--json", required=True)
    ap.add_argument("--min-period", type=int, default=8)
    args = ap.parse_args()

    frames = decode(args.video)
    n = len(frames)
    masks = [norm_mask(f) for f in frames]
    masks = [m for m in masks if m is not None]
    n = len(masks)

    # 配对距离 1-IoU
    best = []
    for P in range(args.min_period, n // 2):
        vals = []
        for i in range(n - P):
            inter = (masks[i] & masks[i + P]).sum()
            union = (masks[i] | masks[i + P]).sum()
            vals.append(1.0 - inter / max(1, union))
        best.append((float(np.mean(vals)), P))
    best.sort()
    print("周期候选（配对距离均值）:")
    for d, P in best[:6]:
        print("   P=%3d  距离=%.4f  (%.2fs)" % (P, d, P / (len(frames) / 4.0)))
    period = best[0][1]
    print("\n选定周期 P=%d" % period)

    # 相位：取「与下一周期首帧最像」的一帧作为起点（即该周期里姿态最标准的位置）
    # 用与全片平均距离最小的那一帧
    dist_to_mean = []
    for i in range(n):
        inter = (masks[i] & masks).sum()
        union = (masks[i] | masks).sum()
        dist_to_mean.append(1.0 - inter / max(1, union))
    phase = int(np.argmin(dist_to_mean))
    print("相位起点 f%d（与全片平均剪影最接近）" % phase)

    picks = []
    for k in range(args.count):
        idx = (phase + int(round(k * period / float(args.count)))) % n
        picks.append(idx)
    picks = sorted(set(picks))
    print("选帧: %s" % ["f%03d" % i for i in picks])

    rec = {"video": str(args.video), "frames": len(frames), "period": period,
           "phase": phase, "count": args.count, "picks": ["f%03d" % i for i in picks]}
    pathlib.Path(args.json).write_text(json.dumps(rec, indent=2, ensure_ascii=False), encoding="utf-8")
    print("记录: %s" % args.json)


if __name__ == "__main__":
    main()
