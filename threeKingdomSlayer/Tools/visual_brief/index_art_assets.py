# -*- coding: utf-8 -*-
"""美术素材索引器：扫描指定目录，把图片登记进 VisualLibrary/index.csv。

用途
    让 AI 不必再满仓库翻 PNG：所有可视素材在一张表里，查表 → 按路径取图。

行为
    · 幂等：可重复运行。重跑只刷新机器可算的字段（path / sha256 / 宽高 / mtime /
      git_commit / status），**不覆盖**人工填写过的 title / desc / tags（非空即视为人工值）。
    · 只读素材本身，不移动、不改名、不删除任何文件。

用法（工程根目录执行）
    python Tools/visual_brief/index_art_assets.py              # 扫描并合并进 index.csv
    python Tools/visual_brief/index_art_assets.py --check      # 只报告差异，不写文件
    python Tools/visual_brief/index_art_assets.py --force-retag # 连 tags 一起重算（会覆盖人工标签）
    python Tools/visual_brief/index_art_assets.py --root _art_process --root Assets/Sprites

退出码
    0 正常；1 索引里有文件已丢失（--check 时）；2 找不到 index.csv
"""
import argparse
import csv
import hashlib
import os
import re
import subprocess
import sys
from datetime import datetime, timezone

PROJ = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
LIBRARY_DIR = os.path.join(PROJ, "_art_process", "VisualLibrary")
INDEX_CSV = os.path.join(LIBRARY_DIR, "index.csv")

IMAGE_EXT = (".png", ".jpg", ".jpeg", ".webp", ".bmp", ".tga")

COLUMNS = [
    "id", "kind", "role", "title", "path", "tags", "desc",
    "captured_at", "sha256", "width", "height",
    "source_hash", "git_commit", "status",
]

# ── 扫描根与打标签规则 ────────────────────────────────────────────────
# roots: (相对路径, 该根下的默认标签)
DEFAULT_ROOTS = [
    ("_art_process", ["art", "authoring"]),
    ("Assets/Sprites", ["sprite", "in-game"]),
]

# 路径片段 → 附加标签（命中即加，大小写不敏感）
TAG_RULES = [
    ("talent_ui", ["talent", "ui"]),
    ("blades_skill_ref", ["reference", "blades"]),
    ("authoring/archive", ["archive"]),
    ("_void", ["void"]),
    ("authoring", ["authoring"]),
    ("art/environmentv2", ["environment"]),
    ("art/stronghold", ["environment"]),
    ("sprites/enemy", ["enemy"]),
    ("sprites/batlehud", ["hud", "combat"]),
    ("sprites/31reward", ["reward"]),
    ("sprites/effectsprites", ["effect", "vfx"]),
    ("sprites/player", ["player"]),
    ("sprites/ui", ["ui"]),
]

# 这些通用目录名不作为标签（信息量为零）
GENERIC_DIRS = {
    "assets", "previews", "preview", "docs", "scripts", "generated", "icon",
    "icons", "numicon", "_void", "archive", "references", "review", "views",
    "model", "candidates", "output", "src", "temp", "images", "img", "sprites",
}


def sha256_of(path):
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def image_size(path):
    """只读文件头拿宽高，避免为一个元数据引入重量级依赖。"""
    try:
        from PIL import Image
        with Image.open(path) as im:
            return im.size
    except Exception:
        pass
    try:
        import struct
        with open(path, "rb") as f:
            head = f.read(32)
            if head[:8] == b"\x89PNG\r\n\x1a\n":
                w, h = struct.unpack(">II", head[16:24])
                return w, h
    except Exception:
        pass
    return 0, 0


def git_commit():
    try:
        out = subprocess.run(
            ["git", "rev-parse", "--short", "HEAD"],
            cwd=PROJ, capture_output=True, text=True, timeout=10)
        return out.stdout.strip() or ""
    except Exception:
        return ""


def slug(rel_path):
    """把相对路径变成稳定 id：去掉扩展名 → 小写 → 非字母数字换 _ 。"""
    stem = os.path.splitext(rel_path)[0].replace("\\", "/")
    out = []
    for ch in stem:
        out.append(ch.lower() if (ch.isalnum() and ch.isascii()) else "_")
    s = "".join(out)
    while "__" in s:
        s = s.replace("__", "_")
    return "art_" + s.strip("_")


def auto_tags(rel_path):
    low = rel_path.replace("\\", "/").lower()
    tags = set()
    for frag, add in TAG_RULES:
        if frag in low:
            tags.update(add)
    parts = low.split("/")
    for part in parts[:-1]:
        if part in GENERIC_DIRS or part.startswith("_") or not part:
            continue
        if part.isascii() and part.isalnum():
            tags.add(part.lower())
    # 敌人编号：Enemy1021 / enemy_1021 / 1021
    for token in parts:
        for piece in token.replace("-", "_").split("_"):
            if piece.isdigit() and len(piece) == 4:
                tags.add(piece)
    return sorted(tags)


def human_title(rel_path):
    stem = os.path.splitext(os.path.basename(rel_path))[0]
    return stem.replace("_", " ").strip() or stem


# 素材角色：整图预览 / 参考 / 游戏内素材 / 零件件 / 源件 / 作废。
# 选图时整图与参考优先于零件（零件只在需求点名该零件时才靠标题命中顶上来）。
PREVIEW_NAME_RE = re.compile(
    r"(preview|overview|full|compare|contact_sheet|sheet|rebuild|mock|montage|composite|page|panel)",
    re.I)
COMPONENT_NAME_RE = re.compile(r"^[A-Z]\d{2}[_-]")


def auto_role(rel_path):
    low = rel_path.replace("\\", "/").lower()
    name = os.path.basename(rel_path)
    if "_void" in low:
        return "void"
    if "blades_skill_ref" in low:
        return "reference"
    if "/previews/" in low:
        return "preview"
    # assets/ 是零件件目录，优先于文件名启发式
    if "/assets/" in low or COMPONENT_NAME_RE.match(name):
        return "component"
    if PREVIEW_NAME_RE.search(name):
        return "preview"
    if low.startswith("assets/sprites/"):
        return "sprite"
    if "/archive/" in low:
        return "archive"
    return "source"


def iter_images(root_abs):
    for dirpath, dirnames, filenames in os.walk(root_abs):
        dirnames[:] = [d for d in dirnames if not d.endswith(".meta")]
        # 视觉库自己不入库（shots/ 已经是 kind=shot 条目，重复登记会污染索引）
        if os.path.basename(dirpath) == "VisualLibrary":
            dirnames[:] = []
            continue
        for name in sorted(filenames):
            if name.lower().endswith(IMAGE_EXT):
                yield os.path.join(dirpath, name)


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


def main(argv):
    ap = argparse.ArgumentParser(description="把美术素材登记进 VisualLibrary/index.csv")
    ap.add_argument("--root", action="append", default=None,
                    help="扫描根（相对工程根），可重复；默认 %s" % ", ".join(r for r, _ in DEFAULT_ROOTS))
    ap.add_argument("--check", action="store_true", help="只报告差异，不写文件")
    ap.add_argument("--force-retag", action="store_true", help="连 tags 一起重算（覆盖人工标签）")
    args = ap.parse_args(argv)

    roots = [(r, []) for r in args.root] if args.root else list(DEFAULT_ROOTS)

    rows = load_index()
    if not rows and not os.path.exists(INDEX_CSV):
        print("提示：index.csv 不存在，将新建。")

    by_id = {r.get("id", ""): r for r in rows}
    commit = git_commit()
    now = datetime.now(timezone.utc).astimezone().strftime("%Y-%m-%d %H:%M:%S")

    found = {}
    scanned = 0
    for root_rel, _ in roots:
        root_abs = os.path.join(PROJ, root_rel.replace("/", os.sep))
        if not os.path.isdir(root_abs):
            print("跳过（目录不存在）：%s" % root_rel)
            continue
        for abs_path in iter_images(root_abs):
            scanned += 1
            rel = os.path.relpath(abs_path, PROJ).replace("\\", "/")
            found[rel] = abs_path

    added, updated, unchanged = 0, 0, 0
    for rel, abs_path in sorted(found.items()):
        sid = slug(rel)
        mtime = datetime.fromtimestamp(os.path.getmtime(abs_path)).strftime("%Y-%m-%d %H:%M:%S")
        digest = sha256_of(abs_path)
        w, h = image_size(abs_path)
        tags = auto_tags(rel)
        existing = by_id.get(sid)
        if existing is None:
            new = {c: "" for c in COLUMNS}
            new.update({
                "id": sid, "kind": "art", "role": auto_role(rel), "title": human_title(rel),
                "path": rel,
                "tags": ";".join(tags), "desc": "",
                "captured_at": mtime, "sha256": digest, "width": str(w), "height": str(h),
                "source_hash": "", "git_commit": commit, "status": "active",
            })
            rows.append(new)
            by_id[sid] = new
            added += 1
            continue

        changed = False
        for key, val in (("path", rel), ("sha256", digest), ("width", str(w)),
                         ("height", str(h)), ("status", "active"), ("role", auto_role(rel))):
            if existing.get(key, "") != val:
                existing[key] = val
                changed = True
        if not existing.get("title", "").strip():
            existing["title"] = human_title(rel)
            changed = True
        if args.force_retag and existing.get("tags", "") != ";".join(tags):
            existing["tags"] = ";".join(tags)
            changed = True
        elif not existing.get("tags", "").strip():
            existing["tags"] = ";".join(tags)
            changed = True
        if existing.get("kind") != "art":
            existing["kind"] = "art"
            changed = True
        if changed:
            updated += 1
        else:
            unchanged += 1

    # 标记丢失
    missing = 0
    for r in rows:
        if r.get("kind") != "art":
            continue
        rel = r.get("path", "")
        if rel and rel not in found and r.get("status") != "missing":
            r["status"] = "missing"
            missing += 1

    print("扫描 %d 个根 / %d 张图片" % (len(roots), scanned))
    print("新增 %d / 更新 %d / 未变 %d / 标记丢失 %d" % (added, updated, unchanged, missing))

    if args.check:
        stale = [r for r in rows if r.get("status") == "missing"]
        if stale:
            print("索引中有 %d 条文件已不存在：" % len(stale))
            for r in stale[:20]:
                print("  MISSING %s  %s" % (r.get("id"), r.get("path")))
        return 1 if stale else 0

    write_index(rows)
    print("已写入 %s" % os.path.relpath(INDEX_CSV, PROJ))
    print("索引总条目 %d" % len(rows))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
