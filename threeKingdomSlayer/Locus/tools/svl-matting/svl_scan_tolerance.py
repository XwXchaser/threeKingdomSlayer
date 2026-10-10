"""Scan the Chroma tolerance for a frame folder / action root and recommend a value.

    python svl_scan_tolerance.py --src "<ArtSource>/03_SelectedFrames"
    python svl_scan_tolerance.py --src "<some frames folder>" --tolerances 6,10,14,20

Recommendation rule: the smallest tolerance whose plate leakage is 0 (and, when possible, whose
green residue is 0) - i.e. the cleanest background that keeps the most character.
"""
from __future__ import annotations

import argparse
import pathlib

import numpy as np
from PIL import Image

import svl_common as common


def targets(src: pathlib.Path, tolerance_list: list[int], softness: int, halo: int, server) -> None:
    folders = []
    if (src / "raw").is_dir():
        folders.append((src.name, src / "raw"))
    else:
        for child in sorted(p for p in src.iterdir() if p.is_dir()):
            if (child / "raw").is_dir():
                folders.append((child.name, child / "raw"))
        if not folders and any(src.glob("*.png")):
            folders.append((src.name, src))
    if not folders:
        raise SystemExit("no frames found under %s" % src)

    for label, frames_dir in folders:
        frames = sorted(frames_dir.glob("*.png"), key=common.frame_order)
        with Image.open(frames[0]) as first:
            plate = common.plate_colour(first)
        coloured = common.is_coloured_plate(plate)
        print("=== %s  plate=(%3d,%3d,%3d) chroma=%.1f  %s ===" % (
            label, plate[0], plate[1], plate[2], float(max(plate) - min(plate)),
            "coloured screen" if coloured else "NEUTRAL plate (needs calibration)"))
        table = {}
        for tolerance in tolerance_list:
            rows = []
            for path in frames:
                with Image.open(path) as source:
                    source_rgb = np.array(source.convert("RGB"), dtype=np.float32)
                    keyed = common.key_frame(server, source.convert("RGBA"), tolerance,
                                             softness=softness, halo=halo)
                rows.append(common.measure(keyed, source_rgb, plate))
            table[tolerance] = {k: float(np.median([r[k] for r in rows]))
                                for k in ("visible_px", "deleted_subject_pct", "plate_leak_px", "green_gt8")}
            print("   T=%-4d 可见=%-8.0f 被删=%.1f%% 幕布残留=%-6.0f 绿>8=%.0f" % (
                tolerance, table[tolerance]["visible_px"], table[tolerance]["deleted_subject_pct"],
                table[tolerance]["plate_leak_px"], table[tolerance]["green_gt8"]), flush=True)

        clean = [t for t in tolerance_list if table[t]["plate_leak_px"] == 0 and table[t]["green_gt8"] == 0]
        no_leak = [t for t in tolerance_list if table[t]["plate_leak_px"] == 0]
        if coloured:
            best = max(tolerance_list)
            print("   → 彩色幕布：按文档默认用 T=%d（对 T 不敏感，取大值多清幕布族）。"
                  "若要更保形可降 T，代价是绿残留上升（看上表两列的反向变化）。" % best)
        elif clean:
            print("   → 中性底：推荐 T=%d（幕布残留 0、绿 0、保形最多）" % min(clean))
        elif no_leak:
            print("   → 中性底：幕布残留可归零的最小 T=%d；若仍丢内容，改用「小 T + 中性幕布清理」" % min(no_leak))
        else:
            print("   → 该容差区间内幕布都没清干净：提高上限重扫，或改用「小 T + 中性幕布清理」")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--src", type=pathlib.Path, required=True)
    parser.add_argument("--tolerances", default="6,10,14,20,30,40,55,70,79")
    parser.add_argument("--softness", type=int, default=16)
    parser.add_argument("--halo", type=int, default=1)
    args = parser.parse_args()
    server = common.load_server()
    values = [int(v) for v in args.tolerances.split(",") if v.strip()]
    targets(args.src, values, args.softness, args.halo, server)


if __name__ == "__main__":
    main()
