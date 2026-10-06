---
id: kd_1b26ca05-47e3-42aa-8750-94e71805f019
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# Enemy 1011 动画部署与播放规则交接

> **用途**：新对话直接读取本文件，即可开始 Enemy 1011 的动画 Clip 部署、Animator 接线、受击方向播放规则和 Launch 落地/起身接线。不要把视频生成目录中的“已生成”误认为 Unity 已部署。
>
> **当前边界**：本文件的“最终验收结论”与第 0 节资产表是当前权威状态；后续素材生产、候选帧和失败尝试章节保留为历史追溯，不应覆盖最终接入结论。

## 0. 当前结论

Enemy 1011 是 enemy101 的复制体，`enemyId = 1011`。正式 Unity 资产路径：

- Prefab：`Assets/Resources/EnemyPrefabs/Enemy_1011.prefab`
- Animator Controller：`Assets/Animations/Enemy_1011.controller`
- 正式精灵目录：`Assets/Sprites/Enemy/Enemy1_1/`
- 当前有效测试配置：`Assets/Experiments/CurvedScroll/RouteTrialData/SmallBattle.asset`
- 当前测试场景：`Assets/Scenes/Battle.scene`

### 最终验收结论（当前权威状态）

- Enemy 1011 的通用动画套件已全部完成并通过用户验收：Idle、正面/左/右受击、Attack、Dead、Walk、Launched_Rise、Launched_Fall、Launched_Getup。
- 这套动画的**动作类别、姿态因果、根部/脚底锚点、节奏和状态语义**可作为后续所有敌人的通用动作设计参考。
- 1011 的剑侧、体型、像素包围框和具体受击剪影属于该敌人的个体美术特征；后续敌人应复用动作语义和技术约束，按自身武器、体型和锚点重新设计，不应直接复制像素几何。
- 当前流程没有独立 Landing Clip：`landing1..6` 已作为 Fall 的下降/落地尾段帧使用，真实落地由运行时代码确认后播放 Getup。

### 已完成并已接入 Unity

| 动作 | Clip | 当前状态 |
|---|---|---|
| Idle | `Assets/Animations/Enemy_1011_Idle.anim` | 已接入；6 帧，30fps，Loop，stopTime 0.6；Idle state speed 0.75 |
| 正面受击 | `Assets/Animations/Enemy_1011_HitFlash.anim` | 已接入；6 帧，0.9s，不循环；末键引用 idle1 |
| 左受击 | `Assets/Animations/Enemy_1011_HitLeft.anim` | 已接入；6 键，30fps，stopTime 0.9，不循环 |
| 右受击 | `Assets/Animations/Enemy_1011_HitRight.anim` | 已接入；6 键，30fps，stopTime 0.9，不循环 |
| Attack | `Assets/Animations/Enemy_1011_Attack.anim` | 已接入；18 个对象引用键，30fps，stopTime 2.9；命中键约 2.0s |
| Dead | `Assets/Animations/Enemy_1011_Dead.anim` | 已接入；6 帧，stopTime 0.6，不循环，末姿势保持 |
| Walk | `Assets/Animations/Enemy_1011_Walk.anim` | 已接入；6 帧，30fps，Loop，stopTime 0.6；实际语义是原地冲锋跑位 |
| 击飞 Rise | `Assets/Animations/Enemy_1011_Launched_Rise.anim` | 已接入；6 键，30fps，stopTime 0.6，不循环 |
| 击飞 Fall | `Assets/Animations/Enemy_1011_Launched_Fall.anim` | 已接入；`landing1..6` 六键，30fps，stopTime 0.6，Loop |
| 起身 Getup | `Assets/Animations/Enemy_1011_Launched_Getup.anim` | 已接入；6 键，30fps，stopTime 0.6，不循环 |

对应已部署精灵：

- `Enemy_1011_idle1..6.png`
- `Enemy_1011_hit1..5.png`
- `Enemy_1011_attack1..16.png`
- `Enemy_1011_dead1..6.png`
- `Enemy_1011_walk1..6.png`
- `Enemy_1011_hitLeft1..6.png`
- `Enemy_1011_hitRight1..6.png`
- `Enemy_1011_rise1..6.png`
- `Enemy_1011_landing1..6.png`
- `Enemy_1011_getup1..6.png`

### 已部署的 Launch Sprite 素材

18 张 Launch 三阶段精灵已经复制到工程并按正式设置导入：

- Rise：`Enemy_1011_rise1..6.png`
- Landing：`Enemy_1011_landing1..6.png`
- Getup：`Enemy_1011_getup1..6.png`（视频内容与选帧/抠图已验收）

工程路径：`Assets/Sprites/Enemy/Enemy1_1/`

18 张均已回读为：

```text
1108×1108
Texture Type = Sprite
Sprite Mode = Single
PPU = 16
Filter = Point
Compression = Uncompressed
Alpha Is Transparency = true
Mipmaps = false
Mesh = Tight
Pivot = 复制 Enemy_1011_idle1.png 的实际 pivot
```

**Launch Clip 已创建并接入；当前没有独立 Landing Clip。** 按用户确认的三段结构，`landing1..6` 作为 Fall 循环帧使用：

```text
Assets/Animations/Enemy_1011_Launched_Rise.anim
Assets/Animations/Enemy_1011_Launched_Fall.anim
Assets/Animations/Enemy_1011_Launched_Getup.anim
```

本轮已修改 `Enemy_1011.controller`、`Enemy.cs`、受击管线和攻击上下文传递。

## 1. 当前 Animator 接线

`Assets/Animations/Enemy_1011.controller` 已核实的 motion：

```text
Idle                                      -> Enemy_1011_Idle.anim
Idle -> Attack                            -> Enemy_1011_Attack.anim
Idle -> HitFlash                          -> Enemy_1011_HitFlash.anim
Idle -> Dead                              -> Enemy_1011_Dead.anim
Idle -> Walk                              -> Enemy_1011_Walk.anim
Idle -> Launched_Rise                     -> Enemy_1011_Launched_Rise.anim
Launched_Rise -> Launched_Fall            -> Enemy_1011_Launched_Fall.anim
Launched_Getup                            -> Enemy_1011_Launched_Getup.anim
HitLeft                                   -> Enemy_1011_HitLeft.anim
HitRight                                  -> Enemy_1011_HitRight.anim
```

现有状态名不能随意改：

```csharp
private void PlayIdleVisual()   => _animator?.Play(IsCoward ? "CowardIdle"     : "Idle", 0, 0f);
private void PlayLaunchVisual() => _animator?.Play(IsCoward ? "CowardLaunched" : "Launched_Rise", 0, 0f);
private void PlayHitVisual(HitReactionDirection direction) // Front / Left / Right
private void PlayGetupVisual()  => _animator?.Play(IsCoward ? "CowardGetup" : "Launched_Getup", 0, 0f);
private void PlayDeadVisual()   => _animator?.Play(IsCoward ? "CowardDead"     : "Dead", 0, 0f);
```

现有状态机事实：

- `PlayLaunchVisual()` 播放 `Launched_Rise`。
- `Launched_Rise` 结束后过渡到 `Launched_Fall`。
- 空中再次受击使用 `ReRise` 回到 `Launched_Rise`。
- `Launched_Fall` 是循环状态，直到代码检测落地。
- 当前没有独立 Landing 状态；Getup 已接入，真实落地后由 `Enemy.UpdateLaunch()` 播放。

旧 101 Clip 的参考接线时长：

```text
Enemy_101_Launched_Rise.anim: 30fps / stopTime 0.55 / Loop Off
Enemy_101_Launched_Fall.anim: 30fps / stopTime 0.45 / Loop On
```

1011 新 Clip 的键数和 stopTime 应以 6 张验收帧及实际接线为准，不能只复制视频 4 秒时长。

## 2. Launch 三阶段素材状态（历史生产记录）

> 本节保留视频、选帧和抠图过程信息。部分“尚未接入/待验收”措辞描述的是部署前阶段，不代表当前 Unity 状态；当前状态以 §0 的最终验收结论为准。

### 2.1 Rise

来源视频：

`C:/Users/Administrator/Videos/doubaoVideo/enemy1011_launch_rise_v1.mp4`

任务：`task_1c5d1f90fb0549f2b649110eda38208d`，费用 `0.48888`，视频/容器已验收。

6 帧候选：

```text
f049 -> rise1：起飞前最后 Idle
f050 -> rise2：挑飞开始
f052 -> rise3：早期离地
f056 -> rise4：空中展开
f070 -> rise5：高点空中姿态
f097 -> rise6：Rise 实际末帧
```

来源帧目录：`Library/Locus/tmp/sword_enemy_launch_v1/frames_rise_v1/`

本地抠图输出：`Library/Locus/tmp/sword_enemy_launch_v1/local_key_rise_v1/`

工程精灵：`Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_rise1..6.png`

### 2.2 Landing tail

来源视频：

`C:/Users/Administrator/Videos/doubaoVideo/enemy1011_launch_landing_v1.mp4`

任务：`task_9f5c4081f24c4206afeae33c071e19c3`，费用 `0.48888`，视频/尾段已验收。

6 帧候选：

```text
f001 -> landing1：空中下降开始
f006 -> landing2：继续下降
f010 -> landing3：接触/压缩
f015 -> landing4：落地过渡
f020 -> landing5：倒地终态开始
f097 -> landing6：已验收落地尾帧
```

来源帧目录：`Library/Locus/tmp/sword_enemy_launch_v1/frames_landing_v1/`

本地抠图输出：`Library/Locus/tmp/sword_enemy_launch_v1/local_key_landing_v1/`

工程精灵：`Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_landing1..6.png`

**范围**：这一段只到倒地终态，不包含起身。

### 2.3 Getup

来源视频：

`C:/Users/Administrator/Videos/doubaoVideo/enemy1011_launch_getup_v1.mp4`

任务：`task_b226b12b3dc24b9aae00e0ad63480260`，费用 `0.48888`，视频内容已验收。

6 帧候选：

```text
f001 -> getup1：倒地起始
f016 -> getup2：手臂开始支撑
f031 -> getup3：膝盖/骨盆收回
f046 -> getup4：躯干抬起
f061 -> getup5：接近直立
f097 -> getup6：Idle 末帧
```

来源帧目录：`Library/Locus/tmp/sword_enemy_launch_v1/frames_getup_v1/`

本地抠图输出：`Library/Locus/tmp/sword_enemy_launch_v1/local_key_getup_v1/`

工程精灵：`Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_getup1..6.png`

**历史范围**：倒地 → 起身 → Idle；视频与18帧素材已验收。Clip 接入已在后续部署阶段完成，当前状态见 §0。

## 3. 受击动画生产记录（历史过程 + 当前结果）

### 3.1 正面 HitFlash

以下“唯一正式接入”是早期部署阶段的历史记录；当前正面、左、右三种受击方向均已正式接入：

- Clip：`Assets/Animations/Enemy_1011_HitFlash.anim`
- 精灵：`Enemy_1011_hit1..5.png`，末帧使用 `Enemy_1011_idle1.png`
- 6 键，0.15s/键，stopTime 0.9，不循环
- Controller 已绑定 HitFlash
- 运行时代码通过状态名播放

### 3.2 左受击 HitLeft

历史部署前记录：视频/选帧/抠图均已验收，随后已复制并部署到正式 `Assets/`：

- 暂存目录：`Library/Locus/tmp/sword_enemy_hit_left_v3/for_deploy/`
- 精灵：`Enemy_1011_hitLeft1..6.png`
- 暂存说明：`Library/Locus/tmp/sword_enemy_hit_left_v3/for_deploy/DEPLOY.md`
- 推荐后续 Clip：`Assets/Animations/Enemy_1011_HitLeft.anim`
- 推荐状态名：`HitLeft`
- 需要复制 `HitFlash` Clip 骨架后替换 6 个 Sprite 引用，并显式设置 stopTime 0.9

### 3.3 右受击 HitRight

历史原始素材记录：视频/选帧/透明抠图已验收；原始 GPT 输出几何未直接部署，后续已使用统一对齐后的正式版本接入：

- 选帧：`Library/Locus/tmp/sword_enemy_hit_right_v1/select6_right_v1/`
- 原始 GPT 输出：`C:/Users/Administrator/Pictures/gptGen/sword_enemy_hit_right_gpt_cutout/`
- 原始 GPT 几何报告：`C:/Users/Administrator/Pictures/gptGen/sword_enemy_hit_right_gpt_cutout/gpt_cutout_report_right_v1.json`
- 右受击视频：`C:/Users/Administrator/Videos/doubaoVideo/sword_enemy_hit_right_v1.mp4`
- 推荐后续 Clip：`Assets/Animations/Enemy_1011_HitRight.anim`
- 推荐状态名：`HitRight`
- 需要先做逐帧几何对齐，再补边到 1108，最后复制 `HitFlash` Clip 骨架

右受击原始 GPT 输出的宽高变化很大，实测 bbox 宽/高为：

```text
642/588, 797/728, 787/707, 747/723, 634/680, 727/781
```

所以不能直接复制进 `Assets/`。

## 4. 受击方向播放规则：已实现

> 本节下方保留的 `impactDirection` 来源与候选映射是实现前的调查记录；实际运行时已改为独立 `HitReactionDirection` 上下文，并以 §0 的最终规则为准。

受击方向已通过独立 `HitReactionDirection` 上下文传递，不再从几何 `impactDirection` 猜测所有攻击：

- Slash：右划固定 `HitLeft`，左划固定 `HitRight`。
- Jab1/Jab2/Jab3：分别为 Front/Left/Right；配置写入对应 `AttackSkillConfig`。
- 未指定的普通攻击/技能随机选择 Front/Left/Right。
- DoT、Launch、旋风击飞及其落地附加伤害不播放 hitted。
- SharedHealthGroup 会把同一次命中的方向传给成员反馈。
- Getup 期间的有效普通命中会替换 Getup；受击结束后恢复普通行为，不重播 Getup。

已核实的方向来源：

- `SweepEffect.cs`：传入 `Vector3.right` 或 `Vector3.left`，取决于 `leftToRight`。
- `StabSweepEffect.cs`：传入 `_rayDirection`。
- `AttackWave.cs`：Launch 类型传入相机上方向；Launch 应回退到正面受击，不应触发左右受击。
- 其他伤害源可能不传水平方向，应回退正面 HitFlash。

下一次实现时必须先确认 `impactDirection` 的语义是“力/运动方向”还是“攻击来源方向”，再确定符号映射。设计候选：

```text
无方向或 abs(x) < threshold -> HitFlash
水平向右的受力结果 -> HitLeft（力来自画面左）
水平向左的受力结果 -> HitRight（力来自画面右）
Launch/竖向方向 -> HitFlash
```

阈值、符号映射、共享血组成员反馈和 UnityEvent/测试场景都必须通过 Play Mode 实测确认，不能只按变量名猜。

## 5. Launch 播放/落地规则：已实现

当前运行时已接入：

- `Enemy.Launch()` 设置 `state = Launched` 并播放 `Launched_Rise`。
- `Launched_Rise` 通过 Animator 进入循环 `Launched_Fall`。
- 空中再次受击仍调用 `PlayLaunchVisual()` 回到 Rise。
- `Enemy.UpdateLaunch()` 确认真实落地后进入 `GettingUp` 并播放 `Launched_Getup`。
- 没有独立 Landing 状态；Fall 使用 `landing1..6` 六帧循环。
- Getup 被有效命中替换时，结束后回普通行为；死亡、再次击飞、对象池回收和落地回调均已处理。

完整流程：

```text
Launched_Rise -> Launched_Fall(loop) -> 真实落地 -> Launched_Getup -> Idle/普通行为
```

## 6. 后续维护与验证参考

### A. Launch 与受击部署已完成

1. 18 张 Sprite 已存在并按正式导入设置校验。
2. Rise/Fall/Getup Clip 已创建并接入；没有独立 Landing Clip。
3. Fall 使用 `landing1..6`，30fps、Loop、stopTime 0.6。
4. Controller 已接入 `HitLeft`、`HitRight`、`Launched_Getup`。
5. 运行时代码和方向规则已实现并通过 Play Mode 回归。

### B. HitLeft/HitRight 已完成

1. HitLeft 已从验收暂存目录部署并回读导入设置。
2. HitRight 使用原始选帧配合本地绿幕键控，统一补边到 1108；没有直接使用几何未对齐的 GPT 输出。
3. 两个 Clip 均为 6 键、30fps、stopTime 0.9、不循环，并接入 Controller。
4. 方向上下文、Jab1/2/3、Slash、随机回退和击飞/DoT 排除已实现。

### C. 最后做 Play Mode 实测

必须只选激活中的 Enemy 实例，避免对象池未激活克隆：

- `FindObjectsOfType<Enemy>(true)` 后筛 `activeInHierarchy`。
- 仅筛选激活中的实例；静态 Clip 序列通过 Animator.Play 读取，状态机/协程验证使用启用中的真实实例。
- 已验证 Rise → Fall → 真实落地 → Getup → 普通行为；未增加 Landing 段。
- 已验证 Front/Left/Right 映射、Getup 中断恢复；DoT、Launch、旋风击飞/落地伤害排除。
- 验证脚底、根位置、碰撞盒、血条和剑的列间重叠。
- 完成后恢复组件状态、退出 Play Mode、回读 Controller/Prefab/Clip 序列化值。

## 7. 已知工程约束

- Prefab：`Assets/Resources/EnemyPrefabs/Enemy_1011.prefab`
- Transform scale：`0.08316832`
- 碰撞盒与 101 同约定，世界尺寸约 `0.502 × 1.287 × 0.036`
- 血条 `EnemyHealthBar.yOffset = 35.27785`
- 1011 剑向画面左侧横伸约 1.7 世界单位，列距 1.0；动画部署不能通过横向重锚定掩盖这个美术事实。
- 当前测试排布：`SmallBattle.asset` 第 0 排为 `[101,101,1011,101,101]`，1011 在中心列。
- 当前编辑器状态：Edit Mode，Active Scene `Assets/Scenes/Battle.scene`；Play Mode 回归结束后已恢复 Edit Mode。

## 8. 关键文件索引

### 正式 Unity 资产

- `Assets/Sprites/Enemy/Enemy1_1/`
- `Assets/Animations/Enemy_1011.controller`
- `Assets/Animations/Enemy_1011_Idle.anim`
- `Assets/Animations/Enemy_1011_HitFlash.anim`
- `Assets/Animations/Enemy_1011_Attack.anim`
- `Assets/Animations/Enemy_1011_Dead.anim`
- `Assets/Animations/Enemy_1011_Walk.anim`
- `Assets/Resources/EnemyPrefabs/Enemy_1011.prefab`

### 来源/过程素材（正式资产已接入）

- `Library/Locus/tmp/sword_enemy_launch_v1/`
- `Library/Locus/tmp/sword_enemy_hit_left_v3/for_deploy/`
- `Library/Locus/tmp/sword_enemy_hit_right_v1/`
- `C:/Users/Administrator/Pictures/gptGen/sword_enemy_hit_right_gpt_cutout/`

### 规则/流程文档

- `Locus/knowledge/skill/workflows/character-hit-animation-video-workflow.md`
- `Locus/knowledge/skill/workflows/image-asset-generation.md`
- `Locus/knowledge/skill/workflows/local-green-screen-cutout.md`
- `Locus/knowledge/memory/video-generation-case-log.md`
- `Locus/knowledge/memory/enemy1011-animation-pose-reference.md`
