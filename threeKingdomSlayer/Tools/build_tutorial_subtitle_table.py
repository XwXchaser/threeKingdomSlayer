# -*- coding: utf-8 -*-
"""生成《教学字幕总表》(xlsx) —— 供用户改文案，我再读取部署。

数据来源（**全部从 Unity 实测导出**，不是照抄文档）：
  · 台词资产清单 + 每行原文/说话人：Library/Locus/tmp/tutorial_subtitles_dump.json
      （由 Editor 侧扫 DialogueEventData 资产导出，含每个资产的引用位置）
  · 教学拍结构：Library/Locus/tmp/tutorial_beats_dump.json（Battle.scene 里的 TutorialStageDirector.gestureSteps）
  · 天赋页教学文案：Assets/Scenes/TalentTree.scene 的 TalentTreePanel 序列化字段

用法：
  1) 在 Unity 里跑一次「导出教学字幕」→ 得到 tmp 下的两个 json（见下）
  2) python Tools/build_tutorial_subtitle_table.py   → 生成 xlsx
  3) 用户在 xlsx 的「新文案」列里改
  4) python Tools/apply_tutorial_subtitle_table.py    → 生成 edits json
  5) Editor 侧按 edits json 写回资产（走 SerializedProperty，不手改 YAML）

输出：Locus/knowledge/plan/tutorial-subtitles-20261107.xlsx
"""
import json
import os

from openpyxl import Workbook
from openpyxl.styles import Alignment, Border, Font, PatternFill, Side
from openpyxl.utils import get_column_letter

PROJ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TMP = os.path.join(PROJ, "Library", "Locus", "tmp")
OUT = os.path.join(PROJ, "Locus", "knowledge", "plan", "tutorial-subtitles-20261107.xlsx")

# ── 样式 ────────────────────────────────────────────────────────────────────
HEAD_FILL = PatternFill("solid", fgColor="3A2A18")
HEAD_FONT = Font(bold=True, color="F3E3C8", size=11)
INPUT_FILL = PatternFill("solid", fgColor="FFF3C4")      # 「新文案」列 = 用户填的
LEGACY_FILL = PatternFill("solid", fgColor="E8E0D0")     # 未被引用的历史遗留
THIN = Side(style="thin", color="B9A98C")
BORDER = Border(left=THIN, right=THIN, top=THIN, bottom=THIN)
WRAP_TOP = Alignment(wrap_text=True, vertical="top")
CENTER = Alignment(horizontal="center", vertical="center", wrap_text=True)

COLS = [
    ("序号", 6),
    ("区域", 10),
    ("节点 / 场景", 14),
    ("波次", 10),
    ("位置（哪一步 / 哪一拍）", 26),
    ("触发时机", 22),
    ("台词资产 / 字段", 26),
    ("资产路径 / 所在对象", 40),
    ("行号", 6),
    ("说话人", 9),
    ("原文", 42),
    ("新文案（请填；留空 = 不改）", 42),
    ("备注", 26),
]


def load(name):
    with open(os.path.join(TMP, name), encoding="utf-8") as f:
        return json.load(f)


def talent_texts():
    """从 TalentTree.scene 读天赋页教学文案（YAML 双引号标量，可跨行）。"""
    import re
    p = os.path.join(PROJ, "Assets", "Scenes", "TalentTree.scene")
    s = open(p, encoding="utf-8").read()
    out = {}
    for key in ("teachingTextPickNode", "teachingTextConfirm"):
        i = s.find(key + ":")
        if i < 0:
            out[key] = ""
            continue
        j = s.find('"', i)
        buf, k = [], j + 1
        while True:
            ch = s[k]
            if ch == "\\":
                buf.append(s[k:k + 2])
                k += 2
                continue
            if ch == '"':
                break
            buf.append(ch)
            k += 1
        raw = '"' + "".join(buf) + '"'
        # YAML 折行（真实换行 + 缩进）→ 一个空格
        val = re.sub(r"\n\s*", " ", raw)
        try:
            out[key] = json.loads(val)
        except Exception:
            out[key] = val
    return out


def build():
    dump = load("tutorial_subtitles_dump.json")
    beats = load("tutorial_beats_dump.json")

    usage_by_asset = {}
    for u in dump["usages"]:
        usage_by_asset.setdefault(u["asset"], []).append(u)

    # ── 教学拍结构（给用户看「这一拍在哪儿」）──
    beat_rows = []
    for b in beats:
        node = b["battle"]
        node_name = {"Tutorial_N1_Battle": "N1", "Tutorial_E0_Battle": "E0"}.get(node, node)
        wave = "路线出口（点箭头）" if b["waveIndex"] == -2 else "波次 %d" % (b["waveIndex"] + 1)
        beat_rows.append({
            "node": node_name,
            "wave": wave,
            "kind": b["kind"],
            "hits": b["requiredHits"],
            "note": {
                "Tap": "点一下触发攻击",
                "TapCombo": "连点三下触发三连",
                "DefendButton": "教按住防御键挡下霸体攻击（冻在 C 技起手瞬间）",
                "SkillSlot": "教用技能打断霸体（点技能槽）",
            }.get(b["kind"], ""),
        })

    rows = []

    def add(area, node, wave, place, trigger, asset, path, idx, speaker, text, note=""):
        rows.append({
            "area": area, "node": node, "wave": wave, "place": place, "trigger": trigger,
            "asset": asset, "path": path, "idx": idx, "speaker": speaker, "text": text, "note": note,
        })

    # 1) 有引用位置的台词资产 → 一行一条台词
    for d in dump["dialogues"]:
        usages = usage_by_asset.get(d["name"], [])
        for li, line in enumerate(d["lines"]):
            if usages:
                for u in usages:
                    add(u["area"], u["node"], u["wave"], u["beat"], u["trigger"],
                        d["name"], d["path"], li + 1, line["speaker"], line["text"], u.get("note", ""))
            else:
                add("（未引用）", "-", "-", "历史遗留 · 未在教学链里用", "-",
                    d["name"], d["path"], li + 1, line["speaker"], line["text"],
                    "教学链里没有引用这个资产；要启用得先在拍表/波次台词里挂上")

    # 2) 场景组件上的字符串（不是独立资产）
    inline_texts = {
        "director.choiceExpLine": "击杀敌人获得经验，经验满了就会升级，升级会弹出三选1奖励。",
        "director.choicePickLine": "听我的，选【{0}】——它能把霸体敌人推回去，打断他的招。",
        "hubGuide.talentText": "点这里去解锁新招式",
        "hubGuide.stageText": "点「开始关卡」进入战斗",
        "levelOrbGuide.guideText": "等级球：液面是经验进度，数字是等你选的三选一数量",
    }
    for it in dump["inline"]:
        # 当前文本由导出工具带出（inline 里的 "text"）；上面的字典只作兵底
        text = it.get("text") or inline_texts.get(it["id"], "")
        where = ("Battle.scene / TutorialStageDirector" if it["id"].startswith("director.")
                 else "Battle.scene / HubTutorialGuide" if it["id"].startswith("hubGuide.")
                 else "Battle.scene / LevelOrbGuide")
        add(it["area"], it["node"], "-", it["trigger"], it["trigger"],
            it["id"], where,
            1, "Player", text, "组件上的字符串字段")

    # 3) 天赋页教学文案（TalentTree.scene / TalentTreePanel）
    tt = talent_texts()
    add("天赋页", "天赋面板", "-", "教学第 1 步：指向节点", "天赋页打开（首次）",
        "TalentTreePanel.teachingTextPickNode", "TalentTree.scene / TalentTreePanel", 1, "Player",
        tt.get("teachingTextPickNode", ""), "组件上的字符串字段")
    add("天赋页", "天赋面板", "-", "教学第 2 步：指向解锁按钮", "点中节点后",
        "TalentTreePanel.teachingTextConfirm", "TalentTree.scene / TalentTreePanel", 1, "Player",
        tt.get("teachingTextConfirm", ""), "组件上的字符串字段")

    # ── 排序：区域 → 节点 → 波次 → 位置 ──────────────────────────────────────
    area_order = {"教学关": 0, "正式关": 1, "落地页": 2, "天赋页": 3, "（未引用）": 9}
    node_order = {"N1": 0, "E0": 1, "路线出口": 2, "天赋入口": 3, "开始关卡": 4, "天赋面板": 5,
                  "招式解锁引导": 6, "Tutorial_N1_Battle": 0, "Tutorial_E0_Battle": 1}
    rows.sort(key=lambda r: (
        area_order.get(r["area"], 5),
        node_order.get(r["node"], 5),
        r["wave"],
        r["place"],
        r["asset"],
        r["idx"],
    ))

    wb = Workbook()

    # ── Sheet 1：字幕总表 ────────────────────────────────────────────────────
    ws = wb.active
    ws.title = "教学字幕总表"
    for c, (title, w) in enumerate(COLS, start=1):
        cell = ws.cell(row=1, column=c, value=title)
        cell.fill = HEAD_FILL
        cell.font = HEAD_FONT
        cell.alignment = CENTER
        cell.border = BORDER
        ws.column_dimensions[get_column_letter(c)].width = w
    ws.row_dimensions[1].height = 30

    for i, r in enumerate(rows, start=2):
        values = [i - 1, r["area"], r["node"], r["wave"], r["place"], r["trigger"],
                  r["asset"], r["path"], r["idx"], r["speaker"], r["text"], "", r["note"]]
        legacy = r["area"] == "（未引用）"
        for c, v in enumerate(values, start=1):
            cell = ws.cell(row=i, column=c, value=v)
            cell.border = BORDER
            cell.alignment = CENTER if c in (1, 4, 9, 10) else WRAP_TOP
            if c == 12:
                cell.fill = INPUT_FILL
            elif legacy:
                cell.fill = LEGACY_FILL
    ws.freeze_panes = "A2"
    ws.auto_filter.ref = "A1:%s%d" % (get_column_letter(len(COLS)), len(rows) + 1)

    # ── Sheet 2：教学拍结构 ──────────────────────────────────────────────────
    ws2 = wb.create_sheet("教学拍结构")
    head2 = ["拍", "节点", "波次", "拍类型", "要打几次", "这一拍拍什么"]
    for c, t in enumerate(head2, start=1):
        cell = ws2.cell(row=1, column=c, value=t)
        cell.fill = HEAD_FILL
        cell.font = HEAD_FONT
        cell.alignment = CENTER
        cell.border = BORDER
    for c, w in enumerate([6, 10, 18, 16, 10, 46], start=1):
        ws2.column_dimensions[get_column_letter(c)].width = w
    for i, b in enumerate(beat_rows, start=2):
        for c, v in enumerate([i - 1, b["node"], b["wave"], b["kind"], b["hits"], b["note"]], start=1):
            cell = ws2.cell(row=i, column=c, value=v)
            cell.border = BORDER
            cell.alignment = CENTER if c in (1, 2, 5) else WRAP_TOP
    ws2.freeze_panes = "A2"

    # ── Sheet 3：说明 ────────────────────────────────────────────────────────
    ws3 = wb.create_sheet("说明")
    notes = [
        ("怎么改", "在「教学字幕总表」的「新文案（请填；留空 = 不改）」列里直接写新文案（黄色底的那一列）。"),
        ("留空 = 不改", "没填的行保持现状；只有填了字的行会被写回资产。"),
        ("说话人", "Player = 玩家自述（张飞），Boss = 敌人。教学字幕基本都是 Player。"),
        ("{0} 占位符", "「三选一弹出时（第二句）」那行的 {0} 是技能名占位符，改文案时保留它。"),
        ("行号", "同一个台词资产里有多行时，部署会按行号逐行写回；请不要改行号列和资产名列。"),
        ("未引用（历史遗留）", "灰色底的行 = 资产存在但教学链里没挂上（如 tutorial_t0_wave2 / wave4 / open / skill / death）。改它们不会生效，除非同时把它们接进拍表。"),
        ("写回方式", "台词资产 = DialogueEventData（Assets/ScriptableObjects/Dialogue/）；字符串字段 = 场景组件上的字段（Battle.scene 的 TutorialStageDirector / HubTutorialGuide、TalentTree.scene 的 TalentTreePanel）。"),
        ("部署", "改完你说一声 → 我读取本表 → 用 SerializedProperty 写回资产 → 编译 → Play Mode 复验。"),
    ]
    ws3.column_dimensions["A"].width = 18
    ws3.column_dimensions["B"].width = 110
    for c, t in enumerate(["项", "说明"], start=1):
        cell = ws3.cell(row=1, column=c, value=t)
        cell.fill = HEAD_FILL
        cell.font = HEAD_FONT
        cell.alignment = CENTER
        cell.border = BORDER
    for i, (k, v) in enumerate(notes, start=2):
        ws3.cell(row=i, column=1, value=k).border = BORDER
        ws3.cell(row=i, column=1).alignment = WRAP_TOP
        ws3.cell(row=i, column=2, value=v).border = BORDER
        ws3.cell(row=i, column=2).alignment = WRAP_TOP

    wb.save(OUT)
    print("wrote", OUT)
    print("rows:", len(rows), "beats:", len(beat_rows))


if __name__ == "__main__":
    build()
