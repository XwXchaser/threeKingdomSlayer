# -*- coding: utf-8 -*-
"""1051 弓手 attack 选帧落地：抠好的帧 → 游戏规格精灵（1108²）。

口径（沿用项目既有规格，不新造）
    画布 1108×1108（透明底）· 人物本体高 **605**（普通兵规格）· 脚底线 **897** · 身体中心 x **521**
    —— 与 `Tools/enemy1012_attack_land.py`（1012 的 idle/attack 落位）同一套数。
    pivot 0.5,0.5（PPU 16）由 Unity 导入设置决定，本脚本只产出 PNG。

输入
    * 视频帧：已用 `Tools/visual_brief/chroma_key.py` 抠好的目录（T=79 / halo 2 轮 / alpha 二值）
    * 静帧姿势图：gpt-image 出的绿幕图（需自己抠 + 二值化）

要点
    * 落位按**可见底行**对齐（PIL 的 bbox 下边是 exclusive，可见底行比几何底高 1px）
    * 缩放后**再抠一次**边缘（LANCZOS 会带出软边），alpha 保持二值
    * 头顶取「身体中心列 ±60px 内最高像素」——弓顶比头顶高，不能直接用 bbox 顶
    * 本脚本只写 PNG，**不碰 Assets/**（1051 还没有 prefab，先放素材包里）

用法
    python Tools/visual_brief/land_1051_attack.py \
        --keyed Library/Locus/tmp/1051_shoot_v3_keyed \
        --still "Enemy1051ArtSource/02_IdentityImages/1051_pose_nock_greenscreen_1024.png" \
        --pick attack1=still --pick attack2=f048 --pick attack3=f114 \
        --out-dir "Enemy1051ArtSource/04_Sprites"
"""
from __future__ import annotations

import argparse
import hashlib
import json
import pathlib

import numpy as np
from PIL import Image

GREEN = (0, 159, 62)
CANVAS = 1108
TARGET_BODY_H = 605      # 游戏里普通兵的人物本体高（1012 已部署规格）
FOOT_LINE = 897          # 可见脚底线
CENTER_X = 521           # 身体中心 x


def sha256_of(p: pathlib.Path) -> str:
    return hashlib.sha256(p.read_bytes()).hexdigest()


def binarize(rgba: np.ndarray) -> np.ndarray:
    """alpha 二值化 + 补内部孔洞。"""
    a = rgba[..., 3]
    alpha = np.where(a >= 128, 255, 0).astype(np.uint8)
    for _ in range(2):
        filled = alpha.copy()
        filled[1:-1, 1:-1] = np.where(
            (alpha[:-2, 1:-1] > 0) & (alpha[2:, 1:-1] > 0)
            & (alpha[1:-1, :-2] > 0) & (alpha[1:-1, 2:] > 0),
            255, alpha[1:-1, 1:-1])
        if (filled == alpha).all():
            break
        alpha = filled
    return alpha


def measure(alpha: np.ndarray):
    """返回 (head_top_y, foot_y, body_h, body_center_x, bbox)。"""
    ys, xs = np.nonzero(alpha > 0)
    if len(ys) == 0:
        raise SystemExit("空掩码")
    foot = int(ys.max())
    top_row = np.full(alpha.shape[1], -1, dtype=np.int64)
    for x in range(int(xs.min()), int(xs.max()) + 1):
        col = np.nonzero(alpha[:, x])[0]
        if len(col):
            top_row[x] = col.min()
    lo = max(0, foot - 140)
    hi = max(1, foot - 39)
    cols = np.nonzero(alpha[lo:hi, :] > 0)[1]
    cx = int(round((cols.min() + cols.max()) / 2.0)) if len(cols) else int((xs.min() + xs.max()) / 2)
    cand = top_row[max(0, cx - 60):min(alpha.shape[1], cx + 61)]
    cand = cand[cand >= 0]
    head = int(cand.min()) if len(cand) else int(ys.min())
    return head, foot, foot - head + 1, cx, [int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())]


def load_source(spec: str, keyed: pathlib.Path):
    """spec = 'still' | 'fNNN'（视频帧）→ (rgba ndarray, 来源说明)"""
    if spec == "still":
        raise SystemExit("静帧请用 --still 单独指定")
    p = keyed / ("f%03d.png" % int(spec[1:]))
    if not p.is_file():
        raise SystemExit("缺帧: %s" % p)
    return np.asarray(Image.open(p).convert("RGBA")).copy(), str(p)


def load_still(path: pathlib.Path):
    """静帧绿幕图 → 抠图 + 二值化。"""
    im = Image.open(path).convert("RGB")
    rgb = np.asarray(im).astype(np.float32)
    d = np.linalg.norm(rgb - np.array(GREEN, dtype=np.float32), axis=2)
    # 绿幕键：到基准色的距离 > 79 记为主体
    alpha = np.where(d > 79, 255, 0).astype(np.uint8)
    # 边缘去绿味（2 轮）
    for _ in range(2):
        greenish = (d < 140) & (alpha > 0)
        border = np.zeros_like(greenish)
        border[1:, :] |= alpha[:-1, :] == 0
        border[:-1, :] |= alpha[1:, :] == 0
        border[:, 1:] |= alpha[:, :-1] == 0
        border[:, :-1] |= alpha[:, 1:] == 0
        alpha[greenish & border] = 0
    rgba = np.dstack([np.asarray(im), alpha])
    return rgba, str(path)


def degreen(rgba: np.ndarray) -> int:
    """清掉主体内部残留的绿味像素（项目交付契约：幕布残留 0）。

    做法：主体内到绿幕基准色距离 < 90 的像素，用周围非绿味邻居的颜色覆盖。
    返回被处理的像素数。
    """
    rgb = rgba[..., :3].astype(np.float32)
    alpha = rgba[..., 3]
    d = np.linalg.norm(rgb - np.array(GREEN, dtype=np.float32), axis=2)
    bad = (alpha > 0) & (d < 90)
    if not bad.any():
        return 0
    out = rgba.copy()
    fixed = 0
    for _ in range(3):
        ys, xs = np.nonzero(bad)
        for y, x in zip(ys.tolist(), xs.tolist()):
            neigh = []
            for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1)):
                ny, nx = y + dy, x + dx
                if 0 <= ny < rgba.shape[0] and 0 <= nx < rgba.shape[1] and not bad[ny, nx] and rgba[ny, nx, 3] > 0:
                    neigh.append(out[ny, nx, :3])
            if neigh:
                out[y, x, :3] = np.median(np.array(neigh, dtype=np.float32), axis=0).astype(np.uint8)
                fixed += 1
        d = np.linalg.norm(out[..., :3].astype(np.float32) - np.array(GREEN, dtype=np.float32), axis=2)
        bad = (out[..., 3] > 0) & (d < 90)
        if not bad.any():
            break
    rgba[...] = out
    return fixed


def land(rgba: np.ndarray, name: str, out_dir: pathlib.Path, fixed_k: float = None,
         fixed_offset=None):
    alpha = binarize(rgba)
    head, foot, body_h, cx, bbox = measure(alpha)
    k = fixed_k if fixed_k else (TARGET_BODY_H / float(body_h))
    x0, y0, x1, y1 = bbox
    crop = Image.fromarray(rgba, mode="RGBA").crop((x0, y0, x1 + 1, y1 + 1))
    nw = max(1, int(round(crop.width * k)))
    nh = max(1, int(round(crop.height * k)))
    small = crop.resize((nw, nh), Image.LANCZOS)
    sa = np.asarray(small).copy()
    sa[..., 3] = binarize(sa)          # 缩放后重抠，alpha 保持二值
    small = Image.fromarray(sa, mode="RGBA")

    # 缩放后重新量：可见底行与身体中心
    a2 = sa[..., 3]
    ys2, xs2 = np.nonzero(a2 > 0)
    visible_bottom = int(ys2.max())
    body_cx_scaled = (cx - x0) * k
    if fixed_offset is not None:
        paste_x, paste_y = fixed_offset
    else:
        paste_x = int(round(CENTER_X - body_cx_scaled))
        paste_y = FOOT_LINE - visible_bottom

    canvas = Image.new("RGBA", (CANVAS, CANVAS), (0, 0, 0, 0))
    canvas.paste(small, (paste_x, paste_y), small)
    # 去绿味残留（交付契约：幕布残留 0）
    carr = np.asarray(canvas).copy()
    degreened = degreen(carr)
    canvas = Image.fromarray(carr, mode="RGBA")
    out = out_dir / ("Enemy_1051_%s.png" % name)
    canvas.save(out)

    # 成品量测
    a3 = np.asarray(canvas)[..., 3]
    ys3, xs3 = np.nonzero(a3 > 0)
    body_top = paste_y + int(round((head - y0) * k))
    return {
        "name": name,
        "output": {"path": str(out), "sha256": sha256_of(out), "bytes": out.stat().st_size},
        "source": {"bbox": bbox, "head_top_y": head, "foot_y": foot,
                   "body_h": body_h, "body_center_x": cx},
        "landing": {"scale": round(k, 5), "paste_xy": [paste_x, paste_y],
                    "visible_bottom_scaled": visible_bottom},
        "result": {"canvas": [CANVAS, CANVAS], "body_h": body_h,
                   "body_top_y": body_top, "foot_line": int(ys3.max()),
                   "body_center_x": round(float((xs3.min() + xs3.max()) / 2), 1),
                   "bbox": [int(xs3.min()), int(ys3.min()), int(xs3.max()), int(ys3.max())],
                   "semi_alpha_px": int(((a3 > 0) & (a3 < 255)).sum()),
                   "degreened_px": degreened},
    }


def main():
    ap = argparse.ArgumentParser(description="1051 attack 选帧落地")
    ap.add_argument("--keyed", required=True, type=pathlib.Path, help="已抠图的视频帧目录")
    ap.add_argument("--still", required=True, help="搭箭静帧（绿幕图）路径")
    ap.add_argument("--pick", action="append", required=True,
                    help="key=still | key=fNNN，例：--pick attack2=f048")
    ap.add_argument("--out-dir", required=True, type=pathlib.Path)
    ap.add_argument("--json", default="")
    ap.add_argument("--fixed-k", type=float, default=0.0,
                    help="固定缩放系数（不给则逐帧归一化到本体高 605）。"
                         "**idle 必须用固定 k**：逐帧归一化会把「头上下浮动」翻译成「整体缩放」")
    ap.add_argument("--fixed-offset", action="store_true",
                    help="统一偏移：k 与贴图位置都取各帧的中位数（**走路用**："
                         "逐帧对位会把角色的水平摆动抹掉）")
    args = ap.parse_args()

    args.out_dir.mkdir(parents=True, exist_ok=True)
    still_rgba, still_path = load_still(pathlib.Path(args.still))
    records = []
    fixed_offset = None
    if args.fixed_offset:
        # 先量各帧，再取中位数：k 固定、贴图位置统一
        meas = []
        for p in args.pick:
            name, _, spec = p.partition("=")
            rgba, _ = (still_rgba, still_path) if spec == "still" else load_source(spec, args.keyed)
            al = binarize(rgba)
            head, foot, body_h, cx, bbox = measure(al)
            k = args.fixed_k if args.fixed_k else (TARGET_BODY_H / float(body_h))
            x0, y0, x1, y1 = bbox
            crop = Image.fromarray(rgba, mode="RGBA").crop((x0, y0, x1 + 1, y1 + 1))
            nw = max(1, int(round(crop.width * k))); nh = max(1, int(round(crop.height * k)))
            small = crop.resize((nw, nh), Image.LANCZOS)
            sa = np.asarray(small).copy(); sa[..., 3] = binarize(sa)
            ys2, _ = np.nonzero(sa[..., 3] > 0)
            meas.append({"k": k, "body_h": body_h, "cx_scaled": (cx - x0) * k,
                         "bottom_scaled": int(ys2.max())})
        med_k = args.fixed_k or TARGET_BODY_H / float(np.median([m["body_h"] for m in meas]))
        for m in meas:
            m["k"] = med_k
        px = int(round(CENTER_X - float(np.median([m["cx_scaled"] for m in meas]))))
        py = FOOT_LINE - int(np.median([m["bottom_scaled"] for m in meas]))
        fixed_offset = (px, py)
        args.fixed_k = med_k          # 固定 k 也统一，避免逐帧缩放差
        print("统一偏移：k=%.5f paste=(%d,%d)（取各帧中位数，保留源片自身的水平摆动）"
              % (med_k, px, py))

    for p in args.pick:
        name, _, spec = p.partition("=")
        if spec == "still":
            rgba, src = still_rgba, still_path
        else:
            rgba, src = load_source(spec, args.keyed)
        rec = land(rgba, name, args.out_dir, args.fixed_k or None, fixed_offset)
        rec["source"]["path"] = src
        records.append(rec)
        print("%-8s ← %s | body_h %d→605  k=%.5f  foot→%d  bbox=%s  半透明=%d"
              % (name, src.rsplit("/", 1)[-1].rsplit("\\", 1)[-1], rec["source"]["body_h"],
                 rec["landing"]["scale"], rec["result"]["foot_line"],
                 rec["result"]["bbox"], rec["result"]["semi_alpha_px"]))

    rec = {"canvas": [CANVAS, CANVAS], "target": {"body_h": TARGET_BODY_H, "foot_line": FOOT_LINE,
                                                  "center_x": CENTER_X},
           "sprites": records}
    jp = pathlib.Path(args.json) if args.json else args.out_dir / "1051_attack_landing.json"
    jp.write_text(json.dumps(rec, indent=2, ensure_ascii=False), encoding="utf-8")
    print("\n落位记录: %s" % jp)


if __name__ == "__main__":
    main()
