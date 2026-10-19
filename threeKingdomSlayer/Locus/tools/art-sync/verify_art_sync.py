#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""素材同步包校验：按各 ArtSource 包里的清单逐条核对 sha256 / 字节数。

用法（在 Unity 项目根目录执行）：
    python Locus/tools/art-sync/verify_art_sync.py            # 校验全部
    python Locus/tools/art-sync/verify_art_sync.py Enemy109ArtSource

清单来源（两种，自动发现）：
    <包>/00_SourceVideos/MANIFEST.csv   列含 file, bytes, sha256
    <包>/00_Index/videos.csv           列含 file, bytes, sha256（视频实际在 <包>/01_SourceVideos/）

退出码：0 全部一致；1 有缺失/不一致；2 找不到清单。
只读脚本，不修改任何文件。
"""
import csv
import hashlib
import os
import sys

PROJECT_ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))


def sha256_of(path, chunk=1 << 20):
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for block in iter(lambda: f.read(chunk), b""):
            h.update(block)
    return h.hexdigest()


def read_manifest(path):
    with open(path, encoding="utf-8-sig", newline="") as f:
        return [r for r in csv.DictReader(f) if r.get("file")]


def resolve(manifest_path, filename):
    """清单里只写文件名；按约定在同一目录或同级 SourceVideos 目录里找。"""
    base = os.path.dirname(manifest_path)
    for cand in (base, os.path.join(base, "..", "01_SourceVideos"), os.path.join(base, "..", "00_SourceVideos")):
        p = os.path.normpath(os.path.join(cand, filename))
        if os.path.exists(p):
            return p
    return None


def collect_manifests(roots):
    found = []
    for root in roots:
        for dirpath, _dirnames, filenames in os.walk(root):
            for name in ("MANIFEST.csv", "videos.csv"):
                if name in filenames:
                    found.append(os.path.join(dirpath, name))
    return sorted(found)


def main(argv):
    roots = argv[1:]
    if roots:
        roots = [os.path.join(PROJECT_ROOT, r) if not os.path.isabs(r) else r for r in roots]
    else:
        roots = [os.path.join(PROJECT_ROOT, d) for d in sorted(os.listdir(PROJECT_ROOT))
                 if d.startswith("Enemy") and d.endswith("ArtSource")]

    manifests = collect_manifests([r for r in roots if os.path.isdir(r)])
    if not manifests:
        print("未找到任何 MANIFEST.csv / videos.csv，检查目录是否正确。")
        return 2

    total_ok = total_bad = 0
    for manifest in manifests:
        print("== %s" % os.path.relpath(manifest, PROJECT_ROOT))
        for row in read_manifest(manifest):
            name = row["file"]
            path = resolve(manifest, name)
            if path is None:
                print("  MISSING  %s" % name)
                total_bad += 1
                continue
            size = os.path.getsize(path)
            expect_bytes = row.get("bytes") or ""
            if expect_bytes.isdigit() and int(expect_bytes) != size:
                print("  SIZE     %s  清单 %s != 实际 %s" % (name, expect_bytes, size))
                total_bad += 1
                continue
            expect_hash = (row.get("sha256") or "").strip().lower()
            if expect_hash:
                actual = sha256_of(path)
                if actual != expect_hash:
                    print("  HASH     %s  清单 %s != 实际 %s" % (name, expect_hash[:16], actual[:16]))
                    total_bad += 1
                    continue
            print("  OK       %s" % name)
            total_ok += 1
        # 目录里多出来的文件（不在清单中）——提示但不判失败
        listed = {row["file"] for row in read_manifest(manifest)}
        folder = os.path.dirname(manifest)
        for entry in sorted(os.listdir(folder)):
            if entry in listed or entry == os.path.basename(manifest) or entry.startswith("."):
                continue
            p = os.path.join(folder, entry)
            if os.path.isfile(p) and os.path.splitext(entry)[1].lower() in ('.mp4', '.mov', '.webm', '.mkv'):
                print("  UNLISTED %s（在目录里但清单没登记）" % entry)

    print("\n结果：%d 条一致，%d 条有问题，共 %d 份清单" % (total_ok, total_bad, len(manifests)))
    return 1 if total_bad else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
