# -*- coding: utf-8 -*-
"""为每个批次写 picked_frames.csv（选中帧清单）+ 1012_batches/README.md 索引。

清单同时给出两条路径：
    keyed_frame  —— 从源视频抠出、选中的那一帧（原始素材）
    landed_frame —— 落位到 1108² 画布后的成品帧（写进工程的就是它）
"""
import csv, pathlib

ROOT = pathlib.Path("Library/Locus/tmp/1012_batches")

BATCHES = [
    {
        "dir": "01_idle_v6",
        "title": "idle（山贼版 v5 待机）",
        "video": "enemy1012_bandit_idle_v6.mp4",
        "task": "task_50d155b9b8154c8ebefcd71066ba83d5",
        "keyed": "keyed/keyed_ui_t79",
        "landed": "picked/landed",
        "picks": [("idle%d" % i, "f%03d" % f) for i, f in
                  enumerate([10, 17, 24, 32, 39, 46], 1)],
        "note": "周期 43 帧（= Enemy_1012_Idle.anim 的 1.8s 键距）内等距取 6 帧；"
                "本批次 2026-11-04 完成，未留落位暂存帧，成品直接写入工程",
    },
    {
        "dir": "02_attack_arc_E2",
        "title": "attack（山贼版 攻击，方案A 表情）",
        "video": "enemy1012_bandit_attack_arc_E2.mp4",
        "task": "task_e0047d6cac32430ba7c9f6629b24e341",
        "keyed": "keyed",
        "landed": "picked/landed",
        "picks": [("attack%d" % i, "f%03d" % f) for i, f in
                  enumerate([4, 7, 10, 13, 16, 50, 54, 58, 62, 66, 70, 74, 78, 82, 86, 90], 1)],
        "note": "举刀段手选 + 劈砍段 DP 最大化最小相邻差；避开 f052/f053 大回旋弧光",
    },
    {
        "dir": "03_walk_v1",
        "title": "walk（山贼版 行走）",
        "video": "enemy1012_bandit_walk_v1.mp4",
        "task": "task_8790cd635992495ea0b836818ae39978",
        "keyed": "keyed",
        "landed": "picked/landed",
        "picks": [("walk%d" % i, "f%03d" % f) for i, f in
                  enumerate([40, 43, 45, 48, 51, 53], 1)],
        "note": "步态周期实测 16 帧（配对距离 0.007），相位起点 f040，周期内等距取 6 帧",
    },
    {
        "dir": "04_hitRight_v1",
        "title": "hitRight（山贼版 向右倒的受击）",
        "video": "enemy1012_bandit_hitHeavy_v1.mp4",
        "task": "task_cddf9defe2df4ac18b752c159e002959",
        "keyed": "keyed",
        "landed": "picked/landed",
        "picks": [("hitRight%d" % i, "f%03d" % f) for i, f in
                  enumerate([12, 26, 44, 76, 86], 1)]
                 + [("hitRight6", "idle1(工程内精灵)")],
        "note": "全程在「剑不离手」窗口内（f018-f025 刀脱手，已排除）；末帧用 idle1 收尾",
    },
    {
        "dir": "06_hitLeft_v1",
        "title": "hitLeft（山贼版 另一面的受击）",
        "video": "enemy1012_bandit_hitLeft_v1.mp4",
        "task": "task_d05a5fceeac646cb97d3dbdf8cc20df7",
        "keyed": "keyed",
        "landed": "picked/landed",
        "picks": [("hitLeft%d" % i, "f%03d" % f) for i, f in
                  enumerate([8, 26, 42, 72, 78], 1)]
                 + [("hitLeft6", "idle1(工程内精灵)")],
        "note": "动势照 v11 hitLeft 原设计（原地站立、刀自身侧抬起向左上）；剑全程在手（0 帧脱手）；避开 f004-f005（刀形变木桩）；末帧用 idle1 收尾",
    },
    {
        "dir": "08_hitHeavy_v4",
        "title": "hitHeavy（山贼版 正面受击，替换视频工作流）",
        "video": "enemy1012_bandit_hitHeavy_v4.mp4",
        "task": "task_9fef6e65a197409895c33765ed31ea0e",
        "keyed": "keyed",
        "landed": "picked/landed",
        "picks": [("hitHeavy%d" % i, "f%03d" % f) for i, f in
                  enumerate([41, 51, 67, 79, 90, 100], 1)],
        "note": "替换视频工作流：动作与表情表演均由母本 dao_v11_hitfront_v1_6s_720p.mp4 继承（轨迹相关 0.92–0.98）；选帧照母本同号帧",
    },
    {
        "dir": "10_hitHeavy_v6",
        "title": "hitHeavy（山贼版 正面受击，首尾帧锚图路线）",
        "video": "enemy1012_bandit_hitHeavy_v6.mp4",
        "task": "task_e2002ba3fb50400fb0fb34cfb6e6a9d2",
        "keyed": "keyed",
        "landed": "picked/landed",
        "picks": [("hitHeavy%d" % i, "f%03d" % f) for i, f in
                  enumerate([30, 62, 68, 100, 118, 140], 1)],
        "note": "首尾帧=02_IdentityImages/anchor_bandit_960.png；跨原型换角色必须走此路线（reference_video 会回退成参考片人物）；选帧按相位",
    },
]

for b in BATCHES:
    d = ROOT / b["dir"]
    (d / "picked").mkdir(exist_ok=True)
    landed = d / b["landed"]
    files = sorted(p.name for p in landed.glob("*.png")) if landed.exists() else []
    with (d / "picked" / "picked_frames.csv").open("w", encoding="utf-8", newline="") as f:
        w = csv.writer(f)
        w.writerow(["sprite", "source_frame", "source_video", "task_id",
                    "keyed_frame_path", "landed_file", "landed_path"])
        for i, (sprite, src) in enumerate(b["picks"]):
            fn = files[i] if i < len(files) else ""
            if src.startswith("f"):
                kp = "%s/%s/%s.png" % (b["dir"], b["keyed"], src)
            else:
                kp = "(工程内精灵) Assets/Sprites/Enemy/Enemy1012/Enemy_1012_idle1.png"
            w.writerow([sprite, src, b["video"], b["task"], kp, fn,
                        "%s/%s/%s" % (b["dir"], b["landed"], fn) if fn else ""])
    print("%-22s 落位帧 %d 张 -> %s" % (b["dir"], len(files), d / b["landed"]))

L = []
L.append("# 1012 山贼版 美术批次索引\n")
L.append("**一批次一目录，互不混放。** 每个批次目录里：")
L.append("")
L.append("| 子目录 | 内容 |")
L.append("|---|---|")
L.append("| `keyed/` | 从源视频抠出的透明帧（97 帧），**选中的那一帧就在里面** |")
L.append("| `picked/landed/` | 落位到 1108² 画布后的**成品帧**（写进工程的就是它） |")
L.append("| `picked/picked_frames.csv` | 逐帧清单：精灵名 ← 源帧号 ← 源视频 ← 成品帧路径 |")
L.append("| `checks/` | 目视/量化校验图（分段图、表情对照、边缘检查等） |")
L.append("| `backup/` | 该批次覆盖前的工程原图备份 |")
L.append("")
L.append("## 批次总表\n")
L.append("| 批次目录 | 内容 | 源视频 | task | 落位帧目录 | 帧数 |")
L.append("|---|---|---|---|---|---|")
for b in BATCHES:
    d = ROOT / b["dir"]
    ld = d / b["landed"]
    n = len(list(ld.glob("*.png"))) if ld.exists() else 0
    L.append("| `%s` | %s | `%s` | `%s` | `%s/%s` | %s |"
             % (b["dir"], b["title"], b["video"], b["task"], b["dir"], b["landed"], n if n else "—"))
L.append("")
for b in BATCHES:
    d = ROOT / b["dir"]
    ld = d / b["landed"]
    n = len(list(ld.glob("*.png"))) if ld.exists() else 0
    L.append("### %s — %s" % (b["dir"], b["title"]))
    L.append("")
    L.append("- **选中的源帧（抠图后）**：`Library/Locus/tmp/1012_batches/%s/%s/`" % (b["dir"], b["keyed"]))
    L.append("- **落位成品帧**：`Library/Locus/tmp/1012_batches/%s/%s/`（%s）"
             % (b["dir"], b["landed"], ("%d 张" % n) if n else "本批次未留暂存"))
    L.append("- **逐帧清单**：`Library/Locus/tmp/1012_batches/%s/picked/picked_frames.csv`" % b["dir"])
    L.append("- 源视频：`%s`（task `%s`）" % (b["video"], b["task"]))
    L.append("- 选帧口径：%s" % b["note"])
    L.append("")
L.append("### 其他目录\n")
L.append("- `05_hitHeavy_front/` —— 正面受击**第一版被否**的记录（`rejected/` 落位帧与预览，`backup/` v11 原图）；已由 `10_hitHeavy_v6/` 取代")
L.append("- `07_hitHeavy_v3/` —— 正面受击 v3（动作幅度失控：头顶下沉 226px vs 母本 49px）的失败记录")
L.append("- `08_hitHeavy_v4/` 与 `09_hitHeavy_v5/` —— 两次 **reference_video 路线**尝试（形象随时间回退成 v11 的宽方脸络腮胡）")
L.append("- `_ref_v11_sprites/` —— v11 旧形象参考图（受击三组朝向对照、母本 A/B 动作对照）")
L.append("- `_tool_validation/` —— 抠图器回归验证（与归档 SVL 产物对拍）")
L.append("")
L.append("> 全部位于 Unity 工程内暂存区 `Library/Locus/tmp/`，不进版本库。")
L.append("> 工程内正式产物：`Assets/Sprites/Enemy/Enemy1012/`；溯源台账：`Enemy1012ArtSource/00_Index/`。")
(ROOT / "README.md").write_text("\n".join(L) + "\n", encoding="utf-8")
print("\n索引写入:", ROOT / "README.md")
