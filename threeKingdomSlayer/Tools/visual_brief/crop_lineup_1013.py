# -*- coding: utf-8 -*-
"""四兵种并排参考图 → 1013 旗手「研究参考裁切」+ 比例量测。

用途
    把 `enemy_proportion_lineup_v1.png` 里的第三位（1013 旗手，含旗杆与旗面）
    从整张并排图中隔离出来，产出一张带透明通道的独立裁切，并量测：
        · 角色身（头顶→脚底）高度、旗杆总高（杆顶→脚底）、两者的比值
        · 与同图 1011 重甲兵（已验收基准）的身高等高对照
    另出一张「1011 vs 1013 等高对照图」，供人工目视复核比例。

边界（务必遵守）
    * 本脚本只做**研究参考裁切**，产出物**不是游戏素材**：
      不得直接当 Sprite / 身份定稿 / Unity 动画帧使用。
      并排图本身是比例研究图（见 plan/enemy-commander-buff-unit-plan.md §2.15）。
    * 不改原图，不覆盖任何既有产物。

用法
    python Tools/visual_brief/crop_lineup_1013.py \
        --src "C:/Users/steam/Pictures/gptGen/enemy_proportion_lineup_v1/enemy_proportion_lineup_v1.png" \
        --out-dir "C:/Users/steam/Pictures/gptGen/enemy1013_idle_ref_v1"
"""
from __future__ import annotations

import argparse
import hashlib
import json
import pathlib
from collections import deque

import numpy as np
from PIL import Image

ALPHA_T = 16          # 参与连通域分析的 alpha 阈值
GREEN = (0, 159, 62)  # 绿幕制作底（仅用于锚图合成时参考，不在本脚本内合成）


def sha256_of(path: pathlib.Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def components(mask: np.ndarray):
    """4 邻接连通域，返回 [(pixel_count, (x0, y0, x1, y1))]，按 x0 排序。"""
    h, w = mask.shape
    seen = np.zeros_like(mask, dtype=bool)
    out = []
    for y in range(h):
        for x in range(w):
            if not mask[y, x] or seen[y, x]:
                continue
            q = deque([(y, x)])
            seen[y, x] = True
            x0 = x1 = x
            y0 = y1 = y
            n = 0
            while q:
                cy, cx = q.popleft()
                n += 1
                x0, x1 = min(x0, cx), max(x1, cx)
                y0, y1 = min(y0, cy), max(y1, cy)
                for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    ny, nx = cy + dy, cx + dx
                    if 0 <= ny < h and 0 <= nx < w and mask[ny, nx] and not seen[ny, nx]:
                        seen[ny, nx] = True
                        q.append((ny, nx))
            out.append((n, (x0, y0, x1, y1)))
    out.sort(key=lambda c: c[1][0])
    return out, seen


def longest_run(col_has: np.ndarray) -> int:
    best = cur = 0
    for v in col_has:
        cur = cur + 1 if v else 0
        best = max(best, cur)
    return best


def measure(mask: np.ndarray):
    """在单个角色的连通域内量测：旗杆列、头顶 y、身高、总高、旗面宽。

    头顶识别：取身体中线带（±95px）内「有像素的行」的**最后一段连续行**的起点。
    1013 的旗面在中线带内是独立的一段（与身体之间有空白行），因此最后一段＝身体；
    1011 的剑在最左侧，不进入中线带，最后一段＝头盔→脚底。
    """
    h, w = mask.shape
    ys, xs = np.nonzero(mask)
    x0, y0, x1, y1 = int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())
    foot_y = y1

    # 旗杆 = 纵向连续覆盖接近全高的窄列
    runs = np.array([longest_run(mask[:, x]) for x in range(w)])
    pole_cols = np.nonzero(runs >= 0.90 * h)[0]
    pole = [int(pole_cols.min()), int(pole_cols.max())] if len(pole_cols) else None

    # 下身（脚踝附近 40~140px）求身体中线，排除旗杆列
    lower = mask[max(0, foot_y - 140):max(1, foot_y - 39), :].copy()
    if pole:
        lower[:, pole[0] - 1:pole[1] + 2] = False
    lcols = np.nonzero(lower.any(axis=0))[0]
    center_x = int((lcols.min() + lcols.max()) / 2) if len(lcols) else (x0 + x1) // 2

    lo = max(0, center_x - 95)
    hi = min(w, center_x + 96)
    band = mask[:, lo:hi].copy()
    if pole:
        band[:, max(0, pole[0] - 1 - lo):max(0, pole[1] + 2 - lo)] = False
    counts = band.sum(axis=1)
    has = counts >= 5
    seg_start, head_y = None, None
    for i in range(h):
        if has[i]:
            if seg_start is None:
                seg_start = i
        else:
            if seg_start is not None:
                head_y = seg_start          # 每段结束时更新 → 最终留下的是最后一段
                seg_start = None
    if seg_start is not None:
        head_y = seg_start
    body_h = (foot_y - head_y + 1) if head_y is not None else None
    total_h = foot_y - y0 + 1

    # 旗面（头顶以上的布，不含杆列）
    top_end = head_y if head_y else y0 + 1
    flag_rows = mask[y0:max(y0 + 1, top_end), :].copy()
    if pole:
        flag_rows[:, pole[0] - 1:pole[1] + 2] = False
    fys, fxs = np.nonzero(flag_rows)
    flag_bbox = [int(fxs.min()), int(fys.min()), int(fxs.max()), int(fys.max())] if len(fxs) else None
    flag_w = (flag_bbox[2] - flag_bbox[0] + 1) if flag_bbox else None

    return {
        "bbox": [x0, y0, x1, y1],
        "width": x1 - x0 + 1,
        "height": y1 - y0 + 1,
        "pole_x": pole,
        "center_x": center_x,
        "head_top_y": head_y,
        "foot_y": foot_y,
        "body_height": body_h,
        "total_height": total_h,
        "total_over_body": round(total_h / body_h, 4) if body_h else None,
        "flag_bbox_local": flag_bbox,
        "flag_width": flag_w,
    }


def build_sheet(ref_img: Image.Image, crop_img: Image.Image, out_png: pathlib.Path,
                ref_bbox, crop_bbox):
    """1011（已验收基准）与 1013（并排图裁切）并排对照：脚底对齐。"""
    r = ref_img.crop(tuple(ref_bbox)).convert("RGBA")
    c = crop_img.crop(tuple(crop_bbox)).convert("RGBA")
    gap, pad, label_h = 40, 30, 0
    h = max(r.height, c.height)
    canvas = Image.new("RGBA", (r.width + gap + c.width + pad * 2, h + pad * 2), (0, 0, 0, 0))
    canvas.paste(r, (pad, pad + (h - r.height)), r)
    canvas.paste(c, (pad + r.width + gap, pad + (h - c.height)), c)
    canvas.save(out_png)


def main():
    ap = argparse.ArgumentParser(description="并排参考图 → 1013 研究参考裁切 + 比例量测")
    ap.add_argument("--src", required=True)
    ap.add_argument("--out-dir", required=True)
    ap.add_argument("--target-index", type=int, default=2, help="从左到右第几个角色（0 基，1013=2）")
    ap.add_argument("--baseline-head-top-y", type=int, default=None,
                    help="1011 基准的盔顶全局 y（自动量测会把盔缨红绸算进去，可用此参数覆盖）")
    ap.add_argument("--green-out", default="", help="绿幕锚图输出路径（1024² 绿幕，杆顶总高固定）")
    ap.add_argument("--green-total-h", type=int, default=870,
                    help="绿幕锚图里杆顶到脚底的总高 px（默认 870 = 人体 696 的 1.25 倍）")
    args = ap.parse_args()

    src = pathlib.Path(args.src)
    out_dir = pathlib.Path(args.out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)

    im = Image.open(src).convert("RGBA")
    arr = np.asarray(im).copy()
    arr_src = arr.copy()          # 原图副本（后面 alpha 会被改写）
    alpha = arr[..., 3]
    mask = alpha > ALPHA_T

    comps, labels = components(mask)
    big = [(n, bb) for n, bb in comps if n > 20000]
    print("角色连通域（从左到右）:")
    for i, (n, bb) in enumerate(big):
        print("  #%d px=%d bbox=%s" % (i, n, bb))
    if len(big) <= args.target_index:
        raise SystemExit("目标角色索引超出范围")
    tgt_n, tgt_bb = big[args.target_index]
    print("\n目标角色 #%d px=%d bbox=%s" % (args.target_index, tgt_n, tgt_bb))

    # 只保留目标角色的像素（把相邻角色的像素清零，避免裁到邻居）
    keep = np.zeros_like(mask)
    keep[labels] = False  # labels 是 visited 标记，这里改用组件像素重建
    # 重新 BFS 标记目标组件
    from collections import deque as _dq
    seed = None
    ys, xs = np.nonzero(mask)
    for y, x in zip(ys.tolist(), xs.tolist()):
        if tgt_bb[0] <= x <= tgt_bb[2] and tgt_bb[1] <= y <= tgt_bb[3]:
            seed = (y, x)
            break
    q = _dq([seed])
    keep[seed] = True
    while q:
        cy, cx = q.popleft()
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            ny, nx = cy + dy, cx + dx
            if 0 <= ny < mask.shape[0] and 0 <= nx < mask.shape[1] and mask[ny, nx] and not keep[ny, nx]:
                keep[ny, nx] = True
                q.append((ny, nx))
    arr[..., 3] = np.where(keep, alpha, 0).astype(np.uint8)
    masked = Image.fromarray(arr, mode="RGBA")

    crop = masked.crop((tgt_bb[0], tgt_bb[1], tgt_bb[2] + 1, tgt_bb[3] + 1))
    crop_path = out_dir / "1013_lineup_crop_rgba.png"
    crop.save(crop_path)

    m1013 = measure(keep[tgt_bb[1]:tgt_bb[3] + 1, tgt_bb[0]:tgt_bb[2] + 1])

    # 1011（第一个角色）作为已验收的共同体型基准
    ref_n, ref_bb = big[0]
    ref_keep = np.zeros_like(mask)
    seed = None
    ys, xs = np.nonzero(mask)
    for y, x in zip(ys.tolist(), xs.tolist()):
        if ref_bb[0] <= x <= ref_bb[2] and ref_bb[1] <= y <= ref_bb[3]:
            seed = (y, x)
            break
    q = _dq([seed])
    ref_keep[seed] = True
    while q:
        cy, cx = q.popleft()
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            ny, nx = cy + dy, cx + dx
            if 0 <= ny < mask.shape[0] and 0 <= nx < mask.shape[1] and mask[ny, nx] and not ref_keep[ny, nx]:
                ref_keep[ny, nx] = True
                q.append((ny, nx))
    m1011 = measure(ref_keep[ref_bb[1]:ref_bb[3] + 1, ref_bb[0]:ref_bb[2] + 1])
    if args.baseline_head_top_y is not None:
        local = args.baseline_head_top_y - ref_bb[1]
        m1011["head_top_y"] = local
        m1011["body_height"] = m1011["foot_y"] - local + 1
        m1011["head_top_y_source"] = "manual(global y=%d)" % args.baseline_head_top_y
    else:
        m1011["head_top_y_source"] = "auto"

    # 落到游戏尺寸的影响（常量来源：plan/enemy-commander-buff-unit-plan.md §2.3 体量约束；
    # memory/enemy1012-art-replacement-pipeline.md §3 落位口径）
    GAME_BODY_PX = 605      # 普通兵在 1108² 精灵画布上的身高（1012 idle 已部署规格）
    GAME_FOOT_Y = 897
    WORLD_BODY_H = 5.76     # 普通兵屏高（世界单位）
    COLUMN_SPACING = 2.6    # 五列阵型列距（世界单位）
    impact = {}
    if m1013.get("body_height"):
        k = GAME_BODY_PX / float(m1013["body_height"])
        total_game = int(round(m1013["total_height"] * k))
        target_total = int(round(1.25 * GAME_BODY_PX))
        col_px = COLUMN_SPACING * (GAME_BODY_PX / WORLD_BODY_H)
        impact = {
            "scale_lineup_to_game": round(k, 5),
            "body_in_game_px": GAME_BODY_PX,
            "total_in_game_px": total_game,
            "pole_top_y_in_game": GAME_FOOT_Y - total_game,
            "fits_1108_canvas": (GAME_FOOT_Y - total_game) >= 0,
            "target_total_at_1_25x_px": target_total,
            "excess_over_target_px": total_game - target_total,
            "pole_top_y_if_1_25x": GAME_FOOT_Y - target_total,
            "column_spacing_px": int(round(col_px)),
            "flag_width_in_game_px": int(round(m1013["flag_width"] * k)) if m1013.get("flag_width") else None,
            "flag_spans_columns": round((m1013["flag_width"] * k) / col_px, 2) if m1013.get("flag_width") else None,
        }

    record = {
        "purpose": "1013 旗手 · 研究参考裁切（非游戏素材 / 非身份定稿）",
        "source": {"path": str(src), "size": list(im.size), "sha256": sha256_of(src)},
        "alpha_threshold": ALPHA_T,
        "character_1013": m1013,
        "character_1011_baseline": m1011,
        "comparison": {
            "1013_body_vs_1011_body": round(m1013["body_height"] / m1011["body_height"], 4)
            if m1013["body_height"] and m1011["body_height"] else None,
            "1013_total_vs_1011_body": round(m1013["total_height"] / m1011["body_height"], 4)
            if m1013["total_height"] and m1011["body_height"] else None,
            "design_target_total_vs_normal": 1.25,
        },
        "game_impact": impact,
        "outputs": {
            "crop": {"path": str(crop_path), "size": list(crop.size), "sha256": sha256_of(crop_path)},
        },
    }
    json_path = out_dir / "1013_lineup_crop_measure.json"
    json_path.write_text(json.dumps(record, indent=2, ensure_ascii=False), encoding="utf-8")
    print("\n" + json.dumps(record, indent=2, ensure_ascii=False))

    # 绿幕锚图（视频首尾帧 / 生图编辑输入都用它）
    # 布局沿用 1012 身份图口径：1024² 画布、绿 (0,159,62)、脚底 y=895、人体中心 x=474.5；
    # 额外约束：**杆顶到脚底的总高**固定（默认 870 = 人体身高 696 的 1.25 倍），保证杆顶不碰顶。
    if args.green_out:
        GREEN_C = (0, 159, 62)
        CANVAS_G = 1024
        FOOT_Y_G = 895
        BODY_CENTER_X = 474.5
        scale_g = float(args.green_total_h) / float(m1013["total_height"])
        nw = int(round(m1013["width"] * scale_g))
        nh = int(round(m1013["total_height"] * scale_g))
        paste_x = int(round(BODY_CENTER_X - m1013["center_x"] * scale_g))
        paste_y = FOOT_Y_G - nh + 1
        canvas_g = Image.new("RGBA", (CANVAS_G, CANVAS_G), GREEN_C + (255,))
        small = crop.resize((nw, nh), Image.LANCZOS)
        canvas_g.paste(small, (paste_x, paste_y), small)
        green_path = pathlib.Path(args.green_out)
        green_path.parent.mkdir(parents=True, exist_ok=True)
        canvas_g.convert("RGB").save(green_path)
        record["outputs"]["green_anchor"] = {
            "path": str(green_path), "size": [CANVAS_G, CANVAS_G],
            "sha256": sha256_of(green_path),
            "scale": round(scale_g, 5),
            "paste_xy": [paste_x, paste_y],
            "green": list(GREEN_C),
            "body_height_px": int(round(m1013["body_height"] * scale_g)),
            "total_height_px": nh,
            "foot_y": FOOT_Y_G,
            "body_center_x": BODY_CENTER_X,
            "pole_top_y": paste_y,
        }
        json_path.write_text(json.dumps(record, indent=2, ensure_ascii=False), encoding="utf-8")
        print("\n绿幕锚图: %s (body=%d, total=%d, pole_top_y=%d)"
              % (green_path, int(round(m1013["body_height"] * scale_g)), nh, paste_y))

    # 等高对照图（脚底对齐）：1011 vs 1013
    sheet = out_dir / "1013_vs_1011_proportion_sheet.png"
    try:
        ref_masked = Image.fromarray(np.where(ref_keep[..., None], arr_src, 0).astype(np.uint8), mode="RGBA")
        ref_only = ref_masked.crop(tuple(ref_bb))
        build_sheet(ref_only, crop, sheet, (0, 0, ref_only.width, ref_only.height),
                    (0, 0, crop.width, crop.height))
        record["outputs"]["proportion_sheet"] = {"path": str(sheet), "sha256": sha256_of(sheet)}
        json_path.write_text(json.dumps(record, indent=2, ensure_ascii=False), encoding="utf-8")
        print("\n对照图: %s" % sheet)
    except Exception as e:  # 对照图失败不影响裁切主产物
        print("对照图生成失败: %s" % e)


if __name__ == "__main__":
    main()
