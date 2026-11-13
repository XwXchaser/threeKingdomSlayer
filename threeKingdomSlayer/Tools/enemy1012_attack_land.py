# -*- coding: utf-8 -*-
"""1012 贼兵 attack 动画：选帧 → 与 idle 对齐 → 落位 → 写入 Unity 精灵（本地，全像素操作）。

口径（与 idle 落地同一套，避免 idle↔attack 体型/站位跳变）
    · 画布       1108×1108
    · 脚底线     897      · 中心 x  521      · 缩放 k = 0.92366（与 idle 一致）
    · 对齐       新片站姿帧 ←→ idle 源片站姿帧 做掩码配准，取整数 (dx, dy)
                  → 新片因此与 idle 处于同一世界站位，之后所有帧用**同一个固定偏移**
                  （刀会伸出画面，但脚不会漂）
    · 写回       只覆盖 PNG 字节，保留 .meta（GUID / pivot 0.5,0.5 / PPU 16 不变）

用法
    python Tools/enemy1012_attack_land.py \
        --keyed <新片抠图目录> --ref-keyed <idle 抠图目录> \
        --picks 4,7,10,13,16,50,54,56,58,60,63,66,69,72,78,84 \
        --apply
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
NAME = "Enemy_1012_attack{}.png"

CANVAS = 1108
FOOT_LINE = 897
CENTER_X = 521
K = 0.9236641221374046          # 与 idle 同一缩放
BACKUP = ROOT / "Library/Locus/tmp"


def mask_of(p: pathlib.Path) -> np.ndarray:
    return np.asarray(Image.open(p).convert("RGBA"))[:, :, 3] > 128


def iou(a: np.ndarray, b: np.ndarray) -> float:
    inter = np.logical_and(a, b).sum()
    union = np.logical_or(a, b).sum()
    return float(inter) / union if union else 0.0


def register(a: np.ndarray, b: np.ndarray, rng: int = 40):
    """找整数 (dx,dy)，把 a 平移后与 b 最像。返回 (dx, dy, score)。"""
    best = (0, 0, -1.0)
    for dy in range(-rng, rng + 1):
        for dx in range(-rng, rng + 1):
            shifted = np.roll(np.roll(a, dy, axis=0), dx, axis=1)
            s = iou(shifted, b)
            if s > best[2]:
                best = (dx, dy, s)
    return best


def bbox_of(m: np.ndarray):
    ys, xs = np.where(m)
    if not len(xs):
        return None
    return int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1


def visible_bottom(m: np.ndarray) -> int:
    ys, _ = np.where(m)
    return int(ys.max())


def main() -> int:
    ap = argparse.ArgumentParser(description="1012 attack 落位与写入")
    ap.add_argument("--keyed", required=True, type=pathlib.Path, help="新片抠图目录")
    ap.add_argument("--ref-keyed", required=True, type=pathlib.Path, help="idle 抠图目录（站姿参考）")
    ap.add_argument("--picks", required=True, help="16 个源帧号，逗号分隔")
    ap.add_argument("--ref-frame", default="f000.png", help="idle 参考帧")
    ap.add_argument("--new-frame", default="f000.png", help="新片站姿帧")
    ap.add_argument("--apply", action="store_true", help="真的写入工程（默认干跑）")
    args = ap.parse_args()

    picks = [int(x) for x in args.picks.split(",") if x.strip() != ""]
    if len(picks) != 16:
        raise SystemExit("必须给 16 个帧号")

    new_dir = args.keyed
    ref_dir = args.ref_keyed

    # 1) 配准：新片站姿帧 ↔ idle 站姿帧
    a = mask_of(new_dir / args.new_frame)
    b = mask_of(ref_dir / args.ref_frame)
    dx, dy, score = register(a, b)
    print("配准（源空间）：dx=%+d dy=%+d  IoU=%.4f" % (dx, dy, score))
    if score < 0.85:
        print("警告：配准 IoU 偏低（<0.85），落位可能不可靠", file=sys.stderr)

    # 2) 用 idle 的规则在「配准后的站姿帧」上算固定画布偏移
    aligned = np.roll(np.roll(a, dy, axis=0), dx, axis=1)
    bb = bbox_of(aligned)
    vb = visible_bottom(aligned)
    scaled_side = int(round(960 * K))
    # 缩放后的 bbox（与 idle 工具一致：以缩放后画布的几何量算）
    # idle 口径：offset_x = 521 - 缩放后中心x；offset_y = 897 - 缩放后可见底行
    scaled_cx = (bb[0] + bb[2]) / 2.0 * K
    scaled_bottom = vb * K
    off_x = int(round(CENTER_X - scaled_cx))
    off_y = int(round(FOOT_LINE - scaled_bottom))
    print("站姿帧 bbox=%s 可见底行=%d → 固定偏移 x=%+d y=%+d" % (bb, vb, off_x, off_y))

    # 3) 逐帧落位
    report = {"keyed": str(new_dir), "ref_keyed": str(ref_dir), "k": K,
              "canvas": CANVAS, "foot_line": FOOT_LINE, "center_x": CENTER_X,
              "registration": {"dx": dx, "dy": dy, "iou": round(score, 4)},
              "offset": {"x": off_x, "y": off_y}, "frames": []}

    staged = BACKUP / ("stage_attack_" + time.strftime("%Y%m%d-%H%M%S"))
    staged.mkdir(parents=True, exist_ok=True)

    for i, fr in enumerate(picks, 1):
        src = new_dir / ("f%03d.png" % fr)
        im = Image.open(src).convert("RGBA")
        w, h = im.size
        scaled = im.resize((scaled_side, scaled_side), Image.LANCZOS)
        canvas = Image.new("RGBA", (CANVAS, CANVAS), (0, 0, 0, 0))
        canvas.paste(scaled, (off_x, off_y), scaled)
        dst = staged / ("s%02d.png" % i)
        canvas.save(dst)
        m = mask_of(dst)
        b2 = bbox_of(m)
        report["frames"].append({
            "order": i, "source_frame": "f%03d.png" % fr,
            "bbox": b2, "visible_bottom": visible_bottom(m),
            "h": (b2[3] - b2[1]) if b2 else None,
            "cx": ((b2[0] + b2[2]) / 2.0) if b2 else None,
        })

    (staged / "landing_report.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")

    print("\n落位结果：")
    for r in report["frames"]:
        print("  s%02d <- %s  bbox=%s  可见底=%s  高=%s  中心x=%s"
              % (r["order"], r["source_frame"], r["bbox"], r["visible_bottom"],
                 r["h"], ("%.1f" % r["cx"]) if r["cx"] else None))

    if not args.apply:
        print("\n干跑完成（未写入）。加 --apply 才写入工程。")
        print("暂存目录: %s" % staged)
        return 0

    # 4) 备份 + 写入（只覆盖 PNG 字节，保留 .meta）
    bdir = BACKUP / ("backup_Enemy1012_attack_" + time.strftime("%Y%m%d-%H%M%S"))
    bdir.mkdir(parents=True, exist_ok=True)
    for i in range(1, 17):
        name = NAME.format(i)
        target = DEST / name
        if target.exists():
            shutil.copy2(target, bdir / name)
        shutil.copy2(staged / ("s%02d.png" % i), target)
        meta = DEST / (name + ".meta")
        if not meta.exists():
            raise SystemExit("缺少 .meta：%s" % meta)
    print("\n已写入 16 张：%s" % DEST)
    print("备份：%s" % bdir)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
