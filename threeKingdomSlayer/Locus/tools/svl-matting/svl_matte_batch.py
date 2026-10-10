"""Batch the adopted Sprite Video Lab matting workflow - no constants to edit.

Recipe: Chroma + auto key colour + tolerance by plate type + softness 16 + halo 1
        + 半透明像素转不透明.   (No Real-ESRGAN smoothing: it repaints detail.)

Usage
-----
    # a root that contains <Action>/raw/*.png  (one output set per action)
    python svl_matte_batch.py --src "<ArtSource>/03_SelectedFrames"

    # a single folder of frames
    python svl_matte_batch.py --src "<some>/raw" --label Attack

Options
-------
    --tolerance auto|N     auto (default) = coloured plate 79 / neutral plate 14
    --neutral-tolerance N  used when auto detects a neutral (grey/white) plate (default 14)
    --coloured-tolerance N used for coloured screens (default 79)
    --softness N           default 16 (do not set 0: it disables the un-mixing step)
    --halo N               default 1
    --keep-semi            do NOT harden semi-transparent pixels (default: harden, i.e. 半透明转不透明)
    --label NAME           output folder suffix when --src is a single folder of frames
    --out-root DIR         write outputs here instead of beside the source
    --sheet                also write a white contact sheet per action
    --report FILE          write a JSON report (default <src or out-root>/svl_matte_batch_report.json)
    --only A,B             restrict to these action folder names / labels
"""
from __future__ import annotations

import argparse
import json
import pathlib

import numpy as np
from PIL import Image

import svl_common as common


def action_dirs(src: pathlib.Path) -> list[tuple[str, pathlib.Path, pathlib.Path]]:
    """Return [(label, frames_dir, output_parent)] for a root-of-actions or a single folder."""
    if (src / "raw").is_dir():
        return [(src.name, src / "raw", src)]
    found = []
    for child in sorted(p for p in src.iterdir() if p.is_dir()):
        if (child / "raw").is_dir():
            found.append((child.name, child / "raw", child))
    if not found:
        if any(src.glob("*.png")):
            return [(src.name, src, src.parent)]
        raise SystemExit("no frames found: %s (expected <Action>/raw/*.png or *.png)" % src)
    return found


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--src", type=pathlib.Path, required=True)
    parser.add_argument("--tolerance", default="auto")
    parser.add_argument("--neutral-tolerance", type=int, default=14)
    parser.add_argument("--coloured-tolerance", type=int, default=79)
    parser.add_argument("--softness", type=int, default=16)
    parser.add_argument("--halo", type=int, default=1)
    parser.add_argument("--keep-semi", action="store_true")
    parser.add_argument("--label")
    parser.add_argument("--out-root", type=pathlib.Path)
    parser.add_argument("--sheet", action="store_true")
    parser.add_argument("--report", type=pathlib.Path)
    parser.add_argument("--only")
    args = parser.parse_args()

    server = common.load_server()
    entries = action_dirs(args.src)
    if args.label and len(entries) == 1:
        entries = [(args.label, entries[0][1], entries[0][2])]
    if args.only:
        wanted = {item.strip() for item in args.only.split(",") if item.strip()}
        entries = [entry for entry in entries if entry[0] in wanted]
    if not entries:
        raise SystemExit("nothing selected")

    report, samples = {}, []
    for label, frames_dir, out_parent in entries:
        frames = sorted(frames_dir.glob("*.png"), key=common.frame_order)
        if not frames:
            continue
        with Image.open(frames[0]) as first:
            plate = common.plate_colour(first)
        coloured = common.is_coloured_plate(plate)
        if args.tolerance == "auto":
            threshold = args.coloured_tolerance if coloured else args.neutral_tolerance
        else:
            threshold = int(args.tolerance)
        parent = args.out_root / out_parent.name if args.out_root else out_parent
        out_dir = parent / ("keyed_ui_t%d" % threshold)
        out_dir.mkdir(parents=True, exist_ok=True)

        rows = []
        for path in frames:
            with Image.open(path) as source:
                source_rgb = np.array(source.convert("RGB"), dtype=np.float32)
                keyed = common.key_frame(server, source.convert("RGBA"), threshold,
                                         softness=args.softness, halo=args.halo,
                                         harden=not args.keep_semi)
            keyed.save(out_dir / path.name, optimize=True, compress_level=9)
            rows.append({"file": path.name, **common.measure(keyed, source_rgb, plate)})

        median = {k: round(float(np.median([r[k] for r in rows])), 3) for k in rows[0] if k != "file"}
        report[label] = {"frames": len(rows), "plate_rgb": [int(v) for v in plate],
                         "plate_type": "coloured" if coloured else "neutral",
                         "threshold": threshold, "softness": args.softness, "halo": args.halo,
                         "harden": not args.keep_semi, "output_dir": str(out_dir),
                         "median": median, "rows": rows}
        print("%-14s n=%-3d %-9s T=%-3d | 可见=%-7.0f 被删=%.1f%% 绿>8=%-5.0f 半透明=%-3.0f 幕布残留=%-4.0f 孔=%-4.0f -> %s" % (
            label, len(rows), report[label]["plate_type"], threshold, median["visible_px"],
            median["deleted_subject_pct"], median["green_gt8"], median["semi_transparent_px"],
            median["plate_leak_px"], median["enclosed_holes_px"], out_dir.name), flush=True)

        best = max(range(len(rows)), key=lambda i: rows[i]["visible_px"])
        samples.append((label, out_dir / rows[best]["file"]))
        if args.sheet:
            print("   sheet:", common.contact_sheet([(label, out_dir / rows[best]["file"])],
                                                    parent / ("svl_%s_overview.png" % label)), flush=True)

    report_path = args.report or ((args.out_root or args.src) / "svl_matte_batch_report.json")
    report_path.parent.mkdir(parents=True, exist_ok=True)
    report_path.write_text(json.dumps(report, ensure_ascii=False, indent=1), encoding="utf-8")
    print("\nreport:", report_path)
    print("(未部署到 Unity - 部署见 svl_deploy_to_unity.py 与文档第 5 章)")


if __name__ == "__main__":
    main()
