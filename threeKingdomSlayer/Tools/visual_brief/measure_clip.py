# -*- coding: utf-8 -*-
"""成片逐帧量测（绿幕 → 抠图 → 逐帧几何/运动统计）。

用法：
    python Tools/visual_brief/measure_clip.py <mp4> --out-dir Library/Locus/tmp/clip_xxx
"""
from __future__ import annotations

import argparse
import json
import pathlib
import sys

import av
import numpy as np
from PIL import Image

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
from chroma_key import key_frame  # noqa: E402


def mask_iou(a, b):
    inter = np.logical_and(a, b).sum()
    union = np.logical_or(a, b).sum()
    return float(inter) / float(union) if union else 0.0


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("video")
    ap.add_argument("--out-dir", required=True)
    ap.add_argument("--json", default="")
    ap.add_argument("--save-frames", action="store_true")
    args = ap.parse_args()

    out = pathlib.Path(args.out_dir)
    out.mkdir(parents=True, exist_ok=True)

    container = av.open(args.video)
    stream = container.streams.video[0]
    w, h = stream.codec_context.width, stream.codec_context.height
    fps = float(stream.average_rate)

    masks = []
    tops, foots, lefts, rights, widths, areas = [], [], [], [], [], []
    frames_meta = []
    idx = 0
    for frame in container.decode(video=0):
        img = frame.to_image().convert("RGB")
        arr = np.asarray(img)
        alpha, _ = key_frame(arr)
        m = alpha > 0
        masks.append(m)
        ys, xs = np.nonzero(m)
        if xs.size == 0:
            frames_meta.append({"index": idx, "empty": True})
            idx += 1
            continue
        top, bot = int(ys.min()), int(ys.max())
        left, right = int(xs.min()), int(xs.max())
        # 身体头顶：只看左 55% 宽度的列（旗在右侧，避免把旗当头顶）
        band = m[:, left:left + max(1, int((right - left) * 0.55))]
        bys = np.nonzero(band.any(axis=1))[0]
        head_top = int(bys.min()) if bys.size else top
        frames_meta.append({
            "index": idx, "bbox": [left, top, right, bot],
            "head_top": head_top, "body_h": bot - head_top + 1,
            "foot_y": bot, "flag_top": top, "right": right, "left": left,
            "area": int(m.sum()),
            "touch_border": bool(left == 0 or top == 0 or right == w - 1 or bot == h - 1),
        })
        tops.append(top); foots.append(bot); lefts.append(left); rights.append(right)
        widths.append(right - left + 1); areas.append(int(m.sum()))
        if args.save_frames:
            rgba = np.zeros((h, w, 4), dtype=np.uint8)
            rgba[:, :, :3] = arr
            rgba[:, :, 3] = alpha
            Image.fromarray(rgba, "RGBA").save(out / ("f%03d.png" % idx))
        idx += 1

    n = len(masks)
    diffs = []
    for i in range(1, n):
        diffs.append(float(np.logical_xor(masks[i], masks[i - 1]).sum()) /
                     max(1, max(masks[i].sum(), masks[i - 1].sum())))
    iou_first_last = mask_iou(masks[0], masks[-1])

    # 步态周期：剪影配对距离（1-IoU）在归一化后取最小
    norm = []
    for m in masks:
        ys, xs = np.nonzero(m)
        if xs.size == 0:
            norm.append(m)
            continue
        crop = m[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
        im = Image.fromarray((crop * 255).astype(np.uint8)).resize((96, 96), Image.NEAREST)
        norm.append(np.asarray(im) > 127)
    periods = {}
    for p in range(4, min(40, n // 2)):
        vals = [1 - mask_iou(norm[i], norm[i + p]) for i in range(0, n - p, 1)]
        periods[p] = round(float(np.median(vals)), 4)
    best_p = min(periods, key=periods.get)

    out_of_frame = [f["index"] for f in frames_meta if f.get("touch_border")]
    still = [i for i, d in enumerate(diffs) if d < 0.01]

    info = {
        "video": args.video,
        "size": [w, h], "fps": fps, "frames": n,
        "head_top_median": int(np.median([f["head_top"] for f in frames_meta if "head_top" in f])),
        "body_h_median": int(np.median([f["body_h"] for f in frames_meta if "body_h" in f])),
        "foot_y_min": int(min(foots)), "foot_y_max": int(max(foots)),
        "flag_top_min": int(min(tops)), "flag_top_max": int(max(tops)),
        "left_min": int(min(lefts)), "left_max": int(max(lefts)),
        "right_min": int(min(rights)), "right_max": int(max(rights)),
        "width_min": int(min(widths)), "width_max": int(max(widths)),
        "frame_diff_median": round(float(np.median(diffs)), 4),
        "frame_diff_max": round(float(max(diffs)), 4),
        "first_last_iou": round(iou_first_last, 4),
        "out_of_frame_frames": out_of_frame,
        "out_of_frame_count": len(out_of_frame),
        "near_still_frames": len(still),
        "best_period": best_p,
        "period_table": periods,
        "per_frame": frames_meta,
    }
    jpath = pathlib.Path(args.json) if args.json else out / "measure.json"
    jpath.write_text(json.dumps(info, indent=2, ensure_ascii=False), encoding="utf-8")
    print(json.dumps({k: v for k, v in info.items() if k != "per_frame"}, indent=2, ensure_ascii=False))
    print("[OK] %s" % jpath)


if __name__ == "__main__":
    main()
