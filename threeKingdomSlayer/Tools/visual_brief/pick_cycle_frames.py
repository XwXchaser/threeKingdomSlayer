# -*- coding: utf-8 -*-
"""按步态周期挑帧并导出（与 03_CastFrames / 04_IdleFrames 同规格的裁切帧 + provenance）。

流程：
  1. 读已抠好的 RGBA 帧目录
  2. 用剪影 IoU 找周期 P 与最干净的相位 i（mask_i 与 mask_{i+P} 最像）
  3. 在一个周期内等距取 N 帧
  4. 逐帧量测（人物身/脚底线/旗顶/边距），导出 PNG + frame-provenance.csv
  5. 打印落位口径（k / 画布 / 脚底线 / pivot）

用法：
    python Tools/visual_brief/pick_cycle_frames.py --frames <dir> --out <dir> \
        --clip 1013_walk_v1.mp4 --prefix walk --count 6
"""
from __future__ import annotations

import argparse
import csv
import json
import pathlib

import numpy as np
from PIL import Image


def iou(a, b):
    u = np.logical_or(a, b).sum()
    return float(np.logical_and(a, b).sum()) / float(u) if u else 0.0


def measure(mask):
    ys, xs = np.nonzero(mask)
    left, right, top, bot = int(xs.min()), int(xs.max()), int(ys.min()), int(ys.max())
    band = mask[:, left:left + max(1, int((right - left) * 0.55))]
    bys = np.nonzero(band.any(axis=1))[0]
    head_top = int(bys.min()) if bys.size else top
    h, w = mask.shape
    margin = min(left, right and w - 1 - right, top, bot and h - 1 - bot)
    return {"bbox": [left, top, right, bot], "head_top": head_top,
            "body_h": bot - head_top + 1, "foot_y": bot, "flag_top": top,
            "left": left, "right": right, "margin": int(margin)}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--frames", required=True, help="已抠好的 RGBA 帧目录")
    ap.add_argument("--out", required=True)
    ap.add_argument("--clip", required=True)
    ap.add_argument("--prefix", default="walk")
    ap.add_argument("--count", type=int, default=6)
    ap.add_argument("--period", type=int, default=0, help="0=自动检测")
    ap.add_argument("--target-body", type=int, default=605, help="游戏内人物身高")
    ap.add_argument("--canvas-w", type=int, default=1108)
    ap.add_argument("--canvas-h", type=int, default=1440)
    ap.add_argument("--foot-y", type=int, default=1350)
    ap.add_argument("--json", default="")
    args = ap.parse_args()

    fdir = pathlib.Path(args.frames)
    odir = pathlib.Path(args.out)
    odir.mkdir(parents=True, exist_ok=True)
    files = sorted(fdir.glob("f*.png"))
    if not files:
        raise SystemExit("no frames in %s" % fdir)

    masks, imgs = [], []
    for f in files:
        im = Image.open(f).convert("RGBA")
        imgs.append(im)
        masks.append(np.asarray(im)[:, :, 3] > 0)
    n = len(masks)

    P = args.period
    if not P:
        best = None
        for p in range(6, min(40, n // 2)):
            vals = [1 - iou(masks[i], masks[i + p]) for i in range(0, n - p)]
            med = float(np.median(vals))
            if best is None or med < best[1]:
                best = (p, med)
        P = best[0]

    phase_scores = [(i, iou(masks[i], masks[i + P])) for i in range(0, n - P)]
    phase = max(phase_scores, key=lambda t: t[1])[0]

    picks = [phase + round(k * P / args.count) for k in range(args.count)]
    picks = [p for p in picks if p < n]

    rows = []
    for idx, p in enumerate(picks, 1):
        m = measure(masks[p])
        name = "%s_%02d_f%03d.png" % (args.prefix, idx, p)
        imgs[p].save(odir / name)
        rows.append({
            "frame": "%02d" % idx, "file": name, "source_clip": args.clip,
            "source_frame": "f%03d" % p, "body_h": m["body_h"],
            "foot_y": m["foot_y"], "flag_top": m["flag_top"],
            "min_margin_px": m["margin"],
        })

    csv_path = odir / "frame-provenance.csv"
    with open(csv_path, "w", newline="", encoding="utf-8-sig") as fh:
        w = csv.DictWriter(fh, fieldnames=list(rows[0].keys()))
        w.writeheader()
        w.writerows(rows)

    med = int(np.median([r["body_h"] for r in rows]))
    k = args.target_body / float(med)
    info = {
        "period": P, "phase": phase, "phase_iou": round(max(v for _, v in phase_scores), 4),
        "picks": picks, "body_h_median": med,
        "landing": {"k": round(k, 5), "target_body": args.target_body,
                    "canvas": [args.canvas_w, args.canvas_h], "foot_y": args.foot_y,
                    "pivot": [round(args.canvas_w * 0.654 / args.canvas_w, 4),
                              round(1 - args.foot_y / args.canvas_h, 4)]},
    }
    jp = pathlib.Path(args.json) if args.json else odir / "pick.json"
    jp.write_text(json.dumps(info, indent=2, ensure_ascii=False), encoding="utf-8")
    print(json.dumps(info, indent=2, ensure_ascii=False))
    print("[OK] frames -> %s" % odir)
    print("[OK] csv -> %s" % csv_path)


if __name__ == "__main__":
    main()
