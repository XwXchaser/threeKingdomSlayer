# -*- coding: utf-8 -*-
"""1012 本地几何修正器（不调用任何生成接口）。

当前实现的操作
    narrow_hips   收窄腰胯：按行分段，只压缩"身体段"（躯干+裤子），武器段与背景原样保留。
                 做法：以躯干轴线为中心做水平压缩，压缩量随高度用 smoothstep 平滑进出目标带。
                 被挤出轮廓的像素直接丢弃 → 该处回到绿幕底，不会产生空洞或糊边。

为什么用"分段"而不是颜色分割
    A6b 的刀在 48–60% 身高带里与身体是**分离的行段**，直接用行段就能把刀排除掉；
    颜色分割在写实图上很不稳（肤色/棕色/钢灰互有交叠）。

用法
    python Tools/visual_brief/local_fix_1012.py --src <图> --out <图> --op narrow_hips
"""
import argparse
import os
import sys
import numpy as np
from PIL import Image

PROJ = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
GREEN = (8, 145, 73)


def load_rgba(path):
    im = Image.open(path).convert("RGB")
    arr = np.asarray(im).astype(int)
    green = np.abs(arr - np.array(GREEN)).sum(axis=2)
    if (green < 90).mean() < 0.02:
        raise SystemExit("这不是绿幕图（绿色像素太少）：%s" % path)
    alpha = np.where(green < 90, 0, 255).astype(np.uint8)
    out = im.copy()
    out.putalpha(Image.fromarray(alpha))
    return out


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


def body_info(rgba):
    al = np.asarray(rgba.split()[-1])
    ys, xs = np.where(al > 0)
    box = (int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1)
    return al, box, box[3] - box[1]


def smoothstep(t):
    t = max(0.0, min(1.0, t))
    return t * t * (3 - 2 * t)


def narrow_hips(rgba, band=(0.48, 0.60), target_ratio=0.865, feather=0.12):
    """把目标带内身体段的横向宽度压到 target_ratio，带外平滑过渡。"""
    al, box, H = body_info(rgba)
    top = box[1]
    band_rows = [(top + int((H - 1) * f), top + int((H - 1) * (f + feather)), top + int((H - 1) * (f - feather)))
                 for f in band]

    # 躯干轴线：取目标带中点那一行身体段的中心
    mid_row = top + int((H - 1) * (band[0] + band[1]) / 2)
    rs = runs_of(al[mid_row])
    if not rs:
        raise SystemExit("中点行没有身体像素")
    # 身体段 = 含"躯干"的段：用整幅图的水平中位轴判断
    axis = int(np.median([(a + b) / 2 for a, b in rs]))
    body_run = max(rs, key=lambda t: t[1] - t[0])
    cx = (body_run[0] + body_run[1]) / 2.0

    src = np.asarray(rgba).astype(np.float32)
    h, w = al.shape
    out = np.zeros_like(src)

    # 逐行的压缩系数与轴线
    def coef(y):
        c = 1.0
        for (b0, b1, b0f) in band_rows:
            if b0f <= y <= b1:
                c = min(c, target_ratio)
            elif b1 < y < b0f:
                pass
        # 用带内目标 + 带外 smoothstep
        return c

    def scale_at(y):
        """该行的压缩系数（1.0 = 不动）。"""
        f = (y - top) / float(H - 1)
        b0, b1 = band
        if b0 <= f <= b1:
            return target_ratio
        if f < b0:
            # 上侧过渡：从 feather 之外渐入
            t = (f - (b0 - feather)) / feather
            return 1.0 + (target_ratio - 1.0) * smoothstep(t)
        if f > b1:
            t = ((b0 + feather) - f) / feather
            return 1.0 + (target_ratio - 1.0) * smoothstep(t)
        return target_ratio

    # 输出：每个输出行 y，按该行的压缩系数做逆向映射采样
    for y in range(h):
        s = scale_at(y)
        row_src = src[y]
        alpha_src = al[y]
        if s >= 0.999:
            out[y] = row_src
            continue
        # 该行身体段（取最长的、含轴线的段）
        rs = runs_of(alpha_src)
        body = None
        for a, b in rs:
            if a <= axis <= b:
                body = (a, b)
                break
        if body is None:
            out[y] = row_src
            continue
        L, R = body
        c = cx
        # 压缩后的新范围
        half_new = (R - L) / 2.0 * s
        L_new, R_new = int(round(c - half_new)), int(round(c + half_new))
        # 背景段（武器等）保持原位：先把它们原样画上
        seg_out = row_src.copy()
        for a, b in rs:
            if (a, b) != body:
                seg_out[a:b + 1] = row_src[a:b + 1]
        # 身体段：逆向映射采样
        for x in range(max(0, L_new), min(w - 1, R_new) + 1):
            sx = int(round(c + (x - c) / s))
            if L <= sx <= R:
                seg_out[x] = row_src[sx]
        # 其余像素（被挤出轮廓的原身体像素）→ 绿幕
        seg_out[:, 3] = 255
        for x in range(0, w):
            if alpha_src[x] > 0 and not (L_new <= x <= R_new):
                # 只清掉原来属于身体段、被挤出轮廓的像素；武器段保留
                if L <= x <= R:
                    seg_out[x] = list(GREEN) + [255]
        out[y] = seg_out

    res = Image.fromarray(out.astype(np.uint8), "RGBA")
    return res


def save_green(rgba, path):
    rgb = Image.new("RGB", rgba.size, GREEN)
    rgb.paste(rgba.convert("RGB"), (0, 0), rgba.split()[-1])
    os.makedirs(os.path.dirname(path), exist_ok=True)
    rgb.save(path, "PNG", optimize=True)


def main(argv):
    ap = argparse.ArgumentParser(description="1012 本地几何修正（不调用生成接口）")
    ap.add_argument("--src", required=True)
    ap.add_argument("--out", required=True)
    ap.add_argument("--op", default="narrow_hips", choices=["narrow_hips"])
    ap.add_argument("--ratio", type=float, default=0.865, help="目标带内横向压缩系数")
    ap.add_argument("--band", default="0.48,0.60", help="目标带（占身高比例）")
    ap.add_argument("--preview", default="", help="额外写一张并排预览图")
    args = ap.parse_args(argv)

    src = args.src if os.path.isabs(args.src) else os.path.join(PROJ, args.src)
    out = args.out if os.path.isabs(args.out) else os.path.join(PROJ, args.out)
    if not os.path.isfile(src):
        print("错误：源图不存在 %s" % src, file=sys.stderr)
        return 2

    rgba = load_rgba(src)
    b0, b1 = [float(x) for x in args.band.split(",")]
    res = narrow_hips(rgba, band=(b0, b1), target_ratio=args.ratio)
    save_green(res, out)
    print("写出 %s（%dx%d）" % (out, res.width, res.height))

    if args.preview:
        a = Image.open(src).convert("RGB")
        b = Image.open(out).convert("RGB")
        w = a.width + b.width + 30
        h = max(a.height, b.height)
        s = Image.new("RGB", (w, h), "#1b1d22")
        s.paste(a, (0, 0))
        s.paste(b, (a.width + 30, 0))
        from PIL import ImageDraw, ImageFont
        d = ImageDraw.Draw(s)
        f = ImageFont.truetype(r"C:\Windows\Fonts\msyh.ttc", 20)
        d.text((8, 6), "修正前", font=f, fill=(255, 180, 120))
        d.text((a.width + 38, 6), "修正后（本地几何）", font=f, fill=(140, 220, 160))
        s.thumbnail((1100, 1100), Image.LANCZOS)
        s.save(args.preview, "PNG", optimize=True)
        print("预览 %s %s" % (args.preview, s.size))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
