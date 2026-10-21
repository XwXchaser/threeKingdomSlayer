"""依赖实测第二版：打印跨目录依赖的「文件 → 被引用的类型」明细。

用法： python Library/Locus/tmp/dep_detail.py [目录对...]
不传参数则打印所有跨目录依赖明细。
"""
import os
import re
import sys
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


edge_types = defaultdict(set)   # (使用方目录, 被依赖目录) -> {类型名}
edge_files = defaultdict(set)   # (使用方目录, 被依赖目录) -> {使用文件}

for f in files:
    src = open(f, encoding="utf-8", errors="ignore").read()
    src = re.sub(r"//.*", "", src)
    src = re.sub(r"/\*.*?\*/", "", src, flags=re.S)
    src = re.sub(r'"(?:\\.|[^"\\])*"', '""', src)
    ff = folder_of(f)
    for n, owner in type_to_file.items():
        if owner == f:
            continue
        if re.search(r"\b" + re.escape(n) + r"\b", src):
            key = (ff, folder_of(owner))
            edge_types[key].add(n)
            edge_files[key].add((f, owner))

if len(sys.argv) > 1:
    want = set(sys.argv[1:])
    keys = [k for k in edge_types if k[0] in want or k[1] in want]
else:
    keys = sorted(edge_types, key=lambda k: -len(edge_types[k]))

for key in sorted(keys, key=lambda k: (-len(edge_types[k]))):
    a, b = key
    if len(sys.argv) > 1 and not (a in want or b in want):
        continue
    print(f"\n### {a}  ->  {b}   ({len(edge_types[key])} 个类型 / {len(edge_files[key])} 条文件关系)")
    for t in sorted(edge_types[key]):
        owners = sorted({o for f, o in edge_files[key] if o and type_to_file.get(t) == o})
        print(f"    {t:38s} <- {owners[0] if owners else '?'}")
