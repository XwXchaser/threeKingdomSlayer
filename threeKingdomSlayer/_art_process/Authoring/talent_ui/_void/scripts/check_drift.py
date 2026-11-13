"""版式漂移比对：底图 vs AI 生成图，逐个量兵符的上下缘，比较中心偏移。

判定口径（plan/combat-ui-reference-image-generation-plan-20261030.md §6.4）：
  每个元件中心偏移 <= 3% canvas 视为通过。
"""
import numpy as np
from PIL import Image

BASE = r"C:\threeKingdomSlayer\threeKingdomSlayer\Library\Locus\tmp\talent_ui\talent_mock_base_notext.png"
GEN = r"C:\Users\steam\Pictures\gptGen\talent_ui_v1\talent_ui_mock_v1.png"

NODES = [
    ("旋风", 512, 300),
    ("聚拢波", 262, 560),
    ("回旋波", 762, 560),
    ("击退波", 262, 820),
    ("箭矢齐射", 762, 820),
    ("海浪", 512, 1080),
]


def plaque_span(path, bx, by):
    im = Image.open(path).convert("RGB")
    a = np.asarray(im).astype(np.int16)
    mx = a.max(axis=2)
    fg = mx >= 24
    half = 45
    x0, x1 = max(0, bx - half), min(fg.shape[1], bx + half)
    y0, y1 = max(0, by - 170), min(fg.shape[0], by + 170)
    frac = fg[y0:y1, x0:x1].mean(axis=1)
    rows = frac > 0.45
    runs = []
    s = None
    for i, v in enumerate(rows):
        if v and s is None:
            s = i
        elif not v and s is not None:
            runs.append((s, i - 1))
            s = None
    if s is not None:
        runs.append((s, len(rows) - 1))
    runs = [r for r in runs if r[1] - r[0] > 40]
    if not runs:
        return None
    a0, a1 = runs[0]
    return y0 + a0, y0 + a1


print(f"{'node':10s} {'base_top':>9s} {'gen_top':>8s} {'base_bot':>9s} {'gen_bot':>8s} "
      f"{'d_center':>9s} {'d_%':>6s}  verdict")
worst = 0.0
for name, bx, by in NODES:
    b = plaque_span(BASE, bx, by)
    g = plaque_span(GEN, bx, by)
    if not b or not g:
        print(f"{name:10s}  detect failed base={b} gen={g}")
        continue
    bc = (b[0] + b[1]) / 2
    gc = (g[0] + g[1]) / 2
    d = gc - bc
    pct = abs(d) / 1536 * 100
    worst = max(worst, pct)
    print(f"{name:10s} {b[0]:9d} {g[0]:8d} {b[1]:9d} {g[1]:8d} {d:9.1f} {pct:6.2f}%  "
          f"{'PASS' if pct <= 3 else 'FAIL'}")
print(f"\nworst drift = {worst:.2f}% of canvas height  (threshold 3%)  -> "
      f"{'PASS' if worst <= 3 else 'FAIL'}")
