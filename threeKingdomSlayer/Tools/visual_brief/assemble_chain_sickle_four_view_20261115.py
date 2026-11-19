# -*- coding: utf-8 -*-
"""追命锁镰四视图 · 本地排版（单图 384x1024 + 无字合图 1536x1024 + 中文标注版 1856x1440）。

依据：Locus/knowledge/plan/weapon-concept-image-plan-20261114.md §5.15
需求书：Locus/knowledge/design/weapon-3d-art-spec-axe-and-chain.md §1.2 / §4.3 / §4.4

规则（与抡斧四视图同一口径）：
- 合图与中文标注全部本地生成，模型不参与；
- 四张单图统一用同一个缩放倍率（同一比例），主体完整不出画，四边留白 >= 16px；
- 左/右不互为水平镜像（各视图独立生成，未做镜像处理）。

★ 与抡斧那套的两处不同（都要随图说明）：
1. LEFT 是从“并排图”里裁出的**真边缘侧视**（模型只有同时画正面时才给得出边缘侧视）；
   RIGHT / BACK 走文生图通道生成（edits 通道会被内容安全拦），风格与 FRONT / LEFT 有轻微差异。

用法
    python Tools/visual_brief/assemble_chain_sickle_four_view_20261115.py
"""
from __future__ import annotations

import hashlib
import json
import pathlib

import numpy as np
from PIL import Image, ImageDraw, ImageFont

SRC_DIR = pathlib.Path("C:/Users/steam/Pictures/gptGen/chain_sickle_four_view_20261115")
OUT_DIR = pathlib.Path("SkillWeaponsArtSource/03_FourView/chain_sickle")
FONT_REGULAR = "C:/Windows/Fonts/msyh.ttc"
FONT_BOLD = "C:/Windows/Fonts/msyhbd.ttc"

CELL_W, CELL_H = 384, 1024
MARGIN_PX = 16                      # 单图四边最小留白（交付规格要求 >= 16px）
PLAIN_BOARD = (1536, 1024)         # 前/左/右/后 四格
ANNOT_BOARD = (1856, 1440)

# (key, 源文件, 裁剪框 or None, 标签, 观察方向, 轴向, 产出说明)
VIEWS = [
    ("front", "chain_sickle_front_d.png", None,
     "正面 FRONT", "观察方向：+Z → −Z", "画面右 = +X，上 = +Y",
     "edits 通道生成，带三张画风参考图"),
    ("left", "chain_sickle_left_a.png", (770, 100, 950, 1430),
     "左侧 LEFT", "观察方向：−X → +X", "画面右 = +Z，上 = +Y",
     "从并排图裁出的真边缘侧视"),
    ("right", "chain_sickle_right_d.png", None,
     "右侧 RIGHT", "观察方向：+X → −X", "画面右 = −Z，上 = +Y",
     "文生图通道生成，钩头窄侧向镜头"),
    ("back", "chain_sickle_back_e.png", None,
     "背面 BACK", "观察方向：−Z → +Z", "画面右 = −X，上 = +Y",
     "文生图通道生成，钩尖朝上偏右"),
]


def sha256_file(path: pathlib.Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def subject_mask(im: Image.Image) -> np.ndarray:
    """主体掩码：优先用 Alpha；无 Alpha 时按“非近白”取。"""
    if im.mode == "RGBA":
        a = np.asarray(im)[:, :, 3]
        if (a > 128).sum() > 1000:
            return a > 128
    arr = np.asarray(im.convert("RGB")).astype(int)
    return arr.min(axis=2) < 232


def load_subject(path: pathlib.Path, crop_box):
    im = Image.open(path)
    if crop_box:
        im = im.crop(crop_box)
    mask = subject_mask(im)
    ys, xs = np.nonzero(mask)
    if len(xs) == 0:
        raise SystemExit("no subject found: %s" % path)
    x0, x1, y0, y1 = int(xs.min()), int(xs.max()), int(ys.min()), int(ys.max())
    rgba = im.convert("RGBA")
    bg = Image.new("RGBA", rgba.size, (255, 255, 255, 255))
    bg.alpha_composite(rgba)
    crop = bg.crop((x0, y0, x1 + 1, y1 + 1)).convert("RGB")
    return crop, (x0, y0, x1, y1), crop_box


def main() -> int:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    (OUT_DIR / "views").mkdir(exist_ok=True)

    loaded = []
    for key, filename, crop_box, label, direction, axis, note in VIEWS:
        src = SRC_DIR / filename
        if not src.is_file():
            raise SystemExit("缺少单图: %s" % src)
        crop, bbox, box = load_subject(src, crop_box)
        loaded.append({"key": key, "src": src, "crop": crop, "bbox": bbox, "box": box,
                       "label": label, "direction": direction, "axis": axis, "note": note})

    # 统一缩放倍率：四张同一个 scale（同一比例），且都留足白
    usable_w = CELL_W - 2 * MARGIN_PX
    usable_h = CELL_H - 2 * MARGIN_PX
    scale = min(min(usable_w / it["crop"].width, usable_h / it["crop"].height)
                for it in loaded)

    record = {
        "source_dir": str(SRC_DIR),
        "output_dir": str(OUT_DIR),
        "cell": [CELL_W, CELL_H],
        "margin_px": MARGIN_PX,
        "common_scale": round(scale, 6),
        "concept_source": "C:/Users/steam/Pictures/gptGen/chain_sickle_concept_20261115/chain_sickle_concept_v1_v6.png",
        "known_limits": [
            "四张为各自独立重绘，不是同一把武器的精确投影；建模以需求书 §3.2/§4.2/§4.3 为准，四视图只作朝向与体量参考",
            "LEFT 由并排图裁切而来，分辨率与独立生成不同",
            "RIGHT / BACK 走文生图通道生成，笔触与 FRONT / LEFT（带参考图的 edits 通道）有轻微差异",
            "四张均非严格正交投影（正交/建模类措辞会被内容安全拦截）",
        ],
        "views": [],
    }

    cells = []
    for it in loaded:
        w = max(1, int(round(it["crop"].width * scale)))
        h = max(1, int(round(it["crop"].height * scale)))
        resized = it["crop"].resize((w, h), Image.LANCZOS)
        cell = Image.new("RGB", (CELL_W, CELL_H), (255, 255, 255))
        ox = (CELL_W - w) // 2
        oy = (CELL_H - h) // 2
        cell.paste(resized, (ox, oy))
        out_path = OUT_DIR / "views" / ("%s.png" % it["key"])
        cell.save(out_path)
        cells.append((it, cell))
        record["views"].append({
            "view": it["key"],
            "label": it["label"],
            "direction": it["direction"],
            "axis": it["axis"],
            "note": it["note"],
            "source_file": it["src"].name,
            "source_sha256": sha256_file(it["src"]),
            "source_crop_box": list(it["box"]) if it["box"] else None,
            "source_subject_bbox": list(it["bbox"]),
            "source_subject_size": [it["crop"].width, it["crop"].height],
            "output": str(out_path).replace("\\", "/"),
            "output_sha256": sha256_file(out_path),
            "output_size": [CELL_W, CELL_H],
            "drawn_size": [w, h],
            "drawn_offset": [ox, oy],
        })

    # 无字合图 1536x1024：前/左/右/后
    plain = Image.new("RGB", PLAIN_BOARD, (255, 255, 255))
    for i, (_it, cell) in enumerate(cells):
        plain.paste(cell, (i * CELL_W, 0))
    plain_path = OUT_DIR / "chain_sickle_four_view_plain.png"
    plain.save(plain_path)

    # 中文标注版 1856x1440
    W, H = ANNOT_BOARD
    gap, margin = 64, 64
    panel_y = 150
    title_f = ImageFont.truetype(FONT_BOLD, 44)
    label_f = ImageFont.truetype(FONT_BOLD, 30)
    dir_f = ImageFont.truetype(FONT_REGULAR, 22)
    ann = Image.new("RGB", (W, H), (255, 255, 255))
    draw = ImageDraw.Draw(ann)
    draw.text((margin, 48), "追命锁链 · 正交四视图（FRONT / LEFT / RIGHT / BACK）", font=title_f, fill=(24, 28, 34))
    draw.text((margin, 104), "三国战场风格 金属锁钩 · 基础档配色 · 供 3D 建模参考（四张为独立重绘，非同一模型投影）",
              font=dir_f, fill=(90, 98, 108))

    def wrap(text: str, font, max_w: int, max_lines: int = 2):
        """按像素宽度折行；超出 max_lines 时截断加省略号。"""
        lines, cur = [], ""
        for ch in text:
            trial = cur + ch
            if draw.textlength(trial, font=font) > max_w and cur:
                lines.append(cur)
                cur = ch
                if len(lines) == max_lines:
                    break
            else:
                cur = trial
        if len(lines) < max_lines and cur:
            lines.append(cur)
        if len(lines) == max_lines and len("".join(lines)) < len(text):
            lines[-1] = lines[-1][:-1] + "…"
        return lines

    for i, (it, cell) in enumerate(cells):
        x = margin + i * (CELL_W + gap)
        ann.paste(cell, (x, panel_y))
        draw.rectangle([x, panel_y, x + CELL_W - 1, panel_y + CELL_H - 1], outline=(206, 212, 220))
        draw.text((x, panel_y + CELL_H + 26), it["label"], font=label_f, fill=(24, 28, 34))
        draw.text((x, panel_y + CELL_H + 72), it["direction"], font=dir_f, fill=(90, 98, 108))
        draw.text((x, panel_y + CELL_H + 104), it["axis"], font=dir_f, fill=(90, 98, 108))
        for j, line in enumerate(wrap(it["note"], dir_f, CELL_W - 6)):
            draw.text((x, panel_y + CELL_H + 136 + j * 26), line, font=dir_f, fill=(120, 128, 138))
    ann_path = OUT_DIR / "chain_sickle_four_view_annotated.png"
    ann.save(ann_path)

    record["plain_board"] = {"path": str(plain_path).replace("\\", "/"),
                             "size": list(PLAIN_BOARD), "sha256": sha256_file(plain_path),
                             "order": ["front", "left", "right", "back"]}
    record["annotated_board"] = {"path": str(ann_path).replace("\\", "/"),
                                 "size": list(ANNOT_BOARD), "sha256": sha256_file(ann_path),
                                 "font": FONT_REGULAR}
    (OUT_DIR / "generation_record.json").write_text(
        json.dumps(record, indent=2, ensure_ascii=False), encoding="utf-8")
    print(json.dumps(record, indent=2, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
