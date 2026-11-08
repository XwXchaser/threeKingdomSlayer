# -*- coding: utf-8 -*-
"""四兵种并排组图 → 1051 弓手：形象裁切 + 绿幕身份图 + 视频锚图。

用途
    把 `enemy_proportion_lineup_v1.png`（四兵种并排比例组图）里**最右边的弓手（1051）**
    从整张组图里切出来，产出视频生成链需要的三件东西：
        1. `1051_lineup_crop_reference_rgba.png`  —— 干净 alpha 的独立裁切（研究参考 + 后续加工源）
        2. `1051_idle_identity_greenscreen_1024.png` —— **身份图 / idle 形象**（视频生成的参考输入）
        3. `1051_cast_anchor_green_1080x1080.png`   —— 视频**首尾帧锚图**（seedance frames 工作流）
    另出一张「1013 / 1051 脚底对齐对照图」，供人工目视复核体型。

口径（沿用项目既有规格，不新造）
    * 身份图：1024² / 绿 RGB(0,159,62) / **人物本体高 696** / 可见脚底 y=895 / 身体中心 x=474.5
      —— 与 `Tools/visual_brief/compose_identity_greenscreen.py`（1012 身份图口径）一致。
    * 锚图：1080×1080 / 绿 RGB(0,159,62) / 人物本体高 470 / 脚底线 y=1000 / 身体居中 x=540
      —— 与 1013 的 `1013_cast_anchor_green_1080x1080_v6` 同一套参数，提示词百分比可照抄。
    * 抠图：组图自带 alpha（直通道，边缘像素 RGB 就是主体色，不会带白边）。
      阈值 `--alpha-threshold`（默认 128）取主体并二值化，再补内部孔洞。

关键实现点（都是踩过的坑）
    * **连通域必须四邻接**。`crop_lineup_1013.py` 的 BFS 邻居表把 `(0,-1)` 写成了 `(-1,0)`，
      会把人物切成一根根竖条（并排图里就会看到竖条状的「鬼影」）。本脚本用正确的四邻接。
    * **并排图里相邻两个角色会粘连**：旗手旗面与弓手箭羽在 4 邻接下并不连通，但 x 区间重叠，
      所以不能靠「x 窗口」选人，要靠**连通域整体**（本脚本取最右侧的连通域）。
    * **头顶 ≠ 最高像素**：弓手头顶发髻在 y=379，而弓顶在 y=368（更高）。
      头顶取「身体中心列 ±60px 内的最高像素」，弓在身体中心右侧 ~100px，不会被算进来。
      需要人工指定时用 `--head-top-y`。

边界（务必遵守）
    * 本脚本**不产出游戏素材**：身份图/锚图是**视频生成输入**，成片抠图落位后才能进 Unity。
    * 并组图本身是比例研究图（见 `plan/enemy-commander-buff-unit-plan.md` §2.15、
      `memory/enemy-proportion-lineup-reference.md`），不是身份定稿。
    * 不改原图，不覆盖任何既有产物。

用法
    python Tools/visual_brief/crop_lineup_1051.py \
        --src "C:/Users/steam/Pictures/gptGen/enemy_proportion_lineup_v1/enemy_proportion_lineup_v1.png" \
        --out-dir "Enemy1051ArtSource/02_IdentityImages"
"""
from __future__ import annotations

import argparse
import hashlib
import json
import pathlib
from collections import deque

import numpy as np
from PIL import Image

GREEN = (0, 159, 62)          # 绿幕制作底
CANVAS = 1024                 # 身份图画布
TARGET_BODY_H = 696           # 身份图里人物本体高（头顶→脚底）
TARGET_FOOT_Y = 895           # 身份图里可见脚底线
TARGET_CENTER_X = 474.5       # 身份图里身体中心 x
ANCHOR_CANVAS = (1080, 1080)  # 锚图画布
ANCHOR_BODY_H = 470           # 锚图里人物本体高
ANCHOR_FEET_Y = 1000          # 锚图里脚底线

# 游戏侧常量（来源：plan/enemy-commander-buff-unit-plan.md §2.3 体量约束；
# memory/enemy1012-art-replacement-pipeline.md §3 落位口径）
GAME_BODY_PX = 605      # 普通兵在 1108² 精灵画布上的身高（1012 idle 已部署规格）
GAME_FOOT_Y = 897
WORLD_BODY_H = 5.76     # 普通兵屏高（世界单位）
COLUMN_SPACING = 2.6    # 五列阵型列距（世界单位）

NEIGHBORS4 = ((1, 0), (-1, 0), (0, 1), (0, -1))


def sha256_of(path: pathlib.Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def components(mask: np.ndarray):
    """四邻接连通域，返回 [(pixel_count, (x0, y0, x1, y1), [(y, x), ...])]，按 x0 排序。"""
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
            pts = []
            while q:
                cy, cx = q.popleft()
                n += 1
                pts.append((cy, cx))
                x0, x1 = min(x0, cx), max(x1, cx)
                y0, y1 = min(y0, cy), max(y1, cy)
                for dy, dx in NEIGHBORS4:
                    ny, nx = cy + dy, cx + dx
                    if 0 <= ny < h and 0 <= nx < w and mask[ny, nx] and not seen[ny, nx]:
                        seen[ny, nx] = True
                        q.append((ny, nx))
            out.append((n, (x0, y0, x1, y1), pts))
    out.sort(key=lambda c: c[1][0])
    return out


def fill_holes(alpha: np.ndarray, rounds: int = 2) -> np.ndarray:
    """补主体内部的针孔（四邻全为主体的背景像素 → 补实）。"""
    for _ in range(rounds):
        filled = alpha.copy()
        filled[1:-1, 1:-1] = np.where(
            (alpha[:-2, 1:-1] > 0) & (alpha[2:, 1:-1] > 0)
            & (alpha[1:-1, :-2] > 0) & (alpha[1:-1, 2:] > 0),
            255, alpha[1:-1, 1:-1])
        if (filled == alpha).all():
            break
        alpha = filled
    return alpha


def geometry(mask: np.ndarray, bbox, head_top_override=None):
    """从角色掩码量出：脚底线 / 身体中心列 / 头顶 / 每列最高像素。"""
    x0, y0, x1, y1 = bbox
    sub = mask[y0:y1 + 1, x0:x1 + 1]
    h, w = sub.shape
    foot_local = int(np.nonzero(sub.any(axis=1))[0].max())

    # 每列最高像素（局部坐标，-1 = 空列）
    top_row = np.full(w, -1, dtype=np.int64)
    for i in range(w):
        col = np.nonzero(sub[:, i])[0]
        if len(col):
            top_row[i] = col.min()

    # 身体中心列：取「脚底往上 40~140px」这段（下身/双腿）的横向中点
    lo = max(0, foot_local - 140)
    hi = max(1, foot_local - 39)
    lower = sub[lo:hi, :]
    cols = np.nonzero(lower.any(axis=0))[0]
    cx = int(round((cols.min() + cols.max()) / 2.0)) if len(cols) else w // 2

    # 头顶：身体中心列 ±60px 内的最高像素（弓/道具在身体中心右侧更远，不会被算进来）
    if head_top_override is not None:
        head_local = head_top_override
        head_source = "manual(local y=%d)" % head_top_override
    else:
        blo = max(0, cx - 60)
        bhi = min(w, cx + 61)
        cand = top_row[blo:bhi]
        cand = cand[cand >= 0]
        head_local = int(cand.min()) if len(cand) else 0
        head_source = "auto(cx=%d, +/-60)" % cx

    return {
        "head_top_y_local": head_local,
        "head_top_source": head_source,
        "foot_y_local": foot_local,
        "body_height": foot_local - head_local + 1,
        "body_center_x_local": cx,
        "body_center_x_global": x0 + cx,
        "top_row_profile": [(x0 + i, int(top_row[i])) for i in range(w)],
    }


def green_check(canvas: Image.Image):
    arr = np.asarray(canvas.convert("RGB")).astype(np.int16)
    H, W, _ = arr.shape
    patches = []
    for y0, y1 in ((0, 40), (H - 40, H)):
        for x0, x1 in ((0, 40), (W - 40, W)):
            patches.append(arr[y0:y1, x0:x1].reshape(-1, 3))
    flat = np.concatenate(patches)
    return {"median": [int(v) for v in np.median(flat, axis=0)],
            "std": [round(float(v), 2) for v in flat.std(axis=0)],
            "target": list(GREEN)}


def subject_bbox(canvas: Image.Image):
    """在绿幕图上量主体（到绿幕基准色的距离 > 79）。"""
    arr = np.asarray(canvas.convert("RGB")).astype(np.float32)
    d = np.linalg.norm(arr - np.array(GREEN, dtype=np.float32), axis=2)
    m = d > 79
    ys, xs = np.nonzero(m)
    return [int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())]


def build_sheet(items, out_png: pathlib.Path):
    """脚底对齐的多角色对照图。items = [(label, RGBA crop)]"""
    gap, pad = 60, 30
    h = max(im.height for _, im in items)
    total_w = sum(im.width for _, im in items) + gap * (len(items) - 1) + pad * 2
    canvas = Image.new("RGBA", (total_w, h + pad * 2), (0, 0, 0, 0))
    x = pad
    for label, im in items:
        canvas.paste(im, (x, pad + (h - im.height)), im)
        x += im.width + gap
    canvas.save(out_png)


def main():
    ap = argparse.ArgumentParser(description="并排组图 → 1051 弓手裁切 / 身份图 / 锚图")
    ap.add_argument("--src", required=True)
    ap.add_argument("--out-dir", required=True)
    ap.add_argument("--alpha-threshold", type=int, default=128, help="主体 alpha 阈值（二值化）")
    ap.add_argument("--min-area", type=int, default=5000, help="连通域最小像素数")
    ap.add_argument("--target-index", type=int, default=-1,
                    help="从左到右第几个角色（-1 = 最右侧，即 1051 弓手）")
    ap.add_argument("--head-top-y", type=int, default=None, help="裁切内头顶 y（默认自动量测）")
    ap.add_argument("--ref-1013", default="", help="1013 裁切（做对照图，可选）")
    args = ap.parse_args()

    src = pathlib.Path(args.src)
    out_dir = pathlib.Path(args.out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)

    im = Image.open(src).convert("RGBA")
    arr = np.asarray(im).copy()
    alpha = arr[..., 3]
    mask = alpha >= args.alpha_threshold

    comps = components(mask)
    big = [(n, bb, pts) for n, bb, pts in comps if n >= args.min_area]
    print("并排图里的角色连通域（从左到右）:")
    for i, (n, bb, _pts) in enumerate(big):
        print("  #%d px=%d bbox=%s w=%d h=%d" % (i, n, bb, bb[2] - bb[0] + 1, bb[3] - bb[1] + 1))

    idx = args.target_index if args.target_index >= 0 else len(big) - 1
    if not (0 <= idx < len(big)):
        raise SystemExit("目标角色索引超出范围")
    tgt_n, tgt_bb, tgt_pts = big[idx]
    print("\n目标角色 #%d: px=%d bbox=%s" % (idx, tgt_n, tgt_bb))

    # 只保留目标角色的像素
    keep_mask = np.zeros_like(mask)
    for y, x in tgt_pts:
        keep_mask[y, x] = True
    keep_mask = fill_holes(keep_mask.astype(np.uint8), 2) > 0

    ys, xs = np.nonzero(keep_mask)
    crop_box = [int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())]
    crop = Image.fromarray(np.where(keep_mask[..., None], arr, 0).astype(np.uint8), mode="RGBA")
    crop = crop.crop((crop_box[0], crop_box[1], crop_box[2] + 1, crop_box[3] + 1))
    # 源图 alpha 最高只有 254（整体被压过一道），边缘统一补成不透明
    ca = np.asarray(crop).copy()
    ca[..., 3] = np.where(ca[..., 3] > 0, 255, 0).astype(np.uint8)
    crop = Image.fromarray(ca, mode="RGBA")
    crop_path = out_dir / "1051_lineup_crop_reference_rgba.png"
    crop.save(crop_path)

    # ---- 量测 ----
    geo = geometry(keep_mask, crop_box, args.head_top_y)
    body_h = geo["body_height"]
    cx_local = geo["body_center_x_local"]
    head_local = geo["head_top_y_local"]
    foot_local = geo["foot_y_local"]
    carr = np.asarray(crop)
    cmask = carr[..., 3] > 0
    cys, cxs = np.nonzero(cmask)
    total_h = int(cys.max() - cys.min() + 1)
    total_w = int(cxs.max() - cxs.min() + 1)

    metrics = {
        "purpose": "1051 弓手 · 并排组图裁切 + 身份图/锚图落位量测",
        "source": {"path": str(src), "size": list(im.size), "sha256": sha256_of(src)},
        "alpha_threshold": args.alpha_threshold,
        "target_index": idx,
        "all_character_components": [{"px": int(n), "bbox": list(bb)} for n, bb, _ in big],
        "crop": {"path": str(crop_path), "size": list(crop.size), "sha256": sha256_of(crop_path),
                 "bbox_global": crop_box},
        "character_1051": {
            "head_top_y_local": head_local,
            "head_top_source": geo["head_top_source"],
            "foot_y_local": foot_local,
            "body_height": body_h,
            "body_center_x_local": cx_local,
            "body_center_x_global": geo["body_center_x_global"],
            "total_height": total_h,
            "total_width": total_w,
            "total_over_body": round(total_h / float(body_h), 4),
            "prop_above_head_px": head_local - int(cys.min()),
        },
    }
    print("\n量测: 头顶=%d 脚底=%d 身体高=%d 身体中心x=%d 总高=%d 总宽=%d"
          % (head_local, foot_local, body_h, cx_local, total_h, total_w))

    # ---- 身份图：绿幕 1024²（身体高 696 / 脚底 895 / 身体中心 474.5）----
    k_id = TARGET_BODY_H / float(body_h)
    new_w = max(1, int(round(total_w * k_id)))
    new_h = max(1, int(round(total_h * k_id)))
    small = crop.resize((new_w, new_h), Image.LANCZOS)
    canvas = Image.new("RGBA", (CANVAS, CANVAS), GREEN + (255,))
    paste_x = int(round(TARGET_CENTER_X - cx_local * k_id))
    paste_y = TARGET_FOOT_Y - new_h + 1
    canvas.paste(small, (paste_x, paste_y), small)
    ident_path = out_dir / "1051_idle_identity_greenscreen_1024.png"
    canvas.convert("RGB").save(ident_path)
    ident_bbox = subject_bbox(canvas)
    metrics["identity_green"] = {
        "path": str(ident_path), "sha256": sha256_of(ident_path), "canvas": [CANVAS, CANVAS],
        "scale": round(k_id, 5), "paste_xy": [paste_x, paste_y],
        "body_height_px": TARGET_BODY_H, "foot_y": TARGET_FOOT_Y,
        "body_center_x": TARGET_CENTER_X, "subject_bbox": ident_bbox,
        "head_top_y_in_canvas": paste_y + int(round(head_local * k_id)),
        "prop_top_y_in_canvas": paste_y,
        "green_check": green_check(canvas),
    }
    print("\n身份图 %s" % ident_path)
    print(json.dumps(metrics["identity_green"], indent=2, ensure_ascii=False))

    # ---- 锚图：绿幕 1080×1080（身体高 470 / 脚底 1000 / 身体居中 540）----
    k_an = ANCHOR_BODY_H / float(body_h)
    new_w = max(1, int(round(total_w * k_an)))
    new_h = max(1, int(round(total_h * k_an)))
    small = crop.resize((new_w, new_h), Image.LANCZOS)
    acanvas = Image.new("RGBA", ANCHOR_CANVAS, GREEN + (255,))
    apaste_x = int(round(ANCHOR_CANVAS[0] / 2.0 - cx_local * k_an))
    apaste_y = ANCHOR_FEET_Y - new_h + 1
    acanvas.paste(small, (apaste_x, apaste_y), small)
    anchor_path = out_dir / "1051_cast_anchor_green_1080x1080.png"
    acanvas.convert("RGB").save(anchor_path)
    anchor_bbox = subject_bbox(acanvas)
    head_top_canvas = apaste_y + int(round(head_local * k_an))
    metrics["cast_anchor"] = {
        "path": str(anchor_path), "sha256": sha256_of(anchor_path),
        "canvas": list(ANCHOR_CANVAS), "scale": round(k_an, 5),
        "paste_xy": [apaste_x, apaste_y], "body_height_px": ANCHOR_BODY_H,
        "feet_y": ANCHOR_FEET_Y, "body_center_x": ANCHOR_CANVAS[0] / 2.0,
        "head_top_y_in_canvas": head_top_canvas,
        "subject_bbox": anchor_bbox,
        "subject_width_pct": round(100.0 * (anchor_bbox[2] - anchor_bbox[0] + 1) / ANCHOR_CANVAS[0], 1),
        "prompt_pct": {
            "body_height_pct": round(100.0 * ANCHOR_BODY_H / ANCHOR_CANVAS[1], 1),
            "head_top_pct": round(100.0 * head_top_canvas / ANCHOR_CANVAS[1], 1),
            "feet_pct": round(100.0 * ANCHOR_FEET_Y / ANCHOR_CANVAS[1], 1),
            "prop_top_pct": round(100.0 * apaste_y / ANCHOR_CANVAS[1], 1),
        },
        "green_check": green_check(acanvas),
    }
    print("\n锚图 %s" % anchor_path)
    print(json.dumps(metrics["cast_anchor"], indent=2, ensure_ascii=False))

    # ---- 游戏侧影响（换算到 1108² 精灵画布 / 五列阵型）----
    k_game = GAME_BODY_PX / float(body_h)
    total_game_h = int(round(total_h * k_game))
    total_game_w = int(round(total_w * k_game))
    col_px = COLUMN_SPACING * (GAME_BODY_PX / WORLD_BODY_H)
    metrics["game_impact"] = {
        "scale_lineup_to_game": round(k_game, 5),
        "body_in_game_px": GAME_BODY_PX,
        "total_in_game_px": total_game_h,
        "total_width_in_game_px": total_game_w,
        "fits_1108_canvas": (GAME_FOOT_Y - total_game_h) >= 0,
        "column_spacing_px": int(round(col_px)),
        "subject_spans_columns": round(total_game_w / col_px, 2),
    }
    print("\n游戏侧影响: %s" % json.dumps(metrics["game_impact"], ensure_ascii=False))

    # ---- 对照图（脚底对齐）----
    sheet_items = []
    if args.ref_1013 and pathlib.Path(args.ref_1013).is_file():
        sheet_items.append(("1013", Image.open(args.ref_1013).convert("RGBA")))
    sheet_items.append(("1051", crop))
    if len(sheet_items) > 1:
        sheet_path = out_dir / "1051_vs_1013_proportion_sheet.png"
        build_sheet(sheet_items, sheet_path)
        metrics["outputs_proportion_sheet"] = {"path": str(sheet_path), "sha256": sha256_of(sheet_path)}
        print("\n对照图: %s" % sheet_path)

    json_path = out_dir / "1051_lineup_crop_measure.json"
    json_path.write_text(json.dumps(metrics, indent=2, ensure_ascii=False), encoding="utf-8")
    print("\n量测记录: %s" % json_path)


if __name__ == "__main__":
    main()
