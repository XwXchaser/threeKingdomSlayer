# -*- coding: utf-8 -*-
"""把生成的单枚链环素材合成为运行时要求的 256×256 链环贴图。

依据
  需求书 §1.1：链环贴图 `Assets/Sprites/SkillWeapons/chain_link.png`，256×256 PNG + Alpha（单个链环，可平铺复用）。
  运行时硬约定（`Assets/Scripts/Effects/ChainLinkRig.cs` → `CropLinkSprite`）：
      裁剪窗口 = 画布 x 20%~78%、y 18%~58%（y 向上）⇒ 链节本体必须落在这个窗口里；
      运行时另建 Sprite，PPU = 100。
  ⇒ 本脚本把链环按比例缩放到**窗口高度**，水平居中、垂直居中于窗口，输出 256² RGBA。

用法
    python Tools/visual_brief/compose_chain_link_sprite_20261116.py <src.png> [dst.png]
输出 JSON 报告：链环在 256² 画布上的像素 bbox、各目标世界尺寸对应的 chainLinkScale。
"""
from __future__ import annotations

import json
import pathlib
import sys

import numpy as np
from PIL import Image

CANVAS = 256
WIN_X0, WIN_X1 = 0.20, 0.78          # 裁剪窗口（横向，占画布比例）
WIN_Y0, WIN_Y1 = 0.18, 0.58          # 裁剪窗口（纵向，y 向上，占画布比例）
PPU = 100.0                          # 运行时 Sprite.Create 用的 PPU


def main(argv: list[str]) -> int:
    if len(argv) < 2:
        print(__doc__)
        return 2
    src = pathlib.Path(argv[1])
    dst = pathlib.Path(argv[2]) if len(argv) > 2 else src.with_name("chain_link_256.png")
    if not src.is_file():
        raise SystemExit("missing source: %s" % src)

    im = Image.open(src).convert("RGBA")
    arr = np.asarray(im)
    alpha = arr[:, :, 3]
    ys, xs = np.where(alpha > 16)
    if not xs.size:
        raise SystemExit("source has no opaque pixels")
    ring = im.crop((int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1))

    # 目标窗口（像素，y 向下）
    win_x0, win_x1 = CANVAS * WIN_X0, CANVAS * WIN_X1
    win_y_top = CANVAS * (1.0 - WIN_Y1)      # 窗口上沿（y 向下）
    win_y_bot = CANVAS * (1.0 - WIN_Y0)      # 窗口下沿
    win_w = win_x1 - win_x0
    win_h = win_y_bot - win_y_top

    # 按窗口高度缩放（保持链环比例），水平方向有余量就居中
    scale = win_h / ring.height
    new_w = max(1, int(round(ring.width * scale)))
    new_h = max(1, int(round(ring.height * scale)))
    ring_small = ring.resize((new_w, new_h), Image.LANCZOS)

    canvas = Image.new("RGBA", (CANVAS, CANVAS), (0, 0, 0, 0))
    px = (CANVAS - new_w) // 2
    py = int(round(win_y_top + (win_h - new_h) / 2.0))
    canvas.alpha_composite(ring_small, (px, py))
    canvas.save(dst)

    arr2 = np.asarray(canvas)
    a2 = arr2[:, :, 3]
    ys2, xs2 = np.where(a2 > 16)
    bbox = (int(xs2.min()), int(ys2.min()), int(xs2.max()), int(ys2.max()))
    ring_w_units = (bbox[2] - bbox[0]) / PPU      # linkScale/2 = 1 时的世界尺寸
    ring_h_units = (bbox[3] - bbox[1]) / PPU

    def scale_for(target: float) -> float:
        # 可见尺寸 = 像素单位尺寸 × (chainLinkScale/2) ⇒ 求 chainLinkScale
        return round(2.0 * target / max(ring_w_units, ring_h_units), 4)

    report = {
        "source": str(src),
        "output": str(dst),
        "canvas": [CANVAS, CANVAS],
        "crop_window_px": {
            "x": [round(win_x0, 1), round(win_x1, 1)],
            "y_top_down": [round(win_y_top, 1), round(win_y_bot, 1)],
        },
        "ring_bbox_px": list(bbox),
        "ring_size_px": [bbox[2] - bbox[0], bbox[3] - bbox[1]],
        "ring_world_units_at_scale_1": [round(ring_w_units, 4), round(ring_h_units, 4)],
        "chainLinkScale_for_target_world_size": {
            "0.26 (需求书 §4.2 链环外径)": scale_for(0.26),
            "0.60": scale_for(0.60),
            "0.90 (与节长 0.95 接近、读成连续链)": scale_for(0.90),
        },
        "note": "运行时 visible = 像素尺寸 × (chainLinkScale/2)；节长 chainSegmentLength=0.95",
    }
    print(json.dumps(report, indent=2, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
