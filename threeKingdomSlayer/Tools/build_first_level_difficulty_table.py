# -*- coding: utf-8 -*-
"""生成《长坂坡·卷轴》第一关难度映射表（xlsx）。

数据来源（全部为实测，不照抄文档）：
  · 节点/波次现状：Assets/RouteData/ChangbanpoScroll/{Nodes,Battles}/*.asset
  · 敌人能力：Assets/Resources/EnemyPrefabs/Enemy_*.prefab
  · 经验曲线：Assets/ScriptableObjects/ExpCurve/ExpCurve_30min.asset（hex 数组）
  · 数值铁律：Locus/knowledge/design/numerical-balance-draft.md §0.5

**2026-10-25 更新**：波次方案已落进资产（RECOMMEND == 现状 CURRENT），
  · 105 弓兵血量 8 → 40（远程威胁才成立）
  · N1 5 波 / E0 5 波（1021 第0波、109 第1波）/ N2 6 波 / N3 6 波（倍率重算）
  · 104 Boss：N2 ×7.5、N3 ×11.5（两条支路都放）
  → 下面的 RECOMMEND 就是**资产里的实际配置**，改配置时两边一起改。

本脚本只产出表格，不改任何游戏资产。
"""
import os, re, struct
from openpyxl import Workbook
from openpyxl.styles import Font, PatternFill, Alignment, Border, Side
from openpyxl.utils import get_column_letter

PROJ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))   # Unity 工程根
OUT = os.path.join(PROJ, "Locus", "knowledge", "plan", "first-level-difficulty-20261025-v2.xlsx")

# ── 经验曲线（实测 hex 数组） ──────────────────────────────────────────────
def load_curve():
    p = os.path.join(PROJ, "Assets/ScriptableObjects/ExpCurve/ExpCurve_30min.asset")
    txt = open(p, encoding="utf-8", errors="ignore").read()
    m = re.search(r"expRequiredPerLevel:\s*([0-9a-fA-F]+)", txt)
    h = m.group(1)
    return [struct.unpack("<i", bytes.fromhex(h[i * 8:(i + 1) * 8]))[0] for i in range(len(h) // 8)]

CURVE = load_curve()

def level_for_xp(xp):
    lv, need = 1, xp
    for c in CURVE:
        if need < c:
            break
        need -= c
        lv += 1
    return lv, need

def xp_to_lv40():
    return sum(CURVE[:39])

# ── 玩家 DPS 模型（**2026-10-25 用机器人实测重标**） ─────────────────────────
# 旧模型 33+1.9×等级（纯假设）→ 实测机器人平A的**有效 DPS** 只有 50~72，且几乎不随等级线性涨
# （升级是随机三选一，连招节奏比等级更决定输出）→ 换成实测拟合的平坦曲线。
BASE_DPS, DPS_PER_LV, DPS_CAP_LV = 58.0, 0.4, 32
def dps_at(lv):
    return round(BASE_DPS + DPS_PER_LV * min(lv, DPS_CAP_LV), 1)

# ── 敌人实测能力 ──────────────────────────────────────────────────────────
ENEMY = {
 "1011": dict(name="剑兵", hp=200, atk=3,  interval=2.0, rng=1, exp=80,  ranged=False, role="杂兵/经验来源"),
 "1021": dict(name="盾兵", hp=150, atk=10, interval=5.0, rng=1, exp=50,  ranged=False, role="肉盾/破盾教学"),
 "109":  dict(name="骑兵", hp=500, atk=10, interval=2.0, rng=1, exp=100, ranged=False, role="冲锋威胁"),
 "105":  dict(name="弓兵", hp=200, atk=5,  interval=3.3, rng=3, exp=50,  ranged=True,  role="后排火力"),
 "104":  dict(name="Boss", hp=500, atk=10, interval=5.0, rng=2, exp=400, ranged=False, role="关底Boss"),
}
def is_boss(eid): return ENEMY[eid]["role"] == "关底Boss"

# ── 现状（= 2026-10-25 已落进资产的配置） ────────────────────────────────
CURRENT = [
 ("N1·当阳乱军", "Formal_N1_Battle", 0, {0: ["1011","1011"]}, 2.3, 0.85, 0.45, 0),
 ("N1·当阳乱军", "Formal_N1_Battle", 1, {0: ["1011","1011","1011"]}, 1.6, 0.85, 0.45, 0),
 ("N1·当阳乱军", "Formal_N1_Battle", 2, {0: ["1011"], 2: ["1011","1011"]}, 1.8, 0.9, 0.5, 0),
 ("N1·当阳乱军", "Formal_N1_Battle", 3, {0: ["1011","1011"], 2: ["1011","1011"], 4: ["1011"]}, 1.3, 0.9, 0.5, 0),
 ("N1·当阳乱军", "Formal_N1_Battle", 4, {0: ["1011","1011"], 1: ["1011","1011"], 2: ["1011"]}, 1.5, 0.9, 0.5, 0),
 ("E0·残门垣前", "Formal_E0_Battle", 0, {0: ["1021"], 2: ["1011","1011"]}, 2.5, 0.85, 0.5, 0),
 ("E0·残门垣前", "Formal_E0_Battle", 1, {0: ["109"], 2: ["1011","1011"]}, 1.8, 0.9, 0.55, 0),
 ("E0·残门垣前", "Formal_E0_Battle", 2, {0: ["1011","1011"], 2: ["1011"]}, 2.7, 0.9, 0.55, 0),
 ("E0·残门垣前", "Formal_E0_Battle", 3, {0: ["1021"], 2: ["1011","1011"]}, 3.2, 0.9, 0.55, 0),
 ("E0·残门垣前", "Formal_E0_Battle", 4, {0: ["1021","1021"], 2: ["1011","1011"], 4: ["1011"]}, 2.2, 0.95, 0.6, 0),
 ("J1·城郊分叉", "（无战斗）", 0, {}, 0, 0, 0, 0),
 ("EV·村落外围", "（无战斗）", 0, {}, 0, 0, 0, 0),
 ("N2·官道断后", "Formal_N2_Battle", 0, {0: ["1011","1011"], 1: ["1011"], 3: ["105"]}, 2.9, 0.95, 0.6, 0),
 ("N2·官道断后", "Formal_N2_Battle", 1, {0: ["1011","1011"], 2: ["105","105"]}, 4.2, 0.95, 0.6, 0),
 ("N2·官道断后", "Formal_N2_Battle", 2, {0: ["1011","1011"], 1: ["1011","1021"]}, 3.0, 0.95, 0.6, 0),
 ("N2·官道断后", "Formal_N2_Battle", 3, {0: ["1021"], 1: ["1011","1011"], 2: ["105","105"]}, 3.8, 0.95, 0.65, 0),
 ("N2·官道断后", "Formal_N2_Battle", 4, {0: ["109"], 1: ["1011","1011"], 2: ["1011"]}, 2.4, 1.0, 0.65, 0),
 ("N2·官道断后", "Formal_N2_Battle", 5, {0: ["1011","1011"], 2: ["1011"], 4: ["104"]}, 1.4, 1.0, 0.65, 7.5),
 ("N3·村落搜救", "Formal_N3_Battle", 0, {0: ["1011","1011"], 2: ["1011"]}, 4.1, 1.0, 0.65, 0),
 ("N3·村落搜救", "Formal_N3_Battle", 1, {0: ["1011","1011"], 2: ["105"]}, 6.1, 1.0, 0.65, 0),
 ("N3·村落搜救", "Formal_N3_Battle", 2, {0: ["1011","1011"], 1: ["1011","1021"]}, 3.8, 1.0, 0.65, 0),
 ("N3·村落搜救", "Formal_N3_Battle", 3, {0: ["1011","1011"], 1: ["109"], 2: ["105","105"], 4: ["105"]}, 3.0, 1.0, 0.65, 0),
 ("N3·村落搜救", "Formal_N3_Battle", 4, {0: ["1021"], 1: ["1011","1011"], 3: ["1011"]}, 4.5, 1.0, 0.7, 0),
 ("N3·村落搜救", "Formal_N3_Battle", 5, {0: ["1021","1011"], 1: ["1011","1011"], 4: ["104"]}, 1.1, 1.0, 0.7, 11.5),
]

# ── 建议配置（= 已落进资产的配置，改配置时两边一起改） ─────────────────────
RECOMMEND = {
 "N1·当阳乱军": [
   dict(rows={0: ["1011","1011"]}, sec=4, spd=0.85, dmg=0.45, note="第1波：两只并排，认识「一列 = 几个敌人」"),
   dict(rows={0: ["1011","1011","1011"]}, sec=6, spd=0.85, dmg=0.45, note="第2波：三只一排，学会用范围技（斩击打整排）"),
   dict(rows={0: ["1011"], 2: ["1011","1011"]}, sec=7, spd=0.9, dmg=0.5, note="第3波：前后两排，要换目标"),
   dict(rows={0: ["1011","1011"], 2: ["1011","1011"], 4: ["1011"]}, sec=9, spd=0.9, dmg=0.5, note="第4波：三排纵深，每排 1~2 只"),
   dict(rows={0: ["1011","1011"], 1: ["1011","1011"], 2: ["1011"]}, sec=9, spd=0.9, dmg=0.5, note="第5波：N1 收尾，排距收紧"),
 ],
 "E0·残门垣前": [
   dict(rows={0: ["1021"], 2: ["1011","1011"]}, sec=8, spd=0.85, dmg=0.5, note="**必须第0波**：对教学台词 tutorial_t0_wave3（蓄力上挑破盾）"),
   dict(rows={0: ["109"], 2: ["1011","1011"]}, sec=10, spd=0.9, dmg=0.55, note="**必须第1波**：对教学台词 tutorial_t0_wave4（骑将登场）"),
   dict(rows={0: ["1011","1011"], 2: ["1011"]}, sec=7, spd=0.9, dmg=0.55, note="第3波：纯 1011，让玩家缓口气"),
   dict(rows={0: ["1021"], 2: ["1011","1011"]}, sec=7, spd=0.9, dmg=0.55, note="第4波：第二只盾兵"),
   dict(rows={0: ["1021","1021"], 2: ["1011","1011"], 4: ["1011"]}, sec=11, spd=0.95, dmg=0.6, note="第5波：双盾压阵，E0 收尾"),
 ],
 "J1·城郊分叉": [dict(rows={}, sec=0, spd=0, dmg=0, note="分叉节点，不打（现状正确）")],
 "EV·村落外围": [dict(rows={}, sec=0, spd=0, dmg=0, note="村落外围不打（现状正确）；要加伏击就 1 波 1011×3 + 105×1")],
 "N2·官道断后": [
   dict(rows={0: ["1011","1011"], 1: ["1011"], 3: ["105"]}, sec=7, spd=0.95, dmg=0.6, note="首次出现远程：105 放最后排（射程3），前排 1011 当掩体"),
   dict(rows={0: ["1011","1011"], 2: ["105","105"]}, sec=7, spd=0.95, dmg=0.6, note="两只 105 同排，逼范围技或突进"),
   dict(rows={0: ["1011","1011"], 1: ["1011","1021"]}, sec=7, spd=0.95, dmg=0.6, note="盾兵正式登场（前面已教过破盾）"),
   dict(rows={0: ["1021"], 1: ["1011","1011"], 2: ["105","105"]}, sec=9, spd=0.95, dmg=0.65, note="**盾兵掩护弓兵**：1021 在 row0 吃伤害，105 在 row2 输出"),
   dict(rows={0: ["109"], 1: ["1011","1011"], 2: ["1011"]}, sec=10, spd=1.0, dmg=0.65, note="骑兵冲锋高潮波"),
   dict(rows={0: ["1011","1011"], 2: ["1011"], 4: ["104"]}, sec=75, spd=1.0, dmg=0.65, boss=7.5, note="**Boss 波：104 必须在最后一排**（铁律九）"),
 ],
 "N3·村落搜救": [
   dict(rows={0: ["1011","1011"], 2: ["1011"]}, sec=8, spd=1.0, dmg=0.65, note="巷战开局：数量低但节奏快"),
   dict(rows={0: ["1011","1011"], 2: ["105"]}, sec=7, spd=1.0, dmg=0.65, note="远程压制"),
   dict(rows={0: ["1011","1011"], 1: ["1011","1021"]}, sec=12, spd=1.0, dmg=0.65, note="盾兵压阵"),
   dict(rows={0: ["1011","1011"], 1: ["109"], 2: ["105","105"], 4: ["105"]}, sec=21, spd=1.0, dmg=0.65, note="全机制同场：骑兵 + 三只弓兵"),
   dict(rows={0: ["1021"], 1: ["1011","1011"], 3: ["1011"]}, sec=12, spd=1.0, dmg=0.7, note="盾兵护住后排"),
   dict(rows={0: ["1021","1011"], 1: ["1011","1011"], 4: ["104"]}, sec=75, spd=1.0, dmg=0.7, boss=9.0, note="**终局 Boss**：104 最后一排；盾兵护 Boss"),
 ],
}
NODE_ROLE = {
 "N1·当阳乱军": "教学后第一战｜只上 1011，建立手感与「列/排」概念",
 "E0·残门垣前": "机制战｜盾兵（破盾）+ 骑兵（冲锋），与教学台词逐波对应",
 "J1·城郊分叉": "纯过场/分叉｜不打（**2026-10-25 已接上右路出口 j1_to_n2**）",
 "EV·村落外围": "叙事/氛围｜不打（可选小伏击）",
 "N2·官道断后": "右路终点｜引入远程 105 + Boss 104（弱化版）",
 "N3·村落搜救": "左路终局｜三段收束 + 终局 104",
}
# ── 已落进资产的 hp×（机器人实测反推后的值，表格里与推导值并列） ────────────
APPLIED = {
 "N1·当阳乱军": [0.50, 0.50, 0.52, 0.52, 0.54],
 "E0·残门垣前": [0.55, 0.55, 0.56, 0.57, 0.58],
 "J1·城郊分叉": [0],
 "EV·村落外围": [0],
 "N2·官道断后": [0.58, 0.58, 0.59, 0.59, 0.60, 0.60],
 "N3·村落搜救": [0.60, 0.60, 0.61, 0.61, 0.62, 0.62],
}
# ── 机器人实测清场秒（平A 基线，2026-10-25 四次跑；同一波不同次 ±30%：随机三选一 → DPS 浮动） ──
MEASURED = {
 ("N1·当阳乱军", 0): 3.8, ("N1·当阳乱军", 1): 5.8, ("N1·当阳乱军", 2): 6.5,
 ("N1·当阳乱军", 3): 9.4, ("N1·当阳乱军", 4): 9.1,
 ("E0·残门垣前", 0): 7.6, ("E0·残门垣前", 1): 9.7, ("E0·残门垣前", 2): 7.3,
 ("E0·残门垣前", 3): 6.8, ("E0·残门垣前", 4): 11.0,
 ("N3·村落搜救", 0): 7.7, ("N3·村落搜救", 1): 7.2, ("N3·村落搜救", 2): 11.8,
 ("N3·村落搜救", 3): 21.0, ("N3·村落搜救", 4): 11.7,
}
NODE_TIME = {"N1·当阳乱军": 5, "E0·残门垣前": 6, "J1·城郊分叉": 1, "EV·村落外围": 2, "N2·官道断后": 7, "N3·村落搜救": 8}
NODE_ORDER = ["N1·当阳乱军", "E0·残门垣前", "J1·城郊分叉", "EV·村落外围", "N2·官道断后", "N3·村落搜救"]

# ── 逐波计算 ─────────────────────────────────────────────────────────────
def base_hp_of(rows, boss_mult=0.0):
    total = 0.0
    for ids in rows.values():
        for e in ids:
            total += ENEMY[e]["hp"] * (boss_mult if (is_boss(e) and boss_mult) else 1.0)
    return total

def exp_of_rows(rows):
    return sum(ENEMY[e]["exp"] for ids in rows.values() for e in ids)

WAVES, node_summary = [], []
cum_xp = 0
for node in NODE_ORDER:
    node_start_xp = cum_xp
    node_waves = []
    for i, w in enumerate(RECOMMEND[node]):
        lv = level_for_xp(cum_xp)[0]
        dps = dps_at(lv)
        boss_mult = w.get("boss", 0.0)
        bh = base_hp_of(w["rows"], boss_mult)
        exp_w = exp_of_rows(w["rows"])
        n = sum(len(v) for v in w["rows"].values())
        hp_mult = round(dps * w["sec"] / bh, 1) if (w["sec"] > 0 and bh > 0) else 0.0
        est = (bh * hp_mult) / dps if (dps and hp_mult) else 0
        comp = {}
        for ids in w["rows"].values():
            for e in ids:
                comp[e] = comp.get(e, 0) + 1
        checks = []
        nrows = len(w["rows"])
        ranged_rows = [r0 for r0, ids in w["rows"].items() if any(ENEMY[e]["ranged"] for e in ids)]
        if ranged_rows:
            checks.append("远程后排✔" if all(r0 >= nrows // 2 for r0 in ranged_rows) else "远程后排✘")
        if any(is_boss(e) for ids in w["rows"].values() for e in ids):
            boss_row = max(r0 for r0, ids in w["rows"].items() if any(is_boss(e) for e in ids))
            checks.append("Boss在最后排✔" if boss_row == max(w["rows"].keys()) else "Boss在最后排✘")
        if est:
            checks.append(f"清场{est:.0f}s" + ("✔" if 25 <= est <= 95 else "⚠"))
        rec = dict(node=node, idx=i, rows=w["rows"], comp=comp, n=n, base_hp=bh, hp_mult=hp_mult,
                   spd=w["spd"], dmg=w["dmg"], boss=boss_mult, exp=exp_w, sec=w["sec"], est=est,
                   lv=lv, dps=dps, note=w["note"], checks=" ".join(checks), total_hp=bh * hp_mult)
        WAVES.append(rec); node_waves.append(rec); cum_xp += exp_w
    node_summary.append(dict(node=node, waves=node_waves, exp=sum(w["exp"] for w in node_waves),
                             kills=sum(w["n"] for w in node_waves), hp=sum(w["total_hp"] for w in node_waves),
                             sec=sum(w["sec"] for w in node_waves), start_xp=node_start_xp, end_xp=cum_xp,
                             lv_start=level_for_xp(node_start_xp)[0], lv_end=level_for_xp(cum_xp)[0]))

TOTAL_XP = cum_xp
FINAL_LV = level_for_xp(TOTAL_XP)[0]
LV40_XP = xp_to_lv40()
GAP_PCT = round((LV40_XP - TOTAL_XP) / TOTAL_XP * 100)

# ── Excel ────────────────────────────────────────────────────────────────
wb = Workbook()
H_FILL = PatternFill("solid", fgColor="2F4F6F"); H_FONT = Font(color="FFFFFF", bold=True, size=11)
SUB_FILL = PatternFill("solid", fgColor="DCE6F1"); WARN = PatternFill("solid", fgColor="FFE0E0")
BORDER = Border(*[Side(style="thin", color="BFBFBF")] * 4)
WRAP = Alignment(wrap_text=True, vertical="top")

def hdr(ws, cols):
    for c in range(1, cols + 1):
        cell = ws.cell(row=1, column=c)
        cell.fill = H_FILL; cell.font = H_FONT; cell.border = BORDER
        cell.alignment = Alignment(horizontal="center", vertical="center", wrap_text=True)

def put(ws, r, vals, fill=None, bold=False):
    for i, v in enumerate(vals, start=1):
        cell = ws.cell(row=r, column=i, value=v)
        cell.border = BORDER; cell.alignment = WRAP
        if fill: cell.fill = fill
        if bold: cell.font = Font(bold=True)

def widths(ws, ws_widths):
    for i, w in enumerate(ws_widths, start=1):
        ws.column_dimensions[get_column_letter(i)].width = w

# 1 总览
ws = wb.active; ws.title = "总览"
ws.append(["节点", "定位", "波次数", "敌人总数", "预估时长", "本节点经验", "累计经验", "到达等级", "预期DPS",
           "总有效HP", "预期总清场", "现状问题", "建议动作"])
hdr(ws, 13)
r = 2
ISSUES = {
 "N1·当阳乱军": ("已落 5 波（摊在纵深、每排 1~2 只）；开发用 Test_Node_Battle 已从 Node_N1 删除", "保持；用机器人量清场时长再拧 hp×"),
 "E0·残门垣前": ("已落 5 波：1021 在第0波、109 在第1波 → 与教学台词顺序对上", "保持；用机器人量清场时长再拧 hp×"),
 "J1·城郊分叉": ("无战斗", "保持"),
 "EV·村落外围": ("无战斗", "保持（或加 1 波小伏击）"),
 "N2·官道断后": ("已落 6 波：105 后排远程 + 盾兵掩护 + 104 Boss（×7.5）在最后一排", "用机器人量清场时长再拧 hp×"),
 "N3·村落搜救": ("已落 6 波：倍率从 ×0.1 占位改为按成长重算；终局 104 ×11.5", "用机器人量清场时长再拧 hp×"),
}
for s in node_summary:
    issue, action = ISSUES[s["node"]]
    put(ws, r, [s["node"], NODE_ROLE[s["node"]], len(s["waves"]), s["kills"], f"{NODE_TIME[s['node']]}min",
               s["exp"], s["end_xp"], s["lv_end"], dps_at(s["lv_end"]), round(s["hp"]), f"{s['sec']:.0f}s", issue, action])
    r += 1
put(ws, r, ["合计", "全程 30 分钟目标", len(WAVES), sum(s["kills"] for s in node_summary),
            f"{sum(NODE_TIME.values())}min", TOTAL_XP, TOTAL_XP, FINAL_LV, dps_at(FINAL_LV),
            round(sum(s["hp"] for s in node_summary)), f"{sum(s['sec'] for s in node_summary):.0f}s",
            f"**成长缺口**：本方案只到 Lv{FINAL_LV}（约 {FINAL_LV} 次三选一），设计目标 Lv40 / 42 次（见「玩家成长」页）",
            "波次数或经验口径二选一（见「待决与风险」#1）"], fill=SUB_FILL, bold=True)
widths(ws, [16, 30, 8, 9, 9, 10, 10, 9, 9, 10, 10, 46, 40])
ws.freeze_panes = "A2"

# 2 波次明细
ws = wb.create_sheet("波次明细")
ws.append(["节点", "波", "行编成（row=排号，0=最前）", "敌人构成", "人数", "波次等级", "预期DPS", "基础总HP",
           "推导hp×", "已落hp×", "实测清场(s)", "建议总HP", "建议spd×", "建议dmg×", "建议bossHp×", "本波经验",
           "目标清场", "预计清场", "铁律自检", "说明"])
hdr(ws, 20)
r = 2
for w in WAVES:
    rows_desc = "；".join(f"row{k}: {'+'.join(v)}" for k, v in sorted(w["rows"].items())) or "（无）"
    comp = "；".join(f"{ENEMY[e]['name']}×{c}" for e, c in w["comp"].items()) or "—"
    applied = APPLIED.get(w["node"], [0] * 99)[w["idx"]] if w["idx"] < len(APPLIED.get(w["node"], [])) else "—"
    meas = MEASURED.get((w["node"], w["idx"]), "—")
    put(ws, r, [w["node"], w["idx"], rows_desc, comp, w["n"], w["lv"], w["dps"], round(w["base_hp"]), w["hp_mult"],
                applied, meas, round(w["total_hp"]), w["spd"], w["dmg"], w["boss"] or "—", w["exp"], f"{w['sec']}s",
                f"{w['est']:.0f}s", w["checks"], w["note"]])
    r += 1
widths(ws, [15, 5, 30, 24, 6, 9, 9, 10, 9, 9, 11, 10, 9, 9, 10, 9, 9, 9, 20, 44])
ws.freeze_panes = "A2"

# 3 敌人能力
ws = wb.create_sheet("敌人能力")
ws.append(["ID", "名称", "HP", "攻击", "攻击间隔(s)", "射程(排)", "经验", "定位", "特点 / 弱点", "建议用法"])
hdr(ws, 10)
usage = {
 "1011": "波次的承伤与经验来源；单波 3~5 只（不做大量对怪）",
 "1021": "放前排当肉盾，可与 105 组成「盾兵掩护弓兵」；破盾后进入胆怯增伤窗口",
 "109":  "波次高潮用；与 1011/1021 混编制造压迫",
 "105":  "必须放后 50% 排；40 血已改为 200 血（2026-10-25）→ 与 1011 同档，≈3 段 jab，不会被一戳秒",
 "104":  "Boss 波最后一排；血量用 bossHealthMultiplier 单独标定",
}
tips = {
 "1011": "任何手段都能秒；提供经验与挡位",
 "1021": "正面出手全被格挡，只剩 1 点伤害；必须上挑破盾或打盾区",
 "109":  "骑乘免控、冲锋直撞；血厚(500)",
 "105":  "射程 3 的随机轨迹远程；后排火力（200 血 ≈ 3 段 jab）",
 "104":  "霸体 + 多阶段 + QTE",
}
r = 2
for eid, e in ENEMY.items():
    put(ws, r, [eid, e["name"], e["hp"], e["atk"], e["interval"], e["rng"], e["exp"], e["role"], tips[eid], usage[eid]])
    r += 1
put(ws, r + 1, ["口径", "以上为 prefab 实测值（2026-10-25）。与 design/numerical-balance-draft.md §1.2 的旧表不一致："
                 "1011 实测 200HP/3攻（文档写 16HP/10攻）、105 实测 200HP（本轮 40→200）、104 实测 500HP/400经验 → 数值表以资产为准。",
                 "", "", "", "", "", "", "", ""], fill=WARN)
widths(ws, [8, 10, 8, 8, 12, 10, 8, 16, 42, 42])
ws.freeze_panes = "A2"

# 4 玩家成长
ws = wb.create_sheet("玩家成长")
ws.append(["节点", "波次等级", "本节点经验", "累计经验", "到达等级", "预期DPS(典型构筑)", "本节点建议总HP", "说明"])
hdr(ws, 8)
r = 2
for s in node_summary:
    put(ws, r, [s["node"], f"Lv{s['lv_start']}→Lv{s['lv_end']}", s["exp"], s["end_xp"], s["lv_end"],
                dps_at(s["lv_end"]), round(s["hp"]), "DPS 模型：33(平A) + 1.9×等级（上限 Lv32）；满配上限 111（草案 §7.1）"])
    r += 1
r += 1
put(ws, r, ["成长缺口分析", "", "", "", "", "", "", ""], fill=SUB_FILL, bold=True)
r += 1
for a, b in [
 ("设计目标（数值草案 §0.1）", "约 42 次有效技能升级、流程约 70% 处技能满级；终局约 Lv40"),
 ("本方案产出", f"总经验 {TOTAL_XP} → 终局 Lv{FINAL_LV}（约 {FINAL_LV} 次三选一）"),
 ("缺口", f"差 {GAP_PCT}%：按当前经验曲线，升到 Lv40 需要约 {LV40_XP} 经验"),
 ("原因", "敌人经验实测偏高（1011=80/只），但单波敌人被压到 3~4 只 → 经验总量不够"),
 ("方案 A（推荐）", f"波次数加到约 {round(LV40_XP/4/80)} 波（每波仍 3~4 只）→ 经验约 11k，节奏约每 50s 一波（适合手机）"),
 ("方案 B", "敌人经验上调（1011 80→200 等）→ 经验够了但每 1~2 只怪就一次三选一，手感会碎"),
 ("方案 C", "启用 StageConfig.killMilestones（现在全空）按里程碑补三选一，不动波次"),
]:
    put(ws, r, [a, "", "", "", "", "", "", b], fill=WARN if a == "缺口" else None)
    r += 1
r += 1
put(ws, r, ["经验曲线（实测 ExpCurve_30min）", "Lv1→2 = 200，之后每级 +4（60 级）"], fill=SUB_FILL, bold=True)
r += 1
put(ws, r, ["等级", "升下一级所需经验"] + [""] * 6, fill=SUB_FILL, bold=True)
r += 1
for i, c in enumerate(CURVE[:20]):
    put(ws, r, [f"Lv{i+1}→{i+2}", c] + [""] * 6)
    r += 1
widths(ws, [22, 14, 12, 11, 14, 18, 16, 56])
ws.freeze_panes = "A2"

# 5 配对与铁律
ws = wb.create_sheet("配对与铁律")
ws.append(["类别", "条目", "规则", "本次落实情况", "自检方法"])
hdr(ws, 5)
rules = [
 ("配对", "盾兵掩护弓兵", "1021 前排吃正面伤害，105 后排输出；玩家要先处理盾或绕后", "N2 第4波 / N3 第6波（Boss 波）", "波次明细：1021 的 row < 105 的 row"),
 ("配对", "骑兵做高潮", "109 血厚(500)+冲锋，与 1011 混编制造压迫", "E0 第2波、N2 第5波、N3 第4波", "每节点 109 ≤ 2"),
 ("配对", "弓兵只做后排火力", "射程 3、本体 40 血（两下平A）；威胁来自箭不是血", "N2 第1/2/4波、N3 第2/4波", "铁律十：105 的 row ≥ 波次排数/2"),
 ("配对", "不要大量对怪", "单波 3~5 只，靠机制与站位制造压力而不是数量",
  f"全程 {sum(s['kills'] for s in node_summary)} 只 / {len(WAVES)} 波", "总览表「敌人总数」"),
 ("铁律一", "敌人经验匹配强度", "Boss 经验 ≥ 同波精英 3 倍", "104=400 ≥ 109=100 的 3 倍 ✔", "敌人能力表经验列"),
    ("铁律二", "hp× 追踪玩家 DPS", "倍率随玩家成长递增（实测 DPS 57→70）",
  "**2026-10-25 改用实测 DPS**（旧公式 33+1.9×等级 是假设，偏大；机器人实测有效 DPS 只有 50~72）", "波次明细：推导hp× / 已落hp× 两列"),
 ("铁律三", "XP 按 per-enemy 实算", "Σ(数量×经验)，不按 HP 估", "波次明细逐波给了本波经验与累计", "总览表经验列"),
 ("铁律四", "加敌人=加经验=提前满级", "补怪时同步抬 hp×", "N1 0.50 → N3 0.62（1011 有效血 100→124 ≈ 3~4 段 jab）", "波次明细 hp× 列"),
 ("铁律五", "Boss 血量直接对准目标", "目标HP ÷ Boss 基础HP，不叠二次乘数", "N2 104 → ×7.5（3750）；N3 104 → ×9（4500）", "波次明细 bossHp× 列"),
 ("铁律六", "机制教学不晚于对应 Boss", "先用普通波教会机制，再上 Boss", "破盾/骑兵 E0 教；105 N2 教；104 N2/N3 出", "N2/N3 之前必须有 105/1021 教学波"),
 ("铁律七", "每波升级进度达标", "每波 ≥1 次升级，每连续两波 ≥3 次", "每波经验 160~330（曲线 200/级）", "总览表各节点经验"),
 ("铁律八", "单波清场时长", "**2026-10-25 改口径**：杂兵 3 段 jab 能杀 → 普通波 4~12s（不是旧表的 30~55s）；Boss 波 45~90s", "波次明细「实测清场」列", "同左"),
 ("铁律九", "Boss 在该波最后一排", "Boss 敌人在 rows 最后一个", "N2/N3 的 104 都在 row4", "波次明细行编成最大 row"),
 ("铁律十", "远程必须后排", "105 只能出现在后 50% 排", "N2/N3 的 105 都在后 50%", "波次明细行编成"),
]
r = 2
for cat, name, rule, status, check in rules:
    put(ws, r, [cat, name, rule, status, check])
    r += 1
widths(ws, [10, 22, 44, 44, 32])
ws.freeze_panes = "A2"

# 6 待决与风险
ws = wb.create_sheet("待决与风险")
ws.append(["#", "事项", "当前状态", "需要谁定", "影响面"])
hdr(ws, 5)
todos = [
 (1, f"成长缺口：只到 Lv{FINAL_LV}，设计目标 Lv40", f"经验产出 {TOTAL_XP} vs 目标 {LV40_XP}；方案 A/B/C 见「玩家成长」页。"
  "2026-10-25 决定：先按方案 A 落表，机器人量完再定。", "你拍板（选 A/B/C）", "整关升级节奏与三选一频率"),
 (2, "104 放在哪条支路", "2026-10-25 已定：两条都放（N2 ×7.5 / N3 ×11.5）", "已定", "终局节奏与支路价值"),
 (3, "N1 第0条开发战斗删不删", "2026-10-25 已删（Node_N1 只剩 Formal_N1_Battle）", "已定", "第一波难度起点"),
 (4, "E0 波次与教学台词的对应", "2026-10-25 已改：1021 第0波、109 第1波", "已定", "教学关可读性"),
 (5, "N3 的 0.1 倍率占位", "2026-10-25 已改为 6 波 + 重算倍率", "已定", "N3 难度"),
 (6, "DPS 模型（33+1.9×等级）", "本表所有 hp× 都按它反推；机器人实测后要替换", "机器人实测后回填", "全部倍率"),
 (7, "105 只有 8 血", "2026-10-25 已提到 40 血", "已定", "远程机制的存在感"),
 (8, "单波敌人上限 3~5 只", "本表按 3~5 只排", "确认口径", "难度上限与刷怪节奏"),
 (9, "30 分钟怎么分", "本表按 N1 5 / E0 6 / J1 1 / EV 2 / N2 7 / N3 8 分钟估；与「一次循环 ≤15min」冲突，待实测", "实机计时校正", "经验曲线与升级节奏"),
 (10, "玩家复活次数", "资产 Hero_Zhangfei 实测 reviveCount=0（100 血），文档写 3；2026-10-25 决定保持 0", "已定（保持 0）", "Boss 墙的容错"),
]
r = 2
for n, item, state, who, impact in todos:
    put(ws, r, [n, item, state, who, impact])
    r += 1
widths(ws, [5, 32, 50, 20, 28])
ws.freeze_panes = "A2"

# 7 无聊根因与解法
ws = wb.create_sheet("无聊根因与解法")
ws.append(["#", "根因（实测）", "证据", "后果", "不堆数值的解法"])
hdr(ws, 5)
BORING = [
 (1, "Stab 一招通吃",
  "Stab 20伤/0.3s = 66 DPS（数值草案按 10伤算的 33 DPS，资产里是 20，翻倍）；Slash 20伤/0.65s = 31 DPS。"
  "**实测校正**：Zhangfei_Slash 的 rangeRows=1（只打当前前排那一排），「Slash 一次 3 排」需要 attack_range_boost 升级（+1 排/级，但伤害 -30%/-25%）。",
  "玩家只要平A；Slash 的价值取决于「前排同时站了几只」，而不是「一列有几只」",
  "① 敌人摊在纵深后，前排变薄 → 斩击打不到人（**别把摊纵深当成给 Slash 创造机会**）；"
  "② 想让范围技有存在空间，要么让前排保持 2~3 只，要么给斩击单独加「跨排」；"
  "③ 本轮落地：N1 第2波保留「前排 3 只」当范围技教学，其余波次每排 1~2 只"),
 (2, "远程没有存在感 → 招架无意义",
  "105 弓兵原本只有 8 血（Stab 一下秒杀）→ 来不及射第二箭 → 远程压力=0；射程 3 这个「必须处理」的信号被 8 血抹掉了。"
  "**2026-10-25 已把 105 调到 40 血**（两下平A），并把 105 与杂兵同波、固定后排。",
  "Parry 在整关里没有使用场景；「处理后排」变成免费收益",
  "不改血量也能救：把 105 与杂兵同波混编并固定后排 → 「绕后/招架/放弃这个输出位」三选一变成真实取舍（打后排的 1011 要花时间，那段时间它一直在射）"),
 (3, "机制关卡用得太少",
  "1021（正面全格挡→只能上挑破盾）是唯一的「必须换招」单位，但只在 E0；109 冲锋有预警（可招架/可打断）却每节点只 1 只且在前排；104 的 QTE/阶段是终局机制，但正式路线里根本没有 Boss",
  "大部分战斗里只有「砍肉」一种交互；连招、招架、上挑都成了可选装饰",
  "编排规则：一波 = 1 个「门」+ 若干杂兵（门 = 1021 格挡 / 105 后排远程 / 109 冲锋）；不要用同种敌人凑数量"),
 (4, "波次太「平」",
  "当前建议表每波 1~2 种敌人、3~5 只、2 排以内",
  "玩家没有「先打谁」的选择；每波都是同一个题",
  "每波至少给一个优先级冲突：后排的门 vs 前排的杂兵，且两者不能同时打（打断/走位成本）"),
 (5, "（附带）硬直压制让难度失真",
  "受击硬直预算 0.6s（已修）后，玩家连续平A仍能几乎无代价地打死敌人",
  "「难度」被玩家自己的操作抹平；Boss 墙也失真",
  "给「平A同一列」加代价：连续命中同列触发敌人格挡/闪避累积 → 逼玩家换列/换招（规则层，不是数值层）"),
]
r = 2
for n, cause, ev, eff, fix in BORING:
    put(ws, r, [n, cause, ev, eff, fix])
    r += 1
r += 1
put(ws, r, ["一句话结论", "无聊不是数值太松，是编排把「每种敌人对应一种解法」的机制抹平了——同种敌人凑数量 + 全摆在平A最舒服的位置 → 平A通吃。修编排，不修数值。", "", "", ""], fill=SUB_FILL, bold=True)
r += 1
put(ws, r, ["落表改法", "已落（2026-10-25）：N1/E0/N2/N3 共 22 波，每排 ≤2 只（N1 第2波「前排 3 只」是范围技教学）；"
                  "每波至少 1 个「门」（1021 格挡 / 105 后排远程 / 109 冲锋）；105 与杂兵同波且固定后排；"
                  "104 Boss 两条支路都放（N2 ×7.5 / N3 ×11.5）。", "", "", ""], fill=WARN)
widths(ws, [5, 26, 52, 40, 60])
ws.freeze_panes = "A2"

os.makedirs(os.path.dirname(OUT), exist_ok=True)
try:
    wb.save(OUT)
    final = OUT
except PermissionError:
    # 文件被 Excel 占用 → 换一个名字写，别让生成失败
    final = OUT.replace(".xlsx", "-v2.xlsx")
    wb.save(final)
    print("!! 目标文件被占用（可能在 Excel 里开着），已改写到", final)
print("written:", final)
print("written:", OUT)
for s in node_summary:
    print(f"  {s['node']}: {len(s['waves'])} 波 / {s['kills']} 只 / 经验 {s['exp']} / 累计 {s['end_xp']} → Lv{s['lv_end']} / DPS {dps_at(s['lv_end'])} / 建议总HP {round(s['hp'])}")
print(f"  合计: 经验 {TOTAL_XP} → Lv{FINAL_LV}；升到 Lv40 需要 {LV40_XP}（缺口 {GAP_PCT}%）")
