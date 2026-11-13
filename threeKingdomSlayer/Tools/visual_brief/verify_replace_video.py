# -*- coding: utf-8 -*-
"""替换视频成片验收：角色不回退 / 盾侧 / G2 片基 / 动作继承。

用法
    python Tools/visual_brief/verify_replace_video.py \
        --video  C:/Users/steam/Videos/doubaoVideo/enemy1021_shield_attack_replace_v1.mp4 \
        --mother C:/Users/steam/Videos/doubaoVideo/enemy1021_shield_attack_v1.mp4 \
        --bg-key 0,172,57 --batch-key 0,159,62 --out-dir C:/Users/steam/Pictures/gptGen/enemy1021_attack_replace
"""
from __future__ import annotations

import argparse
import json
import os

import av
import numpy as np
from PIL import Image


def load(path):
    c = av.open(path)
    frames = [f.to_ndarray(format="rgb24") for f in c.decode(video=0)]
    return frames


def frame_stats(frames, bg):
    bgv = np.array(bg, dtype=float)
    out = []
    for i, f in enumerate(frames):
        a = f.astype(int)
        m = np.linalg.norm(a - bgv, axis=2) > 79
        if m.sum() < 500:
            out.append(None)
            continue
        ys, xs = np.nonzero(m)
        y0, y1, x0, x1 = int(ys.min()), int(ys.max()), int(xs.min()), int(xs.max())
        R, G, B = a[:, :, 0], a[:, :, 1], a[:, :, 2]
        cut = max(1, int(y0 + (y1 - y0) * 0.55))
        upper = m[:cut, :]
        red = upper & (R[:cut] - G[:cut] > 45) & (R[:cut] - B[:cut] > 45) & (R[:cut] > 90)
        # 头部红像素：旧角色的红缨/红围巾都集中在头部带，是"角色回退"的针对性判据
        hcut = max(1, int(y0 + (y1 - y0) * 0.25))
        head = m[:hcut, :]
        red_head = head & (R[:hcut] - G[:hcut] > 45) & (R[:hcut] - B[:hcut] > 45) & (R[:hcut] > 90)
        wood = m & (R > 95) & (R < 200) & (R - G > 35) & (R - G < 105) & (G - B > 12) & (G - B < 75)
        wy, wx = np.nonzero(wood)
        cor = np.concatenate([a[:64, :64].reshape(-1, 3), a[:64, -64:].reshape(-1, 3),
                               a[-64:, :64].reshape(-1, 3), a[-64:, -64:].reshape(-1, 3)])
        out.append({
            "frame": i, "x0": x0, "y0": y0, "x1": x1, "y1": y1,
            "body_h": y1 - y0, "width": x1 - x0,
            "red_frac": float(red.sum()) / max(1, int(upper.sum())),
            "red_head_frac": float(red_head.sum()) / max(1, int(head.sum())),
            "head_band_h": hcut,
            "bg_median": [int(v) for v in np.median(cor, axis=0)],
            "wood_centroid_x": float(wx.mean()) if len(wx) else None,
            "bbox_mid_x": (x0 + x1) / 2.0,
        })
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--video", required=True)
    ap.add_argument("--mother", required=True)
    ap.add_argument("--bg-key", default="0,172,57")
    ap.add_argument("--batch-key", default="0,159,62")
    ap.add_argument("--out-dir", required=True)
    args = ap.parse_args()

    bg = tuple(int(v) for v in args.bg_key.split(","))
    batch = np.array([int(v) for v in args.batch_key.split(",")], dtype=float)
    bgv = np.array(bg, dtype=float)

    out = load(args.video)
    mot = load(args.mother)
    so = frame_stats(out, bg)
    sm = frame_stats(mot, bg)
    os.makedirs(args.out_dir, exist_ok=True)

    print("成片 %d 帧 %s   母片 %d 帧 %s" % (len(out), out[0].shape[:2][::-1], len(mot), mot[0].shape[:2][::-1]))

    print("\n== 成片逐帧（抽样）: 红色占比(红缨/红围巾残留) / 木盾侧 / 身高 ==")
    print("帧    身高  宽   红%     盾质心x 人物中点 盾侧")
    for s in so[::6]:
        if not s:
            continue
        w = s["wood_centroid_x"]
        side = "-" if w is None else ("左" if w < s["bbox_mid_x"] else "右")
        print("%-5d %-5d %-4d %-7.4f %-9.0f %-8.0f %s" % (s["frame"], s["body_h"], s["width"], s["red_frac"],
                                                           w if w is not None else -1, s["bbox_mid_x"], side))

    reds_o = [s["red_frac"] for s in so if s]
    reds_m = [s["red_frac"] for s in sm if s]
    rh_o = [s["red_head_frac"] for s in so if s]
    rh_m = [s["red_head_frac"] for s in sm if s]
    print("\n== 头部带红像素占比（红缨/红围巾的针对性判据；成片应≈0）==")
    print("  成片 均值 %.4f / 最大 %.4f    母片 均值 %.4f / 最大 %.4f"
          % (np.mean(rh_o), max(rh_o), np.mean(rh_m), max(rh_m)))
    print("\n红色像素占比：成片 均值 %.4f / 最大 %.4f   母片 均值 %.4f / 最大 %.4f"
          % (np.mean(reds_o), max(reds_o), np.mean(reds_m), max(reds_m)))

    print("\n== G2 片基 ==")
    med = np.array([s["bg_median"] for s in so if s], dtype=float)
    db = med - batch
    dm = med - bgv
    print("  vs 批次基准键 %s: ΔR %.0f~%.0f  ΔG %.0f~%.0f  ΔB %.0f~%.0f"
          % (list(batch.astype(int)), db[:, 0].min(), db[:, 0].max(), db[:, 1].min(), db[:, 1].max(), db[:, 2].min(), db[:, 2].max()))
    print("  vs 母片实测键 %s: ΔR %.0f~%.0f  ΔG %.0f~%.0f  ΔB %.0f~%.0f"
          % (list(bgv.astype(int)), dm[:, 0].min(), dm[:, 0].max(), dm[:, 1].min(), dm[:, 1].max(), dm[:, 2].min(), dm[:, 2].max()))
    wi = int(np.argmax(np.abs(dm[:, 1])))
    print("  最差帧(对母片键 ΔG 绝对值最大): f%d  中位 %s  ΔG=%+.0f" % (wi, so[wi]["bg_median"], dm[wi, 1]))
    print("  首帧中位 %s   末帧中位 %s" % (so[0]["bg_median"], so[-1]["bg_median"]))

    print("\n== 动作继承（成片 vs 母片）==")
    print("帧    成片身高 母片身高  成片宽  母片宽")
    for i in range(0, len(so), 8):
        a, b = so[i], (sm[i] if i < len(sm) else None)
        if not a:
            continue
        print("%-5d %-8d %-8s %-6d %-6s" % (i, a["body_h"],
                                            b["body_h"] if b else "-", a["width"], b["width"] if b else "-"))

    # 接触表
    sel = [0, 8, 16, 24, 32, 40, 48, 56, 64, 72, 80, 88, 96]
    sel = [s for s in sel if s < len(out)]
    sheet = Image.new("RGB", (7 * 200, 2 * 200), (18, 18, 22))
    for i, fi in enumerate(sel):
        sheet.paste(Image.fromarray(out[fi]).resize((200, 200), Image.LANCZOS), ((i % 7) * 200, (i // 7) * 200))
    cs = os.path.join(args.out_dir, "attack_replace_contactsheet.png")
    sheet.save(cs)

    rep = {"video": args.video, "mother": args.mother, "frames": len(out),
           "bg_key": list(bg), "batch_key": list(batch.astype(int)),
           "red_frac_out_mean": float(np.mean(reds_o)), "red_frac_out_max": float(max(reds_o)),
           "red_head_out_mean": float(np.mean(rh_o)), "red_head_out_max": float(max(rh_o)),
           "red_head_mother_mean": float(np.mean(rh_m)), "red_head_mother_max": float(max(rh_m)),
           "red_frac_mother_mean": float(np.mean(reds_m)), "red_frac_mother_max": float(max(reds_m)),
           "bg_median_delta_vs_batch": {"R": [float(db[:, 0].min()), float(db[:, 0].max())],
                                        "G": [float(db[:, 1].min()), float(db[:, 1].max())],
                                        "B": [float(db[:, 2].min()), float(db[:, 2].max())]},
           "bg_median_delta_vs_mother_key": {"R": [float(dm[:, 0].min()), float(dm[:, 0].max())],
                                             "G": [float(dm[:, 1].min()), float(dm[:, 1].max())],
                                             "B": [float(dm[:, 2].min()), float(dm[:, 2].max())]},
           "worst_frame_vs_mother_key": int(wi),
           "first_frame_bg_median": [int(v) for v in so[0]["bg_median"]],
           "last_frame_bg_median": [int(v) for v in so[-1]["bg_median"]],
           "contactsheet": cs,
           "per_frame": [{k: (int(v) if isinstance(v, (int, float)) and k in
                             ("frame", "x0", "y0", "x1", "y1", "body_h", "width", "head_band_h")
                             else v) for k, v in s.items()} for s in so if s]}
    rp = os.path.join(args.out_dir, "attack_replace_verify.json")
    with open(rp, "w", encoding="utf-8") as fh:
        json.dump(rep, fh, indent=2, ensure_ascii=False)
    print("\n报告: %s\n接触表: %s" % (rp, cs))


if __name__ == "__main__":
    main()
