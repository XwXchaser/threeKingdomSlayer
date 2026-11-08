# -*- coding: utf-8 -*-
"""绿幕抠图（图生视频 → 透明精灵帧），口径复刻项目既有 SVL 配方。

配方（实测反推自归档产物，见下）
    · 主键阈值 T=79：到绿幕基准色 (0,159,62) 的欧氏距离 > T 记为主体
    · halo=1：主体边缘 1–2px 的绿味像素一并抠掉（去绿边）
    · 输出 alpha 严格二值（0 / 255）—— 归档产物「半透明 0」
    · 幕布残留 0：主体区内不再有绿味像素
    · 孔 0：主体内部不残留被挖空的针孔

用法
    python Tools/visual_brief/chroma_key.py \
        --video "C:/Users/steam/Videos/doubaoVideo/xxx.mp4" \
        --out   Library/Locus/tmp/xxx/keyed_ui_t79

    # 与归档产物对拍（回归验证）
    python Tools/visual_brief/chroma_key.py --video <mp4> --out <dir> \
        --compare-against <归档抠图目录>
"""
from __future__ import annotations

import argparse
import json
import pathlib

import av
import numpy as np
from PIL import Image

GREEN = np.array([0.0, 159.0, 62.0])
TOL = 79.0        # 主键阈值
HALO_D = 140.0    # 绿味判定（边缘去绿边用）


def decode(path: pathlib.Path):
    out = []
    with av.open(str(path)) as c:
        s = c.streams.video[0]
        s.thread_type = "AUTO"
        for fr in c.decode(s):
            out.append(fr.to_ndarray(format="rgb24"))
    return out


def key_frame(rgb: np.ndarray, halo_rounds: int = 2):
    """返回 (alpha uint8 0/255, 距离场)。"""
    d = np.linalg.norm(rgb.astype(np.float32) - GREEN, axis=2)
    alpha = (d > TOL).astype(np.uint8) * 255

    if (alpha > 0).any():
        # halo：主体边缘一圈仍带绿味的像素 → 抠掉
        greenish = (d < HALO_D) & (alpha > 0)
        for _ in range(halo_rounds):               # 迭代，覆盖 2px 绿边
            border = np.zeros_like(greenish)
            border[1:, :] |= alpha[:-1, :] == 0
            border[:-1, :] |= alpha[1:, :] == 0
            border[:, 1:] |= alpha[:, :-1] == 0
            border[:, :-1] |= alpha[:, 1:] == 0
            kill = greenish & border
            if not kill.any():
                break
            alpha[kill] = 0
            greenish = (d < HALO_D) & (alpha > 0)

        # 孔填充：四邻全主体的背景像素 → 补上
        for _ in range(2):
            filled = alpha.copy()
            filled[1:-1, 1:-1] = np.where(
                (alpha[:-2, 1:-1] > 0) & (alpha[2:, 1:-1] > 0)
                & (alpha[1:-1, :-2] > 0) & (alpha[1:-1, 2:] > 0),
                255, alpha[1:-1, 1:-1])
            if (filled == alpha).all():
                break
            alpha = filled
    return alpha, d


def frame_stats(alpha: np.ndarray, rgb: np.ndarray, d: np.ndarray):
    m = alpha > 10
    if not m.any():
        return None
    ys, xs = np.where(m)
    sub = rgb[m].astype(np.float32)
    residue = int((np.linalg.norm(sub - GREEN, axis=1) < 90).sum())
    return {
        "bbox": [int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1],
        "h": int(ys.max() - ys.min() + 1),
        "w": int(xs.max() - xs.min() + 1),
        "cx": float((xs.min() + xs.max()) / 2.0),
        "opaque": int(m.sum()),
        "semi": int(((alpha > 10) & (alpha < 245)).sum()),
        "gex_residue": residue,
        "max_subject_d": round(float(d[m].max()), 1),
        "min_bg_d": round(float(d[~m].min()), 1) if (~m).any() else None,
    }


def main() -> int:
    global TOL, HALO_D
    ap = argparse.ArgumentParser(description="绿幕抠图")
    ap.add_argument("--video", required=True, type=pathlib.Path)
    ap.add_argument("--out", required=True, type=pathlib.Path)
    ap.add_argument("--compare-against", type=pathlib.Path, default=None,
                    help="归档抠图目录，逐帧对拍 alpha 一致率")
    ap.add_argument("--tol", type=float, default=TOL, help="主键阈值（到绿幕基准色的距离）")
    ap.add_argument("--halo-dist", type=float, default=HALO_D, help="绿味判定距离（边缘去绿边用）")
    ap.add_argument("--halo-rounds", type=int, default=2, help="边缘扩张轮数")
    args = ap.parse_args()

    TOL = args.tol
    HALO_D = args.halo_dist
    halo_rounds = args.halo_rounds

    frames = decode(args.video)
    out = args.out
    out.mkdir(parents=True, exist_ok=True)
    stats = []
    for i, rgb in enumerate(frames):
        alpha, d = key_frame(rgb, halo_rounds)
        rgba = np.dstack([rgb, alpha])
        Image.fromarray(rgba, "RGBA").save(out / ("f%03d.png" % i))
        s = frame_stats(alpha, rgb, d)
        if s:
            s["name"] = "f%03d.png" % i
            stats.append(s)

    (out / "key_stats.json").write_text(json.dumps(stats, ensure_ascii=False, indent=2), encoding="utf-8")
    semi = sum(s["semi"] for s in stats)
    resid = sum(s["gex_residue"] for s in stats)
    print("帧数 %d  →  %s" % (len(frames), out))
    print("半透明像素合计: %d   幕布残留合计: %d" % (semi, resid))
    if stats:
        print("主体高 %d–%d   中心x %.1f–%.1f"
              % (min(s["h"] for s in stats), max(s["h"] for s in stats),
                 min(s["cx"] for s in stats), max(s["cx"] for s in stats)))

    if args.compare_against:
        agree = []
        for p in sorted(args.compare_against.glob("f*.png")):
            q = out / p.name
            if not q.exists():
                continue
            a = np.asarray(Image.open(p).convert("RGBA"))[:, :, 3] > 128
            b = np.asarray(Image.open(q).convert("RGBA"))[:, :, 3] > 128
            agree.append((a == b).mean())
        if agree:
            print("与归档抠图对拍：逐帧 alpha 一致率 平均 %.3f%%  最低 %.3f%%  (%d 帧)"
                  % (100 * float(np.mean(agree)), 100 * float(np.min(agree)), len(agree)))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
