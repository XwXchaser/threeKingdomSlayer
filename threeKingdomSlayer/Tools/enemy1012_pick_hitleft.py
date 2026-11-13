# -*- coding: utf-8 -*-
"""hitLeft 选帧：DP 最大化最小相邻剪影差，排除武器形变帧 + 约束首末帧区间。"""
import sys, pathlib
sys.path.insert(0, "Tools/visual_brief")
import numpy as np
from PIL import Image
from pick_attack_frames import norm_mask, iou

d = pathlib.Path("Library/Locus/tmp/1012_batches/06_hitLeft_v1/keyed")

# 武器形变帧：像素量相对基线(f000-f003)突增（刀变成木桩）
base = []
for f in range(0, 4):
    a = np.asarray(Image.open(d / ("f%03d.png" % f)).convert("RGBA"))[:, :, 3] > 128
    base.append(int(a.sum()))
bm = float(np.median(base))
BAD = set()
for f in range(0, 97):
    a = np.asarray(Image.open(d / ("f%03d.png" % f)).convert("RGBA"))[:, :, 3] > 128
    if a.sum() > bm * 1.08:
        BAD.add(f)
print("武器形变帧（像素量 > 基线*1.08）:", sorted(BAD))

norms = {}
boxes = {}
for f in range(0, 97):
    a = np.asarray(Image.open(d / ("f%03d.png" % f)).convert("RGBA"))[:, :, 3] > 128
    ys, xs = np.where(a)
    boxes[f] = (int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1)
    norms[f] = norm_mask(a, boxes[f])

def diff(a, b):
    return 1.0 - iou(norms[a], norms[b])

LO, HI, N, GAP = 6, 92, 5, 3
cands = [f for f in range(LO, HI + 1) if f not in BAD]
NEG = -1.0
dp = [[NEG] * (HI + 1) for _ in range(N + 1)]
par = [[None] * (HI + 1) for _ in range(N + 1)]
for f in cands:
    dp[1][f] = 9.9
for k in range(2, N + 1):
    for f in cands:
        bv, bp = NEG, None
        for g in cands:
            if g >= f - GAP:
                break
            if dp[k - 1][g] <= NEG:
                continue
            v = min(dp[k - 1][g], diff(g, f))
            if v > bv:
                bv, bp = v, g
        dp[k][f] = bv
        par[k][f] = bp

best = None
for s in [f for f in cands if 6 <= f <= 14]:
    for e in [f for f in cands if 76 <= f <= 92]:
        if dp[N][e] <= NEG:
            continue
        chain = [e]
        cur = e
        for k in range(N, 1, -1):
            cur = par[k][cur]
            chain.append(cur)
        chain.reverse()
        if chain[0] != s:
            continue
        v = min(diff(chain[i], chain[i + 1]) for i in range(N - 1))
        if best is None or v > best[0]:
            best = (v, chain)
score, picks = best
print("DP 选帧:", picks, " 最小相邻差=%.3f" % score)
print("相邻差:", " ".join("%.3f" % diff(picks[i], picks[i + 1]) for i in range(4)))
print("含形变帧?", [p for p in picks if p in BAD])
for p in picks:
    print("  f%03d bbox=%s" % (p, boxes[p]))
pathlib.Path("Library/Locus/tmp/1012_batches/06_hitLeft_v1/picks5.txt").write_text(
    ",".join(map(str, picks)), encoding="utf-8")
print("已写 picks5.txt")
