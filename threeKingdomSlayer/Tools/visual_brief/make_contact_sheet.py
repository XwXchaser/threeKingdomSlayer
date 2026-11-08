#!/usr/bin/env python
"""把一批参考图拼成带编号的 contact sheet（供用户挑选）。

用法：
    python Tools/visual_brief/make_contact_sheet.py \
        --dir Enemy1013ArtSource/00_Reference/walk --out Enemy1013ArtSource/00_Reference/walk/sheet.png
"""

import argparse
import math
import os

from PIL import Image, ImageDraw, ImageFont


def load_font(size):
    for name in ("msyh.ttc", "msyh.ttf", "simhei.ttf", "arial.ttf"):
        for root in (r"C:\Windows\Fonts", "/usr/share/fonts/truetype/dejavu"):
            path = os.path.join(root, name)
            if os.path.exists(path):
                try:
                    return ImageFont.truetype(path, size)
                except Exception:  # noqa: BLE001
                    continue
    return ImageFont.load_default()


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--dir", required=True)
    ap.add_argument("--out", required=True)
    ap.add_argument("--cols", type=int, default=6)
    ap.add_argument("--cell", type=int, default=220)
    ap.add_argument("--pattern", default="*.png")
    ap.add_argument("--recursive", action="store_true")
    args = ap.parse_args()

    files = []
    if args.recursive:
        for root, _dirs, names in os.walk(args.dir):
            for name in names:
                if name.lower().endswith((".png", ".jpg", ".jpeg", ".webp")):
                    files.append(os.path.relpath(os.path.join(root, name), args.dir).replace("\\", "/"))
        files.sort()
    else:
        files = sorted(f for f in os.listdir(args.dir)
                       if f.lower().endswith((".png", ".jpg", ".jpeg", ".webp")))
    files = [f for f in files if os.path.basename(f) != os.path.basename(args.out)]
    if not files:
        print("[ERROR] no images")
        return 1

    cell = args.cell
    cols = args.cols
    rows = math.ceil(len(files) / cols)
    sheet = Image.new("RGB", (cols * cell, rows * (cell + 22)), (18, 18, 20))
    draw = ImageDraw.Draw(sheet)
    font = load_font(14)

    for i, name in enumerate(files):
        path = os.path.join(args.dir, name)
        try:
            im = Image.open(path).convert("RGB")
        except Exception:  # noqa: BLE001
            continue
        im.thumbnail((cell - 8, cell - 8))
        x = (i % cols) * cell
        y = (i // cols) * (cell + 22)
        ox = x + (cell - im.width) // 2
        oy = y + (cell - im.height) // 2
        sheet.paste(im, (ox, oy))
        draw.rectangle([x + 1, y + 1, x + cell - 2, y + cell - 2], outline=(60, 60, 70))
        draw.text((x + 5, y + cell + 3), name, fill=(220, 220, 230), font=font)

    sheet.save(args.out)
    print(f"[OK] {len(files)} images -> {args.out} ({sheet.width}x{sheet.height})")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
