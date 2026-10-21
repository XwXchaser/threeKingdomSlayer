"""asmdef 分层前的依赖实测：类型级依赖 → 目录级依赖图。

用法： python Library/Locus/tmp/dep_survey.py
输出： 目录级依赖矩阵（谁依赖谁）、环、跨层候选清单。
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
file_types = {}
editor_files = set()
for f in files:
    src = open(f, encoding="utf-8", errors="ignore").read()
    names = [m.group(2) for m in TYPE_RE.finditer(src)]
    # 去掉嵌套/误匹配：只保留真正声明的（同一文件可能声明多个类型）
    file_types[f] = names
    for n in names:
        type_to_file.setdefault(n, f)
    if "using UnityEditor" in src or "/Editor/" in f or "\\Editor\\" in f:
        editor_files.add(f)


def folder_of(f):
    rel = os.path.relpath(f, ROOT).replace("\\", "/")
    parts = rel.split("/")
    return parts[0] if len(parts) > 1 else "(root)"


# 文件级依赖
file_deps = defaultdict(set)
for f in files:
    src = open(f, encoding="utf-8", errors="ignore").read()
    # 去掉注释与字符串，避免注释里的类型名造成假依赖
    src = re.sub(r"//.*", "", src)
    src = re.sub(r"/\*.*?\*/", "", src, flags=re.S)
    src = re.sub(r'"(?:\\.|[^"\\])*"', '""', src)
    for n, owner in type_to_file.items():
        if owner == f:
            continue
        if re.search(r"\b" + re.escape(n) + r"\b", src):
            file_deps[f].add(owner)

# 目录级依赖
folder_deps = defaultdict(set)
for f, deps in file_deps.items():
    ff = folder_of(f)
    for d in deps:
        fd = folder_of(d)
        if fd != ff:
            folder_deps[ff].add(fd)

folders = sorted({folder_of(f) for f in files})

print("=== 目录级依赖（行=使用方，列=被依赖方）===")
print("(root)".ljust(14), " ".join(x[:11].ljust(12) for x in folders))
for a in folders:
    row = []
    for b in folders:
        row.append(("X" if b in folder_deps[a] else ".").ljust(12))
    print(a.ljust(14), " ".join(row))

print()
print("=== 各目录依赖明细 ===")
for a in folders:
    if folder_deps[a]:
        print(f"{a}:")
        for b in sorted(folder_deps[a]):
            n = sum(1 for f, deps in file_deps.items()
                    if folder_of(f) == a and any(folder_of(d) == b for d in deps))
            print(f"    -> {b}  ({n} 个文件)")

# 找目录级环
print()
print("=== 目录级环（Tarjan 简版 DFS）===")
cycles = set()


def dfs(node, stack, seen):
    for nxt in folder_deps[node]:
        if nxt in stack:
            i = stack.index(nxt)
            cycles.add(tuple(stack[i:] + [nxt]))
        elif nxt not in seen:
            seen.add(nxt)
            dfs(nxt, stack + [nxt], seen)


for f in folders:
    dfs(f, [f], set())
for c in sorted(cycles):
    print("  " + " -> ".join(c))

print()
print("=== 文件统计 ===")
print("脚本总数:", len(files), " 其中 Editor:", len(editor_files))
print("类型总数:", len(type_to_file))
for fo in folders:
    fs = [f for f in files if folder_of(f) == fo]
    ed = sum(1 for f in fs if f in editor_files)
    print(f"  {fo}: {len(fs)} 文件（Editor {ed}）")
