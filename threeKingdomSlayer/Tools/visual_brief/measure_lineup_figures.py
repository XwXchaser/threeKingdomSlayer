# -*- coding: utf-8 -*-
"""五人并排同族验证图量测（1011 / 1012 / 1013 / 1051 / 1021）。

沿用 plan/enemy1051-bandit-archer-redesign-handoff.md §9.3/§9.4 的口径与教训：
  · 4 邻接 BFS 的邻居表必须是 (1,0)/(-1,0)/(0,1)/(0,-1)（crop_lineup_1013.py 第 62 行写错过）
  · 相邻角色的道具会 x 区间重叠，不能靠 x 窗口选人；一个连通域 = 一个人
  · 头顶 != 最高像素：道具（旗杆 / 弓 / 盾）会更高，头顶取「腿中心列 ±W 内的最高像素」
  · 背景半透明光晕在 alpha ≤64，主体在 129-253，用阈值 128 二值化可断开背景

输出：JSON 量测 + PNG 标注对照图。
用法：
    python Tools/visual_brief/measure_lineup_figures.py \
        --image C:/Users/steam/Pictures/gptGen/enemy_proportion_lineup_v2_1021/enemy_proportion_lineup_v2_1021.png \
        --out-dir C:/Users/steam/Pictures/gptGen/enemy_proportion_lineup_v2_1021
"""
from __future__ import annotations

import argparse
import json
import os
import sys

try:
    from PIL import Image, ImageDraw
except ImportError:  # pragma: no cover
    print("PIL required", file=sys.stderr)
    raise

ALPHA_MIN = 128         # 可见阈值
MIN_AREA = 1000         # 连通域最小面积，滤掉噪点
BODY_WINDOW = 60        # 头顶判定窗口（腿中心列 ±N）
MIN_HEAD_WIDTH = 24     # 判定"头顶"所需的窗口内连续可见段最小宽度（排除旗杆/弓尖等细杆）
MAX_HEAD_OVERHANG = 40  # 超出窗口多少像素就认为该行是道具（红缨/旗面）而不是人体
LEG_BAND = 0.25         # 取底部 25% 行来求腿中心
NEIGHBORS = ((1, 0), (-1, 0), (0, 1), (0, -1))


def components(mask, w, h):
    """4 邻接连通域，返回 [pixels] 列表。"""
    seen = bytearray(w * h)
    out = []
    for y in range(h):
        row = y * w
        for x in range(w):
            idx = row + x
            if not mask[idx] or seen[idx]:
                continue
            stack = [idx]
            seen[idx] = 1
            pix = []
            while stack:
                cur = stack.pop()
                pix.append(cur)
                cy, cx = divmod(cur, w)
                for dx, dy in NEIGHBORS:
                    nx, ny = cx + dx, cy + dy
                    if 0 <= nx < w and 0 <= ny < h:
                        nidx = ny * w + nx
                        if mask[nidx] and not seen[nidx]:
                            seen[nidx] = 1
                            stack.append(nidx)
            out.append(pix)
    return out


def measure(pix, w, h):
    xs = [p % w for p in pix]
    ys = [p // w for p in pix]
    x0, x1, y0, y1 = min(xs), max(xs), min(ys), max(ys)

    band_y0 = int(y1 - (y1 - y0) * LEG_BAND)
    leg_xs = [p % w for p in pix if (p // w) >= band_y0]
    if not leg_xs:
        leg_xs = xs
    leg_center = sum(leg_xs) / len(leg_xs)

    lo, hi = int(leg_center - BODY_WINDOW), int(leg_center + BODY_WINDOW)
    # 头顶：从上往下找第一行，使腿中心窗口内的连续可见段 ≥ MIN_HEAD_WIDTH，
    # 且该段不越出窗口 MAX_HEAD_OVERHANG 像素（否则是红缨/旗面等道具，不是人体）
    head_top = None
    for y in range(y0, y1 + 1):
        row = sorted(set(p % w for p in pix if (p // w) == y))
        if not row:
            continue
        runs = []
        s = prev = row[0]
        for x in row[1:] + [None]:
            if x is not None and x == prev + 1:
                prev = x
                continue
            runs.append((s, prev))
            if x is not None:
                s = prev = x
        best = None
        for r0, r1 in runs:
            ov = min(r1, hi) - max(r0, lo) + 1
            if ov > 0 and (best is None or ov > best[0]):
                best = (ov, r0, r1)
        if best is None:
            continue
        ov, r0, r1 = best
        overhang = (r0 < lo - MAX_HEAD_OVERHANG) or (r1 > hi + MAX_HEAD_OVERHANG)
        if ov >= MIN_HEAD_WIDTH and not overhang:
            head_top = y
            break
    foot_bottom = y1
    body_h = (foot_bottom - head_top) if head_top is not None else None

    # 盾侧判定：躯干带像素质心 相对 头部带像素质心（头部带不受侧向大道具影响）
    def band_centroid(a, b):
        rows = [p // w for p in pix]
        sel = [p % w for p in pix if y0 + body_h * a <= (p // w) <= y0 + body_h * b]
        return (sum(sel) / len(sel)) if sel else None

    head_cx = band_centroid(0.02, 0.24)
    torso_cx = band_centroid(0.35, 0.65)
    side = None
    if head_cx is not None and torso_cx is not None:
        side = "shield_on_picture_LEFT" if torso_cx < head_cx else "shield_on_picture_RIGHT"

    return {
        "bbox": [x0, y0, x1, y1],
        "leg_center_x": round(leg_center, 1),
        "head_top_y": head_top,
        "foot_bottom_y": foot_bottom,
        "body_height_px": body_h,
        "total_width_px": x1 - x0,
        "total_height_px": y1 - y0,
        "head_band_centroid_x": round(head_cx, 1) if head_cx is not None else None,
        "torso_band_centroid_x": round(torso_cx, 1) if torso_cx is not None else None,
        "shield_side_vs_body": side,
        "pixels": len(pix),
    }


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--image", required=True)
    ap.add_argument("--out-dir", required=True)
    ap.add_argument("--labels", default="1011,1012,1013,1051,1021")
    args = ap.parse_args()

    im = Image.open(args.image).convert("RGBA")
    w, h = im.size
    alpha = im.getchannel("A")
    mask = bytearray(1 if v > ALPHA_MIN else 0 for v in alpha.getdata())

    comps = [c for c in components(mask, w, h) if len(c) >= MIN_AREA]
    comps.sort(key=lambda c: min(p % w for p in c))
    labels = [s.strip() for s in args.labels.split(",")]

    result = {"image": args.image, "size": [w, h], "figure_count": len(comps),
              "components": len(comps), "figures": []}
    for i, pix in enumerate(comps):
        m = measure(pix, w, h)
        m["index"] = i + 1
        m["label"] = labels[i] if i < len(labels) else "?"
        result["figures"].append(m)

    base = None
    for m in result["figures"]:
        if m["label"] == "1011" and m["body_height_px"]:
            base = m["body_height_px"]
    for m in result["figures"]:
        m["body_height_vs_1011"] = (round(m["body_height_px"] / base, 3)
                                    if base and m["body_height_px"] else None)

    # 折算到游戏：普通兵人物本体 605 px；五列阵型列距 2.6 世界单位 = 273 px
    GAME_BODY = 605
    COLUMN_PX = 273
    result["game_projection"] = {
        "normal_body_px": GAME_BODY,
        "column_spacing_px": COLUMN_PX,
        "note": "scale = 605 / 本图人物本体高；width_in_columns = 折算总宽 / 273",
    }
    for m in result["figures"]:
        if not m["body_height_px"]:
            continue
        k = GAME_BODY / m["body_height_px"]
        w_game = m["total_width_px"] * k
        m["game_scale_k"] = round(k, 3)
        m["game_total_width_px"] = round(w_game, 1)
        m["game_width_in_columns"] = round(w_game / COLUMN_PX, 2)

    os.makedirs(args.out_dir, exist_ok=True)
    json_path = os.path.join(args.out_dir, "lineup_v2_1021_measure.json")
    with open(json_path, "w", encoding="utf-8") as fh:
        json.dump(result, fh, indent=2, ensure_ascii=False)

    ann = im.copy()
    dr = ImageDraw.Draw(ann)
    palette = [(200, 40, 40), (40, 160, 60), (60, 90, 220), (220, 140, 20), (150, 60, 200)]
    for i, m in enumerate(result["figures"]):
        x0, y0, x1, y1 = m["bbox"]
        col = palette[i % len(palette)]
        dr.rectangle([x0, y0, x1, y1], outline=col, width=3)
        dr.text((x0 + 4, max(0, y0 - 16)), "%d %s" % (m["index"], m["label"]), fill=col)
        if m["head_top_y"] is not None:
            cx = m["leg_center_x"]
            dr.line([cx - BODY_WINDOW, m["head_top_y"], cx + BODY_WINDOW, m["head_top_y"]],
                    fill=(255, 255, 0), width=2)
            dr.line([cx, m["head_top_y"], cx, m["foot_bottom_y"]], fill=(255, 255, 0), width=1)
    ann_path = os.path.join(args.out_dir, "lineup_v2_1021_annotated.png")
    ann.save(ann_path)

    print(json.dumps(result, indent=2, ensure_ascii=False))
    print("\nJSON: %s" % json_path)
    print("标注图: %s" % ann_path)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
