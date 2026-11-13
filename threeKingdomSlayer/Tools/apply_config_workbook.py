# -*- coding: utf-8 -*-
"""读取《配置总表》(xlsx) 的改动 → 生成写回用的 edits json。

**只读 xlsx、不碰任何 Unity 资产** —— 真正写回由 Editor 侧脚本按 edits json 执行
（走 SerializedProperty / AssetDatabase，不手改 YAML）。

差异判定：表里的值 vs **导出时的原值基线**（dump json）。
所以用户直接改值列即可；没动过的格子不会产生 edit。

用法：
  python Tools/apply_config_workbook.py            # 默认读 Locus/knowledge/plan/config-tables.xlsx
  python Tools/apply_config_workbook.py <xlsx路径>

输出：Library/Locus/tmp/config_table_edits.json
  { "edits": [ {"assetPath":..., "component":..., "property":..., "value":..., "rowKey":..., "column":...} ] }
"""
import json
import os
import sys
import glob

from openpyxl import load_workbook

PROJ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TMP = os.path.join(PROJ, "Library", "Locus", "tmp")
DUMP = os.path.join(TMP, "config_table_dump.json")
OUT = os.path.join(TMP, "config_table_edits.json")
DEFAULT_XLSX = os.path.join(PROJ, "Locus", "knowledge", "plan", "config-tables.xlsx")


def default_workbook():
    """默认用 plan 目录里**最新**的 config-tables*.xlsx（目标文件被 Excel 占用时会生成带日期的副本）。"""
    fixed = os.path.join(PROJ, "Locus", "knowledge", "plan", "config-tables*.xlsx")
    files = sorted(glob.glob(fixed), key=lambda p: os.path.getmtime(p))
    return files[-1] if files else DEFAULT_XLSX

IDENT = ["键", "资产路径", "行标识"]


def g(d, *names, **kw):
    """取字段，兼容大小写写法。"""
    default = kw.get("default")
    if not isinstance(d, dict):
        return default
    for n in names:
        if n in d:
            return d[n]
    lowered = {k.lower(): v for k, v in d.items()}
    for n in names:
        if n.lower() in lowered:
            return lowered[n.lower()]
    return default


def norm(v):
    """单元格值 → 规范化字符串（None → ''，去掉首尾空白）。"""
    if v is None:
        return ""
    if isinstance(v, float) and v.is_integer():
        return str(int(v))
    return str(v).strip()


def is_number(s):
    try:
        float(s)
        return True
    except (TypeError, ValueError):
        return False


def main():
    xlsx = sys.argv[1] if len(sys.argv) > 1 else default_workbook()
    if not os.path.exists(xlsx):
        raise SystemExit("找不到工作簿：%s" % xlsx)
    print("使用工作簿：%s" % xlsx)
    if not os.path.exists(DUMP):
        raise SystemExit("找不到 dump：%s\n先在 Unity 里跑「① 导出 dump」" % DUMP)

    with open(DUMP, encoding="utf-8") as f:
        dump = json.load(f)
    wb = load_workbook(xlsx, data_only=True)

    edits = []
    problems = []
    changed_by_sheet = {}

    for dom in g(dump, "Domains", "domains", default=[]):
        sheet = g(dom, "Sheet", "sheet", default="")
        if sheet not in wb.sheetnames:
            problems.append("工作簿缺少 sheet：%s（该域的改动不会被应用）" % sheet)
            continue
        ws = wb[sheet]

        header = {}
        for c in range(1, ws.max_column + 1):
            header[norm(ws.cell(row=1, column=c).value)] = c
        if "键" not in header:
            problems.append("sheet %s 缺少「键」列" % sheet)
            continue

        # 基线表：key → row
        base_rows = {}
        for r in g(dom, "Rows", "rows", default=[]):
            base_rows[g(r, "Key", "key")] = r

        for r in range(2, ws.max_row + 1):
            key = norm(ws.cell(row=r, column=header["键"]).value)
            if not key:
                continue
            base = base_rows.get(key)
            if base is None:
                problems.append("%s：表里有基线里没有的键 %s（可能行被复制过，已跳过）" % (sheet, key))
                continue

            for cell in g(base, "Cells", "cells", default=[]):
                if not g(cell, "Editable", "editable", default=False):
                    continue
                col = g(cell, "Column", "column")
                if col not in header:
                    continue
                new = norm(ws.cell(row=r, column=header[col]).value)
                old = norm(g(cell, "Baseline", "baseline", default=""))
                if new == old:
                    continue
                # 数值列按数值比（避免 "0.40" vs "0.4" 被当成改动）
                if old and new and is_number(old) and is_number(new):
                    if float(old) == float(new):
                        continue
                if old and is_number(old) and not is_number(new):
                    problems.append("%s / %s / %s：原值 %s，新值 %s 不是数字，已跳过"
                                    % (sheet, key, col, old, new))
                    continue
                edits.append({
                    "assetPath": g(base, "AssetPath", "assetPath", "asset_path"),
                    "component": g(cell, "Component", "component", default=""),
                    "property": g(cell, "Property", "property"),
                    "value": new,
                    "rowKey": key,
                    "column": col,
                    "objectPath": g(cell, "ObjectPath", "objectPath", "object_path", default=""),
                })
                changed_by_sheet[sheet] = changed_by_sheet.get(sheet, 0) + 1

    os.makedirs(TMP, exist_ok=True)
    with open(OUT, "w", encoding="utf-8") as f:
        # 键名用 PascalCase：Unity 侧 JsonUtility.FromJson<ConfigTableEdits> 大小写敏感
        json.dump({"Edits": edits}, f, ensure_ascii=False, indent=1)

    print("改动合计：%d 项" % len(edits))
    for sheet, n in sorted(changed_by_sheet.items()):
        print("  %-16s %d 项" % (sheet, n))
    if problems:
        print("\n需要注意（%d 条）：" % len(problems))
        for p in problems:
            print("  ! " + p)
    print("\nedits json：%s" % OUT)
    print("下一步：在 Unity 里跑「Tools/三国杀戮/配置总表/③ 应用 Excel 回填」")


if __name__ == "__main__":
    main()
