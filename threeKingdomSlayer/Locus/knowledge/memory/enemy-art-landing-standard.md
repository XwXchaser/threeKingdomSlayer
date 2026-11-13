---
id: kd_d657dd39-fe15-4684-acf3-9dadd39b3120
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 敌人美术落地 Unity 的通用口径（2026-11-09 实测固化）

> 来源：1013 / 1051 接入前的全量实测（5 个已上线敌人 + 42 张新素材帧）。
> 与 `design/visual-yoffset-system.md` 有出入的地方以本文为准（见 §6）。

---

## 1. 落位标准（1108² 画布口径）

| 项 | 值 |
|---|---|
| 画布 | 1108×1108（**旗面横扫类要更宽，见 §4**） |
| 导入 | Single / Tight(`spriteMeshType: 1`) / **PPU 16** / `alphaIsTransparency: 1` / Point / maxTextureSize 2048 |
| pivot | 中心 (0.5, 0.5)，**且 `.meta` 必须显式写 `spriteAlignment: 0`** |
| 人物本体高 | **605 px**（= 普通兵标准） |
| prefab `rootScale` | **0.08316832**（1011 / 1012 / 109 同值） |
| 人物本体高（世界） | 605 ÷ 16 × 0.08316832 = **3.144 世界单位** |

> 1011 剪影 3.602 / 1012 剪影 3.166 / 旧 105·106 剪影 3.938（含弓）—— 同族单位彼此可差 10~25%，别拿单一数值当硬标准。

## 2. 地面基准线与 `visualYOffset`

- 地面基准线：world Y = **-1.8**（全项目统一）
- 字段：`Enemy.visualYOffset`（序列化），在 `UpdateWorldPosition()` 里与 `bounceYOffset` 相加
- 公式：

```
visualYOffset = -1.8 - meshMinY × rootScaleY
```

`meshMinY` = 导入后 Sprite 的 **tight mesh** 最低 local Y（px/16，相对 pivot）。

实测校准表（存储值 == 公式反算值，5/5 吻合）：

| Prefab | rootScaleY | visualYOffset | 静止脚底 |
|---|---|---|---|
| Enemy_1011 | 0.08316832 | 0.1389 | -1.800 |
| Enemy_1012 | 0.08316832 | 0.0010 | -1.798 |
| Enemy_104 | 0.18 | 1.0350 | -1.800 |
| Enemy_105 | 0.20 | 0.2625 | -1.800 |
| Enemy_109 | 0.08316832 | 0.7938 | -1.800 |

## 3. 三个坑

### 3.1 `.meta` 不写 `spriteAlignment` → `spritePivot` 被忽略
缺 `spriteAlignment` 字段 → 默认 Center → `.meta` 里写的 `spritePivot` **不生效**。
实测：`Enemy_1012_idle1.png.meta` 写着 `spritePivot {0.65392774, 0.5487343}`，但实际生效是中心 (0.5,0.5)。
→ 写新精灵时**显式写 `spriteAlignment: 0`**（或 `1` + 想要的 pivot，别只写 pivot）。
→ 旧记录 `memory/enemy1012-art-replacement-pipeline.md` §3「pivot idle=0.6539」是失效信息。

### 3.2 `EnemyGroundAligner` 是死代码
它**没挂在任何 prefab / 场景**，代码里也没有任何 `AddComponent` → **运行时自动对齐从不生效**。
→ 对齐只能靠 prefab 手填 `visualYOffset`；不要假设"放上去就会自动对齐"。

### 3.3 Unity tight mesh 底部比可见底行低约 3px
实测 1012：可见底行 897 → `meshMinY = -21.625`（-346px）。
→ 公式算出的 `visualYOffset` 有 **±0.016 世界单位**误差，**导入后必须回读 `meshMinY` 校准**。

## 4. 对齐基点：**用「人」，不要用整幅剪影**

- **垂直基准 = 靴底线**（人），旗杆 / 武器 / 披风不参与定尺与落地基准。
  实测 1013：分离靴底与旗杆底后，靴底 = 剪影底（idle 981 / cast 1332 / walk 1331·1323），仅 1 帧差 4px —— 旗杆没有低于靴子。
- **水平对齐 = 人物身体中心 x**，**不能用剪影 bbox 中心** —— 旗面/旗杆会把 bbox 中心带偏。
  实测 1013 三组的剪影中心分别约 373 / 719 / 782（源坐标），差 2 倍。
- **缩放 k 的分母是「人物本体高」**，不含旗杆：k = 605 / 源帧人物本体高。

### 4.1 ⚠ 台账的 `body_h` 列语义不统一 —— 必须自己重量

素材台账里同一个 `body_h` 列，在不同动作组里记的不是同一个东西：

| 动作组 | 台账 body_h | 实际含义 | 真实人物本体高（实测） |
|---|---|---|---|
| idle | 634 | 人物本体高 ✓ | 629 |
| cast | 621 | 人物本体高 ✓（但逐帧变：沉腰帧 589、举旗帧 692） | 617（取 idle 姿态帧） |
| **walk** | **942** | **「旗顶→脚底」整幅内容高** ✗ | **620** |

walk 的旗是举在头顶上方的，旗顶就是整幅剪影的最高点 → 台账把旗杆算进了「人物身」。
直接拿 942 当分母（k=0.642）会把 walk 的人缩到只有 idle 的 57%，肉眼可见矮一截。

**量人物本体高的可靠办法**（已写进 `Tools/visual_brief/land_1013_frames.py`）：
用**形态学开运算**（水平窗口 NECK=45：比旗杆 ~30px 宽、比人窄）把旗杆与旗面从人物身上剥离，
再取「与脚底相连的那个连通块」的顶端 = 人的头顶。
（用「身体中心列 ±60px 内最高像素」不行 —— 旗面正好悬在头顶正上方，会量到旗面。）

### 4.2 固定 k + 偏移策略

- **k 全组一个**（不逐帧归一化）：逐帧归一化会把「头上下浮动」翻译成「整体缩放」，看着像在缩放。
- **偏移**：
  - 内容裁切原点稳定的组（idle / walk）→ 用**组内中位数统一偏移**，保留源片自身的水平摆动
    （美术规矩：「走路不能逐帧对位，否则会抹掉水平摆动」）。
  - 内容裁切原点随动作变化的组（1013 的 cast：旗杆高度变化 300+px）→ **逐帧对齐人的靴底线**。

## 5. AnimationClip 的两个坑

### 5.1 clip.length = 最后一个 key 的时间 + 1 帧

不是 `m_StopTime`。想要 clip 长度正好 = 目标周期，**末帧必须补一个 `L - 1/30` 的尾 key**（值与末帧相同），
否则长度会短一帧（例：6 帧走路的 key 在 0/0.1/…/0.5 → 长度只有 0.5333，循环会快 12%）。

| 目标 | 做法 |
|---|---|
| 循环 0.6s / 6 帧 | key 0…0.5 + 尾 key 0.5667（与末帧同图）→ 长度 0.6，每帧正好 0.1s |
| 循环 1.98s / 6 帧 | key 0/0.33/…/1.65 + 尾 key 1.9467 → 长度 1.98 |
| 一次性 2.767s | key 0/0.35/0.70/1.05/1.50/2.50 + 尾 key 2.7337 → 长度 2.767（= 旧 105 Attack 的长度） |

### 5.2 clip 只写一条对象曲线

现有敌人 clip 只有一条 `m_Sprite` 的 PPtr 曲线（`path='' type=SpriteRenderer prop=m_Sprite`）+ `m_AnimationClipSettings`（loopTime/startTime/stopTime）。
新建 clip 用 `AnimationUtility.SetObjectReferenceCurve` + `SetAnimationClipSettings` 即可，YAML 与手工做的 clip 逐字段一致。

### 5.3 ⚠⚠ 重建已有 clip 不能「删了再建」

`AssetDatabase.DeleteAsset(path)` + `CreateAsset` 会让 .anim **换 GUID** → AnimatorController 里各状态的 motion 引用全部断成 NULL，
表现为「动画状态进了但没有 clip」（`GetCurrentAnimatorClipInfo` 返回空），而且 **Controller 里看不出异常**。

**正确做法**：原地改 —— `LoadAssetAtPath<AnimationClip>(path)` → `AnimationUtility.SetObjectReferenceCurve` + `SetAnimationClipSettings` → `EditorUtility.SetDirty` → `SaveAssets`（GUID 不变）。
若已经删过，重建后必须逐状态重新赋 `state.motion = 新clip` 并复核。

## 6. Unity 编辑器 API 的三个坑（2022.3）

1. **`TextureImporter` 没有 `spriteAlignment` / `spriteMeshType` / `spriteExtrude` 属性** → 用 `SerializedObject` 改
   （`alignment` / `spriteMeshType` / `spriteExtrude`），或 `TextureImporterSettings`。
   `SpriteAlignment` / `SpriteMeshType` 在 **UnityEngine** 命名空间（不是 UnityEditor）。
2. **`UnityEditor.ObjectReferenceKeyframe` 没有 (time, value) 构造函数且字段只读** → 用反射装箱赋值：
   `object boxed = new ObjectReferenceKeyframe(); field.SetValue(boxed, t); …; (ObjectReferenceKeyframe)boxed`
3. **`PrefabUtility.SaveAsPrefabAsset(root, 新路径)` 会丢部分属性修改**（实测 SpriteRenderer 的 sprite 没写进去，其他字段正常）。
   → 先存到目标路径，再 `LoadPrefabContents(该路径)` 改一次 `SaveAsPrefabAsset(同路径)`。

## 5. 缺动作时怎么占位（不引旧角色的资源）

**建出状态，但把 motion 指向 Idle clip**（同一个 clip）。

依据：三个表现都不依赖动画——
- 受击闪白 = **材质层**（`CreateFlashMaterials` / `ApplyHitFlashImmediate`）
- 死亡倒地 = **代码驱动**（`Enemy.Die()` → `DeathBounceAndFall()` 弹起+旋转+重力）
- 击飞抛物线 = **代码驱动**（`launchGravity` / `launchYHeightMin·Max` / `launchDuration`）

→ 受击时姿势保持 idle，但闪白 / 缩放 / 卡肉照常。

不建状态的后果：`Animator.Play()` 每次受击刷 warning；且 `Dead` / `Launched_Rise` 是 ANY→X 过渡的**目标**，必须存在。

受击动画入口：`Enemy.FormSwitch.PlayHitVisual(direction)` → 播 `HitLeft` / `HitRight` / `HitFlash`，**目标状态不存在时回退 `HitFlash`**（`HasState` + `Base Layer.` 前缀）。
`Enemy.FormSwitch.HasGetupAnimatorState()` → **建不建 `Launched_Getup` 会改变行为分支**（没有 → `BeginGetup()` 直接 false → 保持 Idle）；接新 controller 时要跟原角色保持一致。

## 7. 行走动画的两个可调参数（2026-11-09 加）

`Enemy` 新增两个序列化字段（写在「移动表现」Header 下）：

| 字段 | 默认 | 作用 |
|---|---|---|
| `rushBounceHeight` | **0** | 补齐前进时的 Y 弹跳峰值高度（世界单位）。**0 = 不弹跳**。弹跳由 DOTween 驱动 `bounceYOffset`，直接加进 `UpdateWorldPosition` 的 Y —— 旧的硬编码值是 0.5（普通兵身高的 16%），看起来像「走着走着跳一下」。骑兵（109）走自己的冲锋位移（`CavalryEnemy.Move.cs`），不经过 `StartMoving`，本来就不弹。 |
| `walkCycleScale` | **0.75** | walk 动画周期相对「一排推进时间（moveSpeed 秒/排）」的倍率。1 = 一个 walk 周期走完一排（旧行为）；<1 = 腿动更快。最终播放速度 = walk clip 长度 / (moveSpeed × 本倍率)。行距 2.5、moveSpeed 0.5 时：1.0 → 速度 1.2（周期 0.5s）；0.75 → 速度 **1.6**（周期 0.375s）。 |

**背景口径**：`moveSpeed` 是**秒/排**不是速度（`Enemy.Movement.cs`：`moveProgress += Time.deltaTime / speed`），行距 2.5 世界单位（`DefaultFormation.asset` 的 `rowSpacing`）。
旧公式 `_animator.speed = Mathf.Max(1f, 0.6f / moveSpeed)` 硬编码了 clip 长度 0.6；新公式改成取实际 walk clip 长度。

## 8. 与 `design/visual-yoffset-system.md` 的偏差

该设计文档把 `EnemyGroundAligner` 写成「运行时自动对齐」机制 —— **实测它没有挂在任何 prefab / 场景**，属未启用的设计。
修正该文档需要用户批准（design 角色），暂未改动。

## 9. 相关文档

- 落位与动画方案 + 实施结果：`plan/enemy1013-1051-art-integration-20261109.md`
- 1012 管线经验：`memory/enemy1012-art-replacement-pipeline.md`
- 工具：`Tools/visual_brief/land_1013_frames.py`（1013 落位）、`land_1051_attack.py`（1051 落位）
