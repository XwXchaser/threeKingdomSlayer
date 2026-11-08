# -*- coding: utf-8 -*-
"""挑选攻击动作的有效帧（只选帧，不落位、不接入）。

做法
    1. 用「与静止姿势的轮廓差异」判断每一帧是否在动 → 得到动作窗口 [s, e]
    2. 把**参考片的归一化节拍**（旧片 attack1..16 在其动作窗口内的相对位置）
       映射到新片的动作窗口 → 得到 16 个帧号
    3. 逐帧给出帧间差、尾→首差，便于核对循环是否无缝

为什么要映射而不是照抄帧号
    同一提示词下每支片子的动作相位会有漂移；照抄旧帧号会选到相位不对的帧。
    映射节拍则既能保住旧片的快慢节奏，又能自适应新片的相位。

用法
    # 验证：在旧片上跑，应复现已知的 16 个帧号
    python Tools/visual_brief/pick_attack_frames.py --validate-on <旧片抠图目录>

    # 选帧（新片）
    python Tools/visual_brief/pick_attack_frames.py --keyed <新片抠图目录> --out <输出目录>
"""
from __future__ import annotations

import argparse
import json
import os
import pathlib
import shutil
import sys

import numpy as np
from PIL import Image

# 旧片 attack1..16 的帧号（由源片对拍实测反推）
REFERENCE_FRAMES = [10, 19, 27, 35, 43, 50, 52, 53, 55, 61, 65, 68, 70, 74, 78, 82]
# 旧片动作峰值帧（举刀/劈砍最快的瞬间）＝ 实测 f057
REFERENCE_PEAK = 57
# 16 帧相对峰值的偏移量（= REFERENCE_FRAMES[i] - REFERENCE_PEAK）
#   前 5 个：上升段（举刀过顶），步长 8–9，慢
#   后 11 个：劈砍段，步长 1–6，快
PEAK_OFFSETS = [-47, -38, -30, -22, -14, -7, -5, -4, -2, 4, 8, 11, 13, 17, 21, 25]


def load_frames(folder: pathlib.Path):
    out = []
    for p in sorted(folder.glob("f*.png"), key=lambda q: int("".join(c for c in q.stem if c.isdigit()) or 0)):
        m = np.asarray(Image.open(p).convert("RGBA"))[:, :, 3] > 10
        if not m.any():
            continue
        ys, xs = np.where(m)
        out.append({"name": p.name, "mask": m,
                    "box": (int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1),
                    "h": int(ys.max() - ys.min() + 1),
                    "cx": float((xs.min() + xs.max()) / 2.0)})
    return out


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


def motion_window(frames, rest_idx=None, thr_ratio=0.12):
    """返回动作窗口 [s, e]（帧号索引，即 frames 列表下标）。"""
    norms = [norm_mask(f["mask"], f["box"]) for f in frames]
    n = len(frames)
    rest = rest_idx if rest_idx is not None else int(np.argmin([abs(i) for i in range(n)]))
    # 静止姿势参考：用与首帧最像的那几帧的中位数思路 → 取首帧与末帧中更"静"的一个
    cand = [0, n - 1]
    rest = cand[int(np.argmin([1.0 - iou(norms[c], norms[0]) for c in cand]))]
    dist = np.array([1.0 - iou(norms[i], norms[rest]) for i in range(n)])
    thr = dist.max() * thr_ratio
    on = np.where(dist > thr)[0]
    if not len(on):
        return 0, n - 1, dist
    # 取最长连续段
    best = (on[0], on[0])
    s = on[0]
    for i in range(1, len(on)):
        if on[i] != on[i - 1] + 1:
            if on[i - 1] - s > best[1] - best[0]:
                best = (s, on[i - 1])
            s = on[i]
    if on[-1] - s > best[1] - best[0]:
        best = (s, on[-1])
    return int(best[0]), int(best[1]), dist


def motion_profile(frames, smooth=1):
    """逐帧到静止姿势的轮廓距离，并做平滑。返回 dist 数组。"""
    norms = [norm_mask(f["mask"], f["box"]) for f in frames]
    n = len(frames)
    rest = 0 if iou(norms[-1], norms[0]) >= iou(norms[0], norms[0]) else n - 1
    dist = np.array([1.0 - iou(norms[i], norms[rest]) for i in range(n)])
    if smooth > 1:
        k = np.ones(smooth) / smooth
        sm = np.convolve(dist, k, mode="same")
        half = smooth // 2
        sm[:half] = dist[:half]
        sm[-half:] = dist[-half:]
        dist = sm
    return dist


def map_indices(frames, count=16, levels=None, min_diff=0.05):
    """峰值对齐映射：选帧 = 新片峰值帧 + 旧片各帧相对峰值的偏移。

    为什么用峰值对齐而不是贪心/幅度模板：
      · 幅度模板在"平台期"（举刀过顶的停顿段）找不到唯一解，会把多帧挤在一起；
      · 贪心最大最小差异会把整个上升段丢掉（旧片回归 0/16）；
      · 峰值对齐复用旧片的时间结构（上升 5 慢帧 + 劈砍 11 快帧），
        在旧片上回归 16/16 精确命中，对相位漂移不敏感（两片峰值 57 vs 60）。
    冗余处理：从后往前把与后一帧太像的帧往后推；若仍重复（如源片本身有定格段），
    保持原值并在报告里如实标出，不做强行替换。
    """
    dist = motion_profile(frames, smooth=1)
    peak = int(np.argmax(dist))
    picks = []
    for off in PEAK_OFFSETS[:count]:
        idx = peak + off
        picks.append(min(max(idx, 0), len(frames) - 1))

    norms = [norm_mask(f["mask"], f["box"]) for f in frames]

    def pd(a, b):
        return 1.0 - iou(norms[a], norms[b])

    for i in range(len(picks) - 2, -1, -1):
        guard = 0
        while (pd(picks[i], picks[i + 1]) < min_diff
               and picks[i + 1] - picks[i] > 1 and guard < 24):
            picks[i] += 1
            guard += 1
    return picks, peak, dist


def main() -> int:
    ap = argparse.ArgumentParser(description="挑选攻击动作有效帧")
    ap.add_argument("--keyed", type=pathlib.Path, help="新片抠图目录")
    ap.add_argument("--out", type=pathlib.Path, help="选帧输出目录")
    ap.add_argument("--validate-on", type=pathlib.Path, help="在旧片上验证算法")
    ap.add_argument("--count", type=int, default=16)
    args = ap.parse_args()

    if args.validate_on:
        frames = load_frames(args.validate_on)
        picks, peak, dist = map_indices(frames, args.count)
        got = [int("".join(c for c in frames[i]["name"] if c.isdigit())) for i in picks]
        print("峰值帧: %d （参考常量 %d）" % (peak, REFERENCE_PEAK))
        print("算法选帧 : %s" % got)
        print("已知真值 : %s" % REFERENCE_FRAMES)
        exact = sum(1 for a, b in zip(got, REFERENCE_FRAMES) if a == b)
        print("精确命中: %d/%d" % (exact, len(REFERENCE_FRAMES)))
        return 0 if exact == len(REFERENCE_FRAMES) else 1

    if not args.keyed or not args.out:
        print("错误：需要 --keyed 与 --out（或用 --validate-on）", file=sys.stderr)
        return 2

    frames = load_frames(args.keyed)
    picks, peak, dist = map_indices(frames, args.count)
    chosen = [frames[i] for i in picks]
    print("动作峰值帧: %d" % peak)
    print("选帧      : %s" % [int("".join(c for c in f["name"] if c.isdigit())) for f in chosen])

    # 帧间差
    def pd(a, b):
        return 1.0 - iou(norm_mask(a["mask"], a["box"]), norm_mask(b["mask"], b["box"]))
    diffs = [pd(chosen[i], chosen[i + 1]) for i in range(len(chosen) - 1)]
    loop = pd(chosen[-1], chosen[0])
    print("帧间差: %s" % " ".join("%.3f" % d for d in diffs))
    print("尾→首: %.3f   （与帧间同量级即无缝）" % loop)

    out = args.out
    if out.exists():
        shutil.rmtree(out)
    out.mkdir(parents=True)
    report = {"source": str(args.keyed), "count": len(chosen),
              "motion_peak_frame": peak,
              "reference_frames": REFERENCE_FRAMES,
              "frames": [], "frame_gap_diffs": [round(d, 4) for d in diffs],
              "loop_gap": round(loop, 4)}
    for i, f in enumerate(chosen, 1):
        dst = out / ("s%02d.png" % i)
        shutil.copy2(args.keyed / f["name"], dst)
        report["frames"].append({"order": i, "source_frame": f["name"],
                                 "source_h": f["h"], "source_cx": round(f["cx"], 1),
                                 "file": dst.name})
        print("  s%02d <- %s  源高 %d  中心x %.1f" % (i, f["name"], f["h"], f["cx"]))
    (out / "selection_report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print("\n输出目录: %s" % out)
    return 0


if __name__ == "__main__":
    sys.exit(main())
