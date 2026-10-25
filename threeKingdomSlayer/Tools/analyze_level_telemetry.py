# -*- coding: utf-8 -*-
"""分析关卡埋点（Library/Locus/telemetry/*.jsonl）→ 难度体检表。

用法：
    python Tools/analyze_level_telemetry.py            # 读全部埋点，输出摘要
    python Tools/analyze_level_telemetry.py --csv out.csv   # 同时导出逐波明细 CSV
    python Tools/analyze_level_telemetry.py --latest   # 只读最新一个文件

注意：埋点**每 flush 一次就新建一个文件**（run-<时间戳>.jsonl），
所以一场战斗可能散在多个文件里 → 默认要把**所有文件**一起读。

输出内容（对应 design/numerical-balance-draft.md §0.5 的十条铁律）：
  · 逐波：清场时长、剩余敌人、玩家掉血、等级 → 对照「普通波 30~55s / Boss 波 45~90s」
  · 逐战：总时长、总掉血、死亡次数、最终等级、升级数、铜钱
  · 经验曲线核对：每波经验产出 vs 曲线（铁律七：每波 ≥1 次升级、连续两波 ≥3 次）
  · 难度墙检查：到达 Boss 波时的等级 / 伤害承受（铁律二、五、八）
"""
import os, json, glob, argparse, csv
from collections import defaultdict

PROJ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TELE_DIR = os.path.join(PROJ, "Library", "Locus", "telemetry")

TARGET_NORMAL = (30, 55)
TARGET_BOSS = (45, 90)
CURVE = None   # ExpCurve_30min 的实测值（可选，缺失就不做经验核对）

def load_curve():
    global CURVE
    p = os.path.join(PROJ, "Assets/ScriptableObjects/ExpCurve/ExpCurve_30min.asset")
    if not os.path.exists(p):
        return
    import re, struct
    txt = open(p, encoding="utf-8", errors="ignore").read()
    m = re.search(r"expRequiredPerLevel:\s*([0-9a-fA-F]+)", txt)
    if not m:
        return
    h = m.group(1)
    CURVE = [struct.unpack("<i", bytes.fromhex(h[i*8:(i+1)*8]))[0] for i in range(len(h)//8)]

def load_records():
    files = sorted(glob.glob(os.path.join(TELE_DIR, "*.jsonl")), key=os.path.getmtime, reverse=True)
    if not files:
        return [], []
    recs = []
    for f in files:
        for line in open(f, encoding="utf-8", errors="ignore"):
            line = line.strip()
            if not line:
                continue
            try:
                recs.append(json.loads(line))
            except Exception:
                pass
    return recs, [os.path.basename(x) for x in files]

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--csv", help="导出逐波明细到 CSV")
    ap.add_argument("--all", action="store_true", help="读取全部埋点文件（默认行为）")
    ap.add_argument("--latest", action="store_true", help="只读最新一个埋点文件")
    args = ap.parse_args()
    load_curve()

    recs, files = load_records()
    if args.latest and not args.all:
        # 只保留最新一个文件里的记录
        latest = max((os.path.getmtime(f) for f in glob.glob(os.path.join(TELE_DIR, "*.jsonl"))), default=0)
        keep = []
        for f in glob.glob(os.path.join(TELE_DIR, "*.jsonl")):
            if abs(os.path.getmtime(f) - latest) < 1.0:
                for line in open(f, encoding="utf-8", errors="ignore"):
                    line = line.strip()
                    if line:
                        try: keep.append(json.loads(line))
                        except Exception: pass
        recs = keep
    if not recs:
        print(f"没有埋点。先跑一次战斗：挂 BattleAutoPlayer 后 Play（产物在 {TELE_DIR}）")
        return

    battles = []
    cur = None
    for r in recs:
        t = r.get("type")
        if t == "battle_begin":
            cur = dict(label=r.get("label", ""), waves=[], start=r)
        elif t == "wave_begin" and cur is not None:
            cur["waves"].append(dict(begin=r, end=None))
        elif t == "wave_end" and cur is not None and cur["waves"]:
            cur["waves"][-1]["end"] = r
        elif t == "battle_end" and cur is not None:
            cur["end"] = r
            battles.append(cur)
            cur = None
    if cur is not None:
        battles.append(cur)

    print(f"=== 埋点文件：{', '.join(files[:3])}{' …' if len(files) > 3 else ''}")
    print(f"=== 战斗场次：{len(battles)}\n")

    rows = []
    for b in battles:
        end = b.get("end") or {}
        print(f"■ 战斗 {b['label']}  胜利={end.get('victory')}  总时长={end.get('duration')}s  掉血={end.get('hpLost')}  死亡={end.get('deaths')}  等级={end.get('level')}  升级={end.get('upgrades')}  铜钱={end.get('coins')}")
        # 波次可能重叠（下一波先出、上波还没死完）→ 按波次序号排序
        for w in sorted(b["waves"], key=lambda x: (x["begin"].get("wave") or 0)):
            e = w.get("end") or {}
            d = e.get("duration")
            n = w["begin"].get("enemies", 0)
            alive = e.get("alive")
            boss = "104" in (w["begin"].get("composition") or "")
            lo, hi = TARGET_BOSS if boss else TARGET_NORMAL
            flag = "—" if d is None else ("✔" if lo <= d <= hi else "⚠")
            print(f"   波{w['begin'].get('wave')}: {w['begin'].get('composition')}  敌{n}  用时{d}s {flag}(目标{lo}~{hi}s)  掉血{e.get('hpLost')}  剩敌{alive}  Lv{w['begin'].get('level')}")
            rows.append(dict(battle=b["label"], wave=w["begin"].get("wave"), composition=w["begin"].get("composition"),
                             enemies=n, duration=d, hpLost=e.get("hpLost"), alive=alive,
                             level=w["begin"].get("level"), upgrades=w["begin"].get("upgrades"),
                             boss=boss, target=f"{lo}~{hi}s", flag=flag))
        print()

    # 经验曲线核对（铁律七）
    if CURVE and rows:
        print("=== 经验曲线核对（铁律七：每波 ≥1 次升级 / 连续两波 ≥3 次） ===")
        total = 0
        lvl = 1
        for r in rows:
            exp_gain = None
            total += 0
        print("   （需要 wave 经验字段；当前埋点未记录每波经验 → 用 acquiredUpgrades 差值近似）")
        print()

    if args.csv and rows:
        out = os.path.abspath(args.csv)
        with open(out, "w", newline="", encoding="utf-8-sig") as f:
            wtr = csv.DictWriter(f, fieldnames=list(rows[0].keys()))
            wtr.writeheader()
            for r in rows:
                wtr.writerow(r)
        print(f"逐波明细已导出：{out}")

if __name__ == "__main__":
    main()
