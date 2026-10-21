"""检查指定文件对外部类型的依赖（判断能否独立成程序集）。"""
import re
import os
import sys

ROOT = "Assets/Scripts"
TYPE_RE = re.compile(
    r"^\s*(?:\[[^\]]*\]\s*)*"
    r"(?:public\s+|internal\s+|private\s+|protected\s+|static\s+|sealed\s+|abstract\s+|partial\s+|readonly\s+|unsafe\s+)*"
    r"(class|struct|interface|enum|record)\s+([A-Za-z_][A-Za-z0-9_]*)",
    re.M,
)
STR_RE = re.compile(r'"(?:\\.|[^"\\])*"')

t2f = {}
for dp, dn, fns in os.walk(ROOT):
    for fn in fns:
        if fn.endswith(".cs"):
            p = os.path.join(dp, fn).replace("\\", "/")
            for m in TYPE_RE.finditer(open(p, encoding="utf-8", errors="ignore").read()):
                t2f.setdefault(m.group(2), p)


def deps(f):
    src = open(f, encoding="utf-8", errors="ignore").read()
    src = re.sub(r"//.*", "", src)
    src = re.sub(r"/\*.*?\*/", "", src, flags=re.S)
    src = STR_RE.sub('""', src)
    out = set()
    for n, o in t2f.items():
        if o == f:
            continue
        if re.search(r"\b" + re.escape(n) + r"\b", src):
            out.add((n, o))
    return sorted(out)


for f in sys.argv[1:]:
    print("\n### " + f)
    for n, o in deps(f):
        tag = "[Data]" if o.startswith(ROOT + "/Data/") else "[!!]"
        print(f"    {n:34s} <- {o}  {tag}")
