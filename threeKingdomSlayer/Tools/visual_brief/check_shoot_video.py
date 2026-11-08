# -*- coding: utf-8 -*-
"""射箭视频验收闸门：手中箭数 / 头部朝向 / 瞄准几何 / 画面稳定。

用法
    python Tools/visual_brief/check_shoot_video.py --video <成片.mp4> [--ref <v3失败片.mp4>]
"""
from __future__ import annotations

import argparse
import json
import pathlib
from collections import deque

import av
import numpy as np
from PIL import Image

GREEN = np.array([0., 159., 62.])
WHITE_MIN = 222          # 箭羽白
WHITE_SPREAD = 22        # 饱和度上限（白/灰）
BLOB_MIN = 40            # 连通块最小像素


def decode(path):
    out = []
    with av.open(str(path)) as c:
        s = c.streams.video[0]
        s.thread_type = "AUTO"
        for fr in c.decode(s):
            out.append(fr.to_ndarray(format="rgb24"))
    return out


def blobs(mask, min_px=BLOB_MIN):
    h, w = mask.shape
    seen = np.zeros_like(mask, bool)
    res = []
    for y in range(h):
        for x in range(w):
            if not mask[y, x] or seen[y, x]:
                continue
            q = deque([(y, x)])
            seen[y, x] = True
            n = 0
            cx = cy = 0
            while q:
                cy_, cx_ = q.popleft()
                n += 1
                cx += cx_
                cy += cy_
                for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    ny, nx = cy_ + dy, cx_ + dx
                    if 0 <= ny < h and 0 <= nx < w and mask[ny, nx] and not seen[ny, nx]:
                        seen[ny, nx] = True
                        q.append((ny, nx))
            if n >= min_px:
                res.append({"px": n, "cx": cx / n, "cy": cy / n})
    return res


def analyse(path):
    frames = decode(path)
    n = len(frames)
    fps = n / 6.0
    recs = []
    for i, f in enumerate(frames):
        a = f.astype(np.float32)
        d = np.linalg.norm(a - GREEN, axis=2)
        m = d > 79
        ys, xs = np.nonzero(m)
        mx = a.max(axis=2)
        mn = a.min(axis=2)
        white = (mx > WHITE_MIN) & ((mx - mn) < WHITE_SPREAD) & m
        bl = blobs(white)
        # 头部区域：主体上部 22%（弓在右侧，头在左上）
        y0 = int(ys.min())
        y1 = y0 + int((ys.max() - y0) * 0.22)
        head_sel = ys <= y1
        hb = blobs(white & (np.arange(m.shape[0])[:, None] <= y1))
        bg = np.concatenate([a[:64, :64].reshape(-1, 3), a[:64, -64:].reshape(-1, 3),
                             a[-64:, :64].reshape(-1, 3), a[-64:, -64:].reshape(-1, 3)])
        recs.append({
            "i": i, "t": round(i / fps, 2),
            "white_blobs": len(bl),
            "white_blobs_head": len(hb),
            "white_centroids": [[round(b["cx"]), round(b["cy"])] for b in bl],
            "bbox": [int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())],
            "foot": int(ys.max()), "top": int(ys.min()),
            "w": int(xs.max() - xs.min() + 1), "h": int(ys.max() - ys.min() + 1),
            "bg_d": [int(v) for v in (np.median(bg, axis=0) - GREEN)],
        })
    return frames, recs


def main():
    ap = argparse.ArgumentParser(description="射箭视频验收闸门")
    ap.add_argument("--video", required=True)
    ap.add_argument("--ref", default="", help="对照片（失败样本）")
    ap.add_argument("--json", default="")
    args = ap.parse_args()

    frames, recs = analyse(args.video)
    print("=== %s ===" % pathlib.Path(args.video).name)
    print("帧数 %d  画幅 %dx%d" % (len(frames), frames[0].shape[1], frames[0].shape[0]))
    counts = [r["white_blobs"] for r in recs]
    print("\n白羽连通块数/帧: min=%d max=%d 众数分布=%s"
          % (min(counts), max(counts), {v: counts.count(v) for v in sorted(set(counts))}))
    spikes = [r for r in recs if r["white_blobs"] > min(counts) + 1]
    print("超过基线+1 的帧: %d 帧%s"
          % (len(spikes), ("（例：%s）" % [(r["i"], r["t"], r["white_blobs"]) for r in spikes[:8]]) if spikes else ""))
    feet = [r["foot"] for r in recs]
    print("脚底: %d..%d 漂移 %d px" % (min(feet), max(feet), max(feet) - min(feet)))
    print("背景片基 Δ 最差: %s" % max(recs, key=lambda r: max(abs(v) for v in r["bg_d"]))["bg_d"])
    print("主体高 %d..%d  宽 %d..%d"
          % (min(r["h"] for r in recs), max(r["h"] for r in recs),
             min(r["w"] for r in recs), max(r["w"] for r in recs)))
    print("\n f    t   白羽块  头部白羽块  bbox")
    for r in recs[::8]:
        print("f%-4d %5.2f  %3d    %3d       %s" % (r["i"], r["t"], r["white_blobs"], r["white_blobs_head"], r["bbox"]))

    if args.ref:
        _, rrecs = analyse(args.ref)
        rc = [r["white_blobs"] for r in rrecs]
        print("\n=== 对照（失败样本）%s ===" % pathlib.Path(args.ref).name)
        print("白羽连通块数/帧: min=%d max=%d  超基线+1 的帧: %d"
              % (min(rc), max(rc),
                 sum(1 for x in rc if x > min(rc) + 1)))

    if args.json:
        pathlib.Path(args.json).write_text(json.dumps(recs, ensure_ascii=False, indent=1), encoding="utf-8")


if __name__ == "__main__":
    main()
