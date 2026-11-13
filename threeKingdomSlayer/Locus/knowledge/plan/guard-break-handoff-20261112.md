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
