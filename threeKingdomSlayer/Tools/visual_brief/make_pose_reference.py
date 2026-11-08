# -*- coding: utf-8 -*-
"""旧 105 弓手动作帧 → 与新 1051 身份图**同构图**的绿幕姿势参考图。

用途
    射箭动作的姿势（含弓/箭几何、左右手、瞄准方向）只能从工程内旧 105 的三帧动作里继承，
    不能靠文字转述（实测：纯文字路线下模型把瞄准方向改到了画面右侧，验收不通过）。
    本脚本把 `Assets/Sprites/Enemy/Enemy5/enemy5_attack{1,2,3}.png` 处理成
    **与 1051 身份图同一构图口径**的 1024² 绿幕图，作为 gpt-image 的第 2 张参考图：

        画布 1024² / 绿 RGB(0,159,62) / 人物本体高 696 / 可见脚底 y=895 / 身体中心 x=474.5

    这样两张参考图的主体在画面里的位置、尺度完全一致，模型只需换人（形象）与保留姿势，
    不必再去猜构图。

边界
    * 产物是**姿势参考**，不是游戏素材、不是身份图；旧 105 的脸/甲/红头巾**不属于**新形象，
      提示词里必须写明只取姿势与弓箭几何。
    * 不改原图，不覆盖任何既有产物。

用法
    python Tools/visual_brief/make_pose_reference.py \
        --src Assets/Sprites/Enemy/Enemy5/enemy5_attack2.png \
        --out "C:/Users/steam/Pictures/gptGen/enemy1051_pose/old105_attack2_pose_green_1024.png"
"""
from __future__ import annotations

import argparse
import hashlib
import json
import pathlib

import numpy as np
from PIL import Image

GREEN = (0, 159, 62)
CANVAS = 1024
TARGET_BODY_H = 696
TARGET_FOOT_Y = 895
TARGET_CENTER_X = 474.5


def sha256_of(p: pathlib.Path) -> str:
    return hashlib.sha256(p.read_bytes()).hexdigest()


def subject_metrics(rgba: np.ndarray):
    """返回主体 bbox、人物本体高（头顶→脚底）、身体中心列。

    头顶取「身体中心列 ±60px 内的最高像素」——道具（弓）常比头顶高，不能直接用 bbox 顶。
    """
    alpha = rgba[..., 3] > 0
    ys, xs = np.nonzero(alpha)
    x0, x1 = int(xs.min()), int(xs.max())
    foot = int(ys.max())
    # 每列最高像素
    top_row = np.full(rgba.shape[1], -1, dtype=np.int64)
    for x in range(x0, x1 + 1):
        col = np.nonzero(alpha[:, x])[0]
        if len(col):
            top_row[x] = col.min()
    # 身体中心列：脚底往上 40~140px 的横向中点
    lower = alpha[max(0, foot - 140):max(1, foot - 39), :]
    cols = np.nonzero(lower.any(axis=0))[0]
    cx = int(round((cols.min() + cols.max()) / 2.0)) if len(cols) else (x0 + x1) // 2
    cand = top_row[max(0, cx - 60):min(rgba.shape[1], cx + 61)]
    cand = cand[cand >= 0]
    head = int(cand.min()) if len(cand) else int(ys.min())
    return {
        "bbox": [x0, int(ys.min()), x1, foot],
        "head_top_y": head,
        "foot_y": foot,
        "body_height": foot - head + 1,
        "body_center_x": cx,
    }


def main():
    ap = argparse.ArgumentParser(description="旧 105 动作帧 → 与身份图同构图的绿幕姿势参考")
    ap.add_argument("--src", required=True)
    ap.add_argument("--out", required=True)
    ap.add_argument("--json", default="")
    ap.add_argument("--alpha-threshold", type=int, default=0,
                    help="alpha 二值化阈值（0 = 只判 >0）。生图产物是直通 alpha 且最高只有 254，"
                         "建议传 128 去掉 1~3 alpha 的光晕像素")
    args = ap.parse_args()

    src = pathlib.Path(args.src)
    out = pathlib.Path(args.out)
    im = Image.open(src).convert("RGBA")
    if args.alpha_threshold > 0:
        arr0 = np.asarray(im).copy()
        arr0[..., 3] = np.where(arr0[..., 3] >= args.alpha_threshold, 255, 0).astype(np.uint8)
        im = Image.fromarray(arr0, mode="RGBA")
    arr = np.asarray(im).copy()
    m = subject_metrics(arr)

    k = TARGET_BODY_H / float(m["body_height"])
    new_w = max(1, int(round((m["bbox"][2] - m["bbox"][0] + 1) * k)))
    new_h = max(1, int(round((m["bbox"][3] - m["bbox"][1] + 1) * k)))
    small = im.crop((m["bbox"][0], m["bbox"][1], m["bbox"][2] + 1, m["bbox"][3] + 1)).resize(
        (new_w, new_h), Image.LANCZOS)
    # 缩放后再抠一次绿边（源是透明底，这里只做 alpha 二值化）
    sa = np.asarray(small).copy()
    sa[..., 3] = np.where(sa[..., 3] > 0, 255, 0).astype(np.uint8)
    small = Image.fromarray(sa, mode="RGBA")

    canvas = Image.new("RGBA", (CANVAS, CANVAS), GREEN + (255,))
    # 身体中心要用**裁切内局部坐标**（裁切起点是主体 bbox 左边缘），否则整幅会被推到画外
    cx_local = m["body_center_x"] - m["bbox"][0]
    paste_x = int(round(TARGET_CENTER_X - cx_local * k))
    paste_y = TARGET_FOOT_Y - new_h + 1
    canvas.paste(small, (paste_x, paste_y), small)
    out.parent.mkdir(parents=True, exist_ok=True)
    canvas.convert("RGB").save(out)

    # 量测成品
    arr2 = np.asarray(canvas.convert("RGB")).astype(np.float32)
    d = np.linalg.norm(arr2 - np.array(GREEN, dtype=np.float32), axis=2)
    ys, xs = np.nonzero(d > 79)
    clipped = bool(xs.min() <= 0 or ys.min() <= 0 or xs.max() >= CANVAS - 1 or ys.max() >= CANVAS - 1)
    info = {
        "source": {"path": str(src), "size": list(im.size), "sha256": sha256_of(src)},
        "source_metrics": m,
        "target": {"canvas": [CANVAS, CANVAS], "body_h": TARGET_BODY_H, "foot_y": TARGET_FOOT_Y,
                   "center_x": TARGET_CENTER_X, "scale": round(k, 5), "paste_xy": [paste_x, paste_y]},
        "output": {"path": str(out), "sha256": sha256_of(out), "bytes": out.stat().st_size,
                   "subject_bbox": [int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())],
                   "clipped_at_canvas_edge": clipped},
    }
    if clipped:
        raise SystemExit("主体被画布边缘裁掉：%s（贴图位置 %s）" % (info["output"]["subject_bbox"], [paste_x, paste_y]))
    jp = pathlib.Path(args.json) if args.json else out.with_suffix(".json")
    jp.write_text(json.dumps(info, indent=2, ensure_ascii=False), encoding="utf-8")
    print(json.dumps(info, indent=2, ensure_ascii=False))


if __name__ == "__main__":
    main()
