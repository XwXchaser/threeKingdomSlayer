# -*- coding: utf-8 -*-
"""撞脸对照表：把若干敌兵的头部裁出来并排放大，用于检查新身份图是否与已有兵种撞脸。

用法
    python Tools/visual_brief/build_face_compare_sheet.py --out <path.png> \
        --entry "1021新:<img>:<x0,y0,x1,y1>" --entry "1013旗手:<img>:<x0,y0,x1,y1>" ...
"""
from __future__ import annotations

import argparse

from PIL import Image, ImageDraw, ImageFont


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", required=True)
    ap.add_argument("--entry", action="append", required=True,
                    help="标签:图片路径:x0,y0,x1,y1（可重复）")
    ap.add_argument("--cell", type=int, default=340)
    ap.add_argument("--label-h", type=int, default=34)
    args = ap.parse_args()

    entries = []
    for e in args.entry:
        # 路径里含 "C:"，所以先用最后一个冒号切包围盒，再用第一个冒号切标签/路径
        rest, box = e.rsplit(":", 1)
        label, path = rest.split(":", 1)
        x0, y0, x1, y1 = [int(v) for v in box.split(",")]
        im = Image.open(path).convert("RGB").crop((x0, y0, x1, y1))
        entries.append((label, im))

    cell = args.cell
    pad = 14
    board = Image.new("RGB", (len(entries) * (cell + pad) + pad,
                              cell + args.label_h + pad * 2), (40, 42, 48))
    dr = ImageDraw.Draw(board)
    try:
        font = ImageFont.truetype("C:/Windows/Fonts/msyh.ttc", 20)
    except Exception:
        font = ImageFont.load_default()
    for i, (label, im) in enumerate(entries):
        t = im.copy()
        if max(t.size) < cell:          # 小裁片放大，便于目视比脸
            k = cell / float(max(t.size))
            t = t.resize((max(1, round(t.width * k)), max(1, round(t.height * k))), Image.LANCZOS)
        else:
            t.thumbnail((cell, cell), Image.LANCZOS)
        x = pad + i * (cell + pad)
        y = pad + (cell - t.height) // 2
        board.paste(t, (x, y))
        dr.rectangle([x, y, x + t.width, y + t.height], outline=(210, 210, 210))
        dr.text((x + 4, pad + cell + 4), label, fill=(240, 240, 240), font=font)
    board.save(args.out)
    print("对照表: %s  (%dx%d, %d 张)" % (args.out, board.width, board.height, len(entries)))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
