"""列出全工程接口：被多少个目录使用 + 所在文件（用于判断哪些该下沉到 L0）。"""
import os
import re
from collections import defaultdict

ROOT = "Assets/Scripts"
IFACE_RE = re.compile(
    r"^\s*(?:\[[^\]]*\]\s*)*(?:public\s+|internal\s+)?interface\s+([A-Za-z_][A-Za-z0-9_]*)", re.M)
TYPE_RE = re.compile(
    r"^\s*(?:\[[^\]]*\]\s*)*"
    r"(?:public\s+|internal\s+|private\s+|protected\s+|static\s+|sealed\s+|abstract\s+|partial\s+|readonly\s+|unsafe\s+)*"
    r"(class|struct|interface|enum|record)\s+([A-Za-z_][A-Za-z0-9_]*)", re.M)
STR_RE = re.compile(r'"(?:\\.|[^"\\])*"')

files = []
for dp, dn, fns in os.walk(ROOT):
    for fn in fns:
        if fn.endswith(".cs"):
            files.append(os.path.join(dp, fn).replace("\\", "/"))


def fo(f):
    p = os.path.relpath(f, ROOT).replace("\\", "/").split("/")
    return p[0] if len(p) > 1 else "(root)"


t2f = {}
for f in files:
    for m in TYPE_RE.finditer(open(f, encoding="utf-8", errors="ignore").read()):
        t2f.setdefault(m.group(2), f)

use = defaultdict(set)
for f in files:
    src = open(f, encoding="utf-8", errors="ignore").read()
    src = re.sub(r"//.*", "", src)
    src = re.sub(r"/\*.*?\*/", "", src, flags=re.S)
    src = STR_RE.sub('""', src)
    for n, o in t2f.items():
        if o == f:
            continue
        if re.search(r"\b" + re.escape(n) + r"\b", src):
            use[n].add(fo(f))

rows = []
for f in files:
    src = open(f, encoding="utf-8", errors="ignore").read()
    for m in IFACE_RE.finditer(src):
        rows.append((len(use.get(m.group(1), ())), m.group(1), f, fo(f)))
rows.sort(reverse=True)
print("目录数  接口  所在文件")
for cnt, n, f, d in rows:
    mark = "  [已在 L0]" if d == "Data" else ""
    print(f"{cnt:5d}  {n:36s} {f}{mark}")
