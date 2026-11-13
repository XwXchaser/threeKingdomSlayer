# -*- coding: utf-8 -*-
"""把选中的攻击帧按**已部署旧帧反查出来的口径**落位进 1108 精灵画布。

旧部署帧量出来的事实（不是拍脑袋定的）：
  · **脚底线恒为 870**（12 帧全部）
  · **逐帧缩放**：源片里盾前突时包围盒会涨到 634，旧部署把它压回 518 → 缩放系数约 0.82，
    即"整帧尺度对齐"：不让角色在动作中看起来忽大忽小
  · **水平锚点跟随脚**：脚带中心 x 随动作在 515~604 之间变（不是固定值）

于是落位规则 = 逐帧对齐到旧部署帧的 (bbox 高, 脚底线, 脚带中心 x)：
    s_i   = 旧部署帧 i 的 bbox 高 / 新选帧 i 的 bbox 高
    平移  = 让新帧的脚底线落到 870、脚带中心 x 落到旧部署帧 i 的脚带中心 x

结果：新旧两套 12 帧的几何曲线完全一致，只换掉了角色本身。

用法
    python Tools/visual_brief/land_attack_frames.py \
        --keyed    C:/Users/steam/Pictures/gptGen/enemy1021_attack_replace/selected_frames/keyed \
        --deployed Assets/Sprites/Enemy/Enemy1021 \
        --out      C:/Users/steam/Pictures/gptGen/enemy1021_attack_replace/selected_frames/landed_1108
"""
from __future__ import annotations

import argparse
import json
import os
import pathlib
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
from compose_identity_from_alpha import fill_holes  # noqa: E402

CANVAS = 1108
FOOT_Y = 870
BAND = 0.12          # 脚带高度占比（与反查脚本一致）


def anchor(alpha):
    m = alpha > 10
    ys, xs = np.where(m)
    y0, y1 = int(ys.min()), int(ys.max())
    b0 = int(y1 - (y1 - y0) * BAND)
    by, bx = np.where(m[b0:y1 + 1, :])
    return {"y0": y0, "y1": y1, "x0": int(xs.min()), "x1": int(xs.max()),
            "h": y1 - y0, "w": int(xs.max() - xs.min()),
            "foot_cx": float(bx.mean()), "foot_y": y1}


def resize_rgba(img, s):
    nw, nh = max(1, round(img.width * s)), max(1, round(img.height * s))
    a = np.asarray(img).astype(np.float32) / 255.0
    rgb, al = a[:, :, :3], a[:, :, 3:4]
    pre = np.concatenate([rgb * al, al], axis=2)          # 预乘，避免透明边发黑
    pim = Image.fromarray((pre * 255).astype(np.uint8), "RGBA").resize((nw, nh), Image.LANCZOS)
    pa = np.asarray(pim).astype(np.float32) / 255.0
    rgb2, al2 = pa[:, :, :3], pa[:, :, 3:4]
    al2 = np.clip(al2, 0, 1)
    rgb2 = np.where(al2 > 0, rgb2 / np.maximum(al2, 1e-6), 0)
    out = np.concatenate([rgb2, al2], axis=2)
    return Image.fromarray((np.clip(out, 0, 1) * 255).astype(np.uint8), "RGBA")


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--keyed", required=True)
    ap.add_argument("--deployed", required=True)
    ap.add_argument("--out", required=True)
    ap.add_argument("--prefix", default="Enemy_1021_attack")
    ap.add_argument("--count", type=int, default=12)
    args = ap.parse_args()

    out = pathlib.Path(args.out)
    out.mkdir(parents=True, exist_ok=True)

    old = []
    for i in range(1, args.count + 1):
        p = pathlib.Path(args.deployed) / ("Enemy_1021_attack%d.png" % i)
        old.append(anchor(np.asarray(Image.open(p).convert("RGBA"))[:, :, 3]))

    src = sorted(pathlib.Path(args.keyed).glob("attack*.png"),
                 key=lambda p: int("".join(c for c in p.stem.split("_")[0] if c.isdigit())))
    if len(src) != args.count:
        raise SystemExit("选帧数 %d 与旧帧数 %d 不符" % (len(src), args.count))

    report = {"canvas": CANVAS, "foot_y": FOOT_Y, "frames": []}
    thumbs = []
    for i, (p, o) in enumerate(zip(src, old), 1):
        img = Image.open(p).convert("RGBA")
        a = anchor(np.asarray(img)[:, :, 3])
        s = o["h"] / float(a["h"])
        big = resize_rgba(img, s)
        na = anchor(np.asarray(big)[:, :, 3])
        dx = int(round(o["foot_cx"] - na["foot_cx"]))
        dy = FOOT_Y - na["foot_y"]
        canvas = Image.new("RGBA", (CANVAS, CANVAS), (0, 0, 0, 0))
        canvas.alpha_composite(big, (dx, dy))
        # alpha 二值化 + 填封闭孔（与项目其它抠图帧同口径：半透明 0、幕布残留 0）
        ca = np.asarray(canvas).copy()
        m = ca[:, :, 3] >= 128
        m, holes = fill_holes(m)
        ca[:, :, 3] = np.where(m, 255, 0).astype(np.uint8)
        canvas = Image.fromarray(ca, "RGBA")
        dst = out / ("%s%d.png" % (args.prefix, i))
        canvas.save(dst)
        g = anchor(np.asarray(canvas)[:, :, 3])
        report["frames"].append({
            "index": i, "src": p.name, "out": dst.name, "bytes": os.path.getsize(dst),
            "scale": round(s, 4), "shift": [dx, dy], "filled_holes_px": int(holes),
            "new_bbox": [g["x0"], g["y0"], g["x1"], g["y1"]], "new_h": g["h"], "new_w": g["w"],
            "new_foot_y": g["foot_y"], "new_foot_cx": round(g["foot_cx"], 1),
            "old_bbox": [o["x0"], o["y0"], o["x1"], o["y1"]], "old_h": o["h"], "old_w": o["w"],
            "old_foot_y": o["foot_y"], "old_foot_cx": round(o["foot_cx"], 1),
        })
        thumbs.append(canvas)
        print("  %02d %-20s s=%.4f 位移(%d,%d)  新 高%d 脚底%d 脚心%.0f  |  旧 高%d 脚底%d 脚心%.0f"
              % (i, p.name, s, dx, dy, g["h"], g["foot_y"], g["foot_cx"],
                 o["h"], o["foot_y"], o["foot_cx"]))

    cell = 190
    sheet = Image.new("RGB", (len(thumbs) * cell, cell + 26), (58, 60, 66))
    dr = ImageDraw.Draw(sheet)
    try:
        font = ImageFont.truetype("C:/Windows/Fonts/msyh.ttc", 16)
    except Exception:
        font = ImageFont.load_default()
    for i, c in enumerate(thumbs):
        bg = Image.new("RGBA", c.size, (58, 60, 66, 255))
        bg.alpha_composite(c)
        sheet.paste(bg.convert("RGB").resize((cell, cell), Image.LANCZOS), (i * cell, 0))
        dr.text((i * cell + 6, cell + 4), "%02d" % (i + 1), fill=(235, 235, 235), font=font)
    sheet.save(out / "landed_review_sheet.png")

    with open(out / "landed.json", "w", encoding="utf-8") as fh:
        json.dump(report, fh, indent=2, ensure_ascii=False)

    print("\n  脚底线集合: %s" % sorted({f["new_foot_y"] for f in report["frames"]}))
    print("  高度(新): %s" % [f["new_h"] for f in report["frames"]])
    print("  高度(旧): %s" % [f["old_h"] for f in report["frames"]])
    print("  输出: %s" % out)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
