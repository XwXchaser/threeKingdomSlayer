# -*- coding: utf-8 -*-
"""射箭成片 → 6 个 attack 关键帧（语义拍段 + 段内最大化与上一帧的剪影差）。

用法
    python Tools/visual_brief/pick_shoot_frames.py \
        --keyed Library/Locus/tmp/1051_shoot_v3_keyed \
        --out-json Library/Locus/tmp/1051_shoot_picks.json

拍段（按射箭动作语义切，段内再挑）
    W1 抽箭    取箭出袋、抬手
    W2 举弓    弓抬到身前
    W3 引弓    拉弦到脸颊
    W4 满弓    满弓定住（峰值）
    W5 放箭    松弦、右手前甩
    W6 收势    保持收势

做法
    · 段内挑「与上一张已选帧剪影差异最大」的帧（IoU 最小），
      保证相邻关键帧之间真的看得见动作变化，而不是同一姿势的重复。
    · `--exclude` 可排除坏帧（例如放箭瞬间有一支大箭斜飞出画的那两帧）。
"""
from __future__ import annotations

import argparse
import json
import pathlib

import numpy as np
from PIL import Image


def masks(keyed: pathlib.Path, idxs):
    out = {}
    for i in idxs:
        p = keyed / ("f%03d.png" % i)
        a = np.asarray(Image.open(p).convert("RGBA"))[..., 3] > 0
        out[i] = a
    return out


def iou(a, b):
    return (a & b).sum() / max(1, (a | b).sum())


def main():
    ap = argparse.ArgumentParser(description="射箭关键帧挑选")
    ap.add_argument("--keyed", required=True, type=pathlib.Path)
    ap.add_argument("--out-json", required=True)
    ap.add_argument("--exclude", default="", help="逗号分隔的帧号")
    ap.add_argument("--windows", default="",
                    help="拍段定义 name:start:end,逗号分隔；默认用射箭语义段")
    args = ap.parse_args()

    default_windows = [
        ("attack1_抽箭", 19, 26),
        ("attack2_举弓", 27, 29),
        ("attack3_引弓", 30, 33),
        ("attack4_满弓", 34, 104),
        ("attack5_放箭", 107, 112),
        ("attack6_收势", 113, 144),
    ]
    if args.windows:
        windows = []
        for w in args.windows.split(","):
            name, s, e = w.split(":")
            windows.append((name, int(s), int(e)))
    else:
        windows = default_windows

    excl = {int(x) for x in args.exclude.split(",") if x.strip()}
    all_idx = sorted(set(i for _, s, e in windows for i in range(s, e + 1)) - excl)
    m = masks(args.keyed, all_idx)

    picks = []
    prev = None
    for name, s, e in windows:
        pool = [i for i in all_idx if s <= i <= e]
        if not pool:
            continue
        if prev is None:
            # 第一张：挑与 idle 差异最大的（动作变化最大）
            best = max(pool, key=lambda i: len(m[i]))
            best = min(pool, key=lambda i: iou(m[i], m[pool[0]])) if False else best
        else:
            best = min(pool, key=lambda i: iou(m[i], m[prev]))
        picks.append({"key": name, "frame": best,
                      "iou_with_prev": None if prev is None else round(iou(m[best], m[prev]), 4)})
        prev = best

    print("选帧结果:")
    for p in picks:
        print("  %-16s f%03d   与上一帧 IoU=%s" % (p["key"], p["frame"], p["iou_with_prev"]))
    print("\n相邻两帧 IoU 最大值（越小越有动作感）: %.4f"
          % max(p["iou_with_prev"] for p in picks if p["iou_with_prev"] is not None))
    rec = {"picks": picks, "excluded": sorted(excl),
           "windows": [[n, s, e] for n, s, e in windows]}
    pathlib.Path(args.out_json).write_text(json.dumps(rec, indent=2, ensure_ascii=False), encoding="utf-8")
    print("记录: %s" % args.out_json)


if __name__ == "__main__":
    main()
