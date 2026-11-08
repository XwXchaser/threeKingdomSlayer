# -*- coding: utf-8 -*-
"""循环观感最优的 idle 六帧挑选（不取模绕回 + 最小化循环内最大跳变）。

为什么不用上一版
    上一版按「相位起点 + k·周期」取帧并对总帧数取模，结果最后一步变成
    「19 → 77」＝58 帧而不是一个周期 → 循环接缝处直接跳一大截（实测观感差）。

本版做法
    1. 算每个帧的「运动量」：上半身剪影与首帧的差（1-IoU），代表呼吸/表情的相位；
    2. 搜索 (起点 s, 步长 d)：取 s, s+d, …, s+5d 六帧，**必须落在同一段视频内、不取模**；
    3. 代价 = 循环里六段相邻跳变（含接缝那一段）的**最大**剪影差；
       取代价最小的一组 → 循环内每一步都尽量小，接缝也不跳。

用法
    python Tools/visual_brief/pick_idle_frames2.py \
        --keyed Library/Locus/tmp/1051_idle_keyed --count 6 --json out.json
"""
from __future__ import annotations

import argparse
import json
import pathlib

import numpy as np
from PIL import Image

GREEN = np.array([0., 159., 62.])


def masks(keyed: pathlib.Path, n):
    out = []
    for i in range(n):
        p = keyed / ("f%03d.png" % i)
        a = np.asarray(Image.open(p).convert("RGBA"))[..., 3] > 0
        out.append(a)
    return out


def crop_mask(m):
    ys, xs = np.nonzero(m)
    return m[ys.min():ys.max() + 1, xs.min():xs.max() + 1]


def norm(m, size=128):
    return np.asarray(Image.fromarray((m * 255).astype(np.uint8)).resize((size, size), Image.BILINEAR)) > 127


def iou(a, b):
    h = min(a.shape[0], b.shape[0])
    w = min(a.shape[1], b.shape[1])
    a = a[:h, :w]
    b = b[:h, :w]
    return (a & b).sum() / max(1, (a | b).sum())


def main():
    ap = argparse.ArgumentParser(description="循环观感最优的 idle 帧挑选")
    ap.add_argument("--keyed", required=True, type=pathlib.Path)
    ap.add_argument("--count", type=int, default=6)
    ap.add_argument("--json", required=True)
    ap.add_argument("--min-step", type=int, default=5)
    ap.add_argument("--max-step", type=int, default=14)
    args = ap.parse_args()

    files = sorted(args.keyed.glob("f*.png"))
    n = len(files)
    raw = masks(args.keyed, n)
    # 归一化剪影（去位置差），用于比较姿态变化
    nm = [norm(crop_mask(m)) for m in raw]
    # 上半身带（头+胸腹）：按整体 bbox 的上 48%
    up = []
    for m, nm_i in zip(raw, nm):
        ys, _ = np.nonzero(m)
        top, bot = int(ys.min()), int(ys.max())
        h = bot - top
        band = nm_i[:max(1, int(h * 0.48)), :]
        up.append(band)

    ref = 0
    motion = [1.0 - iou(up[i], up[ref]) for i in range(n)]
    print("运动量曲线（每6帧）:", [(i, round(motion[i], 4)) for i in range(0, n, 6)])

    best = None
    for s in range(0, n):
        for d in range(args.min_step, args.max_step + 1):
            picks = [s + k * d for k in range(args.count)]
            if picks[-1] >= n:
                continue
            steps = []
            for k in range(args.count):
                a = picks[k]
                b = picks[(k + 1) % args.count]
                # 接缝那一步用「下一周期的同相位帧」来量
                nxt = picks[args.count - 1] + d if k == args.count - 1 else b
                nxt = nxt % n
                steps.append(1.0 - iou(nm[a], nm[nxt]))
            cost = max(steps)
            if best is None or cost < best[0]:
                best = (cost, s, d, picks, steps)
    cost, s, d, picks, steps = best
    print("\n最优：起点 f%03d 步长 %d → %s" % (s, d, ["f%03d" % p for p in picks]))
    print("循环内每一步的剪影差:", [round(x, 4) for x in steps])
    print("最大跳变 = %.4f（越小循环越顺）" % cost)

    rec = {"frames": n, "count": args.count, "start": s, "step": d,
           "picks": ["f%03d" % p for p in picks],
           "step_diffs": [round(x, 4) for x in steps], "max_step": round(cost, 4),
           "motion": [round(x, 4) for x in motion]}
    pathlib.Path(args.json).write_text(json.dumps(rec, indent=2, ensure_ascii=False), encoding="utf-8")
    print("记录: %s" % args.json)


if __name__ == "__main__":
    main()
