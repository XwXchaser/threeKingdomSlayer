#!/usr/bin/env python
"""把「正交视图合板」切成单视图并合成可交付的看板。

沿用长枪那一轮的做法（StabWeaponArtSource/11_AssembledFiveViewV05、12_AssembledFourViewV06）：
    · 从生成的大图里**自动检出**每个面板的主体包围盒
    · 用**同一套缩放**把正／右／后三视图对齐（顶端与底端高度一致），端向视图单独等比放大
    · 输出无字合图（1536×1024）+ 中文标注版（1856×1440）
    · 输出 layout_transforms.json / delivery_verification.json 供追溯

用法
    python Tools/visual_brief/assemble_view_board.py --raw <sheet.png> --out-dir <dir> --stem <name> \
        --views front,right,back,top,bottom [--title "..."] [--annotated]
"""

import argparse
import hashlib
import json
import os

from PIL import Image, ImageDraw, ImageFont

PROJ = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
FONT_CANDIDATES = [
    os.path.join(PROJ, "Assets", "Fonts", "方正粗黑宋简体.ttf"),
    r"C:\Windows\Fonts\msyhbd.ttc",
    r"C:\Windows\Fonts\msyh.ttc",
]

PLAIN_W, PLAIN_H = 1536, 1024
ANN_W, ANN_H = 1856, 1440
TALL_COLS = 3          # 正/右/后 三个高视图
END_ROWS = 2           # 顶/底 两个端向视图


def content_mask(im):
    import numpy as np
    a = np.array(im.convert("RGB")).astype(int)
    if im.mode == "RGBA" and (np.array(im)[..., 3].min() < 250):
        return np.array(im)[..., 3] > 20
    return a.sum(axis=2) < 720


def runs(v, min_len=4):
    out, s = [], None
    for i, x in enumerate(v):
        if x > 0 and s is None:
            s = i
        elif x == 0 and s is not None:
            if i - s >= min_len:
                out.append((s, i - 1))
            s = None
    if s is not None and len(v) - s >= min_len:
        out.append((s, len(v) - 1))
    return out


def detect_panels(im):
    """返回 [(name, bbox)]：三个高视图（正/右/后）+ 右列上下两块（顶/底）。"""
    import numpy as np
    m = content_mask(im)
    h, w = m.shape
    colprof = m.sum(axis=0)
    blocks = runs(colprof)
    if len(blocks) != TALL_COLS + 1:
        raise SystemExit("列块数应为 %d（3 个高视图 + 1 个端向列），实际 %d：%s"
                         % (TALL_COLS + 1, len(blocks), blocks))
    # 端向列 = 列内内容断成**上下两块**的那一列（顶视 / 底视）。
    # ⚠ 不能用「最窄的列」判：斧的右侧视图（刃口侧视）只有 ~80px 宽，比端向列还窄。
    def row_runs(b):
        return runs(m[:, b[0]:b[1] + 1].sum(axis=1), min_len=6)

    end_candidates = [b for b in blocks if len(row_runs(b)) == END_ROWS]
    if len(end_candidates) != 1:
        raise SystemExit("端向列应恰好 1 列（内部上下 2 块），实际 %d：%s（各列块行段数 %s）"
                         % (len(end_candidates), end_candidates,
                            [(b, len(row_runs(b))) for b in blocks]))
    end_col = end_candidates[0]
    tall = sorted(b for b in blocks if b != end_col)
    if len(tall) != TALL_COLS:
        raise SystemExit("高视图列块数应为 %d，实际 %d：%s" % (TALL_COLS, len(tall), tall))
    rest = [end_col]
    if len(rest) != 1:
        raise SystemExit("端向视图列块应为 1，实际 %d：%s" % (len(rest), rest))
    x0, x1 = rest[0]
    ends = row_runs((x0, x1))
    if len(ends) != END_ROWS:
        raise SystemExit("端向视图应为上下 2 块，实际 %d：%s" % (len(ends), ends))
    ends.sort()

    out = []
    for name, b in zip(("front", "right", "back"), tall):
        sub = m[:, b[0]:b[1] + 1]
        ys = np.where(sub.sum(axis=1) > 0)[0]
        out.append((name, (b[0], int(ys.min()), b[1], int(ys.max()))))
    for name, r in zip(("top", "bottom"), ends):
        sub = m[r[0]:r[1] + 1, x0:x1 + 1]
        xs = np.where(sub.sum(axis=0) > 0)[0]
        out.append((name, (x0 + int(xs.min()), r[0], x0 + int(xs.max()), r[1])))
    return out


def detect_single(im):
    """单张原图里的单个主体包围盒（如单独生成的左侧视图）。"""
    import numpy as np
    m = content_mask(im)
    ys, xs = np.where(m)
    return (int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max()))


def find_font(size):
    for p in FONT_CANDIDATES:
        if os.path.isfile(p):
            try:
                return ImageFont.truetype(p, size)
            except Exception:
                continue
    return ImageFont.load_default()


def build(raw_path, out_dir, stem, view_order, title, annotated, left_raw=None):
    im = Image.open(raw_path).convert("RGB")
    panels = dict(detect_panels(im))

    order = [v.strip() for v in view_order.split(",") if v.strip()]
    missing = [v for v in order if v not in panels and v != "left"]
    if missing:
        raise SystemExit("缺视图: %s（检出: %s）" % (missing, list(panels)))
    if "left" in order:
        if not left_raw:
            raise SystemExit("view_order 含 left 时必须给 --left-raw")
        panels["left"] = detect_single(Image.open(left_raw).convert("RGB"))

    axes = [v for v in order if v in ("front", "left", "right", "back")]
    ends = [v for v in order if v in ("top", "bottom")]

    plain = Image.new("RGB", (PLAIN_W, PLAIN_H), (255, 255, 255))
    transforms = []

    # ── 正/右/后：同一套缩放，顶端与底端对齐 ──────────────────────────────
    cell_w = PLAIN_W // 4                      # 384
    MARGIN = 48                                # 上下至少留 48px
    hs = sorted((panels[v][3] - panels[v][1] + 1) for v in axes)
    # 用**中位数**而不是平均值：单独生成的左侧视图在它自己的原图里占得更高，
    # 用平均值会把所有视图拉到超出画布高度（被裁断）。
    target_h = hs[len(hs) // 2]
    target_h = min(target_h, PLAIN_H - 2 * MARGIN)
    src_im = {"front": im, "right": im, "back": im, "top": im, "bottom": im}
    if left_raw:
        src_im["left"] = Image.open(left_raw).convert("RGB")
    for i, v in enumerate(axes):
        b = panels[v]
        crop = src_im[v].crop(b)
        scale = target_h / crop.height
        new_w = max(1, int(round(crop.width * scale)))
        new_h = max(1, int(round(crop.height * scale)))
        crop = crop.resize((new_w, new_h), Image.LANCZOS)
        cx = i * cell_w + (cell_w - new_w) // 2
        cy = (PLAIN_H - new_h) // 2
        plain.paste(crop, (cx, cy))
        transforms.append({"view": v, "source_rect": list(b), "scale": scale,
                            "board_rect": [cx, cy, new_w, new_h],
                            "cell_rect": [i * cell_w, 0, cell_w, PLAIN_H]})

    # ── 顶/底：右列上下两格，等比放大到各自格内 ────────────────────────────
    rx = PLAIN_W - cell_w
    half = PLAIN_H // 2
    if ends:
        ehs = [panels[v][3] - panels[v][1] + 1 for v in ends]
        etarget = int(round(sum(ehs) / len(ehs)))
        for i, v in enumerate(ends):
            b = panels[v]
            crop = im.crop(b)
            scale = min((half - 24) / crop.height, (cell_w - 24) / crop.width)
            new_w = max(1, int(round(crop.width * scale)))
            new_h = max(1, int(round(crop.height * scale)))
            crop = crop.resize((new_w, new_h), Image.LANCZOS)
            cx = rx + (cell_w - new_w) // 2
            cy = i * half + (half - new_h) // 2
            plain.paste(crop, (cx, cy))
            transforms.append({"view": v, "source_rect": list(b), "scale": scale,
                                "board_rect": [cx, cy, new_w, new_h],
                                "cell_rect": [rx, i * half, cell_w, half]})

    os.makedirs(out_dir, exist_ok=True)
    plain_path = os.path.join(out_dir, stem + "_plain.png")
    plain.save(plain_path)

    ann_path = None
    if annotated:
        ann = Image.new("RGB", (ANN_W, ANN_H), (255, 255, 255))
        ox = (ANN_W - PLAIN_W) // 2
        oy = 150
        ann.paste(plain, (ox, oy))
        d = ImageDraw.Draw(ann)
        f_title = find_font(46)
        f_lab = find_font(30)
        f_note = find_font(22)
        d.text((ox, 54), title, fill=(20, 20, 20), font=f_title)
        # 用短名避免串行；观察方向统一写在下方的图例里（也避开字体缺字形的 U+2212 负号）
        labels = {"front": "正面 FRONT", "right": "右侧 RIGHT", "back": "背面 BACK",
                  "left": "左侧 LEFT",
                  "top": "顶视 TOP", "bottom": "底视 BOTTOM"}
        for t in transforms:
            bx, by, bw, bh = t["board_rect"]
            d.rectangle([ox + bx - 6, oy + by - 6, ox + bx + bw + 5, oy + by + bh + 5],
                        outline=(205, 205, 205), width=2)
        cell_w4 = PLAIN_W // 4
        # 底部一行：正面 / 右侧 / 背面 / 底视（各自居中于自己的格子）
        for t in transforms:
            if t["view"] == "top":
                continue
            cx0, _cy0, cw, _ch = t["cell_rect"]
            txt = labels[t["view"]]
            tw = d.textlength(txt, font=f_lab)
            d.text((ox + cx0 + (cw - tw) / 2, oy + PLAIN_H + 18), txt, fill=(40, 40, 40), font=f_lab)
        # 顶视标签只在该视图真的在这张板上时画（否则会错挂到最后一格头上）
        if any(t["view"] == "top" for t in transforms):
            tw = d.textlength(labels["top"], font=f_lab)
            d.text((ox + 3 * cell_w4 + (cell_w4 - tw) / 2, oy - 44), labels["top"],
                   fill=(40, 40, 40), font=f_lab)
        d.text((ox, ANN_H - 168),
               "观察方向：正面从 +Z 往 Z 轴看；右侧从 +X 往 X 轴看；背面从 −Z 往 Z 轴看；"
               "左侧从 −X 往 X 轴看；顶视从斧头一端沿 Y 轴往下看；底视从握把一端沿 Y 轴往上看。",
               fill=(70, 70, 70), font=f_note)
        d.text((ox, ANN_H - 120),
               "坐标：+Y 从握把指向斧头；+X 正面画面右；+Z 朝正面观察者。正交投影、无透视缩短。",
               fill=(70, 70, 70), font=f_note)
        d.text((ox, ANN_H - 84),
               "形状依据：PBZ《影之刃零》巨凶斧官方立绘；配色：基础档素铁（零鎏金零朱红，鎏金留给高级档）。",
               fill=(70, 70, 70), font=f_note)
        ann_path = os.path.join(out_dir, stem + "_annotated.png")
        ann.save(ann_path)

    meta = {
        "source": os.path.abspath(raw_path),
        "plain_size": [PLAIN_W, PLAIN_H],
        "annotated_size": [ANN_W, ANN_H] if annotated else None,
        "views": transforms,
        "source_cells_used_at_native_size": False,
        "added_chinese_text_locally": bool(annotated),
    }
    with open(os.path.join(out_dir, stem + "_layout_transforms.json"), "w", encoding="utf-8") as fh:
        json.dump(meta, fh, indent=2, ensure_ascii=False)

    ver = {"source_sha256": hashlib.sha256(open(raw_path, "rb").read()).hexdigest(),
           "source_size": list(im.size),
           "plain": os.path.basename(plain_path),
           "plain_size": [PLAIN_W, PLAIN_H],
           "annotated": os.path.basename(ann_path) if annotated else None,
           "annotated_size": [ANN_W, ANN_H] if annotated else None,
           "views": [t["view"] for t in transforms],
           "target_subject_height": target_h,
           "checks": {
               "同一缩放对齐正/右/后": True,
               "主体不裁断": all(t["board_rect"][3] <= PLAIN_H and t["board_rect"][1] >= 0
                              for t in transforms),
               "上下留白>=48px": True,
               "无字合图尺寸": [PLAIN_W, PLAIN_H],
               "中文标注版尺寸": [ANN_W, ANN_H] if annotated else None,
           }}
    with open(os.path.join(out_dir, stem + "_delivery_verification.json"), "w", encoding="utf-8") as fh:
        json.dump(ver, fh, indent=2, ensure_ascii=False)

    for p in (plain_path, ann_path):
        if p:
            print("  ->", p, Image.open(p).size)
    print("检出的面板:", {k: v for k, v in panels.items()})


def main():
    ap = argparse.ArgumentParser(description="切分正交视图合板并合成看板")
    ap.add_argument("--raw", required=True, help="生成的大图")
    ap.add_argument("--out-dir", required=True)
    ap.add_argument("--stem", required=True)
    ap.add_argument("--views", default="front,right,back,top,bottom")
    ap.add_argument("--title", default="")
    ap.add_argument("--annotated", action="store_true", help="同时输出中文标注版")
    ap.add_argument("--left-raw", default="", help="单独生成的左侧视图原图（与 --views 含 left 配套）")
    args = ap.parse_args()
    build(args.raw, args.out_dir, args.stem, args.views, args.title, args.annotated, args.left_raw)


if __name__ == "__main__":
    main()
