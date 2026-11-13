# -*- coding: utf-8 -*-
"""Dead 并排组图量测（2×2 四格，背景非绿幕时用梯度泛洪分背景）。

依据：Locus/knowledge/plan/enemy-dead-group-reference-20261109.md §6.1
口径（与该文档 §2 复测口径一致）：
  · 4 邻接 BFS 连通块；身体块 = 去掉武器块后的最大块；质心按像素加权
  · 身体块宽高比 = (x1-x0)/(y1-y0)；顶/底 = 身体块 bbox 占**整张画布**高度的百分比
  · 武器相对位置 = 武器块质心 - 身体块质心（dx, dy）
  · 离地 = 身体块底到「地线」的像素距离；工程内已部署 Dead 帧的地线 = y=895（1108² 画布），
    组图没有地面，地线 = 画布底边（y=1024），两者不是同一种距离，报告里分开写
  · 分背景：绿幕按底色距离；深色渐变底用「从画布边界泛洪、逐像素与邻居色差 ≤ tol」的梯度泛洪

用法：
    python Tools/visual_brief/measure_dead_group.py \
        --image C:/Users/steam/Pictures/gptGen/enemy_dead_group_v1/enemy_dead_group_v1.png \
        --out-dir C:/Users/steam/Pictures/gptGen/enemy_dead_group_v1 \
        --labels 1011,1012,1013,1051 --grid 2x2
    python Tools/visual_brief/measure_dead_group.py --alpha-images A.png B.png   # 对照原帧
"""
from __future__ import annotations

import argparse
import json
import os
import sys

import numpy as np
from PIL import Image, ImageDraw

ALPHA_MIN = 128
MIN_AREA = 800
NEIGHBORS = ((1, 0), (-1, 0), (0, 1), (0, -1))


def components(mask: np.ndarray) -> list:
    """4 邻接连通块（numpy bool 数组），返回 [像素索引数组] 列表。"""
    h, w = mask.shape
    seen = np.zeros((h, w), dtype=bool)
    out = []
    for y0 in range(h):
        for x0 in range(w):
            if not mask[y0, x0] or seen[y0, x0]:
                continue
            stack = [(x0, y0)]
            seen[y0, x0] = True
            pix = []
            while stack:
                x, y = stack.pop()
                pix.append((x, y))
                for dx, dy in NEIGHBORS:
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < w and 0 <= ny < h and mask[ny, nx] and not seen[ny, nx]:
                        seen[ny, nx] = True
                        stack.append((nx, ny))
            out.append(np.array(pix, dtype=np.int32))
    return out


def flood_background(rgb: np.ndarray, tol: float) -> np.ndarray:
    """从画布边界泛洪出背景：逐像素与已标记邻居的色差 ≤ tol 才算背景。"""
    h, w, _ = rgb.shape
    bg = np.zeros((h, w), dtype=bool)
    stack = []
    for x in range(w):
        stack.append((x, 0))
        stack.append((x, h - 1))
    for y in range(h):
        stack.append((0, y))
        stack.append((w - 1, y))
    for p in stack:
        bg[p[1], p[0]] = True
    changed = True
    while changed:
        changed = False
        # 逐行扫描扩张（够用且快）
        for y in range(h):
            row = bg[y]
            prev = np.where(row[:-1], rgb[y, :-1], np.float32(1e9))
            nxt = np.where(row[1:], rgb[y, 1:], np.float32(1e9))
            d = np.linalg.norm(rgb[y] - np.minimum(prev, nxt)[:, None, :], axis=2) \
                if False else np.linalg.norm(rgb[y] - rgb[y], axis=2)
            cand = (d <= tol) & (~row)
            cand[0] = False
            cand[-1] = False
            if cand.any():
                row[:] = row | cand
                changed = True
    return bg


def flood_background_fast(rgb: np.ndarray, tol: float) -> np.ndarray:
    """迭代式梯度泛洪（逐像素扩散，邻居色差 ≤ tol），比逐行扫描稳健。"""
    h, w, _ = rgb.shape
    bg = np.zeros((h, w), dtype=bool)
    stack = []
    for x in range(w):
        stack.append((x, 0))
        stack.append((x, h - 1))
    for y in range(h):
        stack.append((0, y))
        stack.append((w - 1, y))
    for x, y in stack:
        bg[y, x] = True
    while stack:
        x, y = stack.pop()
        c = rgb[y, x].astype(np.float32)
        for dx, dy in NEIGHBORS:
            nx, ny = x + dx, y + dy
            if 0 <= nx < w and 0 <= ny < h and not bg[ny, nx]:
                if np.linalg.norm(rgb[ny, nx].astype(np.float32) - c) <= tol:
                    bg[ny, nx] = True
                    stack.append((nx, ny))
    return bg


def measure_figure(pix: np.ndarray, w: int, h: int, canvas_h: int, ground_y: int,
                   label: str) -> dict:
    xs, ys = pix[:, 0], pix[:, 1]
    x0, x1, y0, y1 = int(xs.min()), int(xs.max()), int(ys.min()), int(ys.max())
    bw, bh = x1 - x0, y1 - y0
    cx, cy = float(xs.mean()), float(ys.mean())
    return {
        "label": label,
        "bbox": [x0, y0, x1, y1],
        "block_w": bw, "block_h": bh,
        "aspect_w_over_h": round(bw / bh, 3) if bh else None,
        "body_top_pct_of_canvas": round(y0 / canvas_h * 100, 1),
        "body_bottom_pct_of_canvas": round(y1 / canvas_h * 100, 1),
        "centroid": [round(cx, 1), round(cy, 1)],
        "gap_to_ground_px": int(ground_y - y1),
        "gap_to_ground_over_body_h": round((ground_y - y1) / bh, 3) if bh else None,
        "pixels": int(len(pix)),
    }


def measure_quadrant(rgb: np.ndarray, fg: np.ndarray, x0: int, y0: int, x1: int, y1: int,
                     canvas_h: int, label: str, ground_y: int) -> dict:
    sub = fg[y0:y1, x0:x1]
    comps = [c for c in components(sub) if len(c) >= MIN_AREA]
    comps.sort(key=lambda c: -len(c))
    if not comps:
        return {"label": label, "error": "no foreground"}
    body = comps[0]
    body_m = measure_figure(body, x1 - x0, y1 - y0, canvas_h, ground_y, label)
    body_m["component_count"] = len(comps)
    # 武器块 = 与身体块分离的次大连通块
    if len(comps) > 1:
        wblock = comps[1]
        wx, wy = float(wblock[:, 0].mean()), float(wblock[:, 1].mean())
        body_m["weapon"] = {
            "bbox_in_quadrant": [int(wblock[:, 0].min()), int(wblock[:, 1].min()),
                                 int(wblock[:, 0].max()), int(wblock[:, 1].max())],
            "dx_vs_body_centroid": round(wx - body_m["centroid"][0], 1),
            "dy_vs_body_centroid": round(wy - body_m["centroid"][1], 1),
            "pixels": int(len(wblock)),
        }
    return body_m


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--image", default="")
    ap.add_argument("--out-dir", default="")
    ap.add_argument("--labels", default="1011,1012,1013,1051")
    ap.add_argument("--grid", default="2x2")
    ap.add_argument("--tol", type=float, default=26.0)
    ap.add_argument("--bg-mode", default="auto", choices=["auto", "green", "flood"])
    ap.add_argument("--alpha-images", default="", help="逗号分隔的 RGBA 对照帧")
    ap.add_argument("--alpha-ground-y", type=int, default=895)
    args = ap.parse_args()

    result = {"generated_by": "Tools/visual_brief/measure_dead_group.py"}

    if args.alpha_images:
        ref = []
        for path in args.alpha_images.split(","):
            path = path.strip()
            if not path:
                continue
            im = Image.open(path).convert("RGBA")
            w, h = im.size
            a = np.asarray(im.getchannel("A"))
            mask = a >= ALPHA_MIN
            comps = [c for c in components(mask) if len(c) >= MIN_AREA]
            comps.sort(key=lambda c: -len(c))
            item = {"file": path, "size": [w, h], "components": len(comps), "figures": []}
            if comps:
                body = measure_figure(comps[0], w, h, h, args.alpha_ground_y,
                                      os.path.basename(path))
                body["ground_y_assumed"] = args.alpha_ground_y
                if len(comps) > 1:
                    wb = comps[1]
                    body["weapon"] = {
                        "bbox": [int(wb[:, 0].min()), int(wb[:, 1].min()),
                                 int(wb[:, 0].max()), int(wb[:, 1].max())],
                        "dx_vs_body_centroid": round(float(wb[:, 0].mean()) - body["centroid"][0], 1),
                        "dy_vs_body_centroid": round(float(wb[:, 1].mean()) - body["centroid"][1], 1),
                        "pixels": int(len(wb)),
                    }
                item["figures"].append(body)
            ref.append(item)
        result["reference_frames"] = ref

    if args.image:
        im = Image.open(args.image).convert("RGB")
        w, h = im.size
        arr = np.asarray(im).astype(np.float32)
        corners = np.concatenate([arr[:40, :40].reshape(-1, 3), arr[:40, -40:].reshape(-1, 3),
                                  arr[-40:, :40].reshape(-1, 3), arr[-40:, -40:].reshape(-1, 3)])
        mode = args.bg_mode
        bg_rgb = corners.mean(axis=0)
        if mode == "auto":
            greenish = abs(bg_rgb[0] - 0) < 60 and abs(bg_rgb[1] - 159) < 60 and abs(bg_rgb[2] - 62) < 60
            mode = "green" if greenish else "flood"
        if mode == "green":
            d = np.linalg.norm(arr - np.array([0, 159, 62], dtype=np.float32), axis=2)
            fg = d > 79
        else:
            bg = flood_background_fast(arr, args.tol)
            fg = ~bg
            # 噪点由连通块面积阈值（MIN_AREA）过滤，不做腐蚀，保持与 alpha 帧同一口径
        gy, gx = (int(v) for v in args.grid.lower().split("x"))
        labels = [s.strip() for s in args.labels.split(",")]
        cells = []
        for iy in range(gy):
            for ix in range(gx):
                idx = iy * gx + ix
                cx0 = int(round(ix * w / gx))
                cx1 = int(round((ix + 1) * w / gx))
                cy0 = int(round(iy * h / gy))
                cy1 = int(round((iy + 1) * h / gy))
                label = labels[idx] if idx < len(labels) else "cell%d" % (idx + 1)
                m = measure_quadrant(arr, fg, cx0, cy0, cx1, cy1, h, label, cy1)
                m["cell"] = {"x": [cx0, cx1], "y": [cy0, cy1]}
                m["ground_y_used"] = cy1
                cells.append(m)
        result["group_image"] = {
            "file": args.image, "size": [w, h], "bg_mode": mode,
            "bg_corner_mean_rgb": [round(float(v), 1) for v in bg_rgb],
            "foreground_pixels": int(fg.sum()),
            "grid": args.grid, "cells": cells,
        }

        ann = Image.open(args.image).convert("RGB")
        dr = ImageDraw.Draw(ann)
        palette = [(220, 60, 60), (60, 180, 80), (70, 110, 230), (230, 150, 30)]
        for i, m in enumerate(result["group_image"]["cells"]):
            if "bbox" not in m:
                continue
            col = palette[i % len(palette)]
            x0, y0, x1, y1 = m["bbox"]
            ox, oy = m["cell"]["x"][0], m["cell"]["y"][0]
            dr.rectangle([ox + x0, oy + y0, ox + x1, oy + y1], outline=col, width=3)
            dr.text((ox + x0 + 4, oy + max(0, y0 - 18)), "%s asp=%s" % (m["label"], m["aspect_w_over_h"]),
                    fill=col)
        if args.out_dir:
            os.makedirs(args.out_dir, exist_ok=True)
            ann.save(os.path.join(args.out_dir, "enemy_dead_group_v1_annotated.png"))

    print(json.dumps(result, indent=2, ensure_ascii=False))
    if args.out_dir:
        os.makedirs(args.out_dir, exist_ok=True)
        p = os.path.join(args.out_dir, "enemy_dead_group_v1_measure.json")
        with open(p, "w", encoding="utf-8") as fh:
            json.dump(result, fh, indent=2, ensure_ascii=False)
        print("\nJSON: %s" % p)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
