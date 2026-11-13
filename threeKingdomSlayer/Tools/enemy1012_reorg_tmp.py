# -*- coding: utf-8 -*-
"""把 1012 各批次的产出重排到 Library/Locus/tmp/1012_batches/ 下，一批次一目录。"""
import pathlib, shutil

TMP = pathlib.Path("Library/Locus/tmp")
ROOT = TMP / "1012_batches"
if ROOT.exists():
    shutil.rmtree(ROOT)

B = {
    "01_idle_v6": ["keyed", "checks", "measurements", "backup", "legacy", "scripts"],
    "02_attack_arc_E2": ["keyed", "picked", "E1_probe", "rejected_round1", "checks", "backup"],
    "03_walk_v1": ["keyed", "picked", "checks", "backup"],
    "04_hitRight_v1": ["keyed", "picked", "checks", "backup"],
    "05_hitHeavy_front": ["rejected", "backup"],
    "_ref_v11_sprites": [],
    "_tool_validation": [],
}
for b, subs in B.items():
    d = ROOT / b
    d.mkdir(parents=True, exist_ok=True)
    for s in subs:
        (d / s).mkdir(exist_ok=True)


def mv(src, dst):
    src = pathlib.Path(src)
    dst = pathlib.Path(dst)
    if not src.exists():
        return False
    dst.parent.mkdir(parents=True, exist_ok=True)
    if dst.exists():
        shutil.rmtree(dst) if dst.is_dir() else dst.unlink()
    shutil.move(str(src), str(dst))
    return True


I = ROOT / "01_idle_v6"
A = ROOT / "02_attack_arc_E2"
W = ROOT / "03_walk_v1"
H = ROOT / "04_hitRight_v1"
F = ROOT / "05_hitHeavy_front"
R = ROOT / "_ref_v11_sprites"
V = ROOT / "_tool_validation"
am = TMP / "arc_measure"

# ── idle 批次
mv(TMP / "enemy1012_idle_v6", I / "keyed")
mv(TMP / "enemy1012_idle_v4", I / "legacy" / "v4")
mv(TMP / "enemy1012_idle_v5", I / "legacy" / "v5")
mv(TMP / "enemy1012_idle_v6_preview.gif", I / "preview.gif")
for n in ("enemy1012_E1_expression_arc.py", "enemy1012_E2_expression_arc.py",
          "enemy1012_idle_video_create.py", "enemy1012_idle_video_create_v2.py",
          "enemy1012_idle_video_create_v3.py", "enemy1012_stageA_submit.py",
          "enemy1012_stageA2_submit.py", "enemy1012_stageA3_submit.py",
          "enemy1012_stageA4_submit.py", "enemy1012_stageA5_submit.py",
          "enemy1012_stageA5b_submit.py", "enemy1012_stageA6_submit.py",
          "enemy1012_stageA6b_submit.py", "enemy1012_stageB_submit.py",
          "enemy1012_stageB2_submit.py", "enemy1012_stageB3_submit.py"):
    mv(TMP / n, I / "scripts" / n)
for n in ("idle_v6.json", "idle_v3.json", "idle_arc_v4.json"):
    mv(am / n, I / "measurements" / n)
for d in ("enemy1012_bandit_idle_v6", "enemy1012_idle_arc_v4", "enemy1012_expression_arc_v4"):
    mv(am / d, I / "measurements" / d)
for n in ("RAW_arc_v4_SUCCESS.png", "RAW_idle_v6_DEPLOYED.png", "DEPLOYED_sprites_idle1_6.png",
          "head_sheet_v4_SUCCESS.png", "head_sheet_v6_DEPLOYED.png", "mouth_3way.png"):
    mv(am / n, I / "checks" / n)
for n in ("1012_head_current_vs_stageB.png", "1012_stageB_vs_sprite.png"):
    mv(TMP / n, I / "checks" / n)
mv(TMP / "enemy1012_expression_arc_v4", I / "measurements" / "expr_arc_v4_root")
for n in ("idle_20261030-153143", "idle_v2_20261030-161932", "idle_v2b_20261030-162019",
          "idle_v3_20261030-174531", "idle_v4_20261030-182736", "idle_v4b_20261030-183128",
          "idle_20261104-203945", "idle_20261104-204026"):
    src = TMP / ("backup_Enemy1012_" + n)
    if src.exists():
        mv(src, I / "backup" / n)

# ── attack 批次
mv(TMP / "enemy1012_attack_E2" / "keyed_ui_t79", A / "keyed")
mv(TMP / "enemy1012_attack_E2" / "selection", A / "keyed" / "selection")
for n in ("E2_chop_arc.png", "E2_head_arc.png", "E2_picks16.png", "E2_strip25.png", "landed16.png"):
    mv(TMP / "enemy1012_attack_E2" / n, A / "checks" / n)
mv(TMP / "enemy1012_attack_E2" / "attack_preview.gif", A / "preview.gif")
mv(TMP / "enemy1012_attack_E2" / "picks16.txt", A / "picked" / "picks16.txt")
mv(TMP / "stage_attack_20261105-143619", A / "picked" / "landed")
mv(TMP / "backup_Enemy1012_attack_20261105-143620", A / "backup" / "attack_20261105-143620")
mv(am / "attack_E1", A / "E1_probe" / "keyed")
mv(am / "attack_E1.json", A / "E1_probe" / "arc_metrics.json")
mv(am / "E1_planA_sheet6.png", A / "E1_probe" / "sheet6.png")
for n in ("attack_v1", "enemy1012_bandit_attack_v1"):
    mv(am / n, A / "rejected_round1" / n)
mv(am / "attack_v1.json", A / "rejected_round1" / "arc_metrics.json")
mv(TMP / "enemy1012_attack_ref", A / "rejected_round1" / "attack_ref")
mv(TMP / "enemy1012_attack_v1", A / "rejected_round1" / "v1_dir")
mv(TMP / "enemy1012_attack_v1_preview.gif", A / "rejected_round1" / "preview.gif")

# ── walk 批次
mv(TMP / "enemy1012_walk_v1" / "keyed_ui_t79", W / "keyed")
for n in ("walk_picks6.png", "landed6.png", "feet_rows.png", "holes_f45.png",
          "holes_f51.png", "edge_qc.png", "weapon_compare.png"):
    mv(TMP / "enemy1012_walk_v1" / n, W / "checks" / n)
mv(TMP / "enemy1012_walk_v1" / "walk_preview.gif", W / "preview.gif")
mv(TMP / "enemy1012_walk_v1" / "picks6.txt", W / "picked" / "picks6.txt")
mv(TMP / "stage_walk_20261105-151745", W / "picked" / "landed")
mv(TMP / "backup_Enemy1012_walk_20261105-151745", W / "backup" / "walk_20261105-151745")
mv(TMP / "enemy1012_attack_E2" / "old_walk6.png", W / "checks" / "ref_v11_walk6.png")
mv(TMP / "enemy1012_walk_v1" / "idle_preview.gif", I / "preview_from_walk_batch.gif")

# ── hitRight 批次（原 hitHeavy_v1 目录）
mv(TMP / "enemy1012_hitHeavy_v1" / "keyed_ui_t79", H / "keyed")
for n in ("strip25.png", "head_sheet.png", "cand_A.png", "cand_B.png", "cand_C.png",
          "re_A.png", "re_B.png", "swordscan_A.png", "swordscan_B.png", "picked6.png"):
    mv(TMP / "enemy1012_hitHeavy_v1" / n, H / "checks" / n)
mv(TMP / "enemy1012_hitHeavy_v1" / "hitRight_preview.gif", H / "preview.gif")
mv(TMP / "stage_hitHeavy_20261105-165347", H / "picked" / "landed")
mv(TMP / "backup_Enemy1012_hitHeavy_20261105-165347", H / "backup" / "hitRight_v11_20261105-165347")
mv(TMP / "enemy1012_hitHeavy_v1" / "v11_hitLR.png", R / "v11_hitLeft_hitRight_4each.png")
mv(TMP / "enemy1012_hitHeavy_v1" / "v11_hitLR12.png", R / "v11_hitLeft_hitRight_6each.png")
mv(TMP / "enemy1012_walk_v1" / "v11_hitHeavy6.png", R / "v11_hitHeavy_6frames.png")

# ── 正面受击（未完成）批次
mv(TMP / "enemy1012_hitHeavy_v1" / "hitHeavy_preview.gif", F / "rejected" / "hitHeavy_rejected_preview.gif")
mv(TMP / "enemy1012_hitHeavy_v1" / "picks5.txt", F / "rejected" / "rejected_picks_8_13_17_30_80.txt")
mv(TMP / "stage_hitHeavy_20261105-162102", F / "rejected" / "landed_first_attempt")
mv(TMP / "backup_Enemy1012_hitHeavy_20261105-162103", F / "backup" / "hitHeavy_v11_originals")

# ── 工具验证
for n in ("keycheck_idle_v6", "keycheck_h110", "keycheck_h120"):
    mv(am / n, V / n)

# ── 清理：纯干跑暂存目录、被取代的偏移版本、空壳目录
for d in ("stage_attack_20261105-143513", "stage_walk_20261105-151615",
          "stage_hitHeavy_20261105-161903", "stage_hitHeavy_20261105-162036",
          "stage_hitHeavy_20261105-165209", "enemy1012_hitHeavy_v1"):
    p = TMP / d
    if p.exists():
        shutil.rmtree(p) if p.is_dir() else p.unlink()
if am.exists() and not any(am.iterdir()):
    am.rmdir()
print("重排完成 ->", ROOT)
