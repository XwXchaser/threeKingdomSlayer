# -*- coding: utf-8 -*-
"""1012 贼兵 idle 动画：选帧 → 缩放 → 落位 → 写入 Unity 精灵（本地，全像素操作，零生成成本）。

用法
    # 1) 选帧 + 落位 + 写入（默认干跑，只打印核对表）
    python Tools/enemy1012_idle_land.py --keyed <键帧目录> --apply

    # 2) 只看选帧结果
    python Tools/enemy1012_idle_land.py --keyed <键帧目录> --dry-run

口径（全部实测反推自工程现状，不是猜的）
    · 画布        1108×1108
    · 脚底线      897（六帧必须全同）
    · 中心 x      521
    · 主体高      605
    · 缩放        k = 605 / 源帧主体高  → 缩放边长 ≈ 960×k ≈ 890
    · 偏移        x = 521 - 缩放后中心x ；y = 897 - 缩放后 alpha bbox 底
    · 选帧        6 帧等距覆盖一个呼吸周期；周期取 1.8 s（与 Enemy_1012_Idle.anim 的键距一致）
                 = 43 帧 @24fps；步长 43/6 ≈ 7.17 帧
                 起点取"f(s) 与 f(s+43) 姿态差最小"处 → 保证首尾无缝
    · 写回        只覆盖 PNG 字节，保留 .meta（GUID/导入设置不变）
"""
from __future__ import annotations

import argparse
import hashlib
import json
import pathlib
import shutil
import sys
import time

import numpy as np
from PIL import Image

ROOT = pathlib.Path(r"C:/threeKingdomSlayer/threeKingdomSlayer")
DEST = ROOT / "Assets/Sprites/Enemy/Enemy1012"
NAME = "Enemy_1012_idle{}.png"

CANVAS = 1108
FOOT_LINE = 897
CENTER_X = 521
TARGET_H = 605
CLIP_PERIOD_S = 1.8
FPS = 24
PERIOD = int(round(CLIP_PERIOD_S * FPS))          # 43 帧
COUNT = 6


def frame_order(p: pathlib.Path) -> int:
    digits = "".join(ch for ch in p.stem if ch.isdigit())
    return int(digits) if digits else 0


def load_frames(folder: pathlib.Path):
    files = sorted([p for p in folder.glob("*.png") if frame_order(p) >= 0], key=frame_order)
    frames = []
    for p in files:
        im = Image.open(p).convert("RGBA")
        a = np.asarray(im.split()[-1]) > 10
        if not a.any():
            continue
        ys, xs = np.where(a)
        frames.append({"path": p, "idx": frame_order(p),
                       "bbox": (int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1),
                       "h": int(ys.max() - ys.min() + 1),
                       "w": int(xs.max() - xs.min() + 1),
                       "cx": (int(xs.min()) + int(xs.max())) / 2.0,
                       "alpha": a})
    return frames


def pose_distance(f1, f2) -> float:
    """两帧的轮廓差异（1 - IoU），只看 alpha 形状，按各自在画布中的绝对位置对齐。"""
    x1, y1 = f1["bbox"][0], f1["bbox"][1]
    x2, y2 = f2["bbox"][0], f2["bbox"][1]
    h1, w1 = f1["bbox"][3] - y1, f1["bbox"][2] - x1
    h2, w2 = f2["bbox"][3] - y2, f2["bbox"][2] - x2
    h = max(y1 + h1, y2 + h2)
    w = max(x1 + w1, x2 + w2)
    a1 = np.zeros((h, w), bool)
    a2 = np.zeros((h, w), bool)
    a1[y1:y1 + h1, x1:x1 + w1] = f1["alpha"][y1:y1 + h1, x1:x1 + w1]
    a2[y2:y2 + h2, x2:x2 + w2] = f2["alpha"][y2:y2 + h2, x2:x2 + w2]
    union = (a1 | a2).sum()
    inter = (a1 & a2).sum()
    return 1.0 - inter / union if union else 1.0


def pick_frames(frames):
    """在完整呼吸周期内等距取 COUNT 帧；起点取首尾姿态差最小处。"""
    n = len(frames)
    best = None
    for s in range(0, n - PERIOD):
        # 候选起点：周期内等距 COUNT 帧，且下一周期的同相位帧也在范围内
        picks = [s + round(i * PERIOD / COUNT) for i in range(COUNT)]
        nxt = s + PERIOD
        if nxt >= n:
            continue
        d = pose_distance(frames[s], frames[nxt])
        if best is None or d < best[0]:
            best = (d, s, picks)
    if best is None:                                   # 视频太短，退化为均匀取样
        picks = [round(i * (n - 1) / (COUNT - 1)) for i in range(COUNT)]
        return picks, 1.0
    return best[2], best[0]


def main() -> int:
    ap = argparse.ArgumentParser(description="1012 idle 选帧+落位")
    ap.add_argument("--keyed", required=True, type=pathlib.Path, help="SVL 抠图后的帧目录")
    ap.add_argument("--apply", action="store_true", help="真正写入 Unity（默认干跑）")
    ap.add_argument("--backup-root", type=pathlib.Path, default=None)
    ap.add_argument("--record", type=pathlib.Path, default=None)
    args = ap.parse_args()

    if not args.keyed.is_dir():
        print(f"错误：键帧目录不存在 {args.keyed}", file=sys.stderr)
        return 2
    frames = load_frames(args.keyed)
    if len(frames) < COUNT + PERIOD:
        print(f"错误：帧数不足（{len(frames)}），需要 ≥ {COUNT + PERIOD}", file=sys.stderr)
        return 2

    picks, loop_gap = pick_frames(frames)
    chosen = [frames[i] for i in picks]
    print(f"周期 {PERIOD} 帧（{CLIP_PERIOD_S}s @ {FPS}fps），选帧: "
          + " / ".join(f"f{c['idx']:03d}" for c in chosen) + f"   首尾姿态差 {loop_gap:.4f}")

    h_src = sum(c["h"] for c in chosen) / len(chosen)
    k = TARGET_H / h_src
    side = int(round(frames[0]["bbox"][2] * k)) if False else None
    canvas_side = None
    records = []
    staged = []
    for c in chosen:
        im = Image.open(c["path"]).convert("RGBA")
        s = k
        nw, nh = max(1, int(round(im.width * s))), max(1, int(round(im.height * s)))
        im2 = im.resize((nw, nh), Image.LANCZOS)
        a = np.asarray(im2.split()[-1]) > 10
        ys, xs = np.where(a)
        bx = (int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1)
        canvas_side = nw
        cx = (bx[0] + bx[2]) / 2.0
        off_x = int(round(CENTER_X - cx))
        off_y = FOOT_LINE - (bx[3] - 1)          # PIL bbox 右/下为 exclusive，-1 才是最后一行像素
        out = Image.new("RGBA", (CANVAS, CANVAS), (0, 0, 0, 0))
        out.paste(im2, (off_x, off_y), im2)
        # 可见底行对齐：缩放后图像最底那行可能是透明的，可见底比几何底高 1px
        a2 = np.asarray(out.split()[-1]) > 10
        ys2, xs2 = np.where(a2)
        delta = FOOT_LINE - int(ys2.max())
        if delta:
            off_y += delta
            out = Image.new("RGBA", (CANVAS, CANVAS), (0, 0, 0, 0))
            out.paste(im2, (off_x, off_y), im2)
            a2 = np.asarray(out.split()[-1]) > 10
            ys2, xs2 = np.where(a2)
        staged.append(out)
        records.append({"src_frame": c["path"].name, "src_h": c["h"], "scaled_side": nw,
                        "scaled_bbox": list(bx), "offset": [off_x, off_y],
                        "final_h": int(ys2.max() - ys2.min() + 1),
                        "final_foot": int(ys2.max()),
                        "final_center_x": (int(xs2.min()) + int(xs2.max())) / 2.0})
        print(f"  {c['path'].name}: 源高 {c['h']} → 缩放 {nw}px  偏移 ({off_x},{off_y})  "
              f"落位后 高 {records[-1]['final_h']} 脚底 {records[-1]['final_foot']} 中心 {records[-1]['final_center_x']}")

    foot_bad = [r for r in records if r["final_foot"] != FOOT_LINE]
    center_bad = [r for r in records if abs(r["final_center_x"] - CENTER_X) > 2.0]
    print(f"\n落位核对：脚底全为 {FOOT_LINE} → {'是' if not foot_bad else '否 (%d 帧不符)' % len(foot_bad)}；"
          f"中心在 {CENTER_X}±2px → {'是' if not center_bad else '否 (%d 帧超差)' % len(center_bad)}；"
          f"主体高 {min(r['final_h'] for r in records)}–{max(r['final_h'] for r in records)}（目标 {TARGET_H}）")

    if not args.apply:
        print("[dry run] 未写入任何文件。确认后加 --apply。")
        return 0

    stamp = time.strftime("%Y%m%d-%H%M%S")
    backup = args.backup_root or (ROOT / "Library/Locus/tmp" / f"backup_Enemy1012_idle_{stamp}")
    backup.mkdir(parents=True, exist_ok=True)
    DEST.mkdir(parents=True, exist_ok=True)
    for i, out in enumerate(staged, 1):
        target = DEST / NAME.format(i)
        if target.exists():
            shutil.copy2(target, backup / target.name)
        out.save(target, "PNG", optimize=True)      # 必须保留 alpha：转 RGB 会把透明区填成黑底
    print(f"已写入 {len(staged)} 张到 {DEST}；原图备份在 {backup}")

    if args.record:
        args.record.parent.mkdir(parents=True, exist_ok=True)
        args.record.write_text(json.dumps({
            "keyed": str(args.keyed), "period_frames": PERIOD, "count": COUNT,
            "k": k, "canvas": CANVAS, "foot_line": FOOT_LINE, "center_x": CENTER_X,
            "target_h": TARGET_H, "loop_gap": loop_gap,
            "selected": [c["path"].name for c in chosen], "records": records,
            "written_at": time.strftime("%Y-%m-%d %H:%M:%S"),
        }, ensure_ascii=False, indent=2), encoding="utf-8")
    return 0


if __name__ == "__main__":
    sys.exit(main())
