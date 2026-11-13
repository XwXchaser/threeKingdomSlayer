# 固定抓取流程（CAPTURE-FLOW v1）

本文件是**实机截图采集的唯一规范**。采集器（阶段 1）按此实现，任何人/任何 AI 要新增镜头都按此走，
不允许临时口头描述一套新的拍法。

配套数据：
- 镜头声明表：`shot_defs.csv`（要拍什么）
- 素材索引：`index.csv`（拍到了什么）
- 登记脚本：`Tools/visual_brief/claim_shots.py`

---

## 1. 前提

1. **一个镜头 = `shot_defs.csv` 里的一行**。没有行就不能拍，也不许拍完再补。
2. 每个镜头必须可重放：同样的输入产生同样的画面。禁止依赖随机行为（AI 随机移动、随机波次、AI 意图）。
3. 采集只在 **Play Mode** 下进行（要真实叠加 UI）。进 Play Mode 必须由人显式触发，不自动跑。

---

## 2. 七步固定流程

| 步 | 动作 | 成功判据 |
|---|---|---|
| **S1 解析** | 读 `shot_defs.csv` 该行，解析 scene / camera / setup / capture / time / crop | 五个字段都有合法值；`status` 不是 `deprecated` |
| **S2 加载** | `EditorSceneManager.OpenScene(scene)` → 进 Play Mode → 等场景就绪 | 场景加载完成 + 等固定帧数（默认 30 帧）+ `StageController` 进入就绪态 |
| **S3 摆场** | 按 `setup` 词表依次执行动作码 | 每个动作码返回成功/失败；**任一失败即中止并报错**，不产出半成品图 |
| **S4 相机** | 按 `camera` 设死 position / rotation / orthoSize（或跟随目标） | 相机参数完全确定，不留随机项 |
| **S5 定帧** | 按 `capture` + `time` 决定采样时刻 | `game`=当前帧；`frame`=Animator 精确定位；`sequence`=区间等距采样 |
| **S6 抓取** | `ScreenCapture.CaptureScreenshotAsTexture()` → PNG | 分辨率 = 1080×1920（长边 1080 归档版另存） |
| **S7 登记校验** | 写 `index.csv` + 回读校验图像非空 | 图像非纯色（灰度方差 > 阈值）；`index.csv` 有对应行 |

S7 的纯色校验很重要：黑屏/白屏/未加载完的图**必须判失败并丢弃**，否则会污染库。

---

## 3. `setup` 动作码词表（固定，不得自造）

多个动作用 `+` 连接，按顺序执行。

| 动作码 | 作用 | 参数示例 |
|---|---|---|
| `none` | 空场，只有场景自身 | — |
| `hub` | 打开落地页（Battle 场景的 hub 态） | — |
| `talent_page` | 打开天赋页 | — |
| `stage_select` | 打开选关页 | — |
| `clear` | 清场：移除敌人、特效、跳字 | — |
| `spawn` | 生成敌人/Boss | `spawn:Enemy_1021`、`spawn:Boss_104` |
| `wave` | 推进到第 n 波 | `wave:2` |
| `combo` | 累计连击到 n | `combo:20` |
| `player_attack` | 触发玩家招式 | `player_attack:Stab`（Stab/Slash/Pierce/Sweep/Launch/Parry） |
| `effect` | 播放特效 prefab | `effect:CycloneEffect` |
| `skill` | 触发主动技能配置 | `skill:ActiveSkill_Cyclone` |
| `qte` | 触发 QTE | `qte:Sweep`（Sweep/Swipe/TripleStab） |
| `hit` | 让招式命中指定敌人（产生命中帧与跳字） | `hit:Enemy_1021` |
| `select_node` | 选中天赋节点（弹详情卡） | `select_node:talent_ice_thaw` |

可用资产（实现时按此定位，不接受臆造路径）：
- 敌人 prefab：`Assets/Resources/EnemyPrefabs/Enemy_<id>.prefab`、`Boss_104`
- 特效 prefab：`Assets/Prefabs/Effects/<Name>.prefab`（CycloneEffect / WaveEffect / IceMistEffect / ShootFireEffect / ArrowRainEffect）
- 主动技能：`Assets/ScriptableObjects/ActiveSkills/ActiveSkill_<Name>.asset`（Cyclone / ArrowRain / Disease / FireAOE / FireLine / IceMist / Wave / ChargeAttackShockwave）
- 动画 clip：`Assets/Animations/<Name>.anim`（敌人各动作、Boss_104_QTE_* 等）
- 天赋节点 id：`Assets/ScriptableObjects/Talents/TalentCatalog_Main.asset` 里的 `nodeId`
  （实测值如 `talent_ice_thaw` / `talent_move_slash` / `talent_unlock_c2`）

### 3.1 实现状态（2026-11-03）

已实现（可拍）：`none` `clear` `freeze` `unfreeze` `spawn:<Enemy_1021>` `upgrade_choice:<id|id|id>`
未实现（拍的时候会报"动作码未实现"，不产出半成品）：`hub` `talent_page` `stage_select` `wave:<n>`
`combo:<n>` `player_attack:<Type>` `effect:<Prefab>` `skill:<Asset>` `qte:<Type>` `hit:<Enemy>` `select_node:<id>`

采集入口（Unity Editor 内调用）：
```
VisualLibraryCapture.Start("battle_skill_icons_choice,battle_overall_scene", 1080, 1920)
```
- 一次调用拍完所列镜头，自动切场景 / 进 Play Mode / 抓完自动退
- 任务状态落在 `Library/Locus/tmp/visual_library_job.json`（跨域重载）
- 结果清单落在 `Library/Locus/tmp/visual_library_captured.json`，用
  `python Tools/visual_brief/claim_shots.py --import <该文件>` 写进 `index.csv`

实测踩过的坑（已修）：
- **镜头间状态泄漏**：上一个镜头弹的三选一会留在场景里，下一个镜头就带着它。已加"每镜头前重置"（销毁残留弹窗 + 恢复 timeScale）。
- **Overlay 的 Canvas 抓不到**：Camera → RenderTexture 默认不含 ScreenSpace-Overlay 的 UI。做法是抓前把 Overlay 临时切成 ScreenSpaceCamera 指向抓图相机，抓完还原。
- **抓前必须冻结时间**（`Time.timeScale = 0`），否则画面边抓边动。
- **Unity 会把实例化过的 prefab 重写一遍行尾**（LF vs CRLF），导致 `git status` 里一堆 prefab 变 modified。
  内容其实与 HEAD 逐字节相同，纯噪音。采集后跑 `git checkout -- Assets/Resources/EnemyPrefabs/` 还原即可。
  （仓库里 6 个 `.mat` 也有同样现象，是项目既有情况。）

## 4. `camera` 预设词表（固定）

| 预设 | 说明 |
|---|---|
| `MainCamera` | 场景主相机，参数锁死 |
| `follow:<Target>` | 跟随目标（目标需有相机锚点） |
| `closeup:<Target>@<dist>` | 特写，`<dist>` 为世界单位距离（默认 3） |

---

## 5. 三种抓取模式

### `capture=game`
当前帧直接抓。适合：HUD、界面、敌人站姿等静态或即时状态。

### `capture=frame`（招式动作关键帧 / 特效某一瞬间）
`time` 格式两种：
- `ClipName@0.42` —— 归一化时间（0~1），例：`Enemy_1011_Attack@0.35`
- `ClipName#23` —— 帧号，例：`Enemy_1011_Attack#23`

实现要点：`Animator.Play(clip, layer, normalizedTime)` + `Animator.Update(0f)` 强制采样，
**不靠等帧数碰运气**。这一条是"动作关键帧"可复现的唯一可靠做法。

### `capture=sequence`（连续帧条 / 动作全过程）
`time` 格式 `0.10-0.80`（归一化区间），区间内等距采 N 帧（默认 6）：
- 输出 `shots/<shot_id>__f01.png` … `__fNN.png`
- 另出一张横排帧条 `shots/<shot_id>__strip.png`（拼图脚本：`Tools/visual_brief/build_brief.py` 的拼图能力）

---

## 6. 特殊需求的处理

| 需求 | 做法 |
|---|---|
| **某个特效的游戏实机表现**（如旋风） | `setup=clear+effect:CycloneEffect`，`capture=game` 或 `frame` 配该特效动画的 clip/时间点；`tags` 里必须带 `effect` 与特效名 |
| **某招式的动作关键帧** | `setup=player_attack:Stab` 或直接指定 clip，`capture=frame`，`time=ClipName@0.35`；要全过程用 `capture=sequence` |
| **命中瞬间 / 打击反馈** | `setup=spawn:Enemy_1021+player_attack:Stab+hit:Enemy_1021`，`capture=frame` 对准命中帧 |
| **HUD 局部**（只要顶部/底部条） | 用 `crop` 列裁剪，如 `"0,0,1,0.3"`；不要拍整屏再裁 |
| **敌人受击/死亡** | `setup` 里生成敌人后触发受击动作，`capture=frame` 指向对应 clip |

---

## 6.5 选图时的"实机优先"（打包器内置规则）

需求文本里出现「实机 / 现状 / 游戏里 / 真实显示 / 当前画面…」→ 判定为"问的是实际长什么样"：
- `role=ingame` 的实机截图 **+30**，离线稿（preview/component/source/sprite）**-8**
- 否则：整图 preview(+10) / 参考 reference(+6) / 游戏内素材 sprite(+3) / 零件 component(+1) 正常竞争

理由：需求问的是实机现状时，离线美术稿会让出方案的一方误判现状。实测踩过——
「三选一弹窗的技能图标」这条需求最初被离线美术稿占满，实机图一条没进包。

## 7. 缺图决策树（页面/镜头不存在时）

```
需要一张实机画面，但库里没有
 │
 ├─ Q1 语义相近的条目在 index.csv 里吗？
 │    ├─ 有且 role=ingame ──────────────→ 直接用（核对 status=fresh）
 │    ├─ 有但 role=preview/component ───→ 可当"设计稿"用，但必须在 brief 里标明是素材不是实机；
 │    │                                     同时在 shot_defs.csv 加对应镜头行，走 S1..S7 补实机图
 │    └─ 没有 ─────────────────────────→ 进 Q2
 │
 ├─ Q2 这画面在工程里能摆出来吗？
 │    ├─ 能 ──→ 在 shot_defs.csv 加一行（照 §3 词表填 setup）→ 跑采集 → 登记 → 用
 │    └─ 不能（场景不存在 / 表现未实现 / 无法稳定复现）
 │              → 该行 status 写 unsupported，notes 写清原因
 │              → brief 里显式写"缺图：<原因>"，并把该点列入"未决问题"
 │
 └─ Q3 旧图能顶替吗？
        → 默认不能。旧图标 role=legacy 并注明拍摄时间，
          不得当作"现状"喂给方案模型（会让方案基于过时现状）
```

**硬规则**
1. 每张图必须可追溯：`index.csv` 有行、有 `path`、有 `captured_at`、有 `sha256`。
2. 不许拿离线稿/旧图冒充实机现状；brief 里每张图都要标类别（实机 / 素材 / 参考 / 旧版）。
3. 缺图就写缺图，不许沉默省略——出方案的一方必须知道哪里信息不足。
4. `status=stale`（源资产变了没重拍）要么重拍，要么标"可能已过期"。
5. 采集失败（图非纯色 / 动作码失败）**不产出半成品**，报失败原因，由人决定是修流程还是改需求。
