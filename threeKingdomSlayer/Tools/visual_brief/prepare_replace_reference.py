# -*- coding: utf-8 -*-
"""为「替换视频工作流」准备身份图上传副本：把已验收身份图缩放到母片的构图与色键上。

为什么要做副本（而不是直接传 1024² 原图）：
  · **几何对齐**——母片（960 空间）人物本体高 550、脚底 y=780、腿中心 x=476.6；
    身份图原图是 1024² / 本体 696 / 脚底 895。两者取景尺度不同，模型可能在
    “用视频的尺度”还是“用图的尺度”之间摇摆，输出角色会比 idle 大一圈。
  · **色键对齐（G1）**——母片实测底色 (0,172,57)；身份图底色 (0,159,62)。
    同一请求里两张参考图底色不同，成片底色不可预期。只改副本的底色，**主体像素原样保留**，
    不做生图重绘、不做整体调色。

输出副本是**临时上传件**，不回写已验收资产。

用法
    python Tools/visual_brief/prepare_replace_reference.py \
        --identity Enemy1021ArtSource/01_KeyPosePreparation/1021_idle_identity_greenscreen_1024.png \
        --size 960 --body-h 550 --foot-y 780 --body-center-x 476.6 \
        --src-bg "0,159,62" --dst-bg "0,172,57" \
        --out C:/Users/steam/Pictures/gptGen/enemy1021_attack_replace/1021_identity_for_attack_960.png
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os

import numpy as np
from PIL import Image

BG_TOL = 45          # 判定"底色像素"的容差（与主键距离）
LEG_BAND = 0.25      # 取底部 25% 求腿中心


def parse_rgb(s: str):
    return tuple(int(v) for v in s.split(","))


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--identity", required=True)
    ap.add_argument("--size", type=int, default=960)
    ap.add_argument("--body-h", type=int, required=True)
    ap.add_argument("--foot-y", type=int, required=True)
    ap.add_argument("--body-center-x", type=float, required=True)
    ap.add_argument("--src-bg", required=True)
    ap.add_argument("--dst-bg", required=True)
    ap.add_argument("--out", required=True)
    args = ap.parse_args()

    src_bg = parse_rgb(args.src_bg)
    dst_bg = parse_rgb(args.dst_bg)

    im = Image.open(args.identity).convert("RGB")
    a = np.asarray(im).astype(int)
    d = np.linalg.norm(a - np.array(src_bg, dtype=float), axis=2)
    mask = d > BG_TOL                      # 主体像素
    ys, xs = np.nonzero(mask)
    x0, x1, y0, y1 = int(xs.min()), int(xs.max()), int(ys.min()), int(ys.max())
    subj_h = y1 - y0 + 1
    crop = im.crop((x0, y0, x1 + 1, y1 + 1))

    # 裁切内的腿中心（底部 25%）
    ca = np.asarray(crop).astype(int)
    cmask = np.linalg.norm(ca - np.array(src_bg, dtype=float), axis=2) > BG_TOL
    band = cmask[int(subj_h * (1 - LEG_BAND)):, :]
    _, bx = np.nonzero(band)
    leg_in_crop = float(bx.mean())

    k = args.body_h / float(subj_h)
    new_w = max(1, round(crop.width * k))
    new_h = max(1, round(crop.height * k))
    big = crop.resize((new_w, new_h), Image.LANCZOS)

    # 底色换键：只把"底色像素"改成目标底色，主体像素原样保留
    ba = np.asarray(big).astype(int)
    bmask = np.linalg.norm(ba - np.array(src_bg, dtype=float), axis=2) <= BG_TOL
    out = ba.copy()
    out[bmask] = np.array(dst_bg, dtype=int)
    # 主体边缘的半透明绿边统一归到新底色，避免出现旧键色残留
    edge = (~bmask) & (np.linalg.norm(ba - np.array(src_bg, dtype=float), axis=2) <= BG_TOL * 2)
    out[edge] = np.array(dst_bg, dtype=int)
    big = Image.fromarray(out.astype(np.uint8), mode="RGB")

    canvas = Image.new("RGB", (args.size, args.size), dst_bg)
    ox = int(round(args.body_center_x - leg_in_crop * k))
    oy = args.foot_y - new_h + 1
    canvas.paste(big, (ox, oy))

    os.makedirs(os.path.dirname(args.out), exist_ok=True)
    canvas.save(args.out)

    # 回读量测
    ca2 = np.asarray(canvas).astype(int)
    m2 = np.linalg.norm(ca2 - np.array(dst_bg, dtype=float), axis=2) > 79
    ys2, xs2 = np.nonzero(m2)
    body_top = int(ys2.min())
    feet = int(ys2.max())
    band2 = m2[int(feet - (feet - body_top) * LEG_BAND):feet + 1, :]
    _, lx2 = np.nonzero(band2)
    corners = np.concatenate([ca2[:64, :64].reshape(-1, 3), ca2[:64, -64:].reshape(-1, 3),
                               ca2[-64:, :64].reshape(-1, 3), ca2[-64:, -64:].reshape(-1, 3)])

    report = {
        "source": args.identity,
        "output": args.out,
        "size": [args.size, args.size],
        "source_body_h": subj_h,
        "source_leg_center_x_in_crop": round(leg_in_crop, 1),
        "scale": round(k, 5),
        "paste_xy": [ox, oy],
        "target": {"body_h": args.body_h, "foot_y": args.foot_y, "body_center_x": args.body_center_x},
        "measured": {
            "body_top_y": body_top,
            "foot_y": feet,
            "body_h": feet - body_top,
            "leg_center_x": round(float(lx2.mean()), 1),
            "bbox_x": [int(xs2.min()), int(xs2.max())],
        },
        "bg": {"src": list(src_bg), "dst": list(dst_bg),
               "corner_median": [int(v) for v in np.median(corners, axis=0)],
               "corner_std": [round(float(v), 2) for v in corners.std(axis=0)]},
        "bytes": os.path.getsize(args.out),
        "sha256": hashlib.sha256(open(args.out, "rb").read()).hexdigest(),
    }
    rp = os.path.splitext(args.out)[0] + ".layout.json"
    with open(rp, "w", encoding="utf-8") as fh:
        json.dump(report, fh, indent=2, ensure_ascii=False)
    print(json.dumps(report, indent=2, ensure_ascii=False))
    print("\n量测报告: %s" % rp)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
