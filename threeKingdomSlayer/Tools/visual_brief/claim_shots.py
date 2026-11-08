# -*- coding: utf-8 -*-
"""登记一张实机截图进视觉库（shots/ + index.csv）。

这是「库更新」的写入端：阶段 1 的 Unity 采集器拍完图后也走这条逻辑，
所以索引格式只有一处定义，不会出现采集器与索引各写一套。

用法（工程根目录执行）
    # 把外部截图复制进 shots/ 并登记
    python Tools/visual_brief/claim_shots.py --src C:/tmp/shot.png --shot-id battle_hud_idle \
        --title "战斗 HUD 常态" --tags "combat;hud" --scene Assets/Scenes/Battle.scene

    # 只登记已在库内的文件（采集器出图后用这个）
    python Tools/visual_brief/claim_shots.py --path _art_process/VisualLibrary/shots/battle_hud_idle.png \
        --shot-id battle_hud_idle --title "战斗 HUD 常态"

    # 批量认领一个目录
    python Tools/visual_brief/claim_shots.py --src-dir Library/Locus/Screenshots --title-prefix "旧实机"

    # 导入 Unity 采集器的结果（VisualLibraryCapture 产出的 JSON）
    python Tools/visual_brief/claim_shots.py --import Library/Locus/tmp/visual_library_captured.json

退出码
    0 正常；2 参数错误；3 源文件不存在
"""
import argparse
import csv
import hashlib
import os
import shutil
import subprocess
import sys
from datetime import datetime

PROJ = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
LIBRARY_DIR = os.path.join(PROJ, "_art_process", "VisualLibrary")
SHOTS_DIR = os.path.join(LIBRARY_DIR, "shots")
INDEX_CSV = os.path.join(LIBRARY_DIR, "index.csv")

COLUMNS = [
    "id", "kind", "role", "title", "path", "tags", "desc",
    "captured_at", "sha256", "width", "height",
    "source_hash", "git_commit", "status",
]


def sha256_of(path):
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def image_size(path):
    try:
        from PIL import Image
        with Image.open(path) as im:
            return im.size
    except Exception:
        return (0, 0)


def git_commit():
    try:
        out = subprocess.run(["git", "rev-parse", "--short", "HEAD"],
                             cwd=PROJ, capture_output=True, text=True, timeout=10)
        return out.stdout.strip() or ""
    except Exception:
        return ""


def load_index():
    if not os.path.exists(INDEX_CSV):
        return []
    with open(INDEX_CSV, "r", encoding="utf-8-sig", newline="") as f:
        return list(csv.DictReader(f))


def write_index(rows):
    os.makedirs(LIBRARY_DIR, exist_ok=True)
    tmp = INDEX_CSV + ".tmp"
    with open(tmp, "w", encoding="utf-8-sig", newline="") as f:
        w = csv.DictWriter(f, fieldnames=COLUMNS)
        w.writeheader()
        for r in rows:
            w.writerow({c: r.get(c, "") for c in COLUMNS})
    os.replace(tmp, INDEX_CSV)


def register(shot_id, title, tags, desc, rel_path, abs_path, source_hash, scene):
    rows = load_index()
    by_id = {r.get("id", ""): r for r in rows}
    w, h = image_size(abs_path)
    digest = sha256_of(abs_path)
    mtime = datetime.fromtimestamp(os.path.getmtime(abs_path)).strftime("%Y-%m-%d %H:%M:%S")
    if scene and not desc:
        desc = "拍摄场景：%s" % scene

    row = by_id.get(shot_id)
    if row is None:
        row = {c: "" for c in COLUMNS}
        row.update({
            "id": shot_id, "kind": "shot", "role": "ingame",
            "captured_at": mtime, "git_commit": git_commit(), "status": "fresh",
        })
        rows.append(row)
        by_id[shot_id] = row

    row["kind"] = "shot"
    row["role"] = "ingame"
    row["title"] = title or row.get("title", "") or shot_id
    row["path"] = rel_path.replace("\\", "/")
    row["tags"] = tags or row.get("tags", "")
    if desc:
        row["desc"] = desc
    row["captured_at"] = mtime
    row["sha256"] = digest
    row["width"] = str(w)
    row["height"] = str(h)
    row["source_hash"] = source_hash or row.get("source_hash", "")
    row["git_commit"] = git_commit()
    row["status"] = "fresh"
    write_index(rows)
    return w, h


def shot_defs():
    """读镜头声明表：拿到 tags / scene，用于给实机图打标签与算 source_hash。"""
    path = os.path.join(LIBRARY_DIR, "shot_defs.csv")
    if not os.path.exists(path):
        return {}
    import csv as _csv
    out = {}
    with open(path, "r", encoding="utf-8-sig", newline="") as f:
        for r in _csv.DictReader(f):
            sid = (r.get("shot_id") or "").strip()
            if sid:
                out[sid] = r
    return out


def scene_hash(scene_rel):
    """被拍场景的哈希：源资产变了 → 标 stale，提醒重拍。"""
    if not scene_rel:
        return ""
    p = os.path.join(PROJ, scene_rel.replace("/", os.sep))
    if not os.path.isfile(p):
        return ""
    return sha256_of(p)


def import_captures(json_path):
    """导入 Unity 采集器的结果 JSON。"""
    import json
    if not os.path.isfile(json_path):
        print("错误：找不到采集结果文件 %s" % json_path, file=sys.stderr)
        return 3
    with open(json_path, "r", encoding="utf-8-sig") as f:
        data = json.load(f)
    items = data.get("items") if isinstance(data, dict) else data
    if not items:
        print("采集结果里没有 items")
        return 1

    defs = shot_defs()
    ok, failed = 0, 0
    for it in items:
        sid = (it.get("shotId") or "").strip()
        status = (it.get("status") or "").strip()
        rel = (it.get("file") or "").strip()
        if not sid:
            continue
        if status != "ok" or not rel:
            failed += 1
            print("  跳过（采集失败）%s：%s" % (sid, it.get("message", "")))
            continue
        abs_p = os.path.join(PROJ, rel.replace("/", os.sep))
        if not os.path.isfile(abs_p):
            failed += 1
            print("  跳过（文件不存在）%s：%s" % (sid, rel))
            continue
        d = defs.get(sid, {})
        tags = (d.get("tags") or "").strip()
        title = (it.get("title") or d.get("title") or sid).strip()
        desc = d.get("notes") or ""
        scene = (d.get("scene") or "").strip()
        register(sid, title, tags, desc, rel, abs_p, scene_hash(scene), scene)
        print("  已登记 %-28s %sx%s  %s" % (sid, it.get("width", "?"), it.get("height", "?"), rel))
        ok += 1

    print("登记 %d 张，失败 %d 张，索引：%s" % (ok, failed, os.path.relpath(INDEX_CSV, PROJ)))
    return 0


def main(argv):
    ap = argparse.ArgumentParser(description="登记实机截图进视觉库")
    ap.add_argument("--src", help="源图片路径（会复制进 shots/）")
    ap.add_argument("--src-dir", help="批量登记一个目录")
    ap.add_argument("--path", help="直接登记工程内已存在的路径（不复制）")
    ap.add_argument("--shot-id", help="镜头 id（--src-dir 时可省略，按文件名生成）")
    ap.add_argument("--title", default="", help="中文标题")
    ap.add_argument("--title-prefix", default="", help="--src-dir 时的标题前缀")
    ap.add_argument("--tags", default="", help="标签，分号分隔")
    ap.add_argument("--desc", default="", help="说明")
    ap.add_argument("--scene", default="", help="拍摄场景路径")
    ap.add_argument("--source-hash", default="", help="被拍对象的哈希（用于判定过期）")
    ap.add_argument("--no-copy", action="store_true", help="--src 时不复制，只登记原路径")
    ap.add_argument("--import", dest="import_json", default="",
                    help="导入采集器产出的 JSON（items[] 里含 shotId/status/file）")
    args = ap.parse_args(argv)

    if args.import_json:
        return import_captures(args.import_json)

    if not (args.src or args.src_dir or args.path):
        print("错误：需要 --src / --src-dir / --path 之一", file=sys.stderr)
        return 2

    targets = []
    if args.src:
        if not os.path.isfile(args.src):
            print("错误：源文件不存在 %s" % args.src, file=sys.stderr)
            return 3
        targets.append((args.src, args.shot_id or os.path.splitext(os.path.basename(args.src))[0]))
    elif args.src_dir:
        if not os.path.isdir(args.src_dir):
            print("错误：目录不存在 %s" % args.src_dir, file=sys.stderr)
            return 3
        for name in sorted(os.listdir(args.src_dir)):
            if name.lower().endswith((".png", ".jpg", ".jpeg")):
                stem = os.path.splitext(name)[0]
                sid = args.shot_id or "shot_" + stem
                targets.append((os.path.join(args.src_dir, name), sid))
    else:
        abs_p = args.path if os.path.isabs(args.path) else os.path.join(PROJ, args.path)
        if not os.path.isfile(abs_p):
            print("错误：文件不存在 %s" % args.path, file=sys.stderr)
            return 3
        targets.append((abs_p, args.shot_id or os.path.splitext(os.path.basename(abs_p))[0]))

    os.makedirs(SHOTS_DIR, exist_ok=True)
    count = 0
    for src, sid in targets:
        if args.path:
            abs_p = src
            rel = os.path.relpath(abs_p, PROJ).replace("\\", "/")
        elif args.no_copy:
            abs_p = src
            rel = os.path.relpath(abs_p, PROJ).replace("\\", "/")
        else:
            abs_p = os.path.join(SHOTS_DIR, "%s.png" % sid)
            shutil.copyfile(src, abs_p)
            rel = os.path.relpath(abs_p, PROJ).replace("\\", "/")
        title = args.title or (("%s %s" % (args.title_prefix, sid)).strip())
        w, h = register(sid, title, args.tags, args.desc, rel, abs_p, args.source_hash, args.scene)
        print("已登记 %-30s %dx%d  %s" % (sid, w, h, rel))
        count += 1

    print("共登记 %d 张，索引：%s" % (count, os.path.relpath(INDEX_CSV, PROJ)))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
