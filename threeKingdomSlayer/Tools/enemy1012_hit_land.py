# -*- coding: utf-8 -*-
"""1012 贼兵 hitHeavy（正面受击）动画：选帧 → 与 idle 对齐 → 落位 → 写入 Unity 精灵。

口径（与 idle / walk / attack 同一套）
    · 画布   1108×1108   · 脚底线 897   · 中心 x 521   · k = 0.92366（idle 同缩放）
    · 对齐   受击片里"最接近站姿"的那一帧（冲击前）↔ idle 站姿帧 做掩码配准，取整数 (dx,dy)
              → 全 5 帧共用同一固定偏移（身体后仰是动作本身，必须保留）
    · 末帧   h6 直接用工程里的 idle1（受击收尾 = 回到 idle 姿态与表情，无缝）
    · 写回   只覆盖 PNG 字节，保留 .meta（GUID / pivot 0.6539,0.5487 / PPU 16 不变）

用法
    python Tools/enemy1012_hit_land.py \
        --keyed <受击片抠图目录> --ref-keyed <idle 抠图目录> \
        --picks 8,13,17,30,80 --apply
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
NAME = "Enemy_1012_hitHeavy{}.png"
TMP = ROOT / "Library/Locus/tmp"

CANVAS = 1108
FOOT_LINE = 897
CENTER_X = 521
K = 0.9236641221374046


def mask_of(p: pathlib.Path) -> np.ndarray:
    return np.asarray(Image.open(p).convert("RGBA"))[:, :, 3] > 128


def iou(a: np.ndarray, b: np.ndarray) -> float:
    inter = np.logical_and(a, b).sum()
    union = np.logical_or(a, b).sum()
    return float(inter) / union if union else 0.0


def register(a: np.ndarray, b: np.ndarray, rng: int = 40):
    best = (0, 0, -1.0)
    for dy in range(-rng, rng + 1):
        for dx in range(-rng, rng + 1):
            s = iou(np.roll(np.roll(a, dy, axis=0), dx, axis=1), b)
            if s > best[2]:
                best = (dx, dy, s)
    return best


def bbox_of(m: np.ndarray):
    ys, xs = np.where(m)
    return int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1


def main() -> int:
    ap = argparse.ArgumentParser(description="1012 hitHeavy 落位与写入")
    ap.add_argument("--keyed", required=True, type=pathlib.Path)
    ap.add_argument("--ref-keyed", required=True, type=pathlib.Path, help="idle 抠图目录")
    ap.add_argument("--ref-frame", default="f000.png")
    ap.add_argument("--picks", required=True, help="5 个源帧号（逗号分隔），末帧 idle 单独处理")
    ap.add_argument("--idle-sprite", default="Assets/Sprites/Enemy/Enemy1012/Enemy_1012_idle1.png")
    ap.add_argument("--prefix", default="Enemy_1012_hitHeavy",
                    help="写入的精灵名前缀（hitHeavy / hitRight / hitLeft ...）")
    ap.add_argument("--offset", default="", help="强制偏移 'x,y'（如 110,122）；不给则按站姿帧推算")
    ap.add_argument("--skip-register", action="store_true",
                    help="跳过配准，直接用当前帧算偏移（当新片构图与 idle 已完全一致时用）")
    ap.add_argument("--apply", action="store_true")
    args = ap.parse_args()

    picks = [int(x) for x in args.picks.split(",") if x.strip()]
    if len(picks) not in (5, 6):
        raise SystemExit("必须给 5 或 6 个帧号（5 个时末帧用 idle1）")

    # 1) 配准：用第一帧（最接近站姿）↔ idle 站姿帧
    a = mask_of(args.keyed / ("f%03d.png" % picks[0]))
    b = mask_of(args.ref_keyed / args.ref_frame)
    if args.skip_register:
        dx, dy, score = 0, 0, float("-1")
        print("跳过配准（构图已与 idle 一致）")
    else:
        dx, dy, score = register(a, b)
        print("配准（源空间）: dx=%+d dy=%+d  IoU=%.4f" % (dx, dy, score))
        if score < 0.80:
            print("警告：配准 IoU 偏低（<0.80），落位可能不可靠", file=sys.stderr)

    aligned = np.roll(np.roll(a, dy, axis=0), dx, axis=1)
    bb = bbox_of(aligned)
    vb = int(np.where(aligned)[0].max())
    scaled_side = int(round(960 * K))
    off_x = int(round(CENTER_X - (bb[0] + bb[2]) / 2.0 * K))
    off_y = int(round(FOOT_LINE - vb * K))
    if args.offset:
        ox, oy = (int(v) for v in args.offset.split(","))
        print("强制偏移: x=%+d y=%+d （覆盖按帧推算的 %+d/%+d）" % (ox, oy, off_x, off_y))
        off_x, off_y = ox, oy
    print("站姿帧 bbox=%s 可见底=%d → 固定偏移 x=%+d y=%+d" % (bb, vb, off_x, off_y))

    staged = TMP / ("stage_" + args.prefix.replace("Enemy_1012_", "") + "_" + time.strftime("%Y%m%d-%H%M%S"))
    staged.mkdir(parents=True, exist_ok=True)
    report = {"keyed": str(args.keyed), "k": K, "offset": {"x": off_x, "y": off_y},
              "registration": {"dx": dx, "dy": dy, "iou": round(score, 4)},
              "picks": picks, "frames": []}

    for i, fr in enumerate(picks, 1):
        im = Image.open(args.keyed / ("f%03d.png" % fr)).convert("RGBA")
        scaled = im.resize((scaled_side, scaled_side), Image.LANCZOS)
        canvas = Image.new("RGBA", (CANVAS, CANVAS), (0, 0, 0, 0))
        canvas.paste(scaled, (off_x, off_y), scaled)
        dst = staged / ("h%02d.png" % i)
        canvas.save(dst)
        m = mask_of(dst)
        b2 = bbox_of(m)
        report["frames"].append({"order": i, "source_frame": "f%03d.png" % fr,
                                  "bbox": b2, "h": b2[3] - b2[1],
                                  "cx": (b2[0] + b2[2]) / 2.0})
        print("  h%02d <- f%03d  bbox=%s  高=%d  中心x=%.1f"
              % (i, fr, b2, b2[3] - b2[1], (b2[0] + b2[2]) / 2.0))

    # 末帧：6 个帧号时取自视频，否则用 idle1
    if len(picks) == 6:
        im = Image.open(args.keyed / ("f%03d.png" % picks[5])).convert("RGBA")
        scaled = im.resize((scaled_side, scaled_side), Image.LANCZOS)
        canvas = Image.new("RGBA", (CANVAS, CANVAS), (0, 0, 0, 0))
        canvas.paste(scaled, (off_x, off_y), scaled)
        shutil.copy2(args.keyed / ("f%03d.png" % picks[5]), staged / "h06.png") if False else None
        canvas.save(staged / "h06.png")
        report["frames"].append({"order": 6, "source_frame": "f%03d.png" % picks[5],
                                  "bbox": bbox_of(mask_of(staged / "h06.png"))})
        print("  h06 <- f%03d（视频帧）" % picks[5])
    else:
        shutil.copy2(args.idle_sprite, staged / "h06.png")
        report["frames"].append({"order": 6, "source_frame": "idle1（工程内精灵直接复制）",
                                  "bbox": bbox_of(mask_of(staged / "h06.png"))})
        print("  h06 <- idle1（直接复制）")

    (staged / "landing_report.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")

    if not args.apply:
        print("\n干跑完成（未写入）。加 --apply 才写入工程。暂存：%s" % staged)
        return 0

    bdir = TMP / ("backup_" + args.prefix.replace("Enemy_1012_", "") + "_" + time.strftime("%Y%m%d-%H%M%S"))
    bdir.mkdir(parents=True, exist_ok=True)
    for i in range(1, 7):
        name = args.prefix + str(i) + ".png"
        target = DEST / name
        if target.exists():
            shutil.copy2(target, bdir / name)
        shutil.copy2(staged / ("h%02d.png" % i), target)
        meta = DEST / (name + ".meta")
        if not meta.exists():
            raise SystemExit("缺少 .meta：%s" % meta)
    print("\n已写入 6 张：%s" % DEST)
    print("备份：%s" % bdir)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
