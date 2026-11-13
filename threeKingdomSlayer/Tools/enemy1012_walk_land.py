# -*- coding: utf-8 -*-
"""1012 贼兵 walk 动画：选帧 → 落位 → 写入 Unity 精灵（本地，全像素操作，零生成成本）。

口径（与 idle / attack 同一套，落地后互相无跳变）
    · 画布       1108×1108
    · 脚底线     897      · 中心 x  521      · 缩放 k = 0.92366（与 idle 同一缩放）
    · x 锚点     躯干中心（bbox 高度 30%–55% 带的 x 中点）→ 取 6 帧中位数对齐到 521
                  （不用 bbox 中心：刀伸出画面会把 bbox 拉偏）
    · y 偏移     与 idle 相同（脚底 897）；步态自身的起伏（源片靴底行 827–848）原样保留
    · 写回       只覆盖 PNG 字节，保留 .meta（GUID / pivot / PPU 16 不变）

用法
    python Tools/enemy1012_walk_land.py --keyed <抠图目录> --picks 40,43,45,48,51,53 [--apply]
"""
from __future__ import annotations

import argparse
import json
import pathlib
import shutil
import sys
import time

import numpy as np
from PIL import Image

ROOT = pathlib.Path(r"C:/threeKingdomSlayer/threeKingdomSlayer")
DEST = ROOT / "Assets/Sprites/Enemy/Enemy1012"
NAME = "Enemy_1012_walk{}.png"
TMP = ROOT / "Library/Locus/tmp"

CANVAS = 1108
FOOT_LINE = 897
CENTER_X = 521
K = 0.9236641221374046
SRC_FOOT_ROW = 839          # 源片靴底中位（实测 838–839，与 idle 源片一致）


def mask_of(p: pathlib.Path) -> np.ndarray:
    return np.asarray(Image.open(p).convert("RGBA"))[:, :, 3] > 128


def bbox_of(m: np.ndarray):
    ys, xs = np.where(m)
    if not len(xs):
        return None
    return int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1


def torso_center(m: np.ndarray) -> float:
    ys, xs = np.where(m)
    top, bot = int(ys.min()), int(ys.max())
    band = m[top + int((bot - top) * 0.30): top + int((bot - top) * 0.55), :]
    bys, bxs = np.where(band)
    return float((bxs.min() + bxs.max()) / 2.0)


def main() -> int:
    ap = argparse.ArgumentParser(description="1012 walk 落位与写入")
    ap.add_argument("--keyed", required=True, type=pathlib.Path)
    ap.add_argument("--picks", required=True, help="6 个源帧号，逗号分隔")
    ap.add_argument("--apply", action="store_true")
    args = ap.parse_args()

    picks = [int(x) for x in args.picks.split(",") if x.strip()]
    if len(picks) != 6:
        raise SystemExit("必须给 6 个帧号")

    scaled_side = int(round(960 * K))
    masks = {f: mask_of(args.keyed / ("f%03d.png" % f)) for f in picks}
    torsos = [torso_center(m) for m in masks.values()]
    med = float(np.median(torsos))
    off_x = int(round(CENTER_X - med * K))
    off_y = int(round(FOOT_LINE - SRC_FOOT_ROW * K))

    print("躯干中心: %s  中位=%.1f" % (" ".join("%.1f" % t for t in torsos), med))
    print("落位偏移: x=%+d  y=%+d   （脚底 %.1f→%d, k=%.5f, 缩放边长 %d）"
          % (off_x, off_y, SRC_FOOT_ROW * K, SRC_FOOT_ROW * K + off_y, K, scaled_side))

    staged = TMP / ("stage_walk_" + time.strftime("%Y%m%d-%H%M%S"))
    staged.mkdir(parents=True, exist_ok=True)
    report = {"keyed": str(args.keyed), "k": K, "canvas": CANVAS, "foot_line": FOOT_LINE,
              "center_x": CENTER_X, "offset": {"x": off_x, "y": off_y},
              "torso_centers": [round(t, 1) for t in torsos], "frames": []}

    for i, fr in enumerate(picks, 1):
        im = Image.open(args.keyed / ("f%03d.png" % fr)).convert("RGBA")
        scaled = im.resize((scaled_side, scaled_side), Image.LANCZOS)
        canvas = Image.new("RGBA", (CANVAS, CANVAS), (0, 0, 0, 0))
        canvas.paste(scaled, (off_x, off_y), scaled)
        dst = staged / ("w%02d.png" % i)
        canvas.save(dst)
        m = mask_of(dst)
        bb = bbox_of(m)
        report["frames"].append({"order": i, "source_frame": "f%03d.png" % fr,
                                  "bbox": bb, "h": bb[3] - bb[1],
                                  "cx": (bb[0] + bb[2]) / 2.0})
        print("  w%02d <- f%03d  bbox=%s  高=%d  中心x=%.1f"
              % (i, fr, bb, bb[3] - bb[1], (bb[0] + bb[2]) / 2.0))

    (staged / "landing_report.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")

    if not args.apply:
        print("\n干跑完成（未写入）。加 --apply 才写入工程。暂存：%s" % staged)
        return 0

    bdir = TMP / ("backup_Enemy1012_walk_" + time.strftime("%Y%m%d-%H%M%S"))
    bdir.mkdir(parents=True, exist_ok=True)
    for i in range(1, 7):
        name = NAME.format(i)
        target = DEST / name
        if target.exists():
            shutil.copy2(target, bdir / name)
        shutil.copy2(staged / ("w%02d.png" % i), target)
        meta = DEST / (name + ".meta")
        if not meta.exists():
            raise SystemExit("缺少 .meta：%s" % meta)
    print("\n已写入 6 张：%s" % DEST)
    print("备份：%s" % bdir)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
