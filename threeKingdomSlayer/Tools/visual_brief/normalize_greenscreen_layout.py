# -*- coding: utf-8 -*-
"""绿幕图 → 项目标准落位身份图（1024² / 绿(0,159,62) / 主体高 696 / 脚底 895 / 中心 474.5）。

与 compose_identity_from_alpha.py 的区别：那个吃**带 alpha 的**原图，这个吃**绿幕 RGB 图**
（生成接口的 edits 输出常常是 RGB 绿幕、没有 alpha 通道）。

处理步骤：
  1. 绿幕抠图：像素到 (0,159,62) 的距离 > TOL 记为前景；洪泛填封闭孔；
  2. 前景掩膜缩放成软 alpha（LANCZOS），RGB 同步缩放；
  3. 去绿边（de-spill）：alpha 内的绿味像素把绿通道压到 (r+b)/2 附近；
  4. 缩放到主体高 696，按硬 alpha 贴到 1024² 纯绿画布，脚底 895、中心 474.5；
  5. 输出 JSON 量测（bbox / 落位 / 四角绿幕纯净度）。

用法：
    python Tools/visual_brief/normalize_greenscreen_layout.py <输入.png> --out <输出.png>
"""
from __future__ import annotations

import argparse
import json
import pathlib
import sys

try:
    from PIL import Image
except ImportError:  # pragma: no cover
    print("PIL required", file=sys.stderr)
    raise

import numpy as np

GREEN = (0, 159, 62)
CANVAS = 1024
TARGET_H = 696
TARGET_FOOT_Y = 895
TARGET_CENTER_X = 474.5
GREEN_TOL = 79


def fill_holes(mask: np.ndarray) -> tuple[np.ndarray, int]:
    """把封闭孔填上（4 邻接洪泛背景）。"""
    h, w = mask.shape
    bg = ~mask
    seen = np.zeros_like(bg)
    stack = []
    for x in range(w):
        for y in (0, h - 1):
            if bg[y, x] and not seen[y, x]:
                seen[y, x] = True
                stack.append((y, x))
    for y in range(h):
        for x in (0, w - 1):
            if bg[y, x] and not seen[y, x]:
                seen[y, x] = True
                stack.append((y, x))
    while stack:
        y, x = stack.pop()
        for ny, nx in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
            if 0 <= ny < h and 0 <= nx < w and bg[ny, nx] and not seen[ny, nx]:
                seen[ny, nx] = True
                stack.append((ny, nx))
    filled = mask | (~seen & bg)
    return filled, int((~mask & filled).sum())


def despill(rgb: np.ndarray, alpha: np.ndarray) -> np.ndarray:
    """去绿边：alpha 内的绿味像素把绿通道压到 (r+b)/2 附近。"""
    out = rgb.astype(np.float32).copy()
    r, g, b = out[..., 0], out[..., 1], out[..., 2]
    limit = (r + b) / 2.0 + 12.0
    greenish = (g > limit) & (alpha > 0.02)
    out[..., 1] = np.where(greenish, np.minimum(g, limit), g)
    return out.astype(np.uint8)


def main() -> int:
    ap = argparse.ArgumentParser(description="绿幕图归一化到项目标准落位")
    ap.add_argument("raw")
    ap.add_argument("--out", required=True)
    ap.add_argument("--json", default=None)
    ap.add_argument("--target-h", type=int, default=TARGET_H)
    ap.add_argument("--foot-y", type=int, default=TARGET_FOOT_Y)
    ap.add_argument("--center-x", type=float, default=TARGET_CENTER_X)
    ap.add_argument("--bg-rgb", default=None,
                    help="背景色 'r,g,b'（不给则取画布四边中位色；渐变底或裁切件建议显式指定）")
    args = ap.parse_args()

    raw = pathlib.Path(args.raw)
    out_path = pathlib.Path(args.out)
    json_path = pathlib.Path(args.json) if args.json else out_path.with_suffix(".layout.json")

    img = Image.open(raw).convert("RGB")
    arr = np.asarray(img).astype(np.int16)
    if args.bg_rgb:
        bg_color = tuple(int(v) for v in args.bg_rgb.split(","))
    else:
        border = []
        for x in range(img.width):
            border.append(tuple(arr[0, x][:3])); border.append(tuple(arr[-1, x][:3]))
        for y in range(img.height):
            border.append(tuple(arr[y, 0][:3])); border.append(tuple(arr[y, -1][:3]))
        border.sort()
        bg_color = tuple(border[len(border) // 2][i] for i in range(3))
    d = np.linalg.norm(arr.astype(np.float32) - np.array(bg_color, dtype=np.float32), axis=2)
    mask = d > GREEN_TOL
    if not mask.any():
        raise SystemExit("绿幕抠图失败：没有前景像素")
    mask, holes = fill_holes(mask)
    ys, xs = np.nonzero(mask)
    x0, y0, x1, y1 = int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())
    subj_w, subj_h = x1 - x0 + 1, y1 - y0 + 1

    scale = args.target_h / float(subj_h)
    new_w = max(1, int(round(subj_w * scale)))
    new_h = max(1, int(round(subj_h * scale)))

    # 前景抠成软 alpha，RGB 同步缩放后再去绿边
    crop_rgb = Image.fromarray(arr.astype(np.uint8)).crop((x0, y0, x1 + 1, y1 + 1)).resize(
        (new_w, new_h), Image.LANCZOS)
    crop_a = Image.fromarray((mask[y0:y1 + 1, x0:x1 + 1] * 255).astype(np.uint8)).resize(
        (new_w, new_h), Image.LANCZOS)
    rgb2 = np.asarray(crop_rgb)
    a2 = np.asarray(crop_a).astype(np.float32) / 255.0
    rgb2 = despill(rgb2, a2)

    # 硬 alpha 贴图（避免半透明边缘产生绿晕）
    hard = a2 >= 0.5
    layer = np.where(hard[..., None], rgb2, np.array(GREEN, dtype=np.uint8)).astype(np.uint8)

    canvas = Image.new("RGB", (CANVAS, CANVAS), GREEN)
    paste_x = int(round(args.center_x - new_w / 2.0))
    paste_y = args.foot_y - new_h + 1
    canvas.paste(Image.fromarray(layer), (paste_x, paste_y))
    canvas.save(out_path)

    a = np.asarray(canvas).astype(np.int16)
    dd = np.linalg.norm(a.astype(np.float32) - np.array(GREEN, dtype=np.float32), axis=2)
    m = dd > GREEN_TOL
    ys2, xs2 = np.nonzero(m)
    corners = np.concatenate([
        a[0:64, 0:64].reshape(-1, 3), a[0:64, -64:].reshape(-1, 3),
        a[-64:, 0:64].reshape(-1, 3), a[-64:, -64:].reshape(-1, 3)], axis=0)
    corner_median = [int(v) for v in np.median(corners, axis=0)]
    corner_dev = float(np.abs(corners - np.array(corner_median)).max())

    info = {
        "source": str(raw),
        "source_size": list(img.size),
        "source_background_color": list(bg_color),
        "green_tolerance": GREEN_TOL,
        "filled_holes_px": holes,
        "cut": {"bbox": [x0, y0, x1, y1], "subject_w": subj_w, "subject_h": subj_h},
        "scale": round(scale, 5),
        "composited": {
            "path": str(out_path),
            "canvas": [CANVAS, CANVAS],
            "green": list(GREEN),
            "paste_xy": [paste_x, paste_y],
            "bbox": [int(xs2.min()), int(ys2.min()), int(xs2.max()), int(ys2.max())],
            "subject_w": int(xs2.max() - xs2.min() + 1),
            "subject_h": int(ys2.max() - ys2.min() + 1),
            "visible_bottom_y": int(ys2.max()),
            "center_x": round(float((xs2.min() + xs2.max()) / 2.0), 1),
        },
        "green_purity": {"corner_median": corner_median,
                         "corner_max_channel_deviation": round(corner_dev, 2)},
        "target": {"subject_h": args.target_h, "foot_y": args.foot_y, "center_x": args.center_x},
    }
    json_path.write_text(json.dumps(info, indent=2, ensure_ascii=False), encoding="utf-8")
    print(json.dumps(info, indent=2, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
