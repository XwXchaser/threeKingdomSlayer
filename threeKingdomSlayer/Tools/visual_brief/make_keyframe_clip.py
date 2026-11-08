# -*- coding: utf-8 -*-
"""关键帧姿势图 → 一支「关键帧母片」视频（给 seedance 当 reference_video）。

用途
    seedance 2 **不允许**首尾帧与其它参考素材混用（RJ_BAD_REQUEST），所以「把中间关键帧交给模型」
    只能走 reference_video 路线：把若干张**同一个角色**的姿势图按时间排成一支母片，
    模型在这支母片的基础上补出中间运动。

    对 1051 射箭来说，母片 = idle → 搭箭 → 满弓 → 放箭 四张图，
    这样「瞄准方向」「弓的握法」「idle→射箭的衔接」全部由**姿势图**钉死，模型只负责补运动。

用法
    python Tools/visual_brief/make_keyframe_clip.py \
        --clip "C:/Users/steam/Pictures/gptGen/enemy1051_pose/1051_shoot_keyframes_6s.mp4" \
        --item "C:/.../1051_idle_identity_greenscreen_1024.png:1.2" \
        --item "C:/.../1051_pose_nock_greenscreen_1024.png:1.2" \
        --item "C:/.../1051_pose_draw_greenscreen_1024.png:2.0" \
        --item "C:/.../1051_pose_release_greenscreen_1024.png:1.6"
"""
from __future__ import annotations

import argparse
import hashlib
import json
import pathlib

import av
import numpy as np
from PIL import Image

FPS = 24


def sha256_of(p: pathlib.Path) -> str:
    return hashlib.sha256(p.read_bytes()).hexdigest()


def main():
    ap = argparse.ArgumentParser(description="姿势图 → 关键帧母片 mp4")
    ap.add_argument("--clip", required=True, help="输出 mp4 路径")
    ap.add_argument("--item", action="append", required=True,
                    help="格式 <图片路径>:<时长秒>，按时间顺序重复给")
    ap.add_argument("--size", default="1024x1024", help="母片画幅，默认 1024x1024")
    ap.add_argument("--json", default="", help="记录 JSON 输出路径")
    args = ap.parse_args()

    W, H = (int(v) for v in args.size.lower().split("x"))
    items = []
    for it in args.item:
        path, _, dur = it.rpartition(":")
        items.append((pathlib.Path(path), float(dur)))
    for p, _ in items:
        if not p.is_file():
            raise SystemExit("missing keyframe: %s" % p)

    frames = []
    for p, dur in items:
        im = Image.open(p).convert("RGB")
        if im.size != (W, H):
            im = im.resize((W, H), Image.LANCZOS)
        arr = np.asarray(im)
        n = max(1, int(round(dur * FPS)))
        frames.extend([arr] * n)

    out = pathlib.Path(args.clip)
    out.parent.mkdir(parents=True, exist_ok=True)
    container = av.open(str(out), mode="w")
    stream = container.add_stream("h264", rate=FPS)
    stream.width, stream.height = W, H
    stream.pix_fmt = "yuv420p"
    for arr in frames:
        img = av.VideoFrame.from_ndarray(arr, format="rgb24")
        for pkt in stream.encode(img):
            container.mux(pkt)
    for pkt in stream.encode():
        container.mux(pkt)
    container.close()

    rec = {
        "clip": {"path": str(out), "bytes": out.stat().st_size, "sha256": sha256_of(out),
                 "frames": len(frames), "fps": FPS, "size": [W, H],
                 "duration_s": round(len(frames) / float(FPS), 3)},
        "keyframes": [{"path": str(p), "seconds": d, "sha256": sha256_of(p)} for p, d in items],
    }
    jp = pathlib.Path(args.json) if args.json else out.with_suffix(".json")
    jp.write_text(json.dumps(rec, indent=2, ensure_ascii=False), encoding="utf-8")
    print(json.dumps(rec, indent=2, ensure_ascii=False))


if __name__ == "__main__":
    main()
