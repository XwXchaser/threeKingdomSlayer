---
id: kd_87dc14ba-875e-43e2-a2c6-bc1a18caeca9
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# 1021 胆怯家族四项动画：制作规格与部署合约

> **用途**：1021 破盾后进入 `Coward`（无盾胆怯）形态，该形态的四项动画仍是 102 占位或静态兜底。本文件是这四项的**制作规格（给素材侧）＋接入合约（给部署侧）**，两边按同一份口径交付。
> **分工（用户 2026-10-17 明确）**：素材的生成/选帧/抠图由另一侧负责；**本对话负责素材接入部署**。素材未到之前，本侧只做规格、接线预演与静态核验准备，不自造画面、不改机制。
> **机制与状态机权威**：`design/enemy-1021-shield-guard.md`。**动画矩阵权威**：`design/enemy-1021-animation.md` §2 第 14–17 行。接入步骤：`skill/workflows/action-to-unity-deployment.md`。

---

## 1. 为什么先做这四项

- 破盾是 1021 的核心玩法：玩家用反侧重攻击/精确攻击拆盾后，敌人**长期处于无盾形态**（胆怯是单向的，直到死亡或回池）。
- 现在这四项里，`CowardHit`/`CowardLaunched`/`CowardDead` 全部指向 102 的帧（`Assets/Sprites/Enemy/Enemy2/Coward/`），`CowardIdle` 只有一张破盾末帧的静态兜底。也就是说：破盾之后玩家看到的**主体形象不是 1021**。
- 相比之下 `Launched_Rise/Fall` 只在被挑飞时短暂出现，优先级低于胆怯家族。

---

## 2. 四项的执行顺序与状态契约

| # | 状态 | 播放入口（代码，均按名 `Play`，无需改代码） | 结束条件 | 建议时长/类型 |
|---|---|---|---|---|
| 1 | `CowardIdle` | `Enemy.PlayIdleVisual()` → `Play("CowardIdle")`；破盾结束、胆怯受击结束、击飞落地后都会回到它 | 循环 | 0.8s 循环（4 帧、0.2s 间距），可按读感调整 |
| 2 | `CowardHit` | `Enemy.PlayHitVisual()` 在 `IsCoward` 时 → `Play("CowardHit")` | `CowardHit → CowardIdle` hasExitTime 1.0、duration 0 → **clip 长度就是这次受击表现的总时长** | 0.26s 非循环（3–4 帧）；**v1/v2 已否，等另一侧改写词** |
| 3 | `CowardLaunched` | `Enemy.PlayLaunchVisual()` 在 `IsCoward` 时 → `Play("CowardLaunched")`；落地后由 `PlayIdleVisual()` 回 `CowardIdle` | 见 §4.2：控制器里有 `CowardLaunched → CowardIdle` exit 1.0 的出边 | **建议 2.0s、Clamp、1–2 帧保持**（与 `launchDuration = 2.0` 对齐） |
| 4 | `CowardDead` | `Enemy.PlayDeadVisual()` 在 `IsCoward`/`ShieldBroken` 时 → `Play("CowardDead")` | 无出边，末帧保持；随后由 `DeathBounceAndFall()`（0.3s＋0.8s）或 `LaunchDeathEffect()`（0.8s）接管 | 0.6s 非循环、末帧保持（与持盾 `Dead` 同口径） |

关键点：

- 四项**全部已存在于 `Enemy_1021.controller`**，接线只替换 motion，不改状态名、不改转场、不改参数。
- 1021 的胆怯**仍然前进**（`UpdateMovement` 不判形态），但 `StartMoving()` 里的 `Walk` 触发器只对 `formState == Normal` 生效 → **胆怯形态没有 Walk 动画**，被补齐推移时播放的就是 `CowardIdle`。因此 `CowardIdle` 不能设计成需要配合横向位移的循环（脚步必须原地、可接受"被推着走"的读感）。

---

## 3. 制作规格（给素材侧）

### 3.1 统一的身份与几何

- **身份基准**：沿用已验收的 1021 形象（`enemy1021_shield_hold_v3` 体系）；**无盾形态的基准姿态**是破盾末帧 `Assets/Sprites/Enemy/Enemy1021/Enemy_1021_shieldBreak6.png`（已接入、`CowardIdle` 兜底就用它）。四项都应能从这个姿态自然过渡出去。
- **盾**：破盾后木盾已由独立对象掉落并自毁，**本体四帧一律不得出现盾**，也不得出现"抓着盾"的手型。
- **画布与锚点**：源帧 960×960 RGBA；部署时按固定偏移 `(74, 91)` 零重采样补边到 1108×1108；只钉脚底、不做横向重锚定、不做逐帧缩放。四项共用同一变换。
- **抠图**：沿用已建立的 SVL Chroma 配方（T=80、softness 16、halo 1、半透明转不透明、不平滑）。半透明像素应为 0，透明区 RGB 应为 0。
- **命名**：`Enemy_1021_cowardIdle1..N`、`cowardHit1..N`、`cowardLaunched1..N`、`cowardDead1..N`（与 `hitFlash1..6` / `shieldBreak1..6` / `dead1..3` 同体例）。

### 3.2 逐项读感要求

**CowardIdle（无盾缩身待机，循环）**

- 读感：失去武器后的戒备与畏缩——上身前收、肩内扣、双手（空手）护在胸前或头侧、膝微屈、脚步不稳；可以有轻微起伏/颤动，但**不能有举盾或格挡的姿态**。
- 首尾帧必须可无缝循环（不要用"起手→收势"结构）。
- 相邻帧差异要小于持盾 Idle（胆怯是"紧绷但不怎么动"），但不能是纯静止图——要能看出呼吸/颤抖。

**CowardHit（无盾受击，非循环）**

- 读感：抱头/侧身挨打，身体被冲击带偏，首帧即为受击姿态（命中卡肉会延迟播放，首帧不能还是待机）。
- 末帧回到接近 `CowardIdle` 的姿态，避免切回时闪帧。
- 幅度要大于 `CowardIdle` 的呼吸幅度，能一帧看出"被打到了"。
- **已踩过的坑（v1/v2 均被否，作为硬约束）**：
  - v1：动作太小，抽帧看仍是待机护脸，没有受击感。
  - v2：视频里变成了**两个语义完全不一致的受击动作**——先抱胸、停顿约 0.35 秒、再甩臂，两段之间**切换僵硬**。逐帧实测：f016–f024 抱胸（主体框宽 299→264）、f025–f033 几乎静止、f033–f042 甩臂（框宽 265→399）。
  - v3（§0.16 “一次竖直压缩”）：先变高（f017–f020 顶部 +42 px），f021 压到 266×394 后**保持约 1.35 秒**，f054 一帧弹回。用户判据：**动作抽象、关键受击帧只是毫无表情的捂胸、没有受击感**。
  - v4（§0.17 受击峰值图）：宽比 **1.406**（高度只 1.016），宽度全靠一只手外甩。用户判据：**看着像摆超级英雄的 pose**。不得用作视频参考图。
  - 三次失败的共同点：“抱胸 / 护脸 / 捂胸”这类**防御或静态姿势**都不成立；需要的是**短、硬、一次性的冲击**（首帧即可读出被打）＋随后回到待机，不能有保持段、停顿段或两个独立姿态。
  - 结论：受击必须是**一次连贯动作**（起手→受力→收势），中间不得有停顿段，也不得让两个独立姿态先后出现；**不能靠“抽帧挑三帧”把两个动作硬拼成一段**。
  - v2 抽过的 3 帧候选（`f024`/`f036`/`f044`，`Library/Locus/tmp/enemy1021_coward_hit_v2/selected_v1/`）**不得用于部署**。

**CowardLaunched（无盾被挑飞，滞空）**

- 读感：空手、四肢张开、身体失力，与 `CowardDead` 必须**明显不同**（现在是同一批帧，读不出"被打飞"与"已阵亡"）。
- 这是**滞空保持**用的 clip（时长优先于帧数），不要做成需要多帧连播的短循环，见 §4.2。
- 不要带位移/旋转（根位移与翻滚由代码 DOTween 完成）。

**CowardDead（无盾死亡，非循环、末帧保持）**

- 读感：无盾倒地，剪影与持盾 `Dead` 明显不同（没有盾的宽块、重心与四肢落点不同），**不能直接复用持盾 `Dead` 的三帧**。
- 首帧仍是"被打死那一下"，末帧是稳定的倒地终态（因为要一直保持到对象掉出屏幕）。

### 3.3 交付与验收门禁

1. 有效帧先交用户验收（帧序、姿态），再抠图；
2. 透明结果单独交用户验收（半透明 0、幕布残留 0、核心区 RGB 受保护）；
3. 本侧只做接入＋配置＋静态核验，部署后通知用户开游戏验收；**未经用户验收不得标记为通过**。

---

## 4. 已知技术约束（会影响制作取值，务必先看）

### 4.1 `CowardHit` 的时长就是玩法时长

`CowardHit → CowardIdle` 是 hasExitTime 1.0 的自动出边，中间没有任何条件。所以 **clip 多长，这段受击表现就多长**；想改手感就改 clip 键位，不需要动代码。

### 4.2 `CowardLaunched` 有一个会提前切走的出边

现状（与 102 相同）：`CowardLaunched → CowardIdle` hasExitTime 1.0、duration 0、无条件。含义是"clip 播完就回胆怯待机"，**但击飞本身由 `launchDuration = 2.0` 与 DOTween 位移决定，可能远长于 0.3s**。102 的占位 clip 只有 0.297s，因此空中姿态不到 0.3s 就切成了待机图——这是占位素材的既有缺陷，不是设计意图。

部署侧的两种处理（**选哪种需要用户拍板**）：

- **A（默认、不改控制器结构）**：把 `Enemy_1021_CowardLaunched.anim` 做成 **2.0s、Clamp、1–2 帧保持**。出边要等 2.0s 才触发，覆盖正常击飞全程；落地时 `PlayIdleVisual()` 照常切回 `CowardIdle`。代价：极少数被反复延长浮空的场合，2.0s 后空中会切成待机图。
- **B（改控制器/代码）**：删掉这条出边，改由落地回调驱动（`Landing` 分支已经会 `PlayIdleVisual()`），并保证被反复击飞时重播。代价：动到了既有转场契约，需要单独的验收范围。

### 4.3 起身与眩晕不在本批

`Launched_Getup` / `CowardGetup` / 专属 `Stun` 在 1021 控制器里都不存在，落地直接回待机。这三项属于"待用户拍板"的独立批次，不要顺手做进胆怯家族。

### 4.4 旧分离层盾

四项 clip 都必须带 `ShieldVisual` 的 sprite **null 键**（与已接入动作一致），否则池复用或旧层级可能让盾重新出现。

---

## 5. 素材到达后的部署清单（本侧执行）

1. **记录保护文件哈希基线**（全部 `Enemy1021/*.png`、`Enemy_1021_*.anim`、控制器、Prefab、脚本、`Battle.scene`、关卡配置）。
2. 核对源帧：960×960 RGBA、半透明 0、透明 RGB 0、逐帧 SHA-256 与素材侧清单一致。
3. `(74,91)` 零重采样补边到 1108；粘贴区逐像素差应为 0，画布外非零应为 0。
4. 导入设置逐项镜像 `Enemy_1021_hitFlash1.png`（Sprite/Single/PPU16/Point/Uncompressed/无 Mipmap/Tight/Center），改完比对 `.meta` 应与参照仅差 guid 等记账字段。
5. 新建四个 Clip（命名 `Enemy_1021_CowardIdle/CowardHit/CowardLaunched/CowardDead.anim`），参数按 §2；含 `ShieldVisual` null 键；`CowardIdle` 之外的三个按非循环处理，`CowardLaunched` 用 Clamp。
6. 控制器**只替换四处 motion**；`CowardIdle` 从 `Enemy_1021_CowardIdle_Hold.anim` 改为新的正式循环（旧 `_Hold` 资产保留作回滚，不删）。核对其余 11 个状态 motion、18 条转场、6 个 Trigger 全未变。
7. 静态核验：Clip 长度/键位/末帧保持、精灵引用、`ShieldVisual` null 键、场景与关卡未改、102 个以上保护文件里只有控制器变化、Console 无新增 error。
8. 写 `Library/Locus/tmp/enemy1021_coward_deploy_v1/asset_verification.json`；通知用户开游戏验收。

---

## 6. 待用户拍板

1. `CowardLaunched` 走 §4.2 的 A（长保持 clip）还是 B（删出边、代码驱动）？默认按 A。
2. 四项的目标时长是否按 §2 的取值（0.8s 循环 / 0.26s / 2.0s / 0.6s）？
3. 胆怯形象基准是否就取破盾末帧 `Enemy_1021_shieldBreak6.png`（当前 `CowardIdle` 兜底姿态）？
