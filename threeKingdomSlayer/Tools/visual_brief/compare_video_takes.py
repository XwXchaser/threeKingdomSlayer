#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
compare_video_takes.py —— 判定「已部署精灵 / 源片 A」到底出自哪一支 take。

方法（不依赖文件名，只看像素）：
  1. 用 PyAV 解码候选视频，取指定帧；
  2. 绿幕键出主体二值掩码（源片）或直接取 alpha（已部署精灵）；
  3. 把两张掩码各自裁到内容 bbox 后归一化到同一尺寸，算 IoU。

用法：
  python Tools/visual_brief/compare_video_takes.py \
      --video "C:/path/a.mp4" \
      --video "C:/path/b.mp4" \
      --sprite "Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_attack6.png" \
      --frame 49 \
      --iou-threshold 0.35
"""
from __future__ import annotations

import argparse
import io
import json
import sys
from pathlib import Path

import av
import numpy as np
from PIL import Image

# 绿幕容差：与 (0,159,62) 的距离小于该值视为背景
GREEN_REF = np.array([0.0, 159.0, 62.0])
GREEN_TOL = 78.0


def read_video_frame(path: str, index: int) -> np.ndarray | None:
    """按 0-based 帧序号解出一帧 RGB。"""
    with av.open(path) as container:
        stream = container.streams.video[0]
        stream.thread_type = "AUTO"
        for i, frame in enumerate(container.decode(stream)):
            if i == index:
                return frame.to_ndarray(format="rgb24")
    return None


def green_mask(rgb: np.ndarray) -> np.ndarray:
    d = np.linalg.norm(rgb.astype(np.float32) - GREEN_REF, axis=2)
    return (d > GREEN_TOL).astype(np.uint8)


def alpha_mask(path: str) -> np.ndarray:
    im = Image.open(path).convert("RGBA")
    a = np.array(im)
    return (a[:, :, 3] > 8).astype(np.uint8)


def norm(mask: np.ndarray, size: int = 192) -> np.ndarray:
    """裁到内容 bbox → 放到正方形画布 → 缩放到 size。"""
    ys, xs = np.nonzero(mask)
    if len(xs) == 0:
        return np.zeros((size, size), np.uint8)
    crop = mask[ys.min(): ys.max() + 1, xs.min(): xs.max() + 1]
    h, w = crop.shape
    side = max(h, w)
    canvas = np.zeros((side, side), np.uint8)
    canvas[(side - h) // 2: (side - h) // 2 + h, (side - w) // 2: (side - w) // 2 + w] = crop
    return np.array(Image.fromarray(canvas * 255).resize((size, size), Image.NEAREST)) > 127


def iou(a: np.ndarray, b: np.ndarray) -> float:
    inter = np.logical_and(a, b).sum()
    union = np.logical_or(a, b).sum()
    return float(inter) / float(union) if union else 0.0


def bbox_of(mask: np.ndarray) -> tuple[int, int, int, int] | None:
    ys, xs = np.nonzero(mask)
    if len(xs) == 0:
        return None
    return int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--video", action="append", required=True, help="候选视频，可重复")
    ap.add_argument("--sprite", help="已部署精灵（带 alpha）")
    ap.add_argument("--frame", type=int, default=0, help="0-based 源帧号")
    ap.add_argument("--iou-threshold", type=float, default=0.35)
    ap.add_argument("--json-out", default="")
    args = ap.parse_args()

    ref_mask = alpha_mask(args.sprite) if args.sprite else None
    report = {
        "sprite": args.sprite,
        "frame": args.frame,
        "sprite_bbox": bbox_of(ref_mask) if ref_mask is not None else None,
        "candidates": [],
    }

    for path in args.video:
        rgb = read_video_frame(path, args.frame)
        if rgb is None:
            report["candidates"].append({"video": path, "error": "frame out of range"})
            continue
        m = green_mask(rgb)
        entry = {
            "video": path,
            "bbox": bbox_of(m),
            "subject_pixels": int(m.sum()),
        }
        if ref_mask is not None:
            entry["iou_vs_sprite"] = round(iou(norm(ref_mask), norm(m)), 4)
            entry["verdict"] = "MATCH" if entry["iou_vs_sprite"] >= args.iou_threshold else "no"
        report["candidates"].append(entry)

    text = json.dumps(report, ensure_ascii=False, indent=2)
    print(text)
    if args.json_out:
        Path(args.json_out).parent.mkdir(parents=True, exist_ok=True)
        Path(args.json_out).write_text(text, encoding="utf-8")
    return 0


if __name__ == "__main__":
    sys.exit(main())
