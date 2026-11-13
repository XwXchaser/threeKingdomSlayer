# -*- coding: utf-8 -*-
"""1013 旗手三组动作帧落位：素材包原始帧 → 游戏规格精灵（1440²）。

口径（沿用项目既有规格，不新造）
    画布 1440×1440（透明底）· 人物本体高 **605**（普通兵规格）· 靴底线 **1333** · 身体中心 x **720**（= pivot）
    pivot 0.5,0.5（PPU 16）由 Unity 导入设置决定，本脚本只产出 PNG。

    为什么是 1440² 而不是包 README 写的 1108×1440：
    Cast 的横扫帧（cast_07~10）旗面横向跨度 1164px，按 k=0.972 缩放后 1131px > 1108 → 会被画布切掉。
    1440² 装得下，且 pivot 仍是画布中心，落地口径不变。

以人为基点（**不把旗杆/旗面算进去**）
    * 落地基准 = **人的靴底线**；身体中心 x = 最底 45 行里靴子列区间的中心；
      脚底 = 可见底行（PIL bbox 下边是 exclusive，可见底行比几何底高 1px）。
    * 人物本体高 = **量出来的**（见 person_body_height()）：用水平开运算（NECK 宽）把
      旗杆（细，~30px）与旗面（挂在杆上）剥离，只留与人相连的躯干/头，
      取「最靠下的连通块」的顶端 = 人的头顶。
      ⚠ 素材台账里的 body_h 列**语义不统一**：idle/cast 是「人物本体高」，
      但 walk 那列记的是「旗顶→脚底」的整幅内容高（942），**把旗杆算进去了** ——
      直接拿来当缩放分母会让 walk 的人比 idle 矮 1/3。以人为基点必须重新量。

固定 k + 偏移策略
    * k = 605 / 该组的**人物本体高**（idle 取 6 帧中位；cast 取「idle 姿态」帧，即动作列含 idle 的帧；
      walk 取 6 帧中位）—— 全组一个 k，不逐帧归一化。
      逐帧归一化会把「头上下浮动」翻译成「整体缩放」，看着像在缩放、反倒没呼吸感。
    * idle / walk：组内**统一偏移**（中位数）—— 走路的水平摆动必须保留（逐帧对位会抹掉它）。
    * cast：旗杆高度在动作里变化 300+px，裁切原点跟着变 → **逐帧对齐人的靴底线**。
      人的脚在源片里是稳的（都落在 1332 行），所以逐帧对齐不会破坏动作。

用法
    python Tools/visual_brief/land_1013_frames.py --out-dir "Assets/Sprites/Enemy/Enemy1013"
"""
from __future__ import annotations

import argparse
import csv
import json
import pathlib
import statistics
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
from land_1051_attack import binarize, degreen, sha256_of  # noqa: E402

CANVAS = 1440
TARGET_BODY_H = 605      # 游戏里普通兵的人物本体高（1012 已部署规格）
FOOT_LINE = 1333         # 人的靴底线
CENTER_X = 720           # 身体中心 x（= 画布中心 = pivot x）
BOOT_BAND = 45           # 量靴子用的底部行数
NECK = 45                # 形态学开运算的窗口宽：比旗杆(~30px)宽、比人窄 → 把旗杆/旗面剥离

# 每组用哪几帧当「人物本体高」的基准
#   idle : 全部 6 帧中位
#   cast : 只取 idle 姿态的帧（动作列含 idle）—— 举旗/横扫帧的头顶会被举起的手臂带偏
#   walk : 全部 6 帧中位
GROUP_REF = {"idle": "all", "cast": "idle_pose", "walk": "all"}
# 偏移策略：统一（组内中位数）还是逐帧（对齐人的靴底）
GROUP_MODE = {"idle": "uniform", "cast": "per_frame", "walk": "uniform"}

GROUPS = {
    "idle": ("Enemy1013ArtSource/04_IdleFrames", "Enemy1013ArtSource/04_IdleFrames/frame-provenance.csv", "idle"),
    "cast": ("Enemy1013ArtSource/03_CastFrames", "Enemy1013ArtSource/03_CastFrames/frame-provenance.csv", "cast"),
    "walk": ("Enemy1013ArtSource/05_WalkFrames", "Enemy1013ArtSource/05_WalkFrames/frame-provenance.csv", "walk"),
}


def read_manifest(csv_path: pathlib.Path):
    with csv_path.open(encoding="utf-8-sig", newline="") as fh:
        rows = list(csv.DictReader(fh))
    out = []
    for r in rows:
        f = (r.get("file") or "").strip()
        if f and f.lower().endswith(".png"):
            out.append((f, r))
    return out


def analyze(alpha: np.ndarray):
    """以人为基点的量测：返回 (visible_bottom, boot_center_x, boot_columns, bbox)。"""
    ys, xs = np.nonzero(alpha > 0)
    if len(ys) == 0:
        raise SystemExit("空掩码")
    foot = int(ys.max())
    lo = max(0, foot - BOOT_BAND + 1)
    band_cols = np.nonzero(alpha[lo:foot + 1, :].max(axis=0) > 0)[0]
    if len(band_cols) == 0:
        raise SystemExit("靴子带为空")
    boot_cx = int(round((band_cols.min() + band_cols.max()) / 2.0))
    return foot, boot_cx, [int(band_cols.min()), int(band_cols.max())], \
        [int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())]


def head_top_of_person(alpha: np.ndarray):
    """水平开运算（NECK 宽）剥离旗杆(细)与旗面(挂在杆上)，返回与人相连的那块的顶端（人的头顶）。
    NECK 取 45：比旗杆(~30px)宽、比人窄。"""
    h, w = alpha.shape
    m = alpha > 0
    e = m.copy()
    for s in range(1, NECK // 2 + 1):
        e &= np.roll(m, s, axis=1) & np.roll(m, -s, axis=1)
    d = e.copy()
    for s in range(1, NECK // 2 + 1):
        d |= np.roll(e, s, axis=1) | np.roll(e, -s, axis=1)
    if not d.any():
        return -1
    # 连通块：取 max row 最大的那个（含脚）
    seen = np.zeros_like(d)
    best_top = -1
    best_bottom = -1
    for sy in range(h - 1, -1, -1):
        for sx in np.nonzero(d[sy])[0]:
            if seen[sy, sx]:
                continue
            stack = [(sy, sx)]
            seen[sy, sx] = True
            comp = []
            while stack:
                y, x = stack.pop()
                comp.append((y, x))
                for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    ny, nx = y + dy, x + dx
                    if 0 <= ny < h and 0 <= nx < w and d[ny, nx] and not seen[ny, nx]:
                        seen[ny, nx] = True
                        stack.append((ny, nx))
            arr = np.array(comp)
            ctop, cbottom = int(arr[:, 0].min()), int(arr[:, 0].max())
            if cbottom > best_bottom:
                best_bottom, best_top = cbottom, ctop
            if cbottom == h - 1:
                break
        if best_bottom == h - 1:
            break
    return best_top


def person_body_height(alpha: np.ndarray):
    """返回 (脚底行, 人的头顶行, 人物本体高)。"""
    foot = int(np.nonzero(alpha > 0)[0].max())
    head = head_top_of_person(alpha)
    return foot, head, (foot - head + 1 if head >= 0 else -1)


def land_group(group: str, root: pathlib.Path, out_dir: pathlib.Path, records: list):
    src_dir, csv_rel, prefix = GROUPS[group]
    mode = GROUP_MODE[group]
    ref_mode = GROUP_REF[group]
    entries = read_manifest(root / csv_rel)
    if not entries:
        raise SystemExit("%s 台账里没有帧: %s" % (group, csv_rel))

    # ── 第 1 遍：量每帧的「人物本体高」（以人为基点）──
    srcs = []
    for fname, row in entries:
        p = root / src_dir / fname
        if not p.is_file():
            raise SystemExit("缺帧: %s" % p)
        rgba = np.asarray(Image.open(p).convert("RGBA")).copy()
        alpha = binarize(rgba)
        foot, boot_cx, boot_cols, bbox = analyze(alpha)
        sfoot, shead, sbh = person_body_height(alpha)
        srcs.append({"file": fname, "row": row, "rgba": rgba, "alpha": alpha,
                     "foot": foot, "boot_cx": boot_cx, "boot_cols": boot_cols,
                     "bbox": bbox, "person_head": shead, "person_body_h": sbh})

    # 参考帧：cast 只取 idle 姿态的帧（举旗/横扫帧的头顶会被举起的手臂带偏）
    if ref_mode == "idle_pose":
        refs = [s for s in srcs if "idle" in (s["row"].get("action") or "").lower()]
        if not refs:
            refs = srcs
    else:
        refs = srcs
    ref_body = int(statistics.median([s["person_body_h"] for s in refs]))
    if ref_body <= 0:
        raise SystemExit("%s 量不到人物本体高" % group)
    k = TARGET_BODY_H / float(ref_body)
    print("%s: 参考人物本体高=%d（%d 帧）→ k=%.5f" % (group, ref_body, len(refs), k))

    # ── 第 2 遍：裁切 → 缩放 → 贴到统一画布 ──
    frames = []
    for s in srcs:
        rgba = s["rgba"]
        x0, y0, x1, y1 = s["bbox"]
        crop = Image.fromarray(rgba, mode="RGBA").crop((x0, y0, x1 + 1, y1 + 1))
        nw = max(1, int(round(crop.width * k)))
        nh = max(1, int(round(crop.height * k)))
        small = crop.resize((nw, nh), Image.LANCZOS)
        sa = np.asarray(small).copy()
        sa[..., 3] = binarize(sa)           # 缩放后重抠，alpha 保持二值
        ys2, _ = np.nonzero(sa[..., 3] > 0)
        visible_bottom = int(ys2.max())
        frames.append({
            "file": s["file"], "row": s["row"],
            "small": Image.fromarray(sa, mode="RGBA"),
            "x0": x0, "y0": y0, "foot": s["foot"], "boot_cx": s["boot_cx"],
            "boot_cols": s["boot_cols"], "bbox": s["bbox"],
            "person_head": s["person_head"], "person_body_h": s["person_body_h"],
            "body_h_src": s["row"].get("body_h", ""),
            "paste_x": int(round(CENTER_X - (s["boot_cx"] - x0) * k)),
            "paste_y": FOOT_LINE - int(round((s["foot"] - y0) * k)),
            "visible_bottom_scaled": visible_bottom,
        })

    if mode == "uniform":
        paste_x = int(round(statistics.median([f["paste_x"] for f in frames])))
        paste_y = int(round(statistics.median([f["paste_y"] for f in frames])))
    else:
        paste_x = paste_y = None
    if paste_x is not None:
        spread_x = max(abs(f["paste_x"] - paste_x) for f in frames)
        spread_y = max(abs(f["paste_y"] - paste_y) for f in frames)
    else:
        spread_x = max(f["paste_x"] for f in frames) - min(f["paste_x"] for f in frames)
        spread_y = max(f["paste_y"] for f in frames) - min(f["paste_y"] for f in frames)

    for idx, f in enumerate(frames, start=1):
        px = f["paste_x"] if paste_x is not None else f["paste_x"]
        py = f["paste_y"] if paste_y is not None else f["paste_y"]
        canvas = Image.new("RGBA", (CANVAS, CANVAS), (0, 0, 0, 0))
        canvas.paste(f["small"], (px, py), f["small"])
        carr = np.asarray(canvas).copy()
        degreened = degreen(carr)
        canvas = Image.fromarray(carr, mode="RGBA")

        out_name = "Enemy_1013_%s%d.png" % (prefix, idx)
        out = out_dir / out_name
        canvas.save(out)

        a3 = np.asarray(canvas)[..., 3]
        ys3, xs3 = np.nonzero(a3 > 0)
        band = np.nonzero(a3[max(0, ys3.max() - BOOT_BAND + 1):ys3.max() + 1, :].max(axis=0) > 0)[0]
        boot_cx_out = int(round((band.min() + band.max()) / 2.0)) if len(band) else -1
        head_out = head_top_of_person(a3)

        rec = {
            "index": idx,
            "output": {"path": str(out), "sha256": sha256_of(out), "bytes": out.stat().st_size},
            "source": {"file": f["file"], "bbox": list(f["bbox"]),
                       "foot_y": f["foot"], "boot_center_x": f["boot_cx"],
                       "boot_cols": f["boot_cols"],
                       "person_head_y": f["person_head"],
                       "person_body_h": f["person_body_h"],
                       "ledger_body_h": f["body_h_src"]},
            "landing": {"scale": k, "mode": mode,
                        "paste_xy": [px, py],
                        "visible_bottom_scaled": f["visible_bottom_scaled"]},
            "result": {"canvas": [CANVAS, CANVAS],
                       "foot_line": int(ys3.max()),
                       "boot_center_x": boot_cx_out,
                       "head_top_y": head_out,
                       "body_h": (int(ys3.max()) - head_out + 1) if head_out >= 0 else -1,
                       "bbox": [int(xs3.min()), int(ys3.min()), int(xs3.max()), int(ys3.max())],
                       "semi_alpha_px": int(((a3 > 0) & (a3 < 255)).sum()),
                       "degreened_px": degreened},
        }
        records.append(rec)
        bb = rec["result"]["bbox"]
        clip = "" if bb[0] >= 0 and bb[1] >= 0 and bb[2] < CANVAS and bb[3] < CANVAS else "  ⚠出框"
        print("%-6s %-24s 台账身=%-5s 实测身=%-5s 落地身=%-5s 脚底=%-5d 身体中心x=%-5d 半透明=%d%s"
              % (prefix, f["file"], f["body_h_src"] or "?", f["person_body_h"],
                 rec["result"]["body_h"],
                 rec["result"]["foot_line"], rec["result"]["boot_center_x"],
                 rec["result"]["semi_alpha_px"], clip))

    print("  └ %s  参考人物本体高=%d  k=%.5f  偏移策略=%s  横向散布 %d px  纵向散布 %d px\n"
          % (group, ref_body, k, mode, spread_x, spread_y))
    return {"group": group, "scale": k, "offset_mode": mode, "ref_person_body_h": ref_body,
            "spread_x": spread_x, "spread_y": spread_y, "frames": len(frames)}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--root", default=".")
    ap.add_argument("--out-dir", default="Assets/Sprites/Enemy/Enemy1013")
    ap.add_argument("--json", default="Enemy1013ArtSource/00_Index/landing-1013.json")
    args = ap.parse_args()

    root = pathlib.Path(args.root).resolve()
    out_dir = (root / args.out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)

    records: list = []
    groups = []
    for g in ("idle", "cast", "walk"):
        groups.append(land_group(g, root, out_dir, records))

    rec = {
        "canvas": [CANVAS, CANVAS],
        "target": {"body_h": TARGET_BODY_H, "foot_line": FOOT_LINE, "center_x": CENTER_X},
        "pivot": {"value": [0.5, 0.5], "ppu": 16},
        "note": "以人为基点：脚底=可见底行，身体中心x=靴子列区间中心；旗杆/旗面不参与定尺与落地基准",
        "groups": groups,
        "sprites": records,
    }
    jp = root / args.json
    jp.parent.mkdir(parents=True, exist_ok=True)
    jp.write_text(json.dumps(rec, indent=2, ensure_ascii=False), encoding="utf-8")
    print("落位记录: %s" % jp)


if __name__ == "__main__":
    main()
