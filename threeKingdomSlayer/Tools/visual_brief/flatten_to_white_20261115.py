# -*- coding: utf-8 -*-
"""把概念图 PNG 本地合成到纯白底（RGB），给不显示透明的看图器用，并输出 alpha 统计。

背景：plan/weapon-concept-image-plan-20261114.md §5.9（抡斧概念图实测为透明底，工具预览合成在黑底上
会看着像“暗角 + 辉光”）；锁镰 v3 同样是透明底（Color Type 6，Alpha 最大 254）。
本脚本只做本地合成，不改形状、不改色相；原图一律保留不覆盖。

用法
    python Tools/visual_brief/flatten_to_white_20261115.py <src.png> [dst.png]
"""
from __future__ import annotations

import json
import pathlib
import sys

import numpy as np
from PIL import Image


def main(argv: list[str]) -> int:
    if len(argv) < 2:
        print(__doc__)
        return 2
    src = pathlib.Path(argv[1])
    dst = pathlib.Path(argv[2]) if len(argv) > 2 else src.with_name(src.stem + "_on_white.png")
    if not src.is_file():
        raise SystemExit("missing source: %s" % src)

    im = Image.open(src)
    rgba = im.convert("RGBA")
    arr = np.asarray(rgba)
    alpha = arr[:, :, 3]

    bg = Image.new("RGBA", rgba.size, (255, 255, 255, 255))
    comp = Image.alpha_composite(bg, rgba).convert("RGB")
    dst.parent.mkdir(parents=True, exist_ok=True)
    comp.save(dst)

    ys, xs = np.where(alpha > 16)
    report = {
        "source": str(src),
        "output": str(dst),
        "source_size": list(rgba.size),
        "source_mode": im.mode,
        "alpha_max": int(alpha.max()),
        "alpha_min": int(alpha.min()),
        "pixels_alpha_0": int((alpha == 0).sum()),
        "pixels_alpha_255": int((alpha == 255).sum()),
        "pixels_alpha_partial": int(((alpha > 0) & (alpha < 255)).sum()),
        "subject_bbox_alpha_gt_16": [int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())] if xs.size else None,
        "margin_px": {
            "left": int(xs.min()), "top": int(ys.min()),
            "right": int(rgba.size[0] - 1 - xs.max()), "bottom": int(rgba.size[1] - 1 - ys.max()),
        } if xs.size else None,
    }
    print(json.dumps(report, indent=2, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
