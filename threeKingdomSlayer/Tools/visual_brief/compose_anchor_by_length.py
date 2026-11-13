# -*- coding: utf-8 -*-
"""把「横躺姿态」的绿幕图合成到锚图口径（按身长缩放 + 指定接地 y / 身体中心 x）。

用途（1013 Dead）：倒地的人物是横躺的，"身高"对应"身长"（靴子→头），
所以落位缩放按**身长**算，而不是按竖直身高算。

口径：
    · 抠图复用 Tools/visual_brief/chroma_key.py 的 key_frame
    · scale = 目标身长 / 源图身长
    · 源图身长用调用方给的 x 区间（--body-x0 / --body-x1）量，避免把杆与旗算进去
    · 接地线：源图最低像素 → 成品 y；水平：身长中点 → 成品 x

用法
    python Tools/visual_brief/compose_anchor_by_length.py <raw.png> \
        --body-x0 178 --body-x1 747 --target-len 470 \
        --ground-out 999 --center-out 540 --canvas 1080 \
        --out <out.png> --json <out.json>
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
    ap.add_argument("--body-x0", type=int, required=True, help="源图里身体左端 x（靴子）")
    ap.add_argument("--body-x1", type=int, required=True, help="源图里身体右端 x（头）")
    ap.add_argument("--target-len", type=int, required=True, help="成品身长 px")
    ap.add_argument("--ground-out", type=int, required=True, help="成品里接地 y")
    ap.add_argument("--center-out", type=int, required=True, help="成品里身长中点 x")
    ap.add_argument("--canvas", type=int, default=1080)
    ap.add_argument("--ground-src", type=int, default=-1, help="源图接地 y（-1=取主体最低像素）")
    args = ap.parse_args()

    img = Image.open(args.raw).convert("RGB")
    rgb = np.asarray(img)
    alpha, _ = key_frame(rgb)
    mask = alpha > 0
    if not mask.any():
        raise SystemExit("抠图失败：未检出主体像素")
    ys, xs = np.nonzero(mask)
    bbox = [int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())]

    src_len = args.body_x1 - args.body_x0 + 1
    scale = args.target_len / float(src_len)
    ground_src = args.ground_src if args.ground_src >= 0 else bbox[3]

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
    mid_src = (args.body_x0 + args.body_x1) / 2.0
    mid_scaled = (mid_src - bbox[0]) * scale
    paste_x = int(round(args.center_out - mid_scaled))
    paste_y = int(round(args.ground_out - (ground_src - bbox[1]) * scale))
    out.paste(Image.fromarray(rgba, mode="RGBA"), (paste_x, paste_y),
              Image.fromarray(rgba, mode="RGBA"))

    out_path = pathlib.Path(args.out)
    out_path.parent.mkdir(parents=True, exist_ok=True)
    out.convert("RGB").save(out_path)

    final = np.asarray(Image.open(out_path).convert("RGB"))
    fa, _ = key_frame(final)
    fm = fa > 0
    fys, fxs = np.nonzero(fm)
    info = {
        "source": str(args.raw), "source_bbox": bbox,
        "source_body_len": src_len, "scale": round(scale, 5),
        "paste": [paste_x, paste_y], "canvas": args.canvas,
        "target_len": args.target_len, "ground_out": args.ground_out,
        "center_out": args.center_out,
        "result_bbox": [int(fxs.min()), int(fys.min()), int(fxs.max()), int(fys.max())],
        "result_len": int(round((args.body_x1 - args.body_x0) * scale)) if args.body_x1 > args.body_x0 else None,
        "result_ground_y": int(fys.max()),
        "right_margin": int(args.canvas - 1 - fxs.max()),
        "left_margin": int(fxs.min()),
    }
    jp = pathlib.Path(args.json) if args.json else out_path.with_suffix(".layout.json")
    jp.write_text(json.dumps(info, indent=2, ensure_ascii=False), encoding="utf-8")
    print(json.dumps(info, indent=2, ensure_ascii=False))
    print("[OK] %s" % out_path)


if __name__ == "__main__":
    main()
