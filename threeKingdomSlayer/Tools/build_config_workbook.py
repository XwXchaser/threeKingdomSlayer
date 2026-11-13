# -*- coding: utf-8 -*-
"""生成《配置总表》(xlsx) —— 数值 + 文案配表用，供用户直接在表里改。

数据来源：Library/Locus/tmp/config_table_dump.json
  （由 Unity 侧「Tools/三国杀戮/配置总表/① 导出 dump」生成，含每个字段的**原值基线**）

用法：
  1) 在 Unity 里跑一次「① 导出 dump」
  2) python Tools/build_config_workbook.py [输出路径]   → 生成 xlsx（默认 Locus/knowledge/plan/config-tables.xlsx）
  3) 用户在 xlsx 的**黄底列**里直接改（留空 = 不改）
  4) python Tools/apply_config_workbook.py [xlsx路径]  → 生成 edits json
  5) 在 Unity 里跑「③ 应用 Excel 回填」→ 写回资产（走 SerializedProperty）

注意：目标文件被 Excel 占用时会**自动降级**为带日期的同名文件（config-tables-YYYYMMDD.xlsx），
并打印实际路径——回填时要把这个路径传给 apply_config_workbook.py。
"""
import json
import os
import sys
import datetime

from openpyxl import Workbook
from openpyxl.styles import Alignment, Border, Font, PatternFill, Side
from openpyxl.utils import get_column_letter

PROJ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DUMP = os.path.join(PROJ, "Library", "Locus", "tmp", "config_table_dump.json")
OUT = os.path.join(PROJ, "Locus", "knowledge", "plan", "config-tables.xlsx")

# ── 样式（沿用教学字幕总表的配色） ─────────────────────────────────────────
HEAD_FILL = PatternFill("solid", fgColor="3A2A18")
HEAD_FONT = Font(bold=True, color="F3E3C8", size=11)
INPUT_FILL = PatternFill("solid", fgColor="FFF3C4")      # 黄底 = 可改
LOCK_FILL = PatternFill("solid", fgColor="E8E0D0")       # 灰底 = 只读参考
KEY_FILL = PatternFill("solid", fgColor="EDE3D0")
THIN = Side(style="thin", color="B9A98C")
BORDER = Border(left=THIN, right=THIN, top=THIN, bottom=THIN)
WRAP_TOP = Alignment(wrap_text=True, vertical="top")
CENTER = Alignment(horizontal="center", vertical="center", wrap_text=True)

IDENT = ["键", "资产路径", "行标识"]


def g(d, *names, **kw):
    """取字段，兼容大小写写法（Unity JsonUtility 输出 PascalCase，手写 dump 可能用 camelCase）。"""
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

# 列宽：按列名给经验宽度，长文本列给宽一点
WIDTH_BY_NAME = {
    "资产路径": 46,
    "名称": 18,
    "键": 22,
    "主说明模板": 40,
    "补充说明模板": 40,
    "逐级补充说明": 40,
    "原文": 42,
    "新文案": 42,
}
DEFAULT_WIDTH = 13
TEXT_HINT = ("模板", "说明", "文案", "原文", "新文案", "备注")


def col_width(name):
    if name in WIDTH_BY_NAME:
        return WIDTH_BY_NAME[name]
    for hint in TEXT_HINT:
        if hint in name:
            return 34
    return DEFAULT_WIDTH


def main():
    if not os.path.exists(DUMP):
        raise SystemExit("找不到 dump：%s\n先在 Unity 里跑「Tools/三国杀戮/配置总表/① 导出 dump」" % DUMP)
    with open(DUMP, encoding="utf-8") as f:
        dump = json.load(f)

    wb = Workbook()
    wb.remove(wb.active)
    domains = g(dump, "Domains", "domains", default=[])

    # ── 00_说明 ──
    ws = wb.create_sheet("00_说明")
    ws.append(["项", "说明"])
    for k, v in [
        ("生成时间", g(dump, "generatedAt", "generated_at", default="")),
        ("这份表是什么", "Unity 资产（敌人/技能/大招/升级/台词）的数值与文案总表，用来在一屏里横向对比并直接改。"),
        ("怎么改", "只改**黄底列**；灰底列是只读参考。留空的格子视为不改。"),
        ("为什么没有「原值/新值」两列", "导出会落一份原值基线（dump json），回写时按「表里的值 vs 基线」算差异 —— 所见即所得，改了哪格就写哪格。"),
        ("枚举怎么填", "填英文枚举名，例如 Sweep / Stab / Pierce / Slash / Launch / Parry；布尔填 true/false、1/0 或 是/否。"),
        ("数值怎么填", "纯数字，不带单位（单位写在列名里）；小数用英文句点。"),
        ("文案怎么填", "模板里的占位符原样保留（例如 {damage}），删掉占位符会让卡片少显示一段效果。"),
        ("回写流程", "填完 → python Tools/apply_config_workbook.py → Unity「③ 应用 Excel 回填」→ 资产被写回并打印变更报告。"),
        ("回写是安全的吗", "写回走 SerializedProperty（不手改 YAML），GUID / 引用 / 未改字段都不动；改错了重新导出再导出即可。"),
    ]:
        ws.append([k, v])
    for c in range(1, 3):
        ws.cell(row=1, column=c).fill = HEAD_FILL
        ws.cell(row=1, column=c).font = HEAD_FONT
    for r in range(2, ws.max_row + 1):
        ws.cell(row=r, column=1).fill = KEY_FILL
        for c in range(1, 3):
            ws.cell(row=r, column=c).border = BORDER
            ws.cell(row=r, column=c).alignment = WRAP_TOP
    ws.column_dimensions["A"].width = 24
    ws.column_dimensions["B"].width = 96
    ws.freeze_panes = "A2"

    # ── 各域 ──
    for dom in domains:
        sheet = g(dom, "Sheet", "sheet", default="")
        if not sheet or sheet in wb.sheetnames:
            continue
        ws = wb.create_sheet(sheet)
        cols = g(dom, "Columns", "columns", default=[])
        dup = [c for c in cols if c in IDENT]
        if dup:
            raise SystemExit("sheet %s 的列名与身份列冲突：%s（身份列固定为 %s）" % (sheet, dup, IDENT))
        ws.append(IDENT + cols)

        editable = set()
        for row in g(dom, "Rows", "rows", default=[]):
            for c in g(row, "Cells", "cells", default=[]):
                if g(c, "Editable", "editable", default=False):
                    editable.add(g(c, "Column", "column"))

        for row in g(dom, "Rows", "rows", default=[]):
            values = {}
            for c in g(row, "Cells", "cells", default=[]):
                values[g(c, "Column", "column")] = g(c, "Baseline", "baseline", default="")
            ws.append(
                [g(row, "Key", "key"), g(row, "AssetPath", "assetPath", "asset_path"), g(row, "Label", "label")]
                + [values.get(col, "") if values.get(col, "") is not None else "" for col in cols]
            )

        # 表头
        for c in range(1, ws.max_column + 1):
            cell = ws.cell(row=1, column=c)
            cell.fill = HEAD_FILL
            cell.font = HEAD_FONT
            cell.alignment = CENTER
            cell.border = BORDER

        # 单元格样式：身份列灰、只读列灰、可改列黄
        for r in range(2, ws.max_row + 1):
            for c in range(1, ws.max_column + 1):
                cell = ws.cell(row=r, column=c)
                cell.border = BORDER
                cell.alignment = WRAP_TOP
                name = ws.cell(row=1, column=c).value
                if c <= 3:
                    cell.fill = KEY_FILL
                elif name in editable:
                    cell.fill = INPUT_FILL
                else:
                    cell.fill = LOCK_FILL

        widths = {c: col_width(str(ws.cell(row=1, column=c).value)) for c in range(1, ws.max_column + 1)}
        for c, w in widths.items():
            ws.column_dimensions[get_column_letter(c)].width = w
        ws.freeze_panes = ws.cell(row=2, column=4)
        if ws.max_row > 1:
            ws.auto_filter.ref = "A1:%s%d" % (get_column_letter(ws.max_column), ws.max_row)

    out = sys.argv[1] if len(sys.argv) > 1 else OUT
    os.makedirs(os.path.dirname(out), exist_ok=True)
    try:
        wb.save(out)
    except PermissionError:
        # 被 Excel 占用 → 降级成带日期的文件
        alt = os.path.join(os.path.dirname(out), "config-tables-%s.xlsx" % datetime.date.today().strftime("%Y%m%d"))
        wb.save(alt)
        print("!! 目标文件被占用（多半开着 Excel），已改写到：%s" % alt)
        print("   回填时用这个路径：python Tools/apply_config_workbook.py \"%s\"" % alt)
        out = alt

    print("已生成：%s" % out)
    print("sheet：%s" % "、".join(wb.sheetnames))
    for dom in domains:
        print("  %-16s 行 %-4d 列 %-3d  %s" % (
            g(dom, "Sheet", "sheet"), len(g(dom, "Rows", "rows", default=[])),
            len(g(dom, "Columns", "columns", default=[])), g(dom, "Title", "title", default="")))


if __name__ == "__main__":
    main()
