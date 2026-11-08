# -*- coding: utf-8 -*-
"""1012 下半身本地几何修正（不调用生成接口，不重绘，只做分段横向缩放）。

为什么这样做
    三轮生图已把"形态/位置关系"改对（裤脚高度、裹腿段、靴口位置都与基准对齐），
    剩下的差异全是**体量**：腰胯偏宽、裤腿偏瘦、裹腿偏细、靴子偏小。
    这些都是分段横向缩放，本地几何能做，且不像生图那样重绘整幅图。

机制
    每一行按高度带取缩放系数 s(y)，把该行内容做横向缩放：
        源 x  ->  输出 x = anchor + (x - anchor) * s
    · 带格式  起始%-结束%:系数[:模式]
        模式 run  = 逐段缩放（每个连通段绕自己的中心缩放）→ 用于腰胯/裤腿/裹腿
        模式 axis = 绕身体轴缩放（整行一起）→ 用于靴子（要同时加宽并把两脚拉开）
    · 带间用 smoothstep 平滑过渡（feather 以身高的 0.03 为单位，与 f 同为小数）
    · 刀（冷灰/金黄色）在任何带里都**就地保护**，不被缩放拖动
    · 被挤出轮廓的像素回到原背景色，不产生空洞

用法
    python Tools/visual_brief/local_fix_1012_lowerbody.py \
        --src <图> --out <图> \
        --band 48-62:0.93:run --band 70-82:1.30:run \
        --band 82-89:1.45:run --band 89-100:1.07:axis
"""
import argparse
import os
import sys
import numpy as np
from PIL import Image

PROJ = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
GREEN = (8, 145, 73)
FEATHER = 0.03          # 身高的 3%（与 f 同为小数）


def smoothstep(t):
    t = max(0.0, min(1.0, t))
    return t * t * (3 - 2 * t)


def load_rgba(path):
    im = Image.open(path).convert("RGB")
    arr = np.asarray(im).astype(int)
    green = np.abs(arr - np.array(GREEN)).sum(axis=2)
    if (green < 90).mean() < 0.02:
        raise SystemExit("这不是绿幕图：%s" % path)
    rgba = im.convert("RGBA")
    rgba.putalpha(Image.fromarray(np.where(green < 90, 0, 255).astype(np.uint8)))
    return rgba, arr


def bg_color(arr):
    edge = np.concatenate([arr[0:4].reshape(-1, 3), arr[-4:].reshape(-1, 3),
                           arr[:, 0:4].reshape(-1, 3), arr[:, -4:].reshape(-1, 3)])
    return tuple(int(v) for v in np.median(edge, axis=0))


def sword_mask(arr):
    """刀的特征：冷色。裤子/裹腿/皮肤/红布都是暖色（R-B 大），刀身是冷灰（B>=R）。"""
    R, B = arr[:, :, 0], arr[:, :, 2]
    mx = np.max(arr, axis=2)
    mn = np.min(arr, axis=2)
    cool = (B >= R - 6) & (mx - mn < 70) & (mx > 70)
    gold = (R > 145) & (arr[:, :, 1] > 105) & (B < 115) & ((R - B) > 55)
    return cool | gold


def warm_fraction(arr, y, a, b):
    """一段里"暖色"（布料/皮肤/红布）像素的占比；占比低说明是刀。"""
    seg = arr[y, a:b + 1]
    if not len(seg):
        return 1.0
    R, B = seg[:, 0], seg[:, 2]
    warm = (R - B) > 28
    return float(warm.mean())


def runs_of(row):
    idx = np.where(row > 0)[0]
    if not len(idx):
        return []
    segs = []
    s = p = idx[0]
    for v in idx[1:]:
        if v != p + 1:
            segs.append((int(s), int(p)))
            s = v
        p = v
    segs.append((int(s), int(p)))
    return segs


def parse_bands(specs):
    """'48-62:0.93:run' → (0.48, 0.62, 0.93, 'run')"""
    bands = []
    for sp in specs:
        parts = sp.split(":")
        rng, val = parts[0], parts[1]
        mode = parts[2] if len(parts) > 2 else "run"
        a, b = rng.split("-")
        bands.append((float(a) / 100.0, float(b) / 100.0, float(val), mode))
    bands.sort()
    return bands


def scale_at(f, bands):
    """按高度取缩放系数；带内直接取，带外按 feather 平滑过渡（连续，无跳变）。"""
    for a, b, v, mode in bands:
        if a <= f <= b:
            return v, mode
    s = 1.0
    mode = "run"
    for a, b, v, m in bands:
        if b < f < b + FEATHER:                 # 带上方：v → 1
            t = smoothstep((f - b) / FEATHER)
            s *= 1 + (v - 1) * (1 - t)
            mode = m
        elif a - FEATHER < f < a:              # 带下方：1 → v
            t = smoothstep((f - (a - FEATHER)) / FEATHER)
            s *= 1 + (v - 1) * t
            mode = m
    return s, mode


def apply(path, out_path, bands):
    rgba, arr = load_rgba(path)
    BG = bg_color(arr)
    src = np.asarray(rgba).astype(np.float32)
    h, w = src.shape[:2]
    alpha = np.asarray(rgba.split()[-1]) > 0
    ys, xs = np.where(alpha)
    top, bot = int(ys.min()), int(ys.max())
    H = bot - top + 1

    # 身体轴线：下半身各行"最大段"中点的中位数
    mids = []
    for y in range(top + int(H * 0.55), bot + 1):
        rs = runs_of(alpha[y])
        if rs:
            big = max(rs, key=lambda t: t[1] - t[0])
            mids.append((big[0] + big[1]) / 2.0)
    axis = float(np.median(mids)) if mids else w / 2.0

    out = src.copy()
    for y in range(h):
        f = (y - top) / float(H - 1)
        s, mode = scale_at(f, bands)
        if abs(s - 1.0) < 0.004:
            continue
        rs = runs_of(alpha[y])
        if not rs:
            continue
        row_src = src[y]
        row_out = row_src.copy()

        # 逐段判定：刀段（暖色占比低 = 冷色金属）就地保护
        keep = []
        for a, b in rs:
            keep.append(warm_fraction(arr, y, a, b) < 0.45)

        if mode == "axis":
            # 整行绕身体轴缩放（靴子：加宽 + 两脚拉开）；刀段仍保护
            body_runs = [r for r, k in zip(rs, keep) if not k]
            if not body_runs:
                continue
            L = min(r[0] for r in body_runs)
            R = max(r[1] for r in body_runs)
            targets = [(L, R, axis)]
        else:
            targets = []
            for (a, b), k in zip(rs, keep):
                if k:
                    continue
                targets.append((a, b, (a + b) / 2.0))

        for (L, R, c) in targets:
            if R - L < 3:
                continue
            newL = int(round(c - (R - L) / 2.0 * s))
            newR = int(round(c + (R - L) / 2.0 * s))
            for x in range(max(0, newL), min(w - 1, newR) + 1):
                sx = c + (x - c) / s
                if sx < L - 0.5 or sx > R + 0.5:
                    continue
                x0 = int(np.floor(sx)); x1 = min(w - 1, x0 + 1)
                fx = sx - x0
                row_out[x] = row_src[x0] * (1 - fx) + row_src[x1] * fx
            for x in range(max(0, L), min(w - 1, R) + 1):
                if not (newL <= x <= newR):
                    row_out[x] = list(BG) + [255.0]

        # 刀段原样保留
        for (a, b), k in zip(rs, keep):
            if k:
                row_out[a:b + 1] = row_src[a:b + 1]
        out[y] = row_out

    res = Image.fromarray(np.clip(out, 0, 255).astype(np.uint8), "RGBA")
    rgb = Image.new("RGB", res.size, BG)
    rgb.paste(res.convert("RGB"), (0, 0), res.split()[-1])
    d = os.path.dirname(os.path.abspath(out_path))
    if d:
        os.makedirs(d, exist_ok=True)
    rgb.save(out_path, "PNG", optimize=True)
    return res.size, H


def main(argv):
    ap = argparse.ArgumentParser(description="1012 下半身本地几何修正")
    ap.add_argument("--src", required=True)
    ap.add_argument("--out", required=True)
    ap.add_argument("--band", action="append", default=[],
                    help="起始%-结束%:系数[:run|axis]")
    ap.add_argument("--preview", default="")
    args = ap.parse_args(argv)

    src = args.src if os.path.isabs(args.src) else os.path.join(PROJ, args.src)
    out = args.out if os.path.isabs(args.out) else os.path.join(PROJ, args.out)
    if not os.path.isfile(src):
        print("错误：源图不存在 %s" % src, file=sys.stderr)
        return 2
    bands = parse_bands(args.band)
    if not bands:
        print("错误：至少给一个 --band", file=sys.stderr)
        return 2

    size, H = apply(src, out, bands)
    print("写出 %s（%dx%d，主体高 %d）" % (out, size[0], size[1], H))
    for a, b, v, m in bands:
        print("   带 %g%%-%g%%  系数 %.3f  模式 %s" % (a * 100, b * 100, v, m))

    if args.preview:
        a_im = Image.open(src).convert("RGB")
        b_im = Image.open(out).convert("RGB")
        from PIL import ImageDraw, ImageFont
        s = Image.new("RGB", (a_im.width + b_im.width + 30, max(a_im.height, b_im.height)), "#1b1d22")
        s.paste(a_im, (0, 0)); s.paste(b_im, (a_im.width + 30, 0))
        d = ImageDraw.Draw(s)
        f = ImageFont.truetype(r"C:\Windows\Fonts\msyh.ttc", 20)
        d.text((8, 6), "修正前 (v3)", font=f, fill=(255, 180, 120))
        d.text((a_im.width + 38, 6), "修正后（本地几何）", font=f, fill=(140, 220, 160))
        s.thumbnail((1100, 1100), Image.LANCZOS)
        s.save(args.preview, "PNG", optimize=True)
        print("预览 %s %s" % (args.preview, s.size))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
