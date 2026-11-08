# -*- coding: utf-8 -*-
"""从已验收的动作视频帧 → 生成视频首帧锚图（绿幕画布 + 指定人物高度/脚底线/留白）。

用途
    新动作（无母本）需要首尾帧工作流时，锚图必须同时满足：
      · 人物在画幅里的**像素高度**与已验收 idle 接近（避免到游戏里放大倍数不同而糊）
      · 道具（旗杆/长枪）上方留出**动作行程**（否则一动就出画）
    本脚本把一帧已抠好的角色图缩放到目标身高、按目标脚底线贴进指定画幅，
    并把背景填成均匀绿幕（视频出片再抠一遍）。

用法
    python Tools/visual_brief/build_anchor_from_keyed_frame.py \
        --src "C:/Users/steam/Pictures/gptGen/enemy1013_idle_ref_v1/idle_base_v1_keyed/f000.png" \
        --canvas 834x1112 --body-h 560 --feet-y 1000 --left-frac 0.20 \
        --out "C:/Users/steam/Pictures/gptGen/enemy1013_idle_ref_v1/1013_cast_anchor_green_834x1112.png"

说明
    · 输入 PNG 必须带 alpha（已抠好的角色帧），脚本只做缩放与贴图，不改角色像素。
    · 输出为纯绿幕 RGB PNG；G1 门槛要求绿幕四角颜色一致（脚本会自测并记录）。
"""
from __future__ import annotations

import argparse
import hashlib
import json
import pathlib

import numpy as np
from PIL import Image

GREEN = (0, 159, 62)


def sha256_of(p: pathlib.Path) -> str:
    return hashlib.sha256(p.read_bytes()).hexdigest()


def longest_run(col) -> int:
    best = cur = 0
    for v in col:
        cur = cur + 1 if v else 0
        best = max(best, cur)
    return best


def measure_subject(alpha: np.ndarray):
    """返回 (head_top_y, foot_y, body_h, total_top_y, bbox_x0, bbox_x1)。"""
    h, w = alpha.shape
    ys, xs = np.nonzero(alpha)
    x0, x1 = int(xs.min()), int(xs.max())
    foot = int(ys.max())
    total_top = int(ys.min())

    # 旗杆/长杆列：纵向连续覆盖接近全高（核心列），再按列占空率向两侧扩展到杆的实际边界
    runs = np.array([longest_run(alpha[:, x]) for x in range(w)])
    core = np.nonzero(runs >= 0.9 * h)[0]
    pole = None
    if len(core):
        occ = alpha.sum(axis=0)
        lo_p, hi_p = int(core.min()), int(core.max())
        while lo_p - 1 >= 0 and occ[lo_p - 1] >= 0.85 * h:
            lo_p -= 1
        while hi_p + 1 < w and occ[hi_p + 1] >= 0.85 * h:
            hi_p += 1
        pole = [lo_p, hi_p]

    # 身体中线带（±95px）内「最后一段有像素的行」= 头顶（旗面在中线带里是独立一段）
    lower = alpha[max(0, foot - 140):max(1, foot - 39), :].copy()
    if pole:
        lower[:, pole[0] - 1:pole[1] + 2] = False
    lc = np.nonzero(lower.any(axis=0))[0]
    cx = int((lc.min() + lc.max()) / 2) if len(lc) else (x0 + x1) // 2
    lo, hi = max(0, cx - 95), min(w, cx + 96)
    band = alpha[:, lo:hi].copy()
    if pole:
        band[:, max(0, pole[0] - 1 - lo):max(0, pole[1] + 2 - lo)] = False
    counts = band.sum(axis=1)
    has = counts >= 5
    seg = head = None
    for i in range(h):
        if has[i]:
            if seg is None:
                seg = i
        else:
            if seg is not None:
                head = seg
                seg = None
    if seg is not None:
        head = seg
    return head, foot, (foot - head + 1), total_top, x0, x1


def main():
    ap = argparse.ArgumentParser(description="已抠帧 → 绿幕锚图（指定身高/脚底线/画幅）")
    ap.add_argument("--src", required=True, help="带 alpha 的已抠角色帧 PNG")
    ap.add_argument("--out", required=True)
    ap.add_argument("--canvas", default="834x1112", help="目标画幅 WxH")
    ap.add_argument("--body-h", type=int, required=True, help="目标人物身高（头顶→脚底）px")
    ap.add_argument("--feet-y", type=int, required=True, help="目标脚底线 y")
    ap.add_argument("--left-frac", type=float, default=None,
                    help="人物最左像素在画幅里的位置比例（默认沿用源帧相对位置）")
    ap.add_argument("--center-frac", type=float, default=None,
                    help="人物身体中心在画幅里的位置比例（默认 0.5 居中；比 --left-frac 优先）")
    ap.add_argument("--head-top-y", type=int, default=None,
                    help="源帧里人物头顶 y（默认自动量测）")
    ap.add_argument("--json", default="", help="量测结果 JSON 输出路径")
    args = ap.parse_args()

    src = pathlib.Path(args.src)
    img = Image.open(src).convert("RGBA")
    arr = np.asarray(img).copy()
    alpha = arr[..., 3] > 0
    if not alpha.any():
        raise SystemExit("输入没有 alpha 像素（先抠图）")

    head, foot, body_h, total_top, x0, x1 = measure_subject(alpha)
    if args.head_top_y is not None:
        head = args.head_top_y
        body_h = foot - head + 1

    W, H = (int(v) for v in args.canvas.lower().split("x"))
    k = args.body_h / float(body_h)
    center_x_local = 0.0
    if args.center_frac is not None:
        # 用下半身外沿的中点当身体中心锚点（人��中，旗子往一侧伸）
        lower_mask = alpha[max(0, foot - 140):max(1, foot - 39), :].copy()
        occ = alpha.sum(axis=0)
        runs = np.array([longest_run(alpha[:, x]) for x in range(alpha.shape[1])])
        core = np.nonzero(runs >= 0.9 * alpha.shape[0])[0]
        if len(core):
            lo_p, hi_p = int(core.min()), int(core.max())
            while lo_p - 1 >= 0 and occ[lo_p - 1] >= 0.85 * alpha.shape[0]:
                lo_p -= 1
            while hi_p + 1 < alpha.shape[1] and occ[hi_p + 1] >= 0.85 * alpha.shape[0]:
                hi_p += 1
            lower_mask[:, lo_p - 1:hi_p + 2] = False
        lc = np.nonzero(lower_mask.any(axis=0))[0]
        if len(lc):
            center_x_local = float((lc.min() + lc.max()) / 2) - x0
    nw = max(1, int(round((x1 - x0 + 1) * k)))
    nh = max(1, int(round((foot - total_top + 1) * k)))
    small = img.crop((x0, total_top, x1 + 1, foot + 1)).resize((nw, nh), Image.LANCZOS)

    if args.center_frac is not None:
        # 人物身体中心对齐到画幅指定比例（旗子才往一侧伸，人居中）
        paste_x = int(round(args.center_frac * W - center_x_local * k))
    elif args.left_frac is not None:
        paste_x = int(round(args.left_frac * W))
    else:
        paste_x = int(round((x0 / img.width) * W))
    paste_y = args.feet_y - nh + 1

    canvas = Image.new("RGBA", (W, H), GREEN + (255,))
    canvas.paste(small, (paste_x, paste_y), small)
    out = pathlib.Path(args.out)
    out.parent.mkdir(parents=True, exist_ok=True)
    canvas.convert("RGB").save(out)

    # G1：输出绿幕底色自测（四角 + 四边中点）
    chk = np.asarray(canvas.convert("RGB")).astype(np.int16)
    patches = []
    for y0, y1 in ((0, 40), (H - 40, H)):
        for x0c, x1c in ((0, 40), (W - 40, W)):
            patches.append(chk[y0:y1, x0c:x1c].reshape(-1, 3))
    flat = np.concatenate(patches)
    info = {
        "source": {"path": str(src), "size": list(img.size), "sha256": sha256_of(src),
                   "head_top_y": head, "foot_y": foot, "body_h": body_h,
                   "total_top_y": total_top, "bbox_x": [x0, x1]},
        "target": {"canvas": [W, H], "body_h": args.body_h, "feet_y": args.feet_y,
                   "scale": round(k, 5), "paste_xy": [paste_x, paste_y]},
        "derived": {
            "head_top_y": paste_y,
            "pole_top_y": paste_y,
            "headroom_above_pole_top_px": paste_y,
            "headroom_as_pct_of_body": round(100.0 * paste_y / args.body_h, 1),
            "subject_width_px": nw,
            "subject_width_pct": round(100.0 * nw / W, 1),
            "bottom_margin_px": H - args.feet_y,
        },
        "green_check": {"median": [int(v) for v in np.median(flat, axis=0)],
                         "std": [round(float(v), 2) for v in flat.std(axis=0)],
                         "target": list(GREEN)},
        "output": {"path": str(out), "sha256": sha256_of(out), "bytes": out.stat().st_size},
    }
    jp = pathlib.Path(args.json) if args.json else out.with_suffix(".json")
    jp.write_text(json.dumps(info, indent=2, ensure_ascii=False), encoding="utf-8")
    print(json.dumps(info, indent=2, ensure_ascii=False))


if __name__ == "__main__":
    main()
