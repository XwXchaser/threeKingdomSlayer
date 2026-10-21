"""依赖实测第三版：类型被多少个目录依赖（找全局枢纽）+ 各目录文件清单。"""
import os
import re
from collections import defaultdict

ROOT = "Assets/Scripts"
TYPE_RE = re.compile(
    r"^\s*(?:\[[^\]]*\]\s*)*"
    r"(?:public\s+|internal\s+|private\s+|protected\s+|static\s+|sealed\s+|abstract\s+|partial\s+|readonly\s+|unsafe\s+)*"
    r"(class|struct|interface|enum|record)\s+([A-Za-z_][A-Za-z0-9_]*)",
    re.M,
)

files = []
for dirpath, dirnames, filenames in os.walk(ROOT):
    dirnames[:] = [d for d in dirnames if not d.startswith(".")]
    for fn in filenames:
        if fn.endswith(".cs"):
            files.append(os.path.join(dirpath, fn).replace("\\", "/"))

type_to_file = {}
for f in files:
    src = open(f, encoding="utf-8", errors="ignore").read()
    for m in TYPE_RE.finditer(src):
        type_to_file.setdefault(m.group(2), f)


def folder_of(f):
    rel = os.path.relpath(f, ROOT).replace("\\", "/")
    parts = rel.split("/")
    return parts[0] if len(parts) > 1 else "(root)"


type_folders = defaultdict(set)
type_files = defaultdict(set)
for f in files:
    src = open(f, encoding="utf-8", errors="ignore").read()
    src = re.sub(r"//.*", "", src)
    src = re.sub(r"/\*.*?\*/", "", src, flags=re.S)
    src = re.sub(r'"(?:\\.|[^"\\])*"', '""', src)
    for n, owner in type_to_file.items():
        if owner == f:
            continue
        if re.search(r"\b" + re.escape(n) + r"\b", src):
            type_folders[n].add(folder_of(f))
            type_files[n].add(f)

print("=== 被最多目录依赖的类型（全局枢纽候选）===")
rank = sorted(type_folders.items(), key=lambda kv: -len(kv[1]))
for n, fs in rank[:45]:
    owner = type_to_file[n]
    kind = "enum/struct" if re.search(r"\b(enum|struct)\s+" + re.escape(n), open(owner, encoding="utf-8", errors="ignore").read()) else "class"
    print(f"  {len(fs):2d} 个目录  {kind:11s} {n:34s} {owner}")

print()
print("=== 各目录文件清单 ===")
for fo in sorted({folder_of(f) for f in files}):
    fs = sorted(f for f in files if folder_of(f) == fo)
    print(f"\n--- {fo} ({len(fs)})")
    for f in fs:
        print(f"      {f.replace(ROOT + '/', '')}")
