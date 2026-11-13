# -*- coding: utf-8 -*-
"""面谱候选板：把 enemy_face_sheet_v1 / v5 的 6 格 + 1021 相册裁成带标签的候选板，
并单独导出每张脸的裁切（供生图当参考图输入）。

用法：
    python Tools/visual_brief/build_face_candidate_board.py \
        --out-dir C:/Users/steam/Pictures/gptGen/enemy1021_face_candidates
"""
from __future__ import annotations

import argparse
import os

from PIL import Image, ImageDraw, ImageFont

GG = r"C:/Users/steam/Pictures/gptGen"
SOURCES = [
    ("v1", os.path.join(GG, "enemy_face_sheet_v1/enemy_face_sheet_v1.png"), "面谱v1"),
    ("v5", os.path.join(GG, "enemy_face_sheet_v5/enemy_face_sheet_v5.png"), "面谱v5"),
]
GREEN_TOL = 40


def is_bg(px):
    r, g, b = px[:3]
    return g > 120 and g - max(r, b) > GREEN_TOL


def trim(img):
    """按非绿像素裁掉绿幕边。"""
    rgb = img.convert("RGB")
    w, h = rgb.size
    px = rgb.load()
    xs, ys = [], []
    for y in range(0, h, 2):
        for x in range(0, w, 2):
            if not is_bg(px[x, y]):
                xs.append(x)
                ys.append(y)
    if not xs:
        return img
    return img.crop((min(xs), min(ys), max(xs) + 1, max(ys) + 1))


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--out-dir", required=True)
    args = ap.parse_args()
    os.makedirs(args.out_dir, exist_ok=True)

    crops = []
    for tag, path, label in SOURCES:
        sheet = Image.open(path).convert("RGB")
        sw, sh = sheet.size
        cw, ch = sw // 3, sh // 2
        for idx in range(6):
            cx = (idx % 3) * cw
            cy = (idx // 3) * ch
            cell = sheet.crop((cx, cy, cx + cw, cy + ch))
            face = trim(cell)
            name = "%s_c%d.png" % (tag, idx + 1)
            face.save(os.path.join(args.out_dir, name))
            crops.append((name, "%s 第%d格" % (label, idx + 1), face))
            print("%-14s %s  %s" % (name, face.size, "from %s" % os.path.basename(path)))

    # 1021 自己的相册（整身，盾在画面左）
    p1021 = os.path.join(GG, "enemy1021_face_identity_v1/enemy1021_face_identity_v1.png")
    if os.path.isfile(p1021):
        im = Image.open(p1021).convert("RGB")
        w, h = im.size
        im.crop((int(w * 0.30), 0, int(w * 0.78), int(h * 0.42))).save(
            os.path.join(args.out_dir, "1021_face_v1.png"))
        crops.append(("1021_face_v1.png", "1021相册v1", Image.open(
            os.path.join(args.out_dir, "1021_face_v1.png"))))

    # 候选板
    CELL = 300
    PAD = 12
    LABEL_H = 30
    cols = 5
    rows = (len(crops) + cols - 1) // cols
    board = Image.new("RGB", (cols * (CELL + PAD) + PAD,
                              rows * (CELL + LABEL_H + PAD) + PAD), (38, 40, 46))
    dr = ImageDraw.Draw(board)
    try:
        font = ImageFont.truetype("C:/Windows/Fonts/msyh.ttc", 18)
    except Exception:
        font = ImageFont.load_default()
    for i, (name, label, im) in enumerate(crops):
        r, c = divmod(i, cols)
        x = PAD + c * (CELL + PAD)
        y = PAD + r * (CELL + LABEL_H + PAD)
        t = im.copy()
        t.thumbnail((CELL, CELL), Image.LANCZOS)
        board.paste(t, (x + (CELL - t.width) // 2, y + (CELL - t.height) // 2))
        dr.text((x + 4, y + CELL + 4), "%d. %s" % (i + 1, label), fill=(235, 235, 235), font=font)
        dr.rectangle([x, y, x + CELL, y + CELL], outline=(90, 95, 105))
    out = os.path.join(args.out_dir, "face_candidates_board.png")
    board.save(out)
    print("\n候选板: %s  (%dx%d, %d 张)" % (out, board.width, board.height, len(crops)))
    for i, (name, label, _im) in enumerate(crops):
        print("  %2d. %-16s %s" % (i + 1, name, label))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
