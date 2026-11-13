# -*- coding: utf-8 -*-
"""1021 attack 选帧（替换片）。

流程
    1. **反查旧映射**——把已部署的 12 张 `Enemy_1021_attack*.png`（1108 RGBA）与母片
       `enemy1021_shield_attack_v1.mp4` 的每一帧做归一化轮廓 IoU 匹配，反推出旧片当年
       是从母片的哪几帧选的，得到 1021 自己的「峰值偏移量」。
    2. **套用同一相位**——替换片与母片逐帧动作一致（已在 verify_replace_video.py 量过），
       所以同一组源帧号直接套到替换片上，得到 12 个新帧。
    3. 导出：原始绿幕帧（下游用）+ 抠图透明帧（评审用）+ 评审对照表 + 逐帧量测。

为什么不用 `pick_attack_frames.py` 的 PEAK_OFFSETS：那是 1012 十六帧攻击的常量
（举刀过顶 → 劈砍，步长 8–9 / 1–6），1021 是十二帧的重盾击，节拍结构不同，
必须用 1021 自己的偏移量，不能照抄。

用法
    python Tools/visual_brief/pick_1021_attack_frames.py \
        --video C:/Users/steam/Videos/doubaoVideo/enemy1021_shield_attack_replace_v1.mp4 \
        --deployed Assets/Sprites/Enemy/Enemy1021 \
        --out C:/Users/steam/Pictures/gptGen/enemy1021_attack_replace/selected_frames
"""
from __future__ import annotations

import argparse
import json
import os
import pathlib
import sys

import av
import numpy as np
from PIL import Image, ImageDraw, ImageFont

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
from chroma_key import key_frame  # noqa: E402


def norm_mask(mask, box, size=64):
    x0, y0, x1, y1 = box
    im = Image.fromarray((mask[y0:y1, x0:x1] * 255).astype("uint8")).resize((size, size), Image.LANCZOS)
    return np.asarray(im) > 64


def iou(a, b):
    h = max(a.shape[0], b.shape[0])
    w = max(a.shape[1], b.shape[1])
    A = np.zeros((h, w), bool)
    B = np.zeros((h, w), bool)
    A[:a.shape[0], :a.shape[1]] = a
    B[:b.shape[0], :b.shape[1]] = b
    u = (A | B).sum()
    return (A & B).sum() / u if u else 0.0


def mask_of(alpha):
    m = alpha > 10
    ys, xs = np.where(m)
    return m, (int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1)


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--video", required=True)
    ap.add_argument("--mother", required=True)
    ap.add_argument("--deployed", required=True, help="已部署 attack 精灵目录（1108 RGBA）")
    ap.add_argument("--out", required=True)
    ap.add_argument("--count", type=int, default=12)
    args = ap.parse_args()

    out = pathlib.Path(args.out)
    (out / "raw").mkdir(parents=True, exist_ok=True)
    (out / "keyed").mkdir(parents=True, exist_ok=True)

    # ---- 母片与新片解码 + 抠图 ----
    def decode_keyed(path):
        c = av.open(path)
        frames = []
        for fr in c.decode(video=0):
            rgb = fr.to_ndarray(format="rgb24")
            alpha, _ = key_frame(rgb)
            m, box = mask_of(alpha)
            frames.append({"rgb": rgb, "alpha": alpha, "mask": m, "box": box,
                           "norm": norm_mask(m, box)})
        return frames

    print("解码并抠图 母片 …")
    mot = decode_keyed(args.mother)
    print("解码并抠图 新片 …")
    new = decode_keyed(args.video)
    print("  母片 %d 帧 / 新片 %d 帧" % (len(mot), len(new)))

    # ---- 1. 反查旧映射：已部署帧 ↔ 母片帧 ----
    dep = []
    for i in range(1, args.count + 1):
        p = pathlib.Path(args.deployed) / ("Enemy_1021_attack%d.png" % i)
        arr = np.asarray(Image.open(p).convert("RGBA"))
        m = arr[:, :, 3] > 10
        ys, xs = np.where(m)
        box = (int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1)
        dep.append({"name": p.name, "norm": norm_mask(m, box), "box": box,
                    "h": int(ys.max() - ys.min() + 1)})

    mapping = []
    for d in dep:
        ious = [iou(d["norm"], f["norm"]) for f in mot]
        order = np.argsort(ious)[::-1]
        mapping.append({"deployed": d["name"],
                         "best": int(order[0]), "best_iou": round(float(ious[order[0]]), 4),
                         "second": int(order[1]), "second_iou": round(float(ious[order[1]]), 4)})
    print("\n== 已部署帧 ↔ 母片帧 反查 ==")
    for m in mapping:
        print("  %-26s -> 母片 f%03d (IoU %.3f)   次优 f%03d (%.3f)"
              % (m["deployed"], m["best"], m["best_iou"], m["second"], m["second_iou"]))

    src = [m["best"] for m in mapping]
    peak = int(src[5]) if len(src) > 5 else int(np.argmax([0]))
    print("\n  旧映射源帧号: %s" % src)
    print("  以第 6 张（接触帧）为基准的偏移: %s" % [s - peak for s in src])

    # ---- 2. 套到新片 ----
    picks = [min(max(s, 0), len(new) - 1) for s in src]
    print("\n== 新片选帧 ==\n  帧号: %s" % picks)

    # ---- 3. 导出 ----
    report = {"video": args.video, "mother": args.mother, "count": args.count,
              "old_mapping": mapping, "source_frames": src, "new_picks": picks,
              "peak_index": peak, "frames": []}
    thumbs = []
    for k, fi in enumerate(picks, 1):
        fr = new[fi]
        Image.fromarray(fr["rgb"]).save(out / "raw" / ("attack%02d_f%03d.png" % (k, fi)))
        rgba = np.zeros((fr["rgb"].shape[0], fr["rgb"].shape[1], 4), np.uint8)
        rgba[:, :, :3] = fr["rgb"]
        rgba[:, :, 3] = fr["alpha"]
        Image.fromarray(rgba, "RGBA").save(out / "keyed" / ("attack%02d_f%03d.png" % (k, fi)))
        m = fr["mask"]
        ys, xs = np.where(m)
        R, G, B = fr["rgb"][:, :, 0].astype(int), fr["rgb"][:, :, 1].astype(int), fr["rgb"][:, :, 2].astype(int)
        y0, y1 = int(ys.min()), int(ys.max())
        hcut = max(1, int(y0 + (y1 - y0) * 0.30))
        head = m[:hcut, :]
        red_head = head & (R[:hcut] - G[:hcut] > 90) & (R[:hcut] - B[:hcut] > 90) & (R[:hcut] > 120)
        wood = m & (R > 95) & (R < 200) & (R - G > 35) & (R - G < 105) & (G - B > 12) & (G - B < 75)
        wy, wx = np.nonzero(wood)
        mid = (int(xs.min()) + int(xs.max())) / 2.0
        report["frames"].append({
            "index": k, "src_frame": fi, "time_s": round(fi / 24.0, 3),
            "body_h": int(y1 - y0), "width": int(xs.max() - xs.min()),
            "bbox": [int(xs.min()), y0, int(xs.max()), y1],
            "red_head_frac": round(float(red_head.sum()) / max(1, int(head.sum())), 5),
            "wood_centroid_x": round(float(wx.mean()), 1) if len(wx) else None,
            "bbox_mid_x": mid,
            "shield_side": ("left" if (wx.mean() < mid) else "right") if len(wx) else None,
        })
        thumbs.append(Image.open(out / "keyed" / ("attack%02d_f%03d.png" % (k, fi))))

    # 评审对照表：上排=新片选帧（透明贴中性底），下排=已部署旧帧
    cell = 200
    sheet = Image.new("RGB", (args.count * cell, 2 * cell + 46), (36, 38, 44))
    dr = ImageDraw.Draw(sheet)
    try:
        font = ImageFont.truetype("C:/Windows/Fonts/msyh.ttc", 17)
    except Exception:
        font = ImageFont.load_default()
    for k in range(args.count):
        im = thumbs[k].copy()
        bg = Image.new("RGBA", im.size, (58, 60, 66, 255))
        bg.alpha_composite(im)
        sheet.paste(bg.convert("RGB").resize((cell, cell), Image.LANCZOS), (k * cell, 0))
        dr.text((k * cell + 6, cell + 4), "新 %02d f%03d" % (k + 1, picks[k]), fill=(235, 235, 235), font=font)
        im2 = Image.open(pathlib.Path(args.deployed) / ("Enemy_1021_attack%d.png" % (k + 1))).convert("RGBA")
        bg2 = Image.new("RGBA", im2.size, (58, 60, 66, 255))
        bg2.alpha_composite(im2)
        sheet.paste(bg2.convert("RGB").resize((cell, cell), Image.LANCZOS), (k * cell, cell + 34))
        dr.text((k * cell + 6, 2 * cell + 12), "旧 %02d" % (k + 1), fill=(200, 200, 200), font=font)
    sheet.save(out / "selection_review_sheet.png")

    with open(out / "selection.json", "w", encoding="utf-8") as fh:
        json.dump(report, fh, indent=2, ensure_ascii=False)

    print("\n== 逐帧量测 ==")
    print("  #   源帧  时间   身高  宽   头部红%   木盾质心  人物中点 盾侧")
    for fr in report["frames"]:
        print("  %-3d f%03d  %.3fs  %-5d %-4d %-9.5f %-9.0f %-8.0f %s"
              % (fr["index"], fr["src_frame"], fr["time_s"], fr["body_h"], fr["width"],
                 fr["red_head_frac"], fr["wood_centroid_x"] or -1, fr["bbox_mid_x"], fr["shield_side"]))
    print("\n输出目录: %s" % out)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
