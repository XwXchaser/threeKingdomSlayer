---
id: kd_f0c2f71f-f1ae-4cde-a566-9fb039e7ca5e
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
id: kd_1012_asset_pull_2026_10_26
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
summary: 1012 美术素材接入实录：来源分支 origin/art/1012-v11-resources 只取精灵图，动画与控制器在项目内按 1011 时序重建，Enemy_1012.prefab 由 Enemy_1011.prefab 复制而来；含 Unity 动画导入产物陈旧的坑与脚底对齐实测。
---

# 1012 美术素材接入（2026-10-26）

> 溯源包见 `Enemy1012ArtSource/`（其 README 已含制作配方、命名口径、验收数值）。
> 本文件记录**接入做法、实测数据与坑**。

## 1. 来源分支

| 项 | 值 |
|---|---|
| 分支 | `origin/art/1012-v11-resources`（`git fetch` 后新出现的远程分支） |
| 基点 | `2a83d89`（= `origin/route-scroll-movement`），与 `feat/stage-select-talent-overlays` 同源分叉 |
| 领先当前 HEAD | `be38126`（1021 源视频回填）、`ac6d213`（1012 入库 + 1011 还原 + 1021 命名对调） |

`ac6d213` 夹带了 1021 的命名口径修正（`hitRight*` → `hitLeft*`、控制器状态改名，GUID 不变），
**本次未取**（决定：维持 1021 现状，避免已验收敌人的行为变更）。

## 2. 取用范围（最终）

**保留**：
- 70 张精灵 `Assets/Sprites/Enemy/Enemy1012/`（含 meta 与目录 meta）
- 溯源包 `Enemy1012ArtSource/`（9 支源视频 + 3 身份图 + 4 溯源表 + README）
- `.gitignore` 的 `Enemy1012ArtSource` 白名单块（溯源包需要）

**撤掉**：
- 分支带来的 10 个 clip + 1 个控制器（`Enemy_1012_*.anim`、`Enemy_1012.controller`）→ 改为项目内重建
- 上游交接文档 `enemy1012-v11-art-handoff.md` → 其内容已并入本文件与溯源包 README

## 3. 项目内重建动画（关键做法）

**做法**：以 **1011 的 clip 为时序模板**，`AssetDatabase.CopyAsset` 复制 1011 的 `.anim`，
再用 `AnimationUtility.SetObjectReferenceCurve` 把 sprite 曲线换成 1012 精灵（关键帧时间不动）。

**为什么不用分支的 clip**：分支的 `HitFlash` 状态指向 `HitHeavy`（6 帧 1.05s），与 1011 的
`HitFlash`（5 帧 0.9s）不是一回事；本次决定**沿用 1011 时序**，`hitHeavy1..5` 填入 5 个键位，
`hitHeavy6` 不使用（与 1011 侧 `hitLeft6`/`hitRight6` 同样属于未被引用的冗余帧）。

**帧组映射**：除正面受击外一一对应。`Enemy_1011_hit{n}` → `Enemy_1012_hitHeavy{n}`；
其余 `Enemy_1011_X` → `Enemy_1012_X`。

**实测对齐结果**（10/10 通过）：

| clip | 时长 1011→1012 | 循环 | 帧数 | 关键帧时间 |
|---|---|---|---|---|
| Idle / Walk / Dead / Launched_Rise / Launched_Fall / Launched_Getup | 0.6 → 0.6 | 同 | 6 | 完全一致 |
| Attack | 2.9333 → 2.9333 | 同 | 18 | 完全一致 |
| HitFlash / HitLeft / HitRight | 0.9 → 0.9 | 同 | 6 | 完全一致 |

## 4. 坑：Unity 动画导入产物陈旧（重要）

用 API 改完 `.anim` 后，**磁盘 yaml 与 1011 逐字节相同（仅 `m_Name` 不同）**，
但 Unity 报出的 `AnimationClip.length` 仍是按曲线末尾重算的值（0.5333 而非 0.6）。
`ImportAsset(ForceUpdate)` 无效；**删掉 `.meta` 再 Refresh** 才让导入产物重建到位。

> 实测对照：把同一份字节另存为新资产 → 导入读出 0.6；同一路径上的旧资产仍读出 0.5333。
> 结论：**判断动画资源是否就绪，不能只看磁盘 yaml，要看 Unity 实际读出的 `length`**。

## 5. 控制器与 Prefab

**控制器** `Assets/Animations/Enemy_1012.controller`：复制 `Enemy_1011.controller`，
10 个状态的 motion 改指 1012 clip。实测与 1011 完全一致：10 状态 / 6 参数 /
transition 数与目标状态 / exitTime / speed / writeDefault 全同。

**Prefab** `Assets/Resources/EnemyPrefabs/Enemy_1012.prefab`：由 `Enemy_1011.prefab` 复制。
改动仅 4 处，其余（数值、组件、层级、`FreezePresentation`、`gemSprite`、AttackTelegraph）照抄：

| 字段 | 值 |
|---|---|
| root 名 | `Enemy_1012` |
| `enemyId` | 1012 |
| `enemyName` | `刀兵_1012`（沿用「类别_编号」惯例；1012 美术身份为 v11 刀兵） |
| Animator | `Enemy_1012.controller` |
| SpriteRenderer 默认图 | `Enemy_1012_idle1.png` |

实测：Enemy 组件字段差异仅 `enemyId`/`enemyName` 两项；prefab 依赖里**无任何 1011 资源残留**；
1011 的 prefab / 控制器 / clip 全程未被改动（哈希比对通过）。

## 6. 脚底对齐实测（PPU 16，1108² 画布）

量「画面脚底相对 pivot 的世界偏移」：

| 帧 | pivotY | 脚底−pivot | 相对站立帧 |
|---|---|---|---|
| 1011 idle1 | 0.5487 | −24.844u | 基准 |
| 1011 hit1 | 0.5 | −21.344u | **抬升 3.50u** |
| 1012 idle1 | 0.5487 | −24.844u | 基准（与 1011 完全相同） |
| 1012 hitHeavy1 | 0.5487 | −24.719u | 抬升 0.13u（基本齐平） |

- 站立帧与 1011 同锚点同落位 → prefab 里照抄的 `visualYOffset = 0.1388615` 直接可用。
- 正面受击 1012 不再像 1011 那样抬升 3.5u，属**视觉差异**（应更稳，但需实机看一眼）。

## 7. 待用户验收 / 未做

1. **实机画面验收**：1012 目前只有 prefab，**未接入任何波次/关卡**（本轮不做接线），
   需要手动把 `Enemy_1012.prefab` 摆进场景或接入波次才能看到。
2. 正面受击的姿态/时长观感（0.9s / 5 帧 + `hitHeavy6` 弃用）是否符合预期。
3. `enemyName = 刀兵_1012` 的叫法是否合适。
4. 1012 的独立数值（HP/伤害/韧性）与 `AttackSkillConfig` 专属副本 —— 本轮全部照抄 1011。
5. 1021 命名对调（`HitRight` → `HitLeft`）仍未取，另开一轮处理。

---

## 8. 规格校验实测（2026-10-26 教学关接线前）

> 背景：教学关改造要用 1012 当**霸体教学敌人**，但 1012 的美术此前**没有做过规格校验**。
> 本节是按 `design/pixel-character-and-enemy-animation-spec.md` + `design/visual-yoffset-system.md`
> 做的**实测**（不是目测），结论已写进 `plan/tutorial-parry-defend-redesign-20261026.md` 的批次 0。

### 8.1 画布 / 导入设置

| 项 | 1011 | 1012 | 结论 |
|---|---|---|---|
| 画布尺寸 | 1108×1108 | 1108×1108（70 张全一致） | ✅ 符合"同一动作画布尺寸统一" |
| PPU | 16 | 16 | ✅ 一致 |
| Tight 打包 / alphaIsTransparency | 是 | 是 | ✅ 一致 |
| 导入后实际 pivot | (554,554) = 画布中心 | (554,554) = 画布中心 | ⚠️ 见 §8.5 |

### 8.2 单位尺寸（"部署后会不会比 1011 大不少"）

用 `Sprite.vertices` 的剪影高度 × 根节点 scale（0.08316832）算世界高度：

| 帧 | 1011 身高 | 1012 身高 | 差 |
|---|---|---|---|
| idle1 | 3.602u | 3.166u | **−12%** |
| walk1 | 3.238u | 3.244u | +0.2% |
| 命中帧（1011 a8 / 1012 a6） | 3.670u | 3.763u | +2.5% |

**结论：1012 不比 1011 大，比例一致。** walk / 命中帧与 1011 逐帧对齐（差 ≤0.2%）；
idle 矮 12% 是**姿态差**（1011 的 idle 把剑举高，剪影更高），不是画得小。
→ 想让 1012 站姿"看起来一样高"是**美术决策**（重画 idle 举剑），**不是缩放问题**；
缩放 prefab 会破坏 walk/命中帧与 1011 的一致性。

### 8.3 脚底锚点（要改 prefab）

- prefab 的 `visualYOffset = 0.13886152`（从 1011 照抄）是为 **1011 的 idle1** 调的：
  1011 idle1 脚底世界 Y = **−1.800**，正好落在地面基准线 −1.8（Δ=0）。
- 1012 用同一个值 → idle1 脚底 = **−1.660**，**比地面高 0.140 世界单位**（≈1080×2160 下 14px）→ **会浮空**。
- 修法：`Enemy_1012.prefab` 的 `visualYOffset` 改 **0.001**（≈0）：
  `v = −1.8 − minY×rootScale = −1.8 − (−21.63 × 0.08316832) = +0.00095`。
- 注意：`EnemyGroundAligner`（运行时按剪影自动对齐）**没有挂在任何敌人 prefab 上**（1011/1012/109/1021 都没有），
  所以 prefab 手填值是唯一控制项。
- 各帧脚底漂移（画布像素）：1012 idle 6 帧全 = 898（**完全锁定**，比 1011 的 901..961 更稳）；
  walk 875..907（与 1011 的 walk 完全相同）；attack 897..933（比 1011 的 894..972 更紧）→ **无新增锚点问题**。

### 8.4 命中帧取哪一帧（**原交接推断有误**）

- 1011：大白弧在 **a8** 出现并最大 → 1011 声明的命中帧 `Enemy_1011_attack8` ✅。
- 1012：大白弧在 **a6** 出现（a6 最大、a7/a8/a9 递减）→ **1012 的命中帧应是 `Enemy_1012_attack6`**。
- 1012 的整套 attack 比 1011 **早约 2 帧**（同一套编排，相位前移）。
  → prefab 里现存的 `Enemy_1011_attack8`（1011 抄来的）**既指错了角色也指错了帧**，必须改成 `Enemy_1012_attack6`，
    keyIndex / hitTime 用 `Tools ▸ 三国杀戮 ▸ 校验攻击命中帧` 回填。

### 8.5 两个要注意的坑

1. **.meta 与导入产物不一致**：meta 写的是自定义 pivot `(0.6539, 0.5487)`，
   但 Unity 实际读出的 pivot 是画布中心 `(554,554)`（= 0.5/0.5）→ 与 `plan/enemy1012-asset-pull-20261026.md` §4
   的"导入产物陈旧"是同一类问题。1011 与 1012 状态相同，所以互相比对不受影响；
   但**一旦有人删 .meta 强制重导入，两者的 pivot 会一起变**，`visualYOffset` 需要重算。
2. **动画导入产物已正常**：实测 1012 的 Attack 2.9333s/18 键、Idle/Walk 0.6s/6 键、controller 各 10 个 clip，
   与 1011 一致 → §4 的坑已收尾，没有残留。
