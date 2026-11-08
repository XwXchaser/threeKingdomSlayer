# -*- coding: utf-8 -*-
"""身份图 → 纯绿幕身份图（抠图 + 统一落位口径）。

沿用项目既有 1012 身份图规格（`memory/enemy1012-art-replacement-pipeline.md` §4）：
    1024×1024 画布 / 绿 RGB(0,159,62) / 主体高 696 / 可见脚底 y=895 / 中心 x=474.5
    做法：绿幕抠图 → 裁主体 → 缩放到主体高 696 → 贴 (474.5-w/2, 200) → 合成绿幕

抠图配方直接复用 `Tools/visual_brief/chroma_key.py` 的 `key_frame`
（主键阈值 T=79 相对 (0,159,62)，halo 去绿边，alpha 二值），
不另写一套，避免两条抠图口径分叉。

用法
    python Tools/visual_brief/compose_identity_greenscreen.py <raw.png> [--out path] [--json path]
"""
from __future__ import annotations

import argparse
import json
import pathlib
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
from chroma_key import key_frame  # noqa: E402

GREEN = (0, 159, 62)
TARGET_H = 696          # 主体高（含旗顶）
TARGET_FOOT_Y = 895     # 可见脚底线
TARGET_CENTER_X = 474.5 # 主体水平中心
CANVAS = 1024


def measure_green(rgb: np.ndarray):
    """四角与四边中点的背景色统计（用于确认模型给的绿幕是否可键）。"""
    h, w, _ = rgb.shape
    patches = []
    for y0, y1 in ((0, 64), (h - 64, h)):
        for x0, x1 in ((0, 64), (w - 64, w)):
            patches.append(rgb[y0:y1, x0:x1].reshape(-1, 3))
    flat = np.concatenate(patches, axis=0)
    med = np.median(flat, axis=0)
    return {
        "median": [int(v) for v in med],
        "min": [int(v) for v in flat.min(axis=0)],
        "max": [int(v) for v in flat.max(axis=0)],
        "std": [round(float(v), 2) for v in flat.std(axis=0)],
    }


def main():
    ap = argparse.ArgumentParser(description="身份图绿幕合成与落位量测")
    ap.add_argument("raw", help="生成原图（RGB PNG）")
    ap.add_argument("--out", default=None, help="输出绿幕图路径")
    ap.add_argument("--json", default=None, help="量测结果 JSON 路径")
    ap.add_argument("--mode", default="total", choices=["total", "body"],
                    help="total=总高（旗顶→脚）对齐目标；body=角色身（头顶→脚）对齐目标（旗手用）")
    ap.add_argument("--target-h", type=int, default=696, help="total 模式的目标总高 px")
    ap.add_argument("--body-height", type=int, default=696, help="body 模式的目标角色身高 px")
    ap.add_argument("--body-top-y", type=int, default=None,
                    help="body 模式：原图里角色头顶 y（旗手被旗面遮挡时需手工量）")
    ap.add_argument("--canvas-h", type=int, default=1024, help="画布高")
    ap.add_argument("--foot-y", type=int, default=895, help="可见脚底线 y")
    args = ap.parse_args()

    raw_path = pathlib.Path(args.raw)
    out_path = pathlib.Path(args.out) if args.out else raw_path.with_name(
        raw_path.stem.replace("_raw", "") + "_greenscreen_1024.png")
    json_path = pathlib.Path(args.json) if args.json else out_path.with_suffix(".layout.json")

    img = Image.open(raw_path).convert("RGB")
    rgb = np.asarray(img)
    info = {
        "source": str(raw_path),
        "source_size": list(img.size),
        "background_sampling": measure_green(rgb),
    }

    alpha, _ = key_frame(rgb)
    mask = alpha > 0
    if not mask.any():
        raise SystemExit("抠图失败：未检出主体像素（检查绿幕色是否接近 (0,159,62)）")

    ys, xs = np.nonzero(mask)
    bbox = [int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())]
    h = bbox[3] - bbox[1] + 1
    w = bbox[2] - bbox[0] + 1
    info["cut"] = {"bbox": bbox, "subject_w": w, "subject_h": h,
                   "cutout_pixels": int(mask.sum())}

    if args.mode == "body":
        if args.body_top_y is None:
            raise SystemExit("body 模式需要 --body-top-y（原图里角色头顶 y）")
        body_h = bbox[3] - args.body_top_y + 1
        scale = args.body_height / float(body_h)
        info["body_mode"] = {"raw_body_height": body_h, "raw_body_top_y": args.body_top_y}
    else:
        scale = args.target_h / float(h)
    new_w = max(1, int(round(w * scale)))
    new_h = max(1, int(round(h * scale)))
    crop = img.crop((bbox[0], bbox[1], bbox[2] + 1, bbox[3] + 1)).resize(
        (new_w, new_h), Image.LANCZOS)
    cut = np.asarray(crop.convert("RGB")).astype(np.int16)
    # 缩放后重抠一遍边缘（缩放会引入新的绿边），再取 alpha
    alpha_s, _ = key_frame(cut.astype(np.uint8))
    rgba = np.zeros((new_h, new_w, 4), dtype=np.uint8)
    rgba[:, :, :3] = cut.astype(np.uint8)
    rgba[:, :, 3] = alpha_s

    canvas = np.zeros((args.canvas_h, CANVAS, 3), dtype=np.uint8)
    canvas[:, :] = GREEN
    out = Image.fromarray(canvas, mode="RGB").convert("RGBA")
    paste_x = int(round(TARGET_CENTER_X - new_w / 2.0))
    paste_y = args.foot_y - new_h + 1
    out.paste(Image.fromarray(rgba, mode="RGBA"), (paste_x, paste_y), Image.fromarray(rgba, mode="RGBA"))
    out_path.parent.mkdir(parents=True, exist_ok=True)
    out.convert("RGB").save(out_path)

    # 量测合成结果：主体 bbox 与可见脚底线
    arr = np.asarray(out.convert("RGB"))
    d = np.linalg.norm(arr.astype(np.float32) - np.array(GREEN, dtype=np.float32), axis=2)
    m = d > 79
    ys2, xs2 = np.nonzero(m)
    info["composited"] = {
        "path": str(out_path),
        "canvas": [CANVAS, args.canvas_h],
        "green": list(GREEN),
        "paste_xy": [paste_x, paste_y],
        "scale": round(scale, 5),
        "bbox": [int(xs2.min()), int(ys2.min()), int(xs2.max()), int(ys2.max())],
        "subject_w": int(xs2.max() - xs2.min() + 1),
        "subject_h": int(ys2.max() - ys2.min() + 1),
        "visible_bottom_y": int(ys2.max()),
        "center_x": round(float((xs2.min() + xs2.max()) / 2.0), 1),
    }
    info["target"] = {"mode": args.mode, "subject_h": TARGET_H, "foot_y": TARGET_FOOT_Y,
                      "center_x": TARGET_CENTER_X}
    json_path.write_text(json.dumps(info, indent=2, ensure_ascii=False), encoding="utf-8")
    print(json.dumps(info, indent=2, ensure_ascii=False))


if __name__ == "__main__":
    main()
