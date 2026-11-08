# -*- coding: utf-8 -*-
"""把一张绿幕图合成成「锚图口径」：按角色身高缩放，指定头顶 y / 脚底 y / 身体中心 x 贴到指定画布。

用途（1013）：把生图出来的关键帧统一到已验收锚图的构图口径
（人物身 470 / 头顶 530 / 脚底 1000 / 身体中心 540 / 画布 1080² / 绿 0,159,62），
这样 Idle / Cast / Walk 三套素材落位后彼此对齐。

口径要点：
  · 抠图复用 Tools/visual_brief/chroma_key.py 的 key_frame（T=79 + halo 去绿边 + 二值 alpha）
  · 身体中心取「躯干行带」的主体像素 x 中位（不含旗面、不含杆）
  · 头顶 y 由调用方给定（旗面可能遮挡头顶，需人工量）

用法
    python Tools/visual_brief/compose_anchor_by_body.py <raw.png> \
        --body-top-y 487 --body-height 470 --canvas 1080 --body-top-out 530 \
        --foot-y 1000 --body-center-x 540 --out <out.png> --json <out.json>
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


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("raw")
    ap.add_argument("--out", required=True)
    ap.add_argument("--json", default="")
    ap.add_argument("--body-top-y", type=int, required=True, help="原图里角色头顶 y")
    ap.add_argument("--body-height", type=int, required=True, help="目标角色身高 px（头顶→脚底）")
    ap.add_argument("--body-top-out", type=int, required=True, help="成品里头顶 y")
    ap.add_argument("--foot-y", type=int, required=True, help="成品里可见脚底 y")
    ap.add_argument("--body-center-x", type=int, required=True, help="成品里身体中心 x")
    ap.add_argument("--canvas", type=int, default=1080)
    ap.add_argument("--torso-band", default="", help="躯干行带（原始像素 y0,y1），默认按身高 55%~75% 推算")
    args = ap.parse_args()

    src = pathlib.Path(args.raw)
    img = Image.open(src).convert("RGB")
    rgb = np.asarray(img)
    alpha, _ = key_frame(rgb)
    mask = alpha > 0
    if not mask.any():
        raise SystemExit("抠图失败：未检出主体像素")

    ys, xs = np.nonzero(mask)
    bbox = [int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())]

    foot_y = int(ys.max())
    raw_body_h = foot_y - args.body_top_y + 1
    scale = args.body_height / float(raw_body_h)

    # 躯干行带 → 身体中心 x（排除旗面与杆）
    if args.torso_band:
        y0, y1 = (int(v) for v in args.torso_band.split(","))
    else:
        y0 = args.body_top_y + int(0.55 * raw_body_h)
        y1 = args.body_top_y + int(0.78 * raw_body_h)
    band = mask[y0:y1 + 1]
    band_x = np.nonzero(band)[1]
    body_cx_raw = float(np.median(band_x)) if band_x.size else (bbox[0] + bbox[2]) / 2.0

    crop = img.crop((bbox[0], bbox[1], bbox[2] + 1, bbox[3] + 1))
    new_w = max(1, int(round(crop.width * scale)))
    new_h = max(1, int(round(crop.height * scale)))
    crop = crop.resize((new_w, new_h), Image.LANCZOS)
    cut = np.asarray(crop.convert("RGB")).astype(np.int16)
    alpha_s, _ = key_frame(cut.astype(np.uint8))
    rgba = np.zeros((new_h, new_w, 4), dtype=np.uint8)
    rgba[:, :, :3] = cut.astype(np.uint8)
    rgba[:, :, 3] = alpha_s

    canvas = np.zeros((args.canvas, args.canvas, 3), dtype=np.uint8)
    canvas[:, :] = GREEN
    out = Image.fromarray(canvas, mode="RGB").convert("RGBA")
    # 主体 bbox 内，身体中心相对 bbox 左边的偏移（缩放后）
    body_cx_scaled = (body_cx_raw - bbox[0]) * scale
    paste_x = int(round(args.body_center_x - body_cx_scaled))
    # 头顶上方还挂着旗面，贴图时要让「头顶」落在 body_top_out，而不是裁切块顶端
    head_offset = (args.body_top_y - bbox[1]) * scale
    paste_y = int(round(args.body_top_out - head_offset))
    out.paste(Image.fromarray(rgba, mode="RGBA"), (paste_x, paste_y),
              Image.fromarray(rgba, mode="RGBA"))

    out_path = pathlib.Path(args.out)
    out_path.parent.mkdir(parents=True, exist_ok=True)
    out.convert("RGB").save(out_path)

    # 成品量测
    final = np.asarray(Image.open(out_path).convert("RGB"))
    fa, _ = key_frame(final)
    fmask = fa > 0
    fys, fxs = np.nonzero(fmask)
    info = {
        "source": str(src),
        "source_size": list(img.size),
        "source_bbox": bbox,
        "raw_body_height": raw_body_h,
        "raw_body_top_y": args.body_top_y,
        "raw_body_center_x": round(body_cx_raw, 1),
        "torso_band": [y0, y1],
        "scale": round(scale, 5),
        "paste": [paste_x, paste_y],
        "canvas": args.canvas,
        "target": {"body_height": args.body_height, "body_top_y": args.body_top_out,
                   "foot_y": args.foot_y, "body_center_x": args.body_center_x},
        "result_bbox": [int(fxs.min()), int(fys.min()), int(fxs.max()), int(fys.max())],
        "result_body_height": int(fys.max()) - args.body_top_out + 1,
        "result_foot_y": int(fys.max()),
        "result_pole_top_y": int(fys.min()),
    }
    jpath = pathlib.Path(args.json) if args.json else out_path.with_suffix(".layout.json")
    jpath.write_text(json.dumps(info, indent=2, ensure_ascii=False), encoding="utf-8")
    print(json.dumps(info, indent=2, ensure_ascii=False))
    print("[OK] %s" % out_path)


if __name__ == "__main__":
    main()
