---
id: kd_daa69bcf-7cc2-4f99-ab5d-473befb66f21
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
summary: 持枪防御动作（举枪/格挡/收枪）当前的完整计算方式：参数表、姿态与位移数学、时间线、屏幕落点、已知遗留。改任何一项前先读这里。
---

# 持枪防御动作 · 当前计算方式（2026-11-01 定稿）

> 相关：`PlayerWeaponStance`（`Assets/Scripts/Player/Combat/`）、`AttackSkillConfig`、
> 配置资产 `Assets/Prefabs/UI/Skills/Zhangfei_Parry.asset`、
> 调参窗口 Tools ▸ 三国杀戮 ▸ 招架姿态调参。
> 变更史与每次试错的结论在 `combat-ui-reference-image-generation-plan-20261030.md` §11.21–§11.45。

---

## 1. 参与者

| 对象 | 说明 |
|---|---|
| `PlayerState.WeaponStance` | 持枪实体（`PlayerWeaponStance`），每帧 `Tick(deltaTime, pointerHeld)` |
| `PlayerState.SetDefend(bool)` | 按下防御 → `stance.PlayDefend(cfg)`；`cfg` = `heroConfig.GetSkillConfig(AttackType.Parry)` |
| `AttackSkillConfig` | 全部可调参数（见下表） |
| `Zhangfei_Parry.asset` | 张飞这套参数的实参 |
| 武器模型 | `FantasySpear3D.prefab`（= `attackWavePrefab`；`parryGuardWeaponPrefab` 为 None 时回落到它）。杆长 **10.8** 世界单位，根节点 scale 11.05 |

**没有可见玩家角色**（战斗场景里只有敌兵与场景道具），所以武器就是角色的替身 —— 构图按武器本身算。

---

## 2. 当前参数（`Zhangfei_Parry.asset`）

| 参数 | 值 | 含义 |
|---|---|---|
| `parryStanceTipDegrees` | **42** | 落定姿态：枪在**屏幕平面内**的指向（0=水平向右，90=竖直向上） |
| `parryStanceHeadAnchor` | **(4.70, 5.50, −4.70)** | 落定姿态下**矛头**的世界坐标（= 枪头的落点） |
| `parryStanceEntryAngleDegrees` | **30** | 起手姿态的屏幕内指向 |
| `parryStanceEntryRootOffset` | **(1.00, 0.00, +0.70)** | 起手相对落定根节点的偏移（X 右移 1 单位 + Z 更远 0.7） |
| `parryStanceExitAngleDegrees` | **30** | 收枪姿态的屏幕内指向 |
| `parryStanceExitRootOffset` | **(0.00, 0.00, +0.70)** | 收枪相对落定根节点的偏移（退回到起手深度） |
| `parryStanceEntryRollDegrees` | **90** | 起手/收枪时**绕枪自身长轴**的拧转（落定为 0） |
| `parryStanceSwingInSeconds` | 0.12 | 挥入时长 |
| `parryStanceOvershootDegrees` | 8 | 过冲角（到位前冲过 8° 再回弹） |
| `parryStanceSettleSeconds` | 0.08 | 回弹时长 |
| `parryStanceHoldSeconds` | 0.10 | 定格（"格住了"那一下） |
| `parryStanceDefendAnchorDrop` | 0.18 | 落定相对斜举位的**下沉**（只有 Y） |
| `parryStanceDefendEnterSeconds` | 0.12 | 落到防御位的时长 |
| `parryStanceExitNormalSeconds` | 0.54 | 收枪时长（未格挡成功） |
| `parryStanceExitFastSeconds` | 0.12 | 收枪时长（格挡成功 / 破防） |
| `parryStanceTasselDamping` / `MaxBend` | 26 / 14 | 红缨 6 节物理骨链的阻尼与单节最大弯折 |
| `parryGuardRecoilDistance` / `Seconds` | 0.5 / 0.18 | 受击时武器沿 −Z 后退再回位 |

---

## 3. 数学

### 3.1 姿态（`BuildPoses`）

```
dir(θ) = (cosθ, sinθ, 0)                       // 屏幕平面内的指向
pose(θ, ρ) = FromToRotation(up, dir(θ)) · AngleAxis(ρ, right)
```

- `ρ` = 拧转角，**只加在起手位与收枪位**，落定姿态 `_poseIdle` 不加 → 落定画面与"没加拧转"时一致。
- **枪身长轴 = 武器根节点的 local +Y**（骨骼链 270.02+270.02+89.98 净旋转为 0），所以 `right`（local X）**垂直于枪身** → 拧转 = 一次**俯仰**：枪尖与枪尾朝相反方向各挪 `5.4·sinρ`。
- 拧转 ρ=90° 时枪身轴恒沿视线方向 → **屏幕内角度（起始角）失效**，只影响枪落在画面哪。

### 3.2 根节点（枪身中点）

```
rootAnchor = headAnchor − dir(42°) · 5.4        // 5.4 = 杆长/2
entryRoot  = rootAnchor + entryRootOffset       // (1, 0, +0.7)
exitRoot   = rootAnchor + exitRootOffset        // (0, 0, +0.7)
guardRoot  = rootAnchor + (0, −0.18, 0)         // 防御位只有下沉
```

- 防御根节点 z = **−4.70**（= headAnchor.z）
- 起手根节点 z = **−4.00**，收枪根节点 z = **−4.00** → 挥入是**朝 +Z（更深、离镜头更远）**推进 0.7

### 3.3 武器的世界位置（`ApplyPose`）—— **旋转支点在枪尾**

```
o               = (0, 5.4, 0)                 // 枪尾 → 根节点 的本地偏移
pivotOffset     = pose · o − poseIdle · o
weapon.position = PlayerPosition + root + pivotOffset + recoil
weapon.rotation = pose
```

- 落定姿态下 `pose == poseIdle` → `pivotOffset = 0` → **落定画面逐像素不变**；
- 起手/收枪姿态不同 → 枪尾留在 `root` 那条轨迹上，**枪头绕枪尾甩出弧线**（枪头位移 ≈ 10.8·sin拧角 ≈ 10.8）

### 3.4 枪头/枪尾的屏幕位置

- 枪头 = `weapon.position + pose·(0, 10.8, 0)`（+ 模型自身骨骼偏移）
- 枪尾 = `weapon.position − pose·(0, 10.8, 0)`
- 相机：位置 (0, 3, −10)、俯 18°、FOV 60°（竖）、aspect 0.5、画布 1080×2160

---

## 4. 时间线

| 阶段 | 代码 | 姿态 | 根节点 |
|---|---|---|---|
| 0 按下防御 | `SetDefend(true)` → `PlayDefend` → `PlayParry` | 起手位（θ=30, ρ=90） | entryRoot（z=−4.0, x=+1） |
| 1 挥入 | `StartSwingIn`：`MoveTo(guardRoot, poseIdle, 0.12)` | 起手 → 落定（拧转 90°→0°） | entryRoot → guardRoot |
| 2 过冲 | `MoveTo(guardRoot, poseOvershoot, 0.032)` | +8° | guardRoot |
| 3 回弹 | `MoveTo(guardRoot, poseIdle, 0.048)` | 回 42° | guardRoot |
| 4 定格 | `MoveTo(guardRoot, poseIdle, 0.10)` | 42° | guardRoot |
| 5 进入防御位 | `EnterDefend`：`MoveTo(guardRoot, poseIdle, 0.12)` | 42° | guardRoot（+0.18 下沉） |
| 6 保持 | `Tick`：姿态不动；`Phase=Defending` | 42° | guardRoot |
| 7 松手 | `BeginExit`：`MoveTo(exitRoot, poseExit, 0.54)` | θ=30, ρ=90（拧回） | guardRoot → exitRoot（z=−4.0） |
| 8 结束 | `Reset()` → 销毁武器 | — | — |

- 受击：`PlayerGuard.Break` → `WeaponStance.BeginExit(cfg.parryStanceExitFastSeconds)` → **破防也走"挥出画面"，不再原地消失**
- 挥入被打断：`InterruptToDefend` → 直接跳进防御位（允许跳位）

---

## 5. 落定时屏幕上的样子（Play Mode 实测）

| | 屏幕坐标 | 世界 y |
|---|---|---|
| 枪尖（WeaponTip 骨） | (2761, 2616) | 5.6 |
| 枪尾（SpearRig 骨） | (−409, 216) | −3.9 |
| 杆长（屏幕） | 约 2900 px（屏高 2160 的 134%） | — |
| 地面线（z=−4.7 处） | y = 699 | 0 |

→ 枪尖完全出上缘与右缘、枪尾出左缘，**画面里只剩杆身中段**。

---

## 6. 已知遗留（尚未处理）

1. **收枪时整杆没有完全出画**：`exitRoot` 的 Y 偏移为 0，纯 Z 回退时杆身中段仍压在画面上 → 销毁那一下会"凭空消失"（违 `design/combo-move-state-machine.md` §19 的"出画后才销毁"）。
   纯几何上，只要收枪不含 X/Y 平移就无解（枪的中点投影不动）；要合规需给收枪单独的位姿（如破防那种"甩飞"）。
2. **地面层级未调**：武器 `sortingOrder=2`（`MeshWeaponVisual.SetSorting(2)`）> 地面/敌兵贴图 0 → 地面以下那段是**压在土上完整可见**的；若要"地面把它切断、看起来真插在地上"，需把地面排序提到 > 2。
3. **红缨仍会甩**：`MaxBend=14°/节` × 6 节 → 弯折合计可达 80°+，是"大幅度旋转"的另一个来源。

---

## 7. 改之前要知道的三件事（本轮踩过的坑）

1. **屏幕内角度与拧转是互斥的两条路**：拧转 90° 时起始角失效；要用起始角把枪送到画外，就得保持拧转 30°。
2. **Y 抬高会连带把画面里的枪放大**（相机俯 18°，越靠上前向越近 → 越放大）：落定深度 −4.7 时，Y 每 +1 单位 ≈ 画面上 +8% 尺寸。
3. **只做用户明说的需求**：不要自行追加"某点必须可见/某值必须达标"之类的硬性条件（见 `memory/user-preference.md` 的「不自行追加需求」）。
