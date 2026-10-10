"""Green guarantee: every visible pixel ends with g <= max(r, b).

This is the project's canonical keying step 6, exposed as a reusable post-pass.
Sprite Video Lab cannot do this itself — `alpha_aware_despill_frame` copies alpha>=254
verbatim and `is_background_residue_pixel` returns False for alpha>=255, so a green pixel
that has been hardened to opaque is never touched by any SVL step.

Usage:
  python green_guarantee.py <folder> [--margin 0] [--dry-run] [--backup-dir DIR] [--report FILE]
"""
from __future__ import annotations

import argparse
import json
import pathlib
import shutil

import numpy as np
from PIL import Image

ALPHA_VISIBLE = 5  # 0.02 * 255, same threshold the canonical recipe uses


def gex(array: np.ndarray) -> np.ndarray:
    return array[..., 1] - np.maximum(array[..., 0], array[..., 2])


def apply_green_guarantee(image: Image.Image, margin: int = 0) -> tuple[Image.Image, int, float, float]:
    array = np.array(image.convert("RGBA"), dtype=np.float32)
    visible = array[..., 3] > ALPHA_VISIBLE
    before = float(np.mean(np.maximum(gex(array[..., :3])[visible], 0))) if visible.any() else 0.0
    bad = visible & (gex(array[..., :3]) > 0)
    if bad.any():
        array[..., 1] = np.where(bad, np.maximum(array[..., 0], array[..., 2]) - margin, array[..., 1])
    after = float(np.mean(np.maximum(gex(array[..., :3])[visible], 0))) if visible.any() else 0.0
    fixed = Image.fromarray(np.clip(array, 0, 255).astype(np.uint8), "RGBA")
    return fixed, int(bad.sum()), before, after


def process_folder(folder: pathlib.Path, margin: int, dry_run: bool, backup_dir: pathlib.Path | None,
                   report_path: pathlib.Path | None) -> dict:
    files = sorted(folder.glob("*.png"))
    if not files:
        raise SystemExit("no PNG files in %s" % folder)
    if not dry_run and backup_dir is None:
        raise SystemExit("--backup-dir is required unless --dry-run is used")
    records = []
    changed_total = 0
    for path in files:
        before_hash = None
        with Image.open(path) as image:
            fixed, changed, before_gex, after_gex = apply_green_guarantee(image, margin)
            alpha_before = np.array(image.convert("RGBA"))[..., 3]
        alpha_after = np.array(fixed)[..., 3]
        if not np.array_equal(alpha_before, alpha_after):
            raise SystemExit("alpha changed for %s" % path.name)
        changed_total += changed
        if not dry_run:
            assert backup_dir is not None
            target = backup_dir / path.name
            if not target.exists():
                backup_dir.mkdir(parents=True, exist_ok=True)
                shutil.copy2(path, target)
            temporary = path.with_suffix(".greenfix.tmp.png")
            fixed.save(temporary, optimize=True, compress_level=9)
            temporary.replace(path)
        records.append({"file": path.name, "green_pixels_fixed": changed,
                        "mean_green_excess_before": round(before_gex, 4),
                        "mean_green_excess_after": round(after_gex, 4)})
    result = {"folder": str(folder), "frames": len(files), "dry_run": dry_run, "margin": margin,
              "green_pixels_fixed_total": changed_total, "records": records}
    if report_path is not None:
        report_path.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
    return result


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("folder", type=pathlib.Path)
    parser.add_argument("--margin", type=int, default=0)
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--backup-dir", type=pathlib.Path)
    parser.add_argument("--report", type=pathlib.Path)
    args = parser.parse_args()
    result = process_folder(args.folder, args.margin, args.dry_run, args.backup_dir, args.report)
    print(json.dumps({k: v for k, v in result.items() if k != "records"}, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
