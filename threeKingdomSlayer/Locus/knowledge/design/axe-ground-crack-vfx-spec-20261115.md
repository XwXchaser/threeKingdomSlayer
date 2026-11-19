---
id: kd_2b2772a6-e742-4e8b-988c-28f84f63b351
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
id: kd_axe_ground_crack_vfx_spec_20261115
summary: 抡斧砸地裂纹特效的纯功能需求：触发时刻、落点算法、显现行为与时长、资源规格、预制体与材质契约、接线位置、可勾选的验收判据；不含画风/配色主张与任何生成结论。
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 抡斧砸地裂纹特效 —— 功能需求（2026-11-16）

> 本文件**只描述功能需求**：做什么、何时发生、出现在哪、持续多久、工程里怎么接、怎么验收。
> **不含**画风/配色/材质主张（那部分以 `design/art-style-guide.md` 为准，由美术自行判断），
> **不含**任何关于既有生成素材的评价或结论。

## 1. 功能

抡斧劈砍命中时，在斧刃与地面的接触处出现一次地面裂纹表现，并**从该接触点向外扩散显现**，随后淡出消失。
作用：为"重武器砸地"提供实体感与命中位置指示。

## 2. 触发

| 项 | 值 |
|---|---|
| 触发时刻 | 抡斧技能的**命中帧**（当前时间轴 t = 1.02s） |
| 触发次数 | 每次技能释放 1 次 |
| 触发时机 | 与伤害结算、闪白、命中音效、镜头推近**同帧** |

## 3. 位置

| 项 | 规则 |
|---|---|
| 落点 | **斧刃与地面的实际接触点**：取斧 mesh 在命中帧的最低顶点，其世界 XZ 投影到 y=0 |
| 计算时机 | 命中帧实时计算，**不写死坐标常量** |
| 随姿态变化 | 美术改动三帧姿态后，落点自动跟随，无需改代码 |
| 实测参考 | 命中帧落点 ≈ 世界 (−0.11, 0, −0.76)（屏幕 ≈ (519, 1080) @1080×2160） |

## 4. 表现行为

| 段 | 时长 | 行为 |
|---|---|---|
| 显现 | 0.16s | 从落点**向外**扩散显现（中心先出、边缘后出），扩散速度 Ease.OutCubic |
| 顶住 | 0.12s | 完整显示，保持不动 |
| 淡出 | 0.22s | 整体透明度 1→0，Ease.InQuad |

- 总时长 0.50s，结束后自毁。
- 显现方向：以落点为圆心**径向外扩**（不是整张图等比放大）。
- 显现范围：覆盖落点周围半径约 1.5 单位（世界直径 3.0，`AxeSmashPose.GroundCrackDiameter`）。
- 2.5D 平铺：贴图竖向压扁 `scale.y ≈ scale.x × 0.35`（相机俯角 18°）。
- 随机微转：Z 轴 ±8°，避免每次完全一致。

## 5. 资源规格

| 项 | 要求 |
|---|---|
| 数量 | **1 张**贴图（不做帧序列） |
| 尺寸 | 1024×1024 |
| 格式 | PNG，**真 Alpha**（背景 alpha = 0） |
| 构图 | 中心最密 → 向外分叉 → 外缘渐隐 |
| pivot | 贴图**正中心**＝命中点（导入后不得改 pivot） |
| 导入设置 | Sprite / Single / Point(100) / 无压缩 / 无 mipmap / Bilinear / Clamp / **`alphaIsTransparency = false`** / `alphaSource = FromInput` |
| 落地路径 | `Assets/Sprites/EffectSprites/axeCrack/AxeGroundCrack.png` |

## 6. 工程契约

| 项 | 要求 |
|---|---|
| 预制体 | `Assets/Prefabs/Effects/AxeGroundCrack.prefab`：根 `AxeGroundCrack`（挂 `GroundCrackEffect`）+ 子物体 `Crack`（`SpriteRenderer`） |
| 材质 | `Assets/Sprites/EffectSprites/axeCrack/AxeGroundCrack.mat`，shader `Custom/GroundCrackReveal` |
| shader 参数 | `_MainTex`（裂纹贴图）、`_Reveal`（0=完全隐藏，1=全显，径向遮罩）、`_Fade`（整体透明度）、`_Softness`（显现边缘硬度）、`_Color`（染色） |
| 排序 | `sortingOrder` 需高于战场地面元素、低于斧头本体（斧头为 100）；当前取值 14 |
| 生命周期 | 生成即播，0.50s 后自毁；不进入对象池 |
| 暂停行为 | 随游戏暂停冻结（`GameplayPause.Bind`） |

## 7. 接线

| 项 | 要求 |
|---|---|
| 配置入口 | `AxeSmashLevelConfig.effectPrefab`（`ActiveSkill_AxeSmash.asset` 的 3 个等级都指向该预制体） |
| 代码位置 | `ActiveSkillRunner.RunAxeSmash` 的命中帧（与闪白/音效/镜头推近同一处） |
| 调用形式 | `GroundCrackEffect.Play(worldPosition, worldDiameter)` |
| 落点计算 | `ActiveSkillRunner.AxeLowestRootLocal()`（缓存斧 mesh 最低点在斧根节点局部空间，运行时 O(1)）→ `TransformPoint` → y 置 0 |

## 8. 验收（功能判据）

**触发与位置**
- [ ] 释放技能后，裂纹只在**命中帧**出现一次
- [ ] 裂纹中心＝斧刃入地点（与斧刃最低点重合）
- [ ] 移动/修改三帧姿态后，裂纹落点自动跟随，无需改代码

**表现**
- [ ] 裂纹**从中心向外**扩散显现（不是整张图等比放大）
- [ ] 显现 0.16s、顶住 0.12s、淡出 0.22s，总 0.50s 后消失且无残留
- [ ] 表现随游戏暂停冻结，暂停结束后继续

**画面**
- [ ] 裂纹不遮挡战斗信息（敌人上半身、技能 UI、血条）
- [ ] 裂纹覆盖范围内不出现空白方块/穿帮边
- [ ] 实机 Game 画面里能看出"地面在此处被砸裂"

**技术**
- [ ] 贴图 alpha 生效（背景真透明，无白边/黑边）
- [ ] 材质 `_Reveal` 从 0 起（未播放时不可见）
- [ ] 预制体在场景中不残留（0.50s 自动销毁）

## 9. 范围之外

- 画风、配色、笔触、裂纹造型主张：见 `design/art-style-guide.md`（§4 像素化绘制语言、§5 色彩关系、§8 战斗特效），由美术判断。
- 音效、镜头推近、斧身闪白、顿帧：属于命中反馈的其他环节，本需求不涉及。
- 斧与敌人的相对位置：属于三帧姿态，本需求不涉及。
