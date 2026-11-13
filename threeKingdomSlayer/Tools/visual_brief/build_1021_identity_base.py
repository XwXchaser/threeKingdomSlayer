# -*- coding: utf-8 -*-
"""1021 身份图编辑基件：把五人并排图里的 1021 裁出、水平镜像（把盾翻到画面左）、
重贴到 1024² 绿幕画布，作为"只改面相、盾侧已正确"的编辑输入。

为什么镜像：v2 并排图里 1021 的盾被模型画在画面右（人物左手），与机制相反
（design/enemy-1021-shield-guard.md §3.1：敌人右手持盾 → 盾在画面左）。
镜像整人是确定性的——盾落到画面左、且落在人物的右手上，不必再赌模型听不听话。

用法：
    python Tools/visual_brief/build_1021_identity_base.py \
        --lineup C:/Users/steam/Pictures/gptGen/enemy_proportion_lineup_v2_1021/enemy_proportion_lineup_v2_1021.png \
        --out-dir C:/Users/steam/Pictures/gptGen/enemy1021_identity_v1
"""
from __future__ import annotations

import argparse
import os

from PIL import Image

CANVAS = 1024
BG = (0, 159, 62)
TARGET_BODY_H = 696     # 人物本体高（1012/1013/1051 身份图口径）
TARGET_FOOT_Y = 895     # 脚底线
TARGET_BODY_CX = 474.5  # 身体中心列


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--lineup", required=True)
    ap.add_argument("--out-dir", required=True)
    ap.add_argument("--bbox", default="1202,384,1472,781", help="x0,y0,x1,y1（头顶→脚底，含盾）")
    ap.add_argument("--leg-center-x", type=float, default=1342.2,
                    help="并排图里 1021 的腿中心列（镜像前的原始值）")
    ap.add_argument("--no-mirror", action="store_true")
    args = ap.parse_args()

    os.makedirs(args.out_dir, exist_ok=True)
    x0, y0, x1, y1 = [int(v) for v in args.bbox.split(",")]

    im = Image.open(args.lineup).convert("RGBA")
    crop = im.crop((x0, y0, x1 + 1, y1 + 1))
    leg_in_crop = args.leg_center_x - x0
    if not args.no_mirror:
        crop = crop.transpose(Image.FLIP_LEFT_RIGHT)
        leg_in_crop = crop.width - leg_in_crop

    k = TARGET_BODY_H / (y1 - y0)
    nw, nh = max(1, round(crop.width * k)), max(1, round(crop.height * k))
    big = crop.resize((nw, nh), Image.LANCZOS)

    canvas = Image.new("RGBA", (CANVAS, CANVAS), BG + (255,))
    ox = round(TARGET_BODY_CX - leg_in_crop * k)
    oy = TARGET_FOOT_Y - nh
    canvas.alpha_composite(big, (ox, oy))
    out = os.path.join(args.out_dir, "1021_base_v3_green_1024.png" if args.no_mirror
                       else "1021_base_mirrored_green_1024.png")
    canvas.save(out)
    print("镜像基件: %s" % out)
    print("  1024x1024  绿(0,159,62)  k=%.4f  本体高=%d  脚底=%d  身体中心=%.1f"
          % (k, nh, TARGET_FOOT_Y, TARGET_BODY_CX))
    print("  贴位 x=%d y=%d（人物本体占画幅 %.0f%%）" % (ox, oy, 100.0 * nh / CANVAS))

    raw = os.path.join(args.out_dir, "1021_crop_from_lineup.png")
    crop.save(raw)
    print("  原始裁切: %s  %dx%d" % (raw, crop.width, crop.height))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
