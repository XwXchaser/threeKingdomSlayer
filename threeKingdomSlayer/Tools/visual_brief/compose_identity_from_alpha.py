# -*- coding: utf-8 -*-
"""原生透明身份图 → 项目标准绿幕身份图（落位口径 1024² / 绿(0,159,62) / 主体高 696 / 脚底 895 / 中心 474.5）。

为什么需要：gpt-image 有时直接给原生透明 RGBA（不是绿幕），
`compose_identity_greenscreen.py` 走的是"绿幕抠图"，对透明图不适用。
本脚本改用 alpha 通道做 mask：alpha>=128 记为实，<128 记为透明；填掉封闭孔后
缩放到主体高 696、贴 (474.5-w/2, 895-h)，再合成到纯绿幕上。

同时输出量测：
  · 主体 bbox / 可见脚底线 / 中心列
  · 绿幕纯净度（四角中位与 std）
  · 盾侧判定（躯干带像素质心 相对 腿中心列 的左右）——用来验证"盾在画面左、人物右手"

用法
    python Tools/visual_brief/compose_identity_from_alpha.py <raw.png> [--out path] [--json path]
"""
from __future__ import annotations

import argparse
import json
import pathlib
from collections import deque

import numpy as np
from PIL import Image

GREEN = (0, 159, 62)
TARGET_H = 696
TARGET_FOOT_Y = 895
TARGET_CENTER_X = 474.5
CANVAS = 1024
ALPHA_THR = 128


def fill_holes(mask: np.ndarray) -> np.ndarray:
    """把被主体包围的封闭透明孔填实（洪泛法：从边界向内找背景，其余视为孔）。"""
    h, w = mask.shape
    seen = np.zeros((h, w), dtype=bool)
    q = deque()
    for x in range(w):
        for y in (0, h - 1):
            if not mask[y, x] and not seen[y, x]:
                seen[y, x] = True
                q.append((x, y))
    for y in range(h):
        for x in (0, w - 1):
            if not mask[y, x] and not seen[y, x]:
                seen[y, x] = True
                q.append((x, y))
    while q:
        x, y = q.popleft()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = x + dx, y + dy
            if 0 <= nx < w and 0 <= ny < h and not seen[ny, nx] and not mask[ny, nx]:
                seen[ny, nx] = True
                q.append((nx, ny))
    holes = (~mask) & (~seen)
    out = mask.copy()
    out[holes] = True
    return out, int(holes.sum())


def shield_side(mask: np.ndarray, bbox, foot_y: int) -> dict:
    """盾侧判定：躯干带像素质心 相对 头部带像素质心 的左右。

    头部带（顶部 22%）只含头与布帽，不受侧向大道具影响；
    躯干带（35%~65%）会把竖盾/旗面算进来。盾在画面左 → 躯干质心在头质心左侧。
    （不能用"底部 25% 腿带"：盾的下缘也在那一带，会把两边质心抵消。）
    """
    x0, y0, x1, y1 = bbox
    body_h = y1 - y0

    def band_cx(a, b):
        seg = mask[int(y0 + body_h * a):int(y0 + body_h * b), :]
        _, xs = np.nonzero(seg)
        return float(xs.mean()) if len(xs) else float("nan")

    head_cx = band_cx(0.02, 0.24)
    torso_cx = band_cx(0.35, 0.65)
    side = None
    if head_cx == head_cx and torso_cx == torso_cx:
        side = "shield_on_picture_LEFT" if torso_cx < head_cx else "shield_on_picture_RIGHT"
    return {"head_band_centroid_x": round(head_cx, 1),
            "torso_band_centroid_x": round(torso_cx, 1),
            "delta_x": round(torso_cx - head_cx, 1),
            "shield_side": side}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("raw")
    ap.add_argument("--out", default=None)
    ap.add_argument("--json", default=None)
    ap.add_argument("--target-h", type=int, default=TARGET_H)
    ap.add_argument("--foot-y", type=int, default=TARGET_FOOT_Y)
    ap.add_argument("--center-x", type=float, default=TARGET_CENTER_X)
    ap.add_argument("--canvas-h", type=int, default=CANVAS)
    args = ap.parse_args()

    raw = pathlib.Path(args.raw)
    out_path = pathlib.Path(args.out) if args.out else raw.with_name(raw.stem + "_greenscreen_1024.png")
    json_path = pathlib.Path(args.json) if args.json else out_path.with_suffix(".layout.json")

    img = Image.open(raw).convert("RGBA")
    arr = np.asarray(img)
    alpha = arr[:, :, 3]
    mask = alpha >= ALPHA_THR
    if not mask.any():
        raise SystemExit("alpha 抠图失败：没有可见像素")

    mask, holes = fill_holes(mask)
    ys, xs = np.nonzero(mask)
    bbox = [int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())]
    subj_w = bbox[2] - bbox[0] + 1
    subj_h = bbox[3] - bbox[1] + 1

    scale = args.target_h / float(subj_h)
    new_w = max(1, int(round(subj_w * scale)))
    new_h = max(1, int(round(subj_h * scale)))
    rgba = Image.fromarray(arr, mode="RGBA")
    crop = rgba.crop((bbox[0], bbox[1], bbox[2] + 1, bbox[3] + 1)).resize((new_w, new_h), Image.LANCZOS)
    crop_arr = np.asarray(crop).copy()
    # 缩放后重新按 alpha 二值化（保留硬边，避免半透明混进绿幕）
    crop_arr[:, :, 3] = np.where(crop_arr[:, :, 3] >= ALPHA_THR, 255, 0)

    canvas = Image.new("RGBA", (CANVAS, args.canvas_h), GREEN + (255,))
    paste_x = int(round(args.center_x - new_w / 2.0))
    paste_y = args.foot_y - new_h + 1
    canvas.alpha_composite(crop, (paste_x, paste_y))
    flat = canvas.convert("RGB")
    out_path.parent.mkdir(parents=True, exist_ok=True)
    flat.save(out_path)

    # 量测
    carr = np.asarray(flat).astype(np.int16)
    d = np.linalg.norm(carr.astype(np.float32) - np.array(GREEN, dtype=np.float32), axis=2)
    m = d > 79
    ys2, xs2 = np.nonzero(m)
    nb = [int(xs2.min()), int(ys2.min()), int(xs2.max()), int(ys2.max())]
    corners = np.concatenate([
        carr[0:64, 0:64].reshape(-1, 3), carr[0:64, -64:].reshape(-1, 3),
        carr[-64:, 0:64].reshape(-1, 3), carr[-64:, -64:].reshape(-1, 3)], axis=0)

    # 盾侧：用合成后的 mask 判定
    cmask = m.copy()
    side = shield_side(cmask, nb, int(ys2.max()))

    info = {
        "source": str(raw),
        "source_size": list(img.size),
        "alpha_threshold": ALPHA_THR,
        "filled_holes_px": holes,
        "cut": {"bbox": bbox, "subject_w": subj_w, "subject_h": subj_h},
        "scale": round(scale, 5),
        "composited": {
            "path": str(out_path),
            "canvas": [CANVAS, args.canvas_h],
            "green": list(GREEN),
            "paste_xy": [paste_x, paste_y],
            "bbox": nb,
            "subject_w": int(xs2.max() - xs2.min() + 1),
            "subject_h": int(ys2.max() - ys2.min() + 1),
            "visible_bottom_y": int(ys2.max()),
            "center_x": round(float((xs2.min() + xs2.max()) / 2.0), 1),
        },
        "green_purity": {
            "corner_median": [int(v) for v in np.median(corners, axis=0)],
            "corner_std": [round(float(v), 2) for v in corners.std(axis=0)],
        },
        "shield_side_check": side,
        "target": {"subject_h": args.target_h, "foot_y": args.foot_y, "center_x": args.center_x},
    }
    json_path.write_text(json.dumps(info, indent=2, ensure_ascii=False), encoding="utf-8")
    print(json.dumps(info, indent=2, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
