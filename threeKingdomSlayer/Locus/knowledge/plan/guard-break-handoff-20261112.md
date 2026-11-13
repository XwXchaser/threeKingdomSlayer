---
id: kd_b89fe549-6fc9-41d9-ae29-07e806fd4b9b
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 格挡 / 破防表现链 · 会话交接（2026-11-12）

> **给下一个对话的入口。**
> - 2026-11-12（前半）：三个格挡链问题的**根因定位** + 修掉问题 1（收枪表现丢失）与问题 2 的「先后退」。
> - 2026-11-12（续）：用户验收问题 1（**已修复**）；反馈问题 2「不像玩家破防，像玩家主动收枪」，
>   并自己指出根因是「破防那一刻没有时间减缓」→ 按用户拍板补了**全局时缓（顿帧）+ 破防专用镜头档**。
> - 2026-11-12（第三/四轮）：用户指出「整个画面都在动、像地震」，且「**背景和场景的抖动不同步**」。
>   ⇒ 查出两个根因：**① 远景天空是钉在屏幕上的 clip 空间四边形**（相机一动它不动）；
>   **② 相机震动的旋转项 = 强度×80° + vibrato 14（≈127Hz）** ⇒ 竖屏第一人称下必然读成地震。
>   ⇒ 已改为：旋转 20°/单位、vibrato 4、总幅度 ×0.35（格挡档 ≈4.4 像素 / 0.64°）；
>   背景**只跟滚转、不跟平移**；并把 `CameraFeedbackController` 组件**正式落盘并启用**
>   （之前是未落盘的旧实例 ⇒ 改 C# 默认值与 Inspector 调参**都不生效**）。
> - 问题 3（受击缺卡肉）根因已定，且**实测更正**了玩家侧的表现载体（见 §3.1）。
> - ⚠ **仍未完成**：破防那一拍的**时停感 / 演出包**（按 PBZ 六层语法重建）—— 方案与逐层规格见
>   `design/guard-break-presentation-overhaul.md`，待办登记见 `plan/bugs-and-requests.md` **REQ-006**，
>   执行细节见本文 **§6（下一轮改造指引）**。
> ⚠️ **问题 2 的表现用户尚未在真实战斗里验收** —— 下个对话请先让用户在战斗里确认观感，再谈调参。

---

## 0. 一句话现状

链路 = **玩家防御 → 格挡受击 → 破防 → 收枪**。今天全部改动都落在
`PlayerWeaponStance`（持枪实体的姿态机）上，外加 `PlayerGuard` 的一行调用与 3 个资产参数。

| 问题 | 用户原话 | 状态 |
|---|---|---|
| 1 | 防御被命中后长枪后退，此时取消防御会**丢失收枪的表现**，应在对应的位置继续做收枪表现 | **✅ 已改，用户已验收** |
| 2a | 玩家被「瞬间破防」也应该**先做后退表现再破防** | **已改，待验收** |
| 2b | 破防「**不像玩家破防，像玩家主动收枪**」，可能因为破防那一刻没有时间减缓等效果 | **已加全局时缓+破防专用镜头，待验收** |
| 3 | 玩家受击除了长枪震动外**缺少卡肉停顿**（敌人侧也像没有），打击感很弱 | **根因已定位，待修** |

---

## 1. 改动清单

### 1.1 问题 1：收枪表现丢失（根因：后坐与收枪**方向相反、互相抵消**）

- **根因三层**
  1. `MoveTo` 的起点取的是**原始插值位置**（`CurrentRoot()`，不含后坐/应变），
     而后坐/应变是 `ApplyPose` 里**逐帧叠加**的层 ⇒ 收枪不是「从枪现在在的地方继续」。
  2. **后坐是 −Z（朝镜头）、收枪是 +Z（进画面深处）** ⇒ 两者几乎抵消
     ⇒ 实测：修前松手后 16 帧只挪 0.64，且那些位移**全部来自偏移层**，收枪自己的插值几乎没显形。
  3. 资产里的收枪参数本身偏弱（见 §4 踩坑）。
- **修法**：`MoveTo(..., absorbOffsets: true)`
  - 起点改取**视觉位置**（含后坐/应变/抖动）；
  - 并把叠加偏移**吸收**（归零）进起点 ⇒ 轨迹不再被衰减中的偏移污染。
  - `BeginExit()` 用这个模式；`BeginBreak()` 同理（但问题 2 改成"不吸收后坐、留��等待期播"）。
- **实测（修后）**：同一场景（挡一下→立刻松手）
  `z −5.16 → −4.34 → −3.58 → −2.88 → −2.25 → −1.70 → +1.15`、
  `x 0.71 → −2.47`、`y 1.64 → −1.13` ⇒ **真的挥出画面了**。

### 1.2 问题 2：瞬间破防应先做后退（根因：受击反馈与破防反馈**并行**）

- **根因**：`PlayerGuard.TryAbsorbDamage` 同一帧里先 `Recoil()`（0.18s×倍率）再 `Break()`，
  破防动画立刻接管整杆枪的姿态 ⇒ 0.18s 的后退被 0.55s 的旋转盖住。
- **修法**（`PlayerWeaponStance`）
  - 拆出 `ComputeBase(t)`（原始插值 + 应变，**不含后坐**），`ComputeVisual` = base + 后坐；
  - `BeginBreak()` 的破防起点用 **base**（不含后坐）、且**不归零后坐** ⇒ 后坐留给等待期自己播；
  - 新增 `_brokenWaitForRecoil`：破防后**等 `_recoilTimer` 归零**才起旋转
    （等待期 `_brokenT` 仍为 0 ⇒ 旋转/漂移都没开始，但「被顶开」的位移可见）；
    后坐播完时把后坐归零再起旋转 ⇒ 无跳变。
- **实测（一次 120 伤同时触发后坐与破防）**
  ```
  逐帧 (z | 枪身轴角)：[-5.49|38°] [-5.47|38°] [-5.42|38°] [-5.37|38°] [-4.70|56°] [-4.70|60°] …
  ⇒ 前 4 帧（= 后坐 0.29s 全程）角度恒定 38°（旋转没提前），后坐结束那帧才开始转
  ```
  ⇒ 现在顺序：**被顶开 → 弹回 → 旋转脱手出画**。

### 1.3 顺带做的（上一轮用户提的「恢复不线性」）

- 下沉/枪尖下垂改为**线性跟随 `MeterRatio`**（去掉档位闸门）⇒ 回落跨 0.8/0.45 两档不再跳变；
  ⚠ 代价：「稳档 = 偏移 0」这条 2026-11-09 的口径**不再成立**（低档也按比例微沉）。
- 临界抖动加**淡入淡出包络**（`_shakeBlend`）；格挡后退收尾改**平滑插值**（最后一帧不归零跳变）。
- 实测：90 帧强制衰减 `maxΔY/帧=0.0123  maxΔrot/帧=0.00°  跳变帧=0`。

### 1.4 破防专属动作（2026-11-12 用户定，已实现在上一轮）

- 新增 `StancePhase.Broken` + `BeginBreak()`；`PlayerGuard.Break` 由 `BeginExit` 改调它。
- 动作 = **世界空间**绕 `Vector3.forward` 前乘旋转（屏幕 +X 右 / +Y 上 ⇒ 正向 Z 旋转 = **屏幕逆时针**）
  + `Vector3.left` 轻微漂移；因支点偏移用 pose 解算 ⇒ **枪尾留在原地、整杆翻转**。
- 实测：枪身轴 42°→73°→111°→156°→204°→255°→268°（连续递增），累计 226°（配置 230°），
  枪尾最大位移/帧 0.0246，枪最终 x=−8.9 y=−7.8（出画）⇒ 出画后销毁。

### 1.5 新增资产参数 + 调参窗口

`AttackSkillConfig` 新增 3 个字段，并已接进 `Tools ▸ 三国杀戮 ▸ 招架姿态调参`（`ParryStanceTuningWindow`）：

| 字段 | 默认 | 含义 |
|---|---|---|
| `parryStanceBreakSpinDegrees` | 230 | 破防绕世界 Z 的旋转角（正 = 屏幕逆时针） |
| `parryStanceBreakDrift` | 0.8 | 破防向左漂移（世界单位） |
| `parryStanceBreakSeconds` | 0.55 | 破防动作时长 |

### 1.6 动的文件

```
M Assets/Scripts/Player/Combat/PlayerWeaponStance.cs   ← 主要改动
M Assets/Scripts/Player/Combat/PlayerGuard.cs           ← 2 行（Break 改调 BeginBreak；其余是更早会话的 WIP）
M Assets/Scripts/Data/AttackSkillConfig.cs              ← +3 字段
M Assets/Scripts/Editor/ParryStanceTuningWindow.cs     ← +3 滑条
M Locus/knowledge/design/guard-break-feedback-20261109.md   ← §5.2 / §5.3
M Locus/knowledge/memory/known-issues.md                     ← #25
```

### 1.7 ★ 第二轮（2026-11-12 用户实测：「没有顿帧感，一点都没有受击卡顿」）—— ❌ **方向已被推翻，见 §1.9**

- **用户反馈**：破防时完全没有顿帧感。
- **当时的根因判断（两条）**：
  1. **0.1× 的慢速里没东西在动**：攻击者敌人已被自己的 0.12s 卡肉冻住、其它敌人静止、
     枪只退 0.5 单位 ⇒ 没有「被拖慢」的对象。
  2. **镜头震动走 unscaled** ⇒ 顿帧期间屏幕在**满速抖动** ⇒ 眼睛读到的是「震动」不是「定格」。
- **当时的修法**（全局时缓 0.02 + 镜头改 scaled）—— **方向错误，已全部撤回**：
  顿感来自**对比**（被打的定住、其它还在动）；全局慢动作让所有东西一起停 ⇒ 读成掉帧。
  ⇒ 详见 `memory/hit-feedback-system.md` §「2026-11-12：方向复盘（局部优先）」。
- ⚠ **仍然有效的教训**：新字段的 C# 默认值改了**不生效** —— Unity 域重载会保留 ScriptableObject 的
  内存值 ⇒ 必须写进 `Zhangfei_Parry.asset` 并 SaveAssets。

### 1.9 ★★ 第三轮（2026-11-12，**当前实现**）：局部顿 + 受击慢放，全局时缓已删除

**用户决定**：Layer C（命中点火花/枪闪白/屏幕红闪）先不做；**Layer B（受击慢放）要做**；
Layer D（镜头范围）待定 ⇒ 本轮先把上一轮我自己加的「破防专用更重档」撤回。

| 改动 | 落点 | 说明 |
|---|---|---|
| **命中定格（局部顿）** | `PlayerWeaponStance.ApplyHitStop(seconds)` + `Tick` 早退 | 只把**持枪姿态**定住，**世界照常跑**；格挡 0.033s（≈2 帧）/ 破防 0.066s（≈4 帧），**真实时间**计时 |
| **破防受击慢放** | `PlayerWeaponStance._breakSlowRemaining/_breakSlowScale` | 只把持枪姿态放慢到 0.35×，持续 0.15s 真实时间；世界照常 |
| **后坐命中即写入** | `PlayerWeaponStance.Recoil()` | 原来偏移是在衰减段里算的 ⇒ 定格帧显示的不是「被顶开」的姿态，而是定格结束后突然跳出去；现在命中即写入 |
| **接线** | `PlayerGuard.TryAbsorbDamage` / `Break` | 格挡与破防各调一次 `ApplyHitStop`（破防更长一档） |
| **删除全局时缓** | 删 `CombatTimeDilation.cs` / `CombatTimeDilationRunner.cs`；回滚 `CameraFeedbackController.PlayFeedback` 的 scaled 改动 | 与项目既有口径一致（`memory/hit-feedback-system.md`：局部表现暂停，不修改全局 timeScale） |
| **撤回破防更重镜头档** | 删 `CameraFeedbackController.RequestGuardBreak` + 3 个参数 | 它的旋转项（`intensity×80°` = 6°）是「整个世界在抖」的最大来源 |
| **参数落资产** | `AttackSkillConfig` + `ParryStanceTuningWindow` + `Zhangfei_Parry.asset` | `parryGuardBlockHitStopSeconds 0.033` / `parryGuardBreakHitStopSeconds 0.066` / `parryGuardBreakSlowScale 0.35` / `parryGuardBreakSlowSeconds 0.15` |

**实测（真实 player loop 协程采样）**：
```
格挡 逐帧枪位移: 0.506 0.003 0.013 0.034 0.048 0.060 0.069 0.068 0.067 0.059 0.046 0.029 0.008 0.003
      镜头峰值=0.0339  全局 timeScale 峰值=1.000
破防 逐帧枪位移: 1.116 0.000 0.000 0.000 0.001 0.003 0.005 0.006 0.008 0.010 0.011 0.012 0.014 0.015
      0.049 0.057 0.062 0.069 …
      镜头峰值=0.0344  全局 timeScale 峰值=1.000
```
⇒ 破防定格帧枪身**零位移**（3~4 帧）→ 慢放爬回 → 起旋转；**全局时间全程 1.000**。

**镜头范围实测（相机 (0,3,-10)，fov 60，aspect 0.5；单位=屏幕宽）**：
| 镜头动作 | 地形(z=0) | 天空(z=300) | 敌人(z≈9) |
|---|---|---|---|
| 平移 0.035（普通格挡档） | -0.0061 | -0.0002 | -0.0032 |
| **旋转 2.8°**（`intensity×80°`，普通格挡档） | **-0.0220** | **+0.0001** | -0.0134 |
| **旋转 6.0°**（上一轮的破防档） | **-0.0471** | **+0.0001** | -0.0286 |

⇒ **「地面在晃、天空钉住」的主因是镜头的 Z 轴旋转项**；且背景补偿是**死代码**
（`worldBackground` 指向已禁用的 `Canvas/background`，真正可见的天空是 mesh `Skybox Background - editable`）。

### 1.8 续（全局时缓 + 镜头升级）：**❌ 已被 §1.9 取代**

<details><summary>（存档）当时的改动清单</summary>

```
A Assets/Scripts/Core/CombatTimeDilation.cs         ← 新增：全局时缓（顿帧）工具 + 守护规则
A Assets/Scripts/Core/CombatTimeDilationRunner.cs   ← 新增：时缓驱动组件（unscaled 时钟）
M Assets/Scripts/Core/CameraFeedbackController.cs   ← +RequestGuardBreak()、3 个破防镜头参数、
                                                       以及「顿帧期间镜头跟着停」(PlayFeedback 改 scaled)
M Assets/Scripts/Data/AttackSkillConfig.cs          ← +3 字段（时缓保持/倍率/回正）
M Assets/Scripts/Editor/ParryStanceTuningWindow.cs  ← +3 滑条
M Assets/Scripts/Player/Combat/PlayerGuard.cs       ← Break() 里触发时缓 + 破防镜头
M Assets/Prefabs/UI/Skills/Zhangfei_Parry.asset     ← 真值写入：0.1 / 0.02 / 0.1
```

</details>

### 1.10 当前（§1.9）的改动清单

```
M Assets/Scripts/Player/Combat/PlayerWeaponStance.cs  ← ApplyHitStop() + 命中定格早退 + 受击慢放 + Recoil 命中即写入
M Assets/Scripts/Player/Combat/PlayerGuard.cs          ← 格挡/破防各调一次 ApplyHitStop
M Assets/Scripts/Core/CameraFeedbackController.cs      ← 回滚 scaled 改动、撤回 RequestGuardBreak
M Assets/Scripts/Data/AttackSkillConfig.cs             ← +4 字段（格挡定格/破防定格/慢放倍率/慢放时长）
M Assets/Scripts/Editor/ParryStanceTuningWindow.cs     ← +4 滑条
M Assets/Prefabs/UI/Skills/Zhangfei_Parry.asset        ← 真值写入：0.033 / 0.066 / 0.35 / 0.15
D Assets/Scripts/Core/CombatTimeDilation.cs            ← 删除（全局时缓）
D Assets/Scripts/Core/CombatTimeDilationRunner.cs      ← 删除
M Locus/knowledge/memory/hit-feedback-system.md         ← 方向复盘 + 闪白发现 + 镜头范围实测
M Locus/knowledge/design/guard-break-feedback-20261109.md
M Locus/knowledge/memory/known-issues.md                ← #25
```
### 1.11 ★ 第四轮（2026-11-12）：Layer D —— 镜头「震动范围」

**用户意见**：「一般动作游戏攻击命中不会让整个世界都震动吧？想想震动范围」+「都先接上，我看看效果」。
**先做的功课**：读了当前游戏设计（`design/game-mechanics.md` 竖屏单手一夫当关、`design/curved-scroll-travel-experiment.md` 远景层）
与 `YSkyBackground.shader`；关于《刀锋》的视角**本项目没有可用参考资料**（知识库里只有其战斗/数值逆向资料）。

**两个关键发现**：
1. **远景天空是钉在屏幕上的 clip 空间四边形**（shader 手写 clipXY、不过投影矩阵）⇒ 相机一动它不动 ⇒ “地动山摇”。
2. **`CameraFeedbackController` 组件在 Battle 场景里 `enabled=false`，而且根本没落盘**（脚本 GUID 不在任何场景/预制体里）
   ⇒ 它的 `LateUpdate` 从不跑 ⇒ **旧版背景补偿是双重死代码**（对象禁用 + 组件禁用）。

**改动**：
| 文件 | 改动 |
|---|---|
| `Assets/Experiments/CurvedScroll/Shaders/YSkyBackground.shader` | 加全局 `_LocusScreenShake`（xy=clip 位移，z=滚转），在 Y 翻转**前**应用 |
| `Assets/Scripts/Core/CameraFeedbackController.cs` | 删死代码背景补偿；新增 `UpdateBackgroundFollow()`（挂在**震动 Tween 的 OnUpdate** 上，不能用 LateUpdate）；新增 `RequestGuardBreak()`（0.14s/0.045/18，比格挡时长略长、幅度更小）+ 运行时兜底 |
| `Assets/Scripts/Player/Combat/PlayerGuard.cs` | `Break()` 里接上 `RequestGuardBreak(impactPos)` |

**实测**：震动期间全局参数被写入并随相机变化（`g.y=0.0055` 对应相机 y 偏移 -0.035，理论值 0.006 ✓）；震完自动归零 ✓；Game 截图确认背景渲染未破。

⚠ 工作区**本来就是脏的**（美术落地、配置表管线、骑兵改造等来自更早会话）⇒ 上面只列本次动的。

---

## 2. 验证配方（下个对话可直接复跑，Editor Play 脚本驱动）

Battle 场景 Play Mode；`ps.freezeWeaponStance = true` 接管姿态 Tick（否则游戏自己的 Tick 会用
`DefendHeld=false` 把防御位收掉）：

```csharp
var ps  = UnityEngine.Object.FindObjectOfType<PlayerState>();
var st  = ps.WeaponStance;
var cfg = ps.heroConfig.GetSkillConfig(AttackType.Parry);
ps.freezeWeaponStance = true;
System.Action<bool> drive = held => st.Tick(Time.unscaledDeltaTime, held);

st.PlayParry(cfg);
for (int i = 0; i < 50; i++) { await ctx.WaitFrame(); drive(true); }   // 落防御位

// ① 问题1：挡一下 → 立刻松手，采样收枪轨迹
ps.Guard.TryAbsorbDamage(12f);
// ② 问题2：一次 120 伤同时触发后坐与破防，采样 (z | 枪身轴角)
ps.Guard.TryAbsorbDamage(120f);
// 采样：var w = GameObject.Find("StanceWeapon"); w.transform.position / w.transform.rotation * Vector3.up
```
⚠ `GameObject.Find("StanceWeapon")` 是武器实例名（`EnsureWeapon` 里命名）。
⚠ Editor 里帧率低 ⇒ **按帧数推时间会错**，要按 `Time.unscaledDeltaTime` 折算。

### 2.1 ⚠ 逐帧采样必须跑在**真实 player loop** 里（续）

**`unity_execute` 片段里的 `await ctx.WaitFrame()` 循环不执行 MonoBehaviour 的 Update**
⇒ 在片段里逐帧采样会得到**失真**的数据（时缓不推进、相机照动、姿态被 unscaled 驱动）。
本轮踩过：第一次采样看到「ts 恒为 0.1 永不变」其实是假象（片段期间时缓的驱动组件没跑）。

**正确姿势**：把采样写成协程挂在场景里的 MonoBehaviour 上（如 `CameraFeedbackController`），
用 `yield return null` 逐帧推进并 `Debug.Log`；片段只负责启动协程，然后**等几秒**再去读 Console。

```csharp
var ps = UnityEngine.Object.FindObjectOfType<PlayerState>();
var st = ps.WeaponStance;
var cfg = ps.heroConfig.GetSkillConfig(AttackType.Parry);
ps.freezeWeaponStance = true;                 // 接管姿态 Tick
st.PlayParry(cfg);                            // ★ 先生成持枪实体，否则 gun=null

System.Collections.IEnumerator Recorder()
{
    for (int i = 0; i < 45; i++) { st.Tick(Time.deltaTime, true); yield return null; }  // 落防御位
    ps.Guard.AccumulatedDamage = 119f;
    ps.Guard.TryAbsorbDamage(120f);            // 触发破防
    for (int i = 0; i < 50; i++)
    {
        st.Tick(Time.deltaTime, true);         // ★ 用 **scaled** deltaTime，与真实战斗一致
        var w = GameObject.Find("StanceWeapon");
        Debug.Log($"[BT] f{i:00} ts={Time.timeScale:F2} dil={CombatTimeDilation.IsActive} " +
                  $"axis={Vector3.SignedAngle(Vector3.up, w.transform.rotation * Vector3.up, Vector3.forward):F0}");
        yield return null;
    }
}
UnityEngine.Object.FindObjectOfType<CameraFeedbackController>().StartCoroutine(Recorder());
```
⇒ 跑完后用 `unity_get_console_log(level:"info")` 读 `[BT]` 行；**不要**在片段里 await 采样。

---

## 3. 待办 / 待拍板

### 3.1 问题 3（待修）：受击缺卡肉

- **根因**：卡肉体系（`HitFeedbackManager`）**只覆盖「玩家打敌人」**——
  `HitFeedbackContext.enemy` 指的是「**被打的那个敌人**」⇒ `ApplyHitStop` 挂起的是**敌人的 Animator**。

  | 方向 | 受击者 | 攻击者 | 镜头 |
  |---|---|---|---|
  | 玩家打敌人 | 敌人 Animator 卡肉 + 缩放 + 闪白 ✓ | 玩家攻击视觉同步暂停 ✓ | ✓ |
  | **敌人打玩家（真挨打）** | **只有边框闪 + 镜头 heavy 抖**（`PlayerHitFeedback`） | **完全没有** | ✓ |
  | 敌人打玩家（防御挡下） | 镜头重击抖 ✓ | **有** `source.ApplyHitStop(0.12f)`（`PlayerGuard` 里手动加的） | ✓ |

- **两个方向**（待用户选）：
  1. **攻击者侧**（改动小、收益直接）：玩家真挨打时也给攻击的敌人 `ApplyHitStop` + 相机冲击。
  2. **玩家侧**：⚠ **实测更正（2026-11-12）**：Battle 场景里玩家对象 `Player` 的组件只有
     `Transform + PlayerHitFeedback + ThornArmorEffect + ChargeStabVisual + PierceAimIndicator + PlayerMoveStateMachine`
     —— **连 SpriteRenderer 都没有** ⇒ 「玩家侧缩放挤压 / 闪白 / Animator 停速」**没有载体**；
     玩家侧实际可用的手牌只有四样：**枪的姿态 / 屏幕（`Assets/Sprites/BatlleHUD/hitted.png` 全屏红边框图）/ 镜头 / 时间**。
     ⇒ 本轮的 `CombatTimeDilation`（全局时缓）就是第 4 张牌，「玩家真挨打」可直接复用
     （世界挂一拍 + 攻击者停住），**不需要**照搬敌人的 Animator 停速。
- 设计依据：`design/combat-hit-feedback-direction.md` 早已写明要「受击者/攻击者/镜头」三层协同，
  并提醒「**不要把『敌人停了』误认为完整打击感**」——现状正好只做了其中一层的一个方向。

### 3.2 其它可选项（等用户定）

- **资产里的收枪参数偏弱**：`parryStanceExitRootOffset = (0,0,0.7)`、`parryStanceExitAngleDegrees = 30`
  （斜举角是 42）⇒ 收枪本质是「原地翻 90° 滚一下」，
  与文件头注释里「离开要挥出画面后才销毁」的硬规则不符。⇒ 要不要改成真出画？
- 破防的**旋转幅度/漂移/时长**（230°/0.8/0.55s）都还没被用户过目。
- 破防的**时缓参数**（保持 0.1s / 倍率 ×0.1 / 回正 0.1s）同样没过目；
  若用户反馈「还不够」或「太拖」，优先调这三条（都在 `Zhangfei_Parry.asset`，可用招架姿态调参窗口改）。
- 本轮用户**明确没选**的三项（随时可加）：屏幕红边框闪 / 破防动作的失控感（起手猛冲 + wobble）/ 破防音效降调。

---

## 4. 踩坑清单

1. **parry 姿态参数的真值在资产，不在代码默认值**：
   `Zhangfei_Parry.asset` 与 `AttackSkillConfig` 的 C# 默认值差很多
   （`ExitRootOffset (0,0,0.7)` vs `(-1.2,-1.5,0)`、`ExitAngle 30` vs `-100`、
   `EntryAngle 30` vs `-48`、`EntryRoll 90` vs `0`、`HeadAnchor (4.7,5.5,-4.7)` vs `(1.10,2.63,-5.5)`），
   且 `parryGuardWeaponPrefab` 是空的（走 `attackWavePrefab` 兜底）⇒ 调参只看代码会得出错误结论。
2. **玩家没有 Animator**（Battle 场景里是 SpriteRenderer）⇒ 玩家侧卡肉/受击表演不能照搬敌人那套。
3. **`HitFeedbackManager` 的 `context.enemy` 是「被打的那个敌人」**，不是「攻击者」⇒
   想给攻击者反馈必须显式传 `source`（`PlayerGuard` 就是这么做的）。
4. **格挡后坐是「瞬间跳开 + 平滑弹回」**，不是「推出去停住」⇒ 若要做「真实的顶开」得另写一段。
5. `PlayerState.SetDefend(true)` 在**非战斗状态**会被硬门拦下（`CanAcceptDefendIntent` 要求
   `stageState == InProgress`）⇒ 脚本实测要用 `freezeWeaponStance` + 手动 Tick，不能靠 `SetDefend`。
6. 破防起点用 `ComputeBase`（不含后坐）而不是 `ComputeVisual`（含后坐）：否则等待期会**双算**后坐。
7. **`unity_execute` 片段的 `await ctx.WaitFrame()` 循环里 MonoBehaviour 的 Update 不跑**
   ⇒ 片段内逐帧采样失真（时缓看起来"永不回正"、镜头却在动）。
   采样必须写成**挂在场景 MonoBehaviour 上的协程**（真实 player loop）+ 事后读 Console（见 §2.1）。
8. **用 unscaled deltaTime 驱动姿态会与 timeScale 脱钩**：时缓/慢动作类实验必须用 `Time.deltaTime` 驱动。
9. **`Time.timeScale` 是全局共享资源**：`Time.timeScale = 0` 会被教学冻结/对话/三选一/暂停菜单当「已暂停」
   （`GameplayPauseWatcher` 判 `timeScale <= 0`）；时缓用 **0.1** 而不是 0，就绕开了这个判定
   （输入门、`ActiveSkillInventory`、药水等也都只拦 `== 0`）。
10. **改 ScriptableObject 新字段的 C# 默认值 ≠ 生效**：Unity 域重载会**保留 ScriptableObject 的内存值**
    ⇒ 资产里没序列化的新字段会一直用旧值 ⇒ 必须写进资产 + `AssetDatabase.SaveAssets()`。
11. **慢动作 ≠ 顿帧**：只有「被拖慢的东西」才会被读出来。场景里没东西在动时（静态背景、静止敌人、
    已被卡肉的敌人）慢动作完全不可见；而且任何走 **unscaled** 的表现（镜头震动、伤害数字、UI）
    会在慢动作期间**满速继续**，把「顿」读成「抖」⇒ 顿帧必须把这类表现也改成跟着停。

---

## 5. 详细记录位置

- `design/guard-break-feedback-20261109.md` §5.2（连续化 + 破防脱手 + 实测）、§5.3（三个问题的根因/修法/状态）。
- `memory/known-issues.md` #25（索引条目）。
- `memory/hit-feedback-system.md`（打击反馈体系的权威口径 + 本轮四次方向复盘）。
- `design/guard-break-presentation-overhaul.md`（**下一轮要做的破防演出重做：方案 A 六层规格**）。
- `design/pbz-reference-analysis-20261109.md`（《影之刃零》研究）。
- `plan/bugs-and-requests.md` **REQ-006**（待办登记，自包含）。

---

## 6. ★ 下一轮改造指引（2026-11-12 交接 · 详细）

### 6.0 一句话

上一轮把「破防像主动收枪」「整个画面在抖」这两个方向性问题都解决了；**剩下的是"破防那一拍没有时停感"**，
根因与逐层改造方案见 `design/guard-break-presentation-overhaul.md`（本节只给执行层面的细节）。

### 6.1 上一轮到底改了什么（当前代码状态）

| 文件 | 改动 | 状态 |
|---|---|---|
| `Assets/Scripts/Player/Combat/PlayerWeaponStance.cs` | ① `ApplyHitStop(float)`：把**持枪姿态**定住 N 秒（真实时间），世界照常跑；② `_breakSlowRemaining/_breakSlowScale`：破防时只把持枪姿态放慢（0.35×/0.15s）；③ `Recoil()` 命中即写入偏移（否则定格帧显示的不是"被顶开"的姿态）；④ `Reset()` 清理新状态 | ✅ 已验收方向，待实机观感确认 |
| `Assets/Scripts/Player/Combat/PlayerGuard.cs` | `TryAbsorbDamage` 调 `ApplyHitStop(parryGuardBlockHitStopSeconds)`；`Break(cfg, impactPos)` 调 `ApplyHitStop(parryGuardBreakHitStopSeconds)` + `RequestGuardBreak(impactPos)` | ✅ |
| `Assets/Scripts/Data/AttackSkillConfig.cs` | +4 字段：`parryGuardBlockHitStopSeconds 0.033` / `parryGuardBreakHitStopSeconds 0.066` / `parryGuardBreakSlowScale 0.35` / `parryGuardBreakSlowSeconds 0.15` | ✅ |
| `Assets/Scripts/Editor/ParryStanceTuningWindow.cs` | +4 滑条（同一窗口调） | ✅ |
| `Assets/Prefabs/UI/Skills/Zhangfei_Parry.asset` | 真值写入 0.033 / 0.066 / 0.35 / 0.15 | ✅ |
| `Assets/Scripts/Core/CameraFeedbackController.cs` | 删死代码背景补偿；新增 `UpdateBackgroundFollow()`（挂**震动 Tween 的 OnUpdate**）；新增 `RequestGuardBreak()`；新增 3 个震动参数 `shakeIntensityScale 0.35` / `rotationDegreesPerUnit 20` / `rotationVibrato 4`；`PlayFeedback` 去掉按档位的 vibrato | ✅ |
| `Assets/Experiments/CurvedScroll/Shaders/YSkyBackground.shader` | 新增全局 `_LocusScreenShake`（xy=clip 位移，z=滚转），在 Y 翻转**前**应用 | ✅ |
| `Assets/Scenes/Battle.scene` | **`CameraFeedbackController` 组件正式落盘**（`Main Camera`，`m_Enabled: 1`），参数写进场景 | ✅（**必要的**：见坑 12） |

**已删除**：`Assets/Scripts/Core/CombatTimeDilation.cs`、`CombatTimeDilationRunner.cs`（全局时缓整套）。

### 6.2 当前实测基线（下一轮回归对照用）

| 档位 | 最大位移（世界单位） | 1080 宽像素 | 最大滚转 | 频率 |
|---|---|---|---|---|
| 格挡 / 重击 | **0.0118** | ≈4.4 px | **0.64°** | ~36Hz（4 次/0.11s） |
| 破防 | ≈0.015 | ≈5.6 px | ≈0.83° | 同上 |
| 玩家受伤 | ≈0.024 | ≈9 px | ≈1.23° | 同上 |

> 改前（地震）：0.035~0.07 / 2.8~5.6° / 127Hz。

**破防链的时序**（60fps）：定格 4 帧（枪完全零位移）→ 慢放 0.35× 爬回 → 起旋转脱手；**全局 `Time.timeScale` 全程 1.000**。

### 6.3 下一轮要做的事（REQ-006 · 方案 A 三批）

> 完整规格见 `design/guard-break-presentation-overhaul.md` §3；此处只列执行要点。

**A1 被打方（敌人侧）—— 最重要的第一批**
- 目标：破防时**攻击者**「被顶住/被打慢」，而不是"我的动作停了"。
- 现状：`PlayerGuard.TryAbsorbDamage` 里 `source.ApplyHitStop(0.12f)` → `Enemy.PushSuspend` → `_animator.enabled = false`（**冻结**）。
- 要做：破防时改为「冻结 0.25s → `Animator.speed = 0.25` 慢放 0.35s → 回 1」，并让攻击者播一个受击动画
  （敌人现成 `Enemy.PlayHitVisual()` → `HitFlash`/`HitLeft`/`HitRight`）。
- 落点：`Assets/Scripts/Enemy/Enemy.Combat.cs`（`PushSuspend`/`PopSuspend`）与 `EnemyHitFeedback.cs`；
  新增一个与 `SuspendSource.HitStop` 并列的挂起来源（或给 `ApplyHitStop` 加 speed 参数）。
- ⚠ **坑**：慢放只改 `Animator.speed`（**不改攻击判定时钟**）⇒ 会出现"动作慢但判定正常"的错拍；
  要么把判定也纳入慢放，要么接受并实测。慢放结束必须还原 `speed = 1`（对象池复用会带脏值）。
- ⚠ **坑**：敌人正在攻击 ⇒ 受击动画会打断攻击表现 ⇒ 需确认可接受（PBZ 里成功格挡确实让对方武器被弹开）。

**A2 命中确认（闪白 + 火花 + 红边框）**
- 枪闪白：给 `Assets/Shaders/WeaponColorUnlit.shader` 加 `_FlashStrength`
  （`c.rgb = lerp(c.rgb, white, _Flash)` 后 `c.rgb *= c.a`），由 `Assets/Scripts/Effects/MeshWeaponVisual.cs`
  用 `MaterialPropertyBlock` 驱动（**不实例化材质**）。
  参照敌人 `Assets/Shaders/EnemyOutline.shader` 的 `_PierceHighlightStrength` + `EnemyOutline.cs:63` 的现成范式。
  ⚠ **乘法混合的 `_Color` 变不亮** ⇒ 只能新增通道。
- 火花：`Assets/Scripts/Core/PixelHitEffectManager.cs` 的 `RequestHit(context)`（现成通路）；
  需要**接触点坐标**（现在只有 `source` 敌人 ⇒ 取敌人的受击点/武器位置；取不到就退回枪尖）。
- 红边框：`Assets/Scripts/Player/Combat/PlayerHitFeedback.cs` 加公开方法，复用 `Assets/Sprites/BattleHUD/hitted.png`。

**A3 收尾（专属音 + 镜头推近）**
- 音效：`Assets/Scripts/Services/AudioManager.cs` 加 `Guard_Break` 事件 + `PostEvent(name, pitch, volume)` 重载
  （**现有没有 pitch/音量参数**）；素材先用 `_guardBlockClips` 降调 + `_heavyHitClips` 叠一层低频。
- 镜头：`CameraFeedbackController.RequestGuardBreakPush()` —— **FOV 脉冲**（60→58，0.08s 进 / 0.25s 出），**不是抖**；
  ⚠ FOV 动构图 ⇒ 幅度 ≤2° + 一键关闭 + 单独验收。

### 6.4 参数落点与"改了不生效"的对策

- 新参数全部加到 `AttackSkillConfig`（破防那组附近），**真值写进 `Assets/Prefabs/UI/Skills/Zhangfei_Parry.asset`**，
  并在 `Tools ▸ 三国杀戮 ▸ 招架姿态调参` 加滑条。
- ⚠ **必须写资产**：Unity 域重载会保留 ScriptableObject / 旧场景实例的 managed 状态 ⇒ 只改 C# 默认值**运行时不生效**。
- ⚠ 场景组件（`Main Camera` 的 `CameraFeedbackController`）已落盘 ⇒ 现在改它的字段**有效**（改完保存场景）。

### 6.5 验证配方（脚本驱动逐帧采样）

Battle 场景 Play Mode；采样必须跑在**真实 player loop**（见坑 7）：

```csharp
var ps  = UnityEngine.Object.FindObjectOfType<PlayerState>();
var st  = ps.WeaponStance;
var cfg = ps.heroConfig.GetSkillConfig(AttackType.Parry);
ps.freezeWeaponStance = true;                 // 接管姿态 Tick
System.Collections.IEnumerator Recorder()
{
    st.PlayParry(cfg);
    for (int i = 0; i < 45; i++) { st.Tick(Time.deltaTime, true); yield return null; }  // ★ scaled
    var w = GameObject.Find("StanceWeapon");
    ps.Guard.AccumulatedDamage = 119f;
    ps.Guard.TryAbsorbDamage(120f);             // ★ 触发破防（source 传 null ⇒ 无接触点/无火花）
    for (int i = 0; i < 40; i++)
    {
        st.Tick(Time.deltaTime, true);
        Debug.Log($"[BT] f{i:00} ts={Time.timeScale:F2} axis={Vector3.SignedAngle(Vector3.up, w.transform.rotation*Vector3.up, Vector3.forward):F0}");
        yield return null;
    }
}
UnityEngine.Object.FindObjectOfType<CameraFeedbackController>().StartCoroutine(Recorder());
```
- 采样结果从 **Console** 读（`unity_get_console_log(level:"info")`），不要在片段里 await 采样。
- `TryAbsorbDamage(damage, source)` 的 `source` 传真实敌人才能测"接触点/火花/敌人慢放"。
- ⚠ Editor 帧率可能很高（实测 ~270fps）⇒ **按帧推时间会错**，一律用 `Time.unscaledDeltaTime` / `Time.deltaTime` 折算。

### 6.6 相机震动的可调参数（Inspector：`Main Camera ▸ Camera Feedback Controller`）

| 字段 | 当前值 | 说明 |
|---|---|---|
| `shakeIntensityScale` | 0.35 | **总幅度倍率**；0 = 关掉所有震动 |
| `rotationDegreesPerUnit` | 20 | 旋转幅度 = 强度×该系数 |
| `rotationVibrato` | 4 | 整段来回次数（少 = 闷响） |
| `hitRotationScale` / `playerDamageRotationScale` | 1 | 各路线的旋转倍率（0 = 不产生整屏旋转） |
| `heavyIntensity` / `playerDamageIntensity` / `guardBreakIntensity` | 0.035 / 0.07 / 0.045 | 各档位位移（已再乘总倍率） |
| `backgroundFollowRotation` | 1 | 背景跟随相机**滚转**的比例；0 = 天空钉死 |
| `disableBackgroundFollow` | false | 勾上 = 完全不跟随（回到"地动山摇"老行为，仅供对比） |

### 6.7 踩坑清单（本轮新增，续 §4 的 1~11）

12. **未落盘 / 旧的场景组件会吃掉你的改动**：Unity 域重载会**保留 managed 状态** ⇒
    改 C# 默认值对旧实例无效，**Inspector 调参也无效**（场景里没有这份数据）。
    ⇒ 改完必须写进场景资产；本轮已把 `CameraFeedbackController` 正式落盘并启用。
13. **`Time.timeScale` 类的"全局时间"实验在这个项目里永远读不出顿感**（已否，见 `design/guard-break-presentation-overhaul.md` §1.4）。
14. **背景是 clip 空间四边形（钉屏）**：相机平移时它不该动（动了会与远景错位 ⇒"背景和场景不同步"），
    只有**滚转**必须跟（否则整幅画面只有世界在歪 ⇒ 地动山摇）。
15. **乘法混合的 sprite shader 变不亮**：`_Color` 是染色乘数 ⇒ 闪白必须新增 `_FlashStrength` 通道。
16. **震动的"停"与"慢"是两种读法**：`Animator.enabled=false`（冻结）读成"卡住"；`Animator.speed=0.25`（慢放）读成"被打飞"。
17. **一个正在攻击的敌人被打断受击动画**是否可接受，需要用户拍板（PBZ 里成功格挡确实会让对方武器被弹开）。

### 6.8 提交与环境状态（交接时）

- 分支：`feat/stage-select-talent-overlays`；远端 `origin@github.com:XwXchaser/threeKingdomSlayer.git`。
- 本轮改动已 commit + push（见仓库历史）；**工作区可能仍有其它会话的未提交改动**（美术、配置表、1013/1051 素材等），
  下一轮开工前先 `git status` 确认，**不要**顺手提交/丢弃不属于本任务的改动。
- Editor：Battle 场景，Play/Edit 均可；本轮结束时运行时已复位（`ts=1`、guard=None、stance=Idle、全局着色器参数归零）。

---

# 七、第五十四轮（2026-11-13）：REQ-006 破防演出包落地（A1/A2/A3）

> 本节是**当前最新**状态；§1~§6 保留作推理链与验证配方。

## 7.0 一句话

**破防那一拍的演出包按 `design/guard-break-presentation-overhaul.md` 方案 A 全部落地**（A1 被打方慢放 / A2 命中确认 / A3 专属音 + 镜头推近），
脚本化逐帧实测通过；**只差用户实机验收**（「爽不爽」脚本测不出）。
★ 按用户拍板：**破防不打断攻击者的攻击表现** ⇒ 不切受击动画，只做「冻结 → 慢放 → 回 1」。

## 7.1 改动清单

```
M Assets/Shaders/WeaponColorUnlit.shader              ← 新增 _FlashStrength 通道（乘法混合的 _Color 变不亮）
M Assets/Scripts/Effects/MeshWeaponVisual.cs          ← SetFlash/PlayFlash（MaterialPropertyBlock，不实例化材质）
M Assets/Scripts/Player/Combat/PlayerWeaponStance.cs  ← FlashGuardBreak() + 缓存 _meshVisual
M Assets/Scripts/Enemy/Enemy.Combat.cs                ← ApplyGuardBreakReaction()（对外入口）
M Assets/Scripts/Enemy/EnemyHitFeedback.cs            ← PlayGuardBreakSlow/StopGuardBreakSlow（冻结→慢放→回1）
M Assets/Scripts/Enemy/Enemy.Pool.cs                  ← OnDisable/ResetEnemy 清理慢放协程 + 还原 speed=1
M Assets/Scripts/Player/Combat/PlayerHitFeedback.cs    ← TriggerGuardBreakFlash()（屏幕红边框）
M Assets/Scripts/Core/CameraFeedbackController.cs      ← RequestGuardBreakPush()（FOV 脉冲，不是抖）
M Assets/Scripts/Services/AudioManager.cs             ← PostEvent(name,pitch,volume) + Guard_Break + 独立变调音源
M Assets/Scripts/Player/Combat/PlayerGuard.cs         ← Break() 串起 A1/A2/A3（Break 多了 source 参数）
M Assets/Scripts/Data/AttackSkillConfig.cs             ← +8 个破防演出字段
M Assets/Scripts/Editor/ParryStanceTuningWindow.cs    ← +8 条滑条
M Assets/Prefabs/UI/Skills/Zhangfei_Parry.asset       ← 8 个字段真值写入（域重载吃内存值，必须写资产）
```

## 7.2 实测（Play Mode，真实 player loop 协程采样）

采样文件：`Library/Locus/tmp/guardbreak-smoke.txt` / `guardbreak-a1-smoke.txt` / `guardbreak-full-smoke.txt`

| 项 | 结果 |
|---|---|
| A1 敌人侧 | f00–f15 `animator.enabled=False`（冻结）→ f18–f36 `speed=0.25`（慢放）→ f39+ `speed=1.00` ✔ |
| A2 枪闪白 | `flash 0.85 → 0.57 → 0.32 → 0.04 → 0`（0.12s）✔ |
| A3 镜头推近 | `fov 60.00 → 58.68 → 58.09 → 59.00 → 60.00`（2°，回位）✔ |
| 破防判定 | `absorbed=True broken=True state=Broken` ✔ |
| 全局时间 | 全程 `Time.timeScale = 1.00` ✔（不动全局时间） |
| 运行期异常 | 0 条 ✔ |

## 7.3 本轮踩到的坑

1. **`print` 在 Locus 片段的协程里不回传控制台** ⇒ 采样必须 `Debug.Log` 或**写文件**。
   ⇒ 交接文档 §2.1 的「读 Console」只对 `Debug.Log` 有效，片段内 `print` 不行。
2. **`MaterialPropertyBlock.GetFloat` 在本版本只有单参重载**（`GetFloat(int)`），两参（int, out float）编译不过。
3. ★ **`PlayerState` 在 `Player/PlayerState`，而 `PlayerHitFeedback` 在父物体 `Player`** ⇒
   `GetComponent<PlayerHitFeedback>()` **取不到**（返回 null，红边框静默失效）⇒ 必须 `GetComponentInParent<>()`。
4. ★ **手工 `Instantiate` 敌人 prefab 测不了敌人侧**：`Enemy._animator` 在 `Enemy.Init.cs:186` 赋值、
   `Enemy.Update()` 在 `!initialized` 时早退 ⇒ 卡肉计时不��� ⇒ 慢放协程等不到冻结结束。
   ⇒ 必须走对象池：`EnemyPool.GetEnemy(1011)` + `enemy.Initialize(col,row)`（初始化后 `state=Attacking`、Animator 有效）。
5. `DOTween.To(...)` 返回 `Tween`（不是 `Sequence`）⇒ **没有 `.Append`**；要串两段必须用 `DOTween.Sequence().Append(...)`。
6. 场景里的 `CameraFeedbackController` **本轮确认已落盘**（Main Camera，`m_Enabled: 1`，参数齐全）
   ⇒ 上一轮交接说的「已正式落盘」属实；⚠ 但本仓库 `.meta` 里的 GUID 是 base64 串，
   **拿它去 grep 场景一定搜不到** ⇒ 查场景引用要用 Unity 的 32 位 hex GUID（`AssetDatabase.AssetPathToGUID`）。

## 7.4 剩下的

- ★ **用户实机验收**（破防那一拍的观感；设计文档 §6：脚本测不出）。
- **慢放的错拍取舍**：慢放只改 `Animator.speed`、不改攻击判定时钟 ⇒ 慢放窗口内「动作慢、判定正常」。
  若实机觉得别扭，可选：① 把攻击 tween 也纳入慢放（会改变敌人出伤时机）；② 缩短慢放窗口；③ 放弃慢放只留冻结。
- **被打方受击表现**（PBZ 里「对方武器被弹开」）本轮**按用户拍板未做**（不打断攻击表现）⇒ 需要时可再议。
- 破防专属音目前是「格挡音降调 + 重击音低频垫底」拼的 ⇒ 素材到位后可换成正式 clip（`AudioManager._guardBreakClips`）。

---

# 八、第五十五轮（2026-11-13 晚）：用户反馈「没有慢放感 / 武器生硬」→ 节奏修正

> 用户原话：「感觉关键破防的时刻没有慢放的感觉？武器还是很生硬的离开，没有破防感也没有慢放」。

## 8.1 根因（三条，均实测确认，不是猜测）

1. ★★ **慢放窗口没覆盖旋转段** —— `BeginBreak()` 就开了慢放（0.15 真实秒），但它只够盖住**后坐的前 29%**；
   **230° 的脱手旋转全程是 1×** ⇒ 这就是「生硬地飞出去」的直接原因。
2. ★★ **旋转曲线方向反了** —— `BreakSpinCurve(k) = k*(0.6+0.4k)` 是「**前慢后快**」= 被甩出去反而加速
   ⇒ 读成机械匀速旋转。重量感需要「**先快后慢**」（甩出去后失速）。
3. ★★★ **敌人的慢放被它自己的状态机踩掉** —— 敌人代码里有 **6 处**会写 `animator.speed`：
   `Enemy.Attack.cs:334`（攻击结束）、`Enemy.StateSwitch.cs:96`（转走路，另有 `:222`）、
   `Enemy.Movement.cs:36`、`Enemy.FormSwitch.cs:55`、`CavalryVisualController.cs:197/292`…
   ⇒ 只在慢放开始时写一次 `speed`，下一帧就被写回 1 ⇒ **实测慢放只活了 0.07s**（本应 0.55s）。

## 8.2 修正

| 改动 | 位置 | 说明 |
|---|---|---|
| **慢放改到「起旋转那一刻」才开** | `PlayerWeaponStance` | 口径：**后坐 = 冲击**（1×，快而脆）／**脱手旋转 = 重量**（慢放） |
| **旋转曲线改成失速** | `BreakSpinCurve` | `k*(0.6+0.4k)` → `1-(1-k)²`（先快后慢） |
| **起手余震** | `PlayerWeaponStance` + 新字段 `parryStanceBreakWobbleDegrees` | 旋转前 1/3 段叠衰减抖动（默认 3°）⇒「被顶了一下」 |
| **慢放每帧重申** | `EnemyHitFeedback.GuardBreakSlowRoutine` | 协程在 Update 之后恢复 ⇒ 盖得住敌人状态机的 `speed=1` |

参数（已写入 `Assets/Prefabs/UI/Skills/Zhangfei_Parry.asset`）：

| 字段 | 改前 | 改后 | 理由 |
|---|---|---|---|
| `parryStanceBreakSeconds` | 0.55 | **0.30** | 旋转的游戏时长（配合 0.35× 慢放 ⇒ 真实 ~0.8s） |
| `parryStanceBreakWobbleDegrees` | — | **3**（新增） | 起手余震 |
| `parryGuardBreakSlowSeconds` | 0.15 | **0.95** | ★ 盖住整段脱手旋转 |
| `parryGuardBreakHitStopSeconds` | 0.066 | **0.033** | 枪自己的定格缩短：4 帧以上读成「卡住」 |
| `parryGuardBreakEnemyFreezeSeconds` | 0.25 | **0.12** | 「卡住」→「被顶住」 |
| `parryGuardBreakEnemySlowSeconds` | 0.35 | **0.55** | 「被打慢」拉长 |
| `parryGuardBreakWeaponFlashSeconds` | 0.12 | **0.25** | 闪白盖住慢放窗口开头 |
| `parryGuardBreakScreenFlashSeconds` | 0.25 | **0.40** | 慢放窗口里画面上一直有东西在动（设计文档 §3.1） |

## 8.3 实测（`Library/Locus/tmp/guardbreak-timeline2.txt`）

```
f00 | 0.000 | 枪定格 + 敌人冻结(enabled=0)
f09 | 0.157 | 敌人解冻 → speed 0.25
f21 | 0.357 | 武器起旋转，角速度 8.0°/帧
f39 | 0.656 | 角速度 5.1°/帧（失速中）
f42 | 0.708 | 敌人慢放结束（0.157 + 0.55 ✓）
f66 | 1.111 | 角速度 0.3°/帧
--- 武器出画 @ 1.161s
```

| 指标 | 修正前 | 修正后 |
|---|---|---|
| 脱手旋转的真实时长 | 0.55s（**1× 速**） | **0.80s**（0.35× 慢放 + 失速曲线） |
| 脱手角速度 | 恒定加速（~418°/s 峰值） | 8.0 → 0.3 °/帧（**先快后慢**） |
| 敌人慢放实际存活 | **0.07s**（被状态机踩掉） | **0.55s** ✓ |
| 命中确认覆盖 | 0.12s | 0.25s 闪白 + 0.40s 红边框 |
| 全局 `Time.timeScale` | 1.00 | 1.00（不变） |

## 8.4 待办

- ★ **用户实机验收**：破防那一拍的手感（慢放是否被读出来、武器的「重量」对不对）。
  嫌拖就调 `Tools ▸ 三国杀戮 ▸ 招架姿态调参` 的两条滑条：「破防·时长」与「破防·受击慢放时长」。
- 敌人的慢放仍是「只改 Animator.speed、不改攻击判定时钟」⇒ 慢放窗口内「动作慢、判定正常」的错拍仍在。

---

# 九、第五十六轮（2026-11-13 晚）：用户再驳「破防瞬间长枪还加速离开」→ 起步角速度是元凶

> 用户原话：「完全没有，甚至破防的瞬间长枪还加速离开了。你慢放了啥？」

## 9.1 ★ 我上一轮（§8）改错了：失速曲线把**起步**变成了 2 倍速

| 曲线 | 起始角速度（真实） | 末速 | 用户读感 |
|---|---|---|---|
| 旧版 `k*(0.6+0.4k)`（前慢后快） | ~251°/s | ~418°/s | 加速飞走 |
| §8 改的 `1-(1-k)²`（失速） | **~538°/s** | ~0 | **起步更炸**（用户反馈） |
| ★ 现在 `k*(1.12-0.12k)`（匀速微收） | **~170°/s** | ~130°/s | 缓缓打飞 |

⇒ **教训：失速曲线的代价是起步 = 2 倍均值**；想让破防“看起来被顶住”，**起步必须就是慢速**。
⇒ 速度靠「破防时长 ÷ 慢放倍率」调，**不要靠改曲线**。

## 9.2 其余修正

- **旋转量 230° → 150° + 漂移 0.8 → 2.0** ⇒ 靠**平移**出画，而不是扫过屏幕（扫描式的运动最像“弹射”）。
- **慢放更深**：0.35× → **0.25×**（破防时长 0.30 → 0.26 ⇒ 真实旋转 1.04s，均速 144°/s）。
- **敌人慢放 0.55 → 1.10s** ⇒ 盖住**整个脱手窗口** ⇒ 破防那一拍里「枪与敌人都慢、世界照常」对比最强。
- **镜头推近改成「推进 + 保持 + 回位」**（0.08s 进 / **0.55s 保持** / 0.3s 回）
  ⇒ 一次性推完就回只剩一下普通抖动；保持住才能让镜头在整个慢放里“压着”。
- 闪白 0.25 → **0.35s**、红边框 0.40s（命中确认覆盖到起旋转）。

## 9.3 实测（`Library/Locus/tmp/guardbreak-timeline4.txt`）

```
f00 | t=0.000 | dAxis=0.0°/帧 | espd=1.00(冻结) | fov=60.00 | ts=1.00
f06 | t=0.107 | dAxis=0.0°/帧 | espd=1.00(冻结) | fov=58.00 | ts=1.00
f12 | t=0.208 | dAxis=0.0°/帧 | espd=0.25        | fov=58.00 | ts=1.00
f24 | t=0.408 | dAxis=2.6°/帧 | espd=0.25        | fov=58.00 | ts=1.00
f54 | t=0.909 | dAxis=2.6°/帧 | espd=0.25        | fov=60.00 | ts=1.00
f78 | t=1.308 | dAxis=1.5°/帧 | espd=1.00        | fov=60.00 | ts=1.00
--- 武器出画销毁 @ 1.326s
```

| 指标 | 最初 | §8（错） | 现在 |
|---|---|---|---|
| 破防瞬间角速度 | 0（定格 4 帧） | 0 | **0（定格 2 帧）** |
| 脱手角速度（起步） | ~251°/s | ~538°/s | **~170°/s** |
| 脱手角速度（峰值） | ~418°/s | ~538°/s | **~170°/s** |
| 脱手真实时长 | 0.55s | 0.86s | **1.04s** |
| 敌人慢放 | 无 | 0.55s（被踩成 0.07s） | **1.15s** |
| 镜头推近 | 无 | 0.33s | **0.08 进 + 0.55 保持 + 0.3 回** |
| 全局 timeScale | 1.00 | 1.00 | 1.00 |

## 9.4 若还是读不出“慢放”的剩余杠杆（按代价排序）

1. **破防动作整体再短**（「破防·时长」0.26 → 0.18）⇒ 画面上少一段呆滞。
2. **命中确认再加强**（火花放大、红边框加一层）。
3. ★ **真·子弹时间**（全局 `Time.timeScale` 短降）——⚠ **设计文档 §1.4 已试过并被否**（「没有时停感」/像掉帧），
   且语义上「破防是失败事件，子弹时间会读成我赢了」⇒ 需用户明确点头才做。
4. **镜头推近幅度** 2° → 4°（动构图，风险自负）。

---

# 十、第五十七轮（2026-11-13 晚）：上代理重查无双 → 实施「时停 + 慢放 + 径向模糊」

> 用户指示：「给你开了代理（梯子），自己去查」+「当前完全就没有时停慢放的效果，
> 即使是无双里玩家破防也会慢放+模糊啊」。

## 10.1 联网后核实到的一手事实

| 事实 | 出处 |
|---|---|
| 古典无双的**ガード**是「按键时机」机制：按住防御、命中瞬间再按攻击键 ⇒ **反击并把敌人打倒**（ガードnirai = 绕后打） | JP 维基《真・三國無双》「操作方法→ガード」 |
| ★ **乱舞（Musou）进入子弹时间：普通敌兵慢速、敌方将不受影响、玩家正常** | EN 维基《Samurai Warriors》“Differences from Dynasty Warriors series” |
| 格斗游戏的 **guard crush**：格挡消耗防御崩溃槽，耗尽 ⇒ **一段时间无防备** | JP 维基《CAPCOM VS. SNK 2》《対戦型格闘ゲーム》 |
| 无双里**武器不会因破防脱手**（我们保留用户拍板的脱手做法） | 系列内无该状态 |
| ★ 用户第一手观察：**无双破防也有「慢放 + 模糊」** | 用户 2026-11-13 |

⇒ **无双的子弹时间语法 = 慢的是「别人」、清晰且正常速度的是「自己」**
⇒ 我们前几轮把慢放全堆在「自己的枪」上 ⇒ 无对比、无模糊 ⇒ 读不出来（这是根因）。

## 10.2 实施

| 文件 | 内容 |
|---|---|
| `Assets/Scripts/Core/GuardBreakTimeFx.cs`（新增） | 破防那一拍：全局 timeScale → **0.1**（绝不用 0）+ 径向模糊；时间窗口 0.25s，模糊 0.06s 冲起 / 保持 / 0.25s 淡出；OnDisable/OnDestroy 必定还原 timeScale |
| `Assets/Shaders/GuardBreakRadialBlur.shader`（新增） | 径向拖影（以玩家为焦点、6 段采样）+ 按强度与原图混合；**两趟 Pass，半分辨率**（移动端可控） |
| `Assets/Scenes/Battle.scene` | Main Camera 上**落盘** `GuardBreakTimeFx`（shader 走 Inspector 引用，符合 `design/anti-ghost-reference.md`） |
| `Assets/Scripts/Player/Combat/PlayerGuard.cs` | `Break()` 里触发 `GuardBreakTimeFx.Ensure()?.Play()` |
| `Assets/Scripts/Player/Combat/PlayerWeaponStance.cs` | 破防窗口内**持枪姿态改用 unscaled 时间** ⇒ 枪照常速度飞、保持清晰；世界被压住 |
| `Assets/Scripts/Enemy/EnemyHitFeedback.cs` | 敌人的本地慢放**等全局定格结束后再上** ⇒ 不再二次减速（0.1 × 0.25 = 0.025 会彻底冻住） |

## 10.3 实测（`Library/Locus/tmp/guardbreak-timefx.txt`）

```
f00 | t=0.000 | ts=0.10 | fx=True  | 敌人动画 normalizedTime=0.920
f08 | t=0.141 | ts=0.10 | fx=True  | 敌人动画 normalizedTime=0.922   ← 0.26s 真实时间里动画只走了 0.017
f16 | t=0.273 | ts=1.00 | fx=False | 时间回正
f17 | t=0.291 | ts=1.00 | espd=0.25 ← 敌人的本地慢放接手
f20 | t=0.340 | dAxis=11.1°/帧 ← 枪开始脱手旋转（unscaled ⇒ 正常速度）
```

- 世界定格：0.26s 真实时间内世界只走了 **0.017 归一化时间**（≈ 1 帧）⇒ 视觉上是**时停**；
- 模糊：**截图对照已确认**（`Library/Locus/Screenshots/locus_game_20261113_144157_578.png` 开、
  `..._144212_289.png` 关）⇒ 开启时整幅战场被径向拖影抹开，**HUD 保持锐利**；
- 全局 timeScale 必定还原（窗口结束 / 组件禁用 / 销毁都还原）。

## 10.4 ★★ 第五十八轮修正（用户实机反馈：「糊得五颜六色」「我慢放呢？当前慢放多长」）

**两个问题都是实现错误，不是错觉：**

| 问题 | 实情 | 修正 |
|---|---|---|
| **慢放根本不像慢放** | 全局只压到 **0.1 倍速、持续 0.25 秒真实时间** ⇒ 世界只走了 0.025 秒游戏时间（**约 1.5 帧**）⇒ 那是**一次卡帧**，而且 0.25 秒后世界立刻回到 1× | 改成 **0.25 倍速、持续 0.6 秒真实时间** ⇒ 世界实际走 0.15 秒游戏时间（**约 9 帧**）⇒ **真的慢放**。实测：0.59s 真实时间里世界只前进 0.145 归一化时间；同期**枪以 ~10°/帧**（正常速度）⇒ 对比出来了 |
| **画面「五颜六色」** | 只做了**彩色**径向拖影 ⇒ 暖色背景（夕阳/红旗）被抹成彩虹，看着像渲染故障 | 复合阶段改为 **拖影 + 去饱和(0.6) + 压暗(0.85)** ⇒ 读作「时间凝住」；同时把默认参数从 `blur 0.85 / radial 26px` 降到 **`0.6 / 12px`** |

修正后的实测（`Library/Locus/tmp/guardbreak-slowmo.txt`）：

```
f00 | t=0.000 | ts=0.25 | fx=True | 世界动画只走了 0.0000
f19 | t=0.321 | ts=0.25 | fx=True | 世界动画累计 0.0620   | 枪 12.2°/帧（正常速度）
f35 | t=0.587 | ts=0.25 | fx=True | 世界动画累计 0.1448
f36 | t=0.604 | ts=1.00 | fx=False | 时间回正（模糊再淡出 0.35s）
```

⇒ **慢放总时长**：世界 0.25× 持续 **0.6 秒真实时间**（≈ 0.15 秒游戏时间）；模糊从命中帧冲起、跟到慢放结束、再淡出 **0.35 秒**。
（这些都在 Main Camera 的 `Guard Break Time Fx` 组件上，Inspector 可直接调。）

## 10.5 仍未做 / 待用户实机确认

- ★ 用户实机验收这一拍（慢放 + 模糊 + 我方清晰是否读得出「破防」）。
- **敌人追击**（无双口径：破防是攻击方的收益窗口）本轮未做 —— 用户本轮未拍板。
- 数值（破防硬直 1.5s / 防御锁 3s）按用户指示**未动**。

---

# 十一、第六十轮（2026-11-13 深夜）：查清 PBZ / 无双的子弹时间 + 拖影重做 + 慢放归位

> 用户反馈两条：「**给我完全去除拖影**（现在像超大马赛克糊在屏幕上）」+「**我慢放呢？武器根本没有慢放，离开还在加速**」，
> 并要求「弄清楚这些问题再出方案，在网上好好找答案」。

## 11.1 网上查到的答案（一手出处）

| 结论 | 出处 |
|---|---|
| ★ **PBZ 的完美闪避触发子弹时间**；完美格挡则是「在命中瞬间点按」触发**鬼步**绕后 + 削敌杀气 | 游侠 MOD 描述「自动触发完美闪避，**触发子弹时间**」；游民星空《影之刃零》防御架势解析 / 格挡与闪避使用策略（2026-10-30） |
| PBZ 的**完美格挡窗口**：入阵者 10 帧(0.17s) / 破局者·逆天者 8 帧(0.13s) / 六十六天 6 帧(0.10s)，**从按下开始计时** | 同上 |
| PBZ 的**普通闪避 0.25s 无敌；完美闪避延长到 0.6s** | 同上 |
| PBZ 的**破防**（杀气耗尽）= **无法格挡 + 承受额外伤害 + 陷入硬直**，并伴随**屏幕爆裂效果** | 同上 |
| ★ **真三国无双：起源** 的「**完美闪避子弹时间**」——**子弹时间窗口内玩家还能按键触发反弹** | 游侠网《真三国无双起源体验版动作系统机制详解》：「在**完美闪避子弹时间内**先按攻击再按防御可触发反弹」 |
| DW 起源的**弹反/精准防御**会让敌人进入**白光状态** | 同上：「在受到攻击瞬间按下防御可打断敌人招式并使敌人进入**白光状态**」 |
| **Musou 乱舞 = 子弹时间**：普通敌兵慢速、敌方将不受影响 | EN 维基《Samurai Warriors》 |

⇒ **品类的共同做法**：子弹时间是**一段玩家仍可操作的慢速窗口**；命中确认靠**白闪 + 音效**；破防是「惩罚 + 窗口」。
⇒ **我们的两个错误**：① 把慢放做成了「只慢自己的枪」且窗口极短（0.25s @0.1×）⇒ 读作卡帧；
② 拖影用 6 段采样在 12~45px 上拉 ⇒ 变成大颗粒马赛克。

## 11.2 修正一：拖影**彻底删除**，换成正规可分离高斯模糊

- 旧的 `GuardBreakRadialBlur.shader`（径向拖影）**已删除**。
- 新 `Assets/Shaders/GuardBreakFreeze.shader`（四趟）：**降采样(半分辨率) → 水平 9-tap 高斯 → 垂直 9-tap 高斯 → 合成+调色**；
  合成趟 = `lerp(原图, 模糊图, strength)` + **去饱和 0.55** + **压暗 0.85**。
- 默认参数：`blurStrength 0.75` / `blurSize 2.2`（半分辨率像素）。
- 截图对照：`Library/Locus/Screenshots/locus_game_20261113_150402_474.png`（开）与 `..._150047_988.png`（关）
  ⇒ 开启时是**柔焦+去饱和**，无拖影、无马赛克；**HUD 保持锐利**。

## 11.3 修正二：慢放归位到「脱手那一刻」

**症状根因**（实测）：子弹时间原先从**破防命中帧**开 ⇒ 后坐 0.18 游戏秒在 0.35× 下拖成 0.5 秒真实时间
⇒ **旋转等窗口结束才开始** ⇒ 窗口内一动不动、窗口一结束就弹射（= 用户说的「还在加速离开」）。

**改法**：
| 阶段 | 时间 | 速度 | 读感 |
|---|---|---|---|
| 命中帧（后坐/被顶开） | 0 ~ 0.28s 真实 | **1×** | 冲击是脆的 |
| **脱手旋转** | 0.28 ~ 1.14s 真实 | **0.35×**（子弹时间） | ★ **整段脱手都在慢放**：2.9~3.9°/帧（原来 10~12.9°/帧） |
| 收尾 | 0.35s | 1× | 模糊淡出 |

实测（`Library/Locus/tmp/guardbreak-final.txt`、`guardbreak-diag2.txt`）：
```
f00 | t=0.000 | ts=1.00 | 等待后坐（recoil 0.288 → 0.009，1× 速度）
f21 | t=0.327 | ts=0.35 | 子弹时间启动（fx=True）→ brokenT 开始推进
f42 | t=0.708 | ts=0.35 | brokenT=0.586（旋转全程 0.35×）
f74 | t=1.243 | ts=1.00 | 时间回正
```
⇒ 旋转真实时长 0.857s（= 0.30 游戏秒 / 0.35×），角速度 ≈ **3.5°/帧**（原来 10~12.9°/帧）⇒ **慢 3 倍以上**。

## 11.4 修正三：两个实现 bug（都是我自己埋的）

1. **子弹时间触发点写错**：原代码把 `Play()` 挂在「等后坐」分支里 ⇒ **后坐已播完时那个分支不会进** ⇒ 慢放整段丢失。
   ⇒ 改为在「旋转真的开始」那一帧触发（`_breakSpinStarted` 标记）。
2. **持枪姿态在窗口内走 unscaled**：我上轮按「慢的是别人」把枪改成正常速度 ⇒ 恰好违背用户要的「武器慢放离开」。
   ⇒ 已撤销 ⇒ **枪跟着世界一起在子弹时间里慢放**。

## 11.5 现在的参数（Main Camera ▸ Guard Break Time Fx）

`worldTimeScale 0.35` / `freezeSeconds 0.9` / `blurStrength 0.75` / `blurSize 2.2` / `blurFadeSeconds 0.35`，
shader = `Hidden/GuardBreakFreeze`（Inspector 引用）。

## 11.6 还没做的（等用户拍板）

- **敌人追击**（无双/影之刃口径：破防是攻击方的收益窗口）；
- **数值收紧**（硬直 1.5s / 防御锁 3s）；
- **白闪**：DW 起源的弹反让**敌人进入白光状态**，我们现在是**自己的枪**闪白 ⇒ 命中确认的对象可能该换成敌人。

---

# 十二、第六十一轮（2026-11-14 凌晨）：**定稿** —— 模糊/全局时间缩放全删，慢放改由动画本身做

> 用户指令：「**别搞这些模糊什么的效果了，先给我全部移除**」+「我就问你一个问题：**为什么破防时枪离开的动作这么快**」。

## 12.1 ★ 那个问题的答案（两条根因，都是实锤）

### 根因一：**脱手动画本身只有 0.26 秒**

`Assets/Prefabs/UI/Skills/Zhangfei_Parry.asset`：
```
parryStanceBreakSeconds: 0.26     ← 整段脱手旋转的时长（游戏秒）
parryStanceBreakSpinDegrees: 150
```
⇒ 150° 要在 **0.26 秒**内走完 ⇒ 平均 **577°/秒 ≈ 9.6°/帧**（60fps）。
⇒ 再加上曲线 `k*(1.12-0.12k)` 的**起步是均值的 1.12 倍**（≈646°/秒）⇒ **起飞那一下最快** ⇒ 读作“弹射”。
⇒ ★ **这么短的动画，任何慢放技巧都救不回来**（就算整段按 0.35×，真实时间也只有 0.74 秒，仍然是“唰一下”）。

### 根因二：**全局慢放在真实游戏里会被直接改掉**

项目里写 `Time.timeScale` 的地方有一大堆（教学冻结/对话/三选一/暂停/场景切换/道具丢弃），
其中这些会**无条件把 timeScale 顶回 1**：
- `Assets/Scripts/UI/DialogueManager.cs:95` → `if (Time.timeScale == 0f) Time.timeScale = 1f;`
- `Assets/Scripts/Tutorial/TutorialStageDirector.cs:250` → `if (_frozenByTutorial || _introPaused) Time.timeScale = 1f;`
- `Assets/Scripts/Core/UpgradeChoiceManager.cs:174/465`、`PauseMenuUI.cs:83`、`StageController.cs:329` …

⇒ 我们的 `timeScale = 0.35` 在**教学场景里一触发就被顶回 1** ⇒ 枪以全速飞走
（这正是用户在游戏里看到的“还是这么快”）。

⇒ ★ **结论：破防的“慢”必须来自动画本身（时长 + 曲线），不能靠全局时间缩放。**

## 12.2 定稿实现

| 动作 | 内容 |
|---|---|
| **删除模糊/冻结后处理** | 删除 `Assets/Shaders/GuardBreakFreeze.shader`、`Assets/Scripts/Core/GuardBreakTimeFx.cs`，并从 Battle 场景移除残留组件（已存场景） |
| **不再有任何全局 timeScale 操作** | 破防链路上不再写 `Time.timeScale` ⇒ 不会被教学/对话/三选一/暂停顶掉 |
| ★ **脱手动画变慢** | `parryStanceBreakSeconds` 0.26 → **2.0**（用户 2026-11-14 定稿：0.85 → 1.5 → **2 秒**）⇒ 150° 匀速 = **75°/秒 ≈ 1.25°/帧** |
| ★ **曲线改匀速** | `BreakSpinCurve(k) = k`（角速度恒定）。⚠ 数学上「角速度单调递减且末速 0」⇒ 起步必然 ≥2 倍均值 ⇒ 必然读成加速；故不再用失速曲线 |
| 敌人侧不变 | 冻结 0.12s + 本地慢放 0.25× / 1.1s（**局部**慢放，不受全局系统影响） |
| 命中确认不变 | 枪闪白 0.35s / 接触点火花 / 屏幕红边框 0.40s / 破防专属音 / 镜头推近 |

## 12.3 实测（`Library/Locus/tmp/guardbreak-slow-departure.txt`）

| 指标 | 修正前 | 定稿（0.85s） | 1.5s | **最终（2s）** |
|---|---|---|---|---|
| 脱手角速度 | 9.6~12.9°/帧 | 2.94°/帧 | 1.65°/帧 | **~1.25°/帧（75°/秒）** |
| 脱手到出画 | 0.26 游戏秒 | 0.83s | 1.83s | **2.33 秒** |
| 全局 timeScale | 被教学/对话顶掉 ⇒ 实际 1.0 | 1.00 | 1.00 | **全程 1.00** |
| 冲击帧（被顶开的后坐） | — | 0.33s、1× 速度（脆） | 同 | 0.33s、1× 速度（脆） |
| 敌人 | 冻结 0.12s + 0.25× 慢放 | 同 | 同 | 冻结 0.12s + 0.25× 慢放 ~1.1s |

⇒ 现在的破防节奏：**被顶开（脆，0.33s，1×）→ 枪被缓缓打飞（≈1.25°/帧，2.33 秒出画）→ 敌人爬行 0.25×**。

### 12.5 ★ 本轮结项（2026-11-14，用户：「这个改造就先这样了」）

**最终状态**：破防 = 脆冲击 + 缓慢脱手（2 秒匀速）+ 敌人局部慢放 + 命中确认（闪白/火花/红边框/专属音/镜头推近）。
**已删除**：所有屏幕模糊/冻结后处理与全局 `Time.timeScale` 操作（理由见 §12.1 根因二）。
**实测基线**（`Library/Locus/tmp/guardbreak-2s.txt`）：角速度 ~1.25°/帧、2.33s 出画、timeScale 全程 1.00、异常 0 条。

**留给后续的三条**（均未实施，等用户安排）：
1. **敌人追击**：无双/影之刃口径是「破防 = 攻击方的收益窗口」（子弹时间内敌人还能压上来）；
2. **白闪对象**：DW 起源的弹反是让**敌人**进白光状态；我们闪的是自己的枪；
3. **数值收紧**：破防硬直 1.5s / 防御锁 3s（用户指示暂不动）。

## 12.4 还挂着（等用户拍板）

1. **敌人追击**（无双/影之刃口径：破防是攻击方的收益窗口）；
2. **白闪对象**：DW 起源的弹反是让**敌人**进白光状态；我们闪的是自己的枪；
3. **数值收紧**（硬直 1.5s / 防御锁 3s，按用户指示未动）。
