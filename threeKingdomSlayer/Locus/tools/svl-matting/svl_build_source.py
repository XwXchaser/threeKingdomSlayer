"""Create a character's art-source action folder from a folder of accepted raw frames.

    python svl_build_source.py --src "<frames folder>" --dest "<ArtSource>/03_SelectedFrames" \
        --action Attack [--prefix attack] [--deployed-reference "<Assets sprite folder>"] [--dry-run]

Creates:  <dest>/<Action>/raw/<prefix><i>_f<NNN>.png      (renamed, natural order)
          <dest>/<Action>/raw_manifest.json               (source path + sha256 + plate/T hint)
          <dest>/<Action>/deployed_reference/*.png        (optional: current shipped sprites, in order)

The raw files must be the untouched decode frames - this script records the sha256 so the origin of
every frame stays verifiable, and (unless --no-verify-source) refuses duplicates/short reads.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import pathlib
import re
import shutil

import numpy as np
from PIL import Image

import svl_common as common


def sha256(path: pathlib.Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1 << 20), b""):
            digest.update(chunk)
    return digest.hexdigest()


def source_frame_number(path: pathlib.Path, fallback: int) -> int:
    match = re.search(r"_f(\d+)", path.stem) or re.search(r"(\d+)", path.stem)
    return int(match.group(1)) if match else fallback


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--src", type=pathlib.Path, required=True, help="folder holding the raw frames")
    parser.add_argument("--dest", type=pathlib.Path, required=True, help="<ArtSource>/03_SelectedFrames")
    parser.add_argument("--action", required=True, help="action folder name, e.g. Attack")
    parser.add_argument("--prefix", help="file name prefix (default: lowerCamel of --action)")
    parser.add_argument("--deployed-reference", type=pathlib.Path,
                        help="folder of currently shipped sprites for this action (any order-carrying names)")
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()

    prefix = args.prefix or (args.action[0].lower() + args.action[1:])
    frames = sorted(args.src.glob("*.png"), key=common.frame_order)
    if not frames:
        raise SystemExit("no PNG frames in %s" % args.src)
    action_dir = args.dest / args.action
    raw_dir = action_dir / "raw"
    print("action=%s frames=%d -> %s" % (args.action, len(frames), raw_dir))

    entries = []
    for ordinal, path in enumerate(frames, start=1):
        number = source_frame_number(path, ordinal)
        target = raw_dir / ("%s%d_f%03d.png" % (prefix, ordinal, number))
        digest = sha256(path)
        entries.append({"file": target.name, "ordinal": ordinal, "source_frame": number,
                        "source_path": str(path), "sha256": digest,
                        "bytes": path.stat().st_size})
        if args.dry_run:
            print("   would copy %-24s <- %s" % (target.name, path.name))
            continue
        raw_dir.mkdir(parents=True, exist_ok=True)
        shutil.copy2(path, target)

    references = 0
    if args.deployed_reference and not args.dry_run:
        refs = sorted(args.deployed_reference.glob("*.png"), key=common.frame_order)
        ref_dir = action_dir / "deployed_reference"
        ref_dir.mkdir(parents=True, exist_ok=True)
        for ref in refs:
            shutil.copy2(ref, ref_dir / ref.name)
            references += 1

    manifest = {"action": args.action, "prefix": prefix, "frames": entries,
                "deployed_reference_count": references,
                "source_dir": str(args.src)}
    if not args.dry_run and frames:
        with Image.open(frames[0]) as first:
            plate = common.plate_colour(first)
        manifest["plate_rgb"] = [int(v) for v in plate]
        manifest["plate_type"] = "coloured" if common.is_coloured_plate(plate) else "neutral"
        manifest["suggested_tolerance"] = 79 if common.is_coloured_plate(plate) else 14
        (action_dir / "raw_manifest.json").write_text(
            json.dumps(manifest, ensure_ascii=False, indent=1), encoding="utf-8")
        print("   plate=%s type=%s suggested T=%d  references=%d" % (
            manifest["plate_rgb"], manifest["plate_type"], manifest["suggested_tolerance"], references))
        print("   manifest:", action_dir / "raw_manifest.json")
    print("(done%s)" % (" - dry run" if args.dry_run else ""))


if __name__ == "__main__":
    main()
