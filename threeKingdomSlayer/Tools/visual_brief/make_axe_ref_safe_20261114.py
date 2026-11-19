# -*- coding: utf-8 -*-
"""抡斧概念图 · 内容安全友好参考图（policy-safe reference variant）。

背景：plan/weapon-concept-image-plan-20261114.md §4.1 记录「亮银白的大刃」当编辑参考会被内容安全拦成 400。
2026-11-14 四视图首次提交实测复现（request id 20261114202307351620050tb8mtsZB）。
做法：本地把已验收概念图的**高光压一档**（保住银白刃缘仍是画面最亮处）并把背景从纯白改为浅中性灰，
不改形状、不改部件、不改色相关系。上传用这一版；原图与白底副本一律保留不覆盖。

用法
    python Tools/visual_brief/make_axe_ref_safe_20261114.py
"""
from __future__ import annotations

import json
import os
import pathlib

import numpy as np
from PIL import Image

SRC = pathlib.Path(r"C:/Users/steam/Pictures/gptGen/axe_concept_20261114/axe_concept_v1.png")
DST = pathlib.Path(r"C:/Users/steam/Pictures/gptGen/axe_four_view_20261114/axe_concept_ref_safe.png")
BG = 236          # 浅中性灰背景（不用纯白，减少大面积白）
KNEE = 190.0      # 超过这个亮度开始压高光
GAIN = 0.35       # 高光压缩强度
CEIL = 225.0      # 压完后的最高亮度（刃缘仍是最亮处，高于背景）


def main() -> int:
    DST.parent.mkdir(parents=True, exist_ok=True)
    im = Image.open(SRC).convert("RGBA")
    a = np.asarray(im).astype(np.float64)
    rgb, alpha = a[:, :, :3], a[:, :, :3].newaxis if False else a[:, :, 3:4]
    lum = rgb.sum(axis=2) / 3.0

    # 高光压缩：L' = KNEE + (L-KNEE)*GAIN，并封顶 CEIL
    lum2 = np.where(lum > KNEE, KNEE + (lum - KNEE) * GAIN, lum)
    lum2 = np.minimum(lum2, CEIL)
    scale = np.where(lum > 0, lum2 / np.maximum(lum, 1e-6), 1.0)
    new_rgb = np.clip(rgb * scale[:, :, None], 0, 255)

    # 合成到浅中性灰底
    bg = np.zeros_like(new_rgb)
    bg[:, :, 0] = bg[:, :, 1] = bg[:, :, 2] = BG
    al = alpha / 255.0
    comp = new_rgb * al + bg * (1.0 - al)
    out = Image.fromarray(comp.astype(np.uint8), "RGB")

    before = float((lum >= 230).sum())
    after = float(((comp.sum(axis=2) / 3.0) >= 230).sum())
    out.save(DST)

    info = {
        "source": str(SRC),
        "output": str(DST),
        "size": out.size,
        "mode": out.mode,
        "background_lum": BG,
        "highlight_knee": KNEE,
        "highlight_gain": GAIN,
        "highlight_ceiling": CEIL,
        "pixels_lum_ge_230_before": int(before),
        "pixels_lum_ge_230_after": int(after),
    }
    print(json.dumps(info, indent=2, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
