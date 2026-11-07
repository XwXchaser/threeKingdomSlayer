# -*- coding: utf-8 -*-
"""读取《教学字幕总表》(xlsx) 里的「新文案」列 → 生成写回用的 edits json。

**只读 xlsx、不碰任何 Unity 资产** —— 真正写回由 Editor 侧脚本按 edits json 执行
（走 SerializedProperty / AssetDatabase，不手改 YAML）。

用法：
  python Tools/apply_tutorial_subtitle_table.py            # 默认读 Locus/knowledge/plan/tutorial-subtitles-20261107.xlsx
  python Tools/apply_tutorial_subtitle_table.py <xlsx路径>

输出：Library/Locus/tmp/tutorial_subtitle_edits.json
  {
    "dialogues": [ {"path": "...asset", "lineIndex": 0, "text": "新文案"} ],     # DialogueEventData 资产
    "components": [ {"scene": "...scene", "object": "...", "component": "...",
                     "field": "...", "text": "新文案"} ]                          # 场景组件上的字符串字段
  }
"""
import json
import os
import sys

from openpyxl import load_workbook

PROJ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TMP = os.path.join(PROJ, "Library", "Locus", "tmp")
OUT = os.path.join(TMP, "tutorial_subtitle_edits.json")

DEFAULT_XLSX = os.path.join(PROJ, "Locus", "knowledge", "plan", "tutorial-subtitles-20261107.xlsx")

# 组件字段 → (场景, 对象名, 组件类型, 字段名)
COMPONENT_TARGETS = {
    "director.choiceExpLine": ("Assets/Scenes/Battle.scene", "Tutorial Director", "TutorialStageDirector", "choiceExpLine"),
    "director.choicePickLine": ("Assets/Scenes/Battle.scene", "Tutorial Director", "TutorialStageDirector", "choicePickLine"),
    "hubGuide.talentText": ("Assets/Scenes/Battle.scene", "HubTutorialGuide", "HubTutorialGuide", "talentText"),
    "hubGuide.stageText": ("Assets/Scenes/Battle.scene", "HubTutorialGuide", "HubTutorialGuide", "stageText"),
    "levelOrbGuide.guideText": ("Assets/Scenes/Battle.scene", "LevelOrbGuide", "LevelOrbGuide", "guideText"),
    "TalentTreePanel.teachingTextPickNode": ("Assets/Scenes/TalentTree.scene", "TalentTreePanel", "TalentTreePanel", "teachingTextPickNode"),
    "TalentTreePanel.teachingTextConfirm": ("Assets/Scenes/TalentTree.scene", "TalentTreePanel", "TalentTreePanel", "teachingTextConfirm"),
}


def main():
    xlsx = sys.argv[1] if len(sys.argv) > 1 else DEFAULT_XLSX
    wb = load_workbook(xlsx, data_only=True)
    ws = wb["教学字幕总表"]

    # 表头 → 列号
    header = {}
    for c in range(1, ws.max_column + 1):
        header[str(ws.cell(row=1, column=c).value or "").strip()] = c

    col_asset = header.get("台词资产 / 字段")
    col_idx = header.get("行号")
    col_new = header.get("新文案（请填；留空 = 不改）")
    col_old = header.get("原文")
    if not all((col_asset, col_idx, col_new)):
        raise SystemExit("表头不对：缺少 台词资产/行号/新文案 列")

    dialogues = []
    components = []
    for r in range(2, ws.max_row + 1):
        asset = ws.cell(row=r, column=col_asset).value
        if not asset:
            continue
        new = ws.cell(row=r, column=col_new).value
        if new is None:
            continue
        new = str(new).strip()
        if not new:
            continue
        old = str(ws.cell(row=r, column=col_old).value or "")
        if new == old:
            continue
        if asset in COMPONENT_TARGETS:
            scene, obj, comp, field = COMPONENT_TARGETS[asset]
            components.append({"scene": scene, "object": obj, "component": comp,
                               "field": field, "text": new, "old": old, "xlsxRow": r})
        else:
            dialogues.append({"asset": str(asset), "lineIndex": int(ws.cell(row=r, column=col_idx).value) - 1,
                              "text": new, "old": old, "xlsxRow": r})

    os.makedirs(TMP, exist_ok=True)
    with open(OUT, "w", encoding="utf-8") as f:
        json.dump({"dialogues": dialogues, "components": components}, f, ensure_ascii=False, indent=1)

    print("edits:", len(dialogues), "dialogue lines,", len(components), "component fields")
    for d in dialogues:
        print("  [资产]", d["asset"], "行", d["lineIndex"] + 1, "|", d["old"], "→", d["text"])
    for c in components:
        print("  [字段]", c["scene"], c["object"], c["field"], "|", c["old"], "→", c["text"])
    print("wrote", OUT)


if __name__ == "__main__":
    main()
