"""Deploy keyed frames into a Unity sprite folder as 1108x1108 sprites (no constants to edit).

    # 1) dry run (default): shows the placement check + what would happen, writes nothing
    python svl_deploy_to_unity.py --src "<ArtSource>/03_SelectedFrames" \
        --dest "<project>/Assets/Sprites/Enemy/Enemy109" \
        --name-template "Enemy_109_{Action}{i}.png"

    # 2) apply
    ... --apply

Behaviour
---------
  * paste each 960x960 keyed frame at (74,120) into a 1108x1108 transparent canvas (--offset)
  * target mode: replace if the file already exists (keeps its .meta -> GUID/pivot/PPU unchanged),
    otherwise create a new asset (Unity will import it; set its importer settings per the manual 5.3)
  * backups of every overwritten file go OUTSIDE Assets (default <project>/Library/Locus/tmp/backup_*)
  * placement check: for existing targets it measures the offset each frame would need
    (target alpha bbox min - frame alpha bbox min). A spread <= ~6px means the (74,120) convention
    holds for this character; a large spread means that set was laid out differently - then either
    use --align-deployed (per-frame bbox alignment) or --foot-line N (bbox bottom -> N).

Nothing here touches Unity's importer settings; do step 2 of the manual 5.3/5.4 in the Editor.
"""
from __future__ import annotations

import argparse
import json
import pathlib
import shutil
import time

import numpy as np
from PIL import Image

import svl_common as common

CANVAS = 1108


def bbox(path: pathlib.Path):
    alpha = np.array(Image.open(path).convert("RGBA"))[..., 3]
    ys, xs = np.nonzero(alpha > 20)
    return None if not len(ys) else (int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max()))


def collect(src: pathlib.Path) -> list[tuple[str, pathlib.Path, pathlib.Path]]:
    """[(action, source_dir, output_parent)] - root of actions first, then a single frame folder."""
    found = []
    for child in sorted(p for p in src.iterdir() if p.is_dir()):
        for directory in sorted(child.glob("keyed_ui_t*")):
            found.append((child.name, directory, child))
    if found:
        return found
    if any(src.glob("*.png")):
        return [(src.parent.name, src, src.parent)]
    raise SystemExit("no keyed frames found under %s (expected <Action>/keyed_ui_t*/ or *.png)" % src)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--src", type=pathlib.Path, required=True)
    parser.add_argument("--dest", type=pathlib.Path, required=True, help="Unity sprite folder")
    parser.add_argument("--name-template", required=True,
                        help='e.g. "Enemy_109_{Action}{i}.png" ({Action} = action label, {i} = ordinal)')
    parser.add_argument("--offset", default="74,120")
    parser.add_argument("--align-deployed", action="store_true",
                        help="align each frame's bbox min to the existing target instead of --offset")
    parser.add_argument("--foot-line", type=int,
                        help="put each frame's alpha bbox bottom on this canvas row (y)")
    parser.add_argument("--apply", action="store_true", help="actually write (default: dry run)")
    parser.add_argument("--backup-root", type=pathlib.Path)
    args = parser.parse_args()

    offset = tuple(int(v) for v in args.offset.split(","))
    sources = collect(args.src)
    stamp = time.strftime("%Y%m%d-%H%M%S")
    default_backup = (args.dest.parents[2] / "Library/Locus/tmp" if "Assets" in args.dest.parts
                      else args.dest.parent)
    backup_root = args.backup_root or (default_backup / ("backup_%s_%s" % (args.dest.name, stamp)))

    records = []
    for action, source_dir, _parent in sources:
        frames = sorted(source_dir.glob("*.png"), key=common.frame_order)
        needs = []
        for ordinal, frame in enumerate(frames, start=1):
            target = args.dest / args.name_template.format(Action=action, i=ordinal,
                                                           name=frame.stem)
            frame_box = bbox(frame)
            target_box = bbox(target) if target.exists() else None
            if target_box and frame_box:
                needs.append((target_box[0] - frame_box[0], target_box[1] - frame_box[1]))
            records.append({"target": str(target), "source": frame.name, "action": action,
                            "ordinal": ordinal, "existing": target_box is not None,
                            "frame_bbox": list(frame_box) if frame_box else None,
                            "target_bbox": list(target_box) if target_box else None})
        if needs:
            xs = [n[0] for n in needs]
            ys = [n[1] for n in needs]
            print("%-14s n=%-3d 既有目标 %d 个：所需偏移 x %d..%d（跨度%d） y %d..%d（跨度%d）%s" % (
                action, len(frames), len(needs), min(xs), max(xs), max(xs) - min(xs),
                min(ys), max(ys), max(ys) - min(ys),
                "" if max(max(xs) - min(xs), max(ys) - min(ys)) <= 6 else "  <== 跨度偏大：考虑 --align-deployed 或 --foot-line"),
                flush=True)
        else:
            print("%-14s n=%-3d （目标不存在，将新建）" % (action, len(frames)), flush=True)

    if not args.apply:
        print("\n[dry run] 未写入任何文件。确认上面的偏移/跨度可接受后加 --apply。")
        print("将写入 %d 个文件到 %s" % (len(records), args.dest))
        print("其中新建 %d，替换 %d" % (sum(1 for r in records if not r["existing"]),
                                       sum(1 for r in records if r["existing"])))
        return

    backup_root.mkdir(parents=True, exist_ok=True)
    args.dest.mkdir(parents=True, exist_ok=True)
    created = replaced = 0
    for record in records:
        source = None
        for action, source_dir, _parent in sources:
            if action == record["action"]:
                source = source_dir / record["source"]
                break
        target = pathlib.Path(record["target"])
        with Image.open(source) as image:
            frame = image.convert("RGBA")
        frame_box = bbox(source)
        if args.align_deployed and record["target_bbox"]:
            placement = (record["target_bbox"][0] - frame_box[0], record["target_bbox"][1] - frame_box[1])
        elif args.foot_line is not None:
            placement = (offset[0], args.foot_line - frame_box[3])
        else:
            placement = offset
        canvas = Image.new("RGBA", (CANVAS, CANVAS), (0, 0, 0, 0))
        canvas.paste(frame, placement, frame)
        if target.exists():
            backup = backup_root / target.name
            if not backup.exists():
                shutil.copy2(target, backup)
            replaced += 1
        else:
            created += 1
        canvas.save(target, optimize=True, compress_level=9)
        record["placement"] = list(placement)
        record["new_bbox"] = list(bbox(target))
    report = {"dest": str(args.dest), "backup_root": str(backup_root), "created": created,
              "replaced": replaced, "offset": list(offset), "align_deployed": args.align_deployed,
              "foot_line": args.foot_line, "records": records}
    (backup_root / "deploy_report.json").write_text(json.dumps(report, ensure_ascii=False, indent=1),
                                                   encoding="utf-8")
    print("\n写入完成：新建 %d，替换 %d" % (created, replaced))
    print("备份 + 报告:", backup_root)
    print("接着做：Unity 里 reimport（改动的）→ 新建的要按手册 5.3 设导入设置 → 手册 5.4 的验证")


if __name__ == "__main__":
    main()
