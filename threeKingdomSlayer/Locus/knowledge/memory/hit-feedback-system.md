---
id: kd_cef5972f-5ef5-40cd-8471-38580aa751e0
injectMode: inherit
summary: 首批打击反馈已实现：局部 Animator Hit Stop、命中来源/强度上下文、受击缩放与伤害数字分级；DoT 不卡肉。
aiEditMode: inherit
---

## 已实现的首批打击反馈

- 新增 `Assets/Scripts/Core/HitFeedbackManager.cs`，定义命中来源、反馈强度和命中上下文。
- `Enemy.TakeDamage` 支持反馈来源/强度；`SharedHealthGroup` 只触发一次主反馈，同时保留成员闪白/缩放。
- Hit Stop 采用局部表现暂停，不修改全局 `Time.timeScale`：受击敌人 Animator 暂停；Stab/Slash/AttackWave 的本体攻击视觉在首个标准/重命中时同步暂停，避免出现“敌人停了但武器继续走”而无法感知卡肉。对象池回收、死亡和禁用时清理。
- 现有受击缩放按 Light/Standard/Heavy 分级，始终从原始 Scale 重启；伤害数字按强度缩放，Heavy 增加短促放大。
- DoT 显式为 None，不触发卡肉；幻影/被动/道具/箭雨/火焰为轻反馈；Parry、Launch、旋风、终极技能为重反馈；基础攻击首击标准、后续轻反馈。
- `Enemy.TakeDamage` 与 `SharedHealthGroup.TakeDamage` 的敌人受击音效都必须检查 `feedbackSource != HitFeedbackSource.Dot`；DoT 仍可扣血和更新血条，但不播放 `Enemy_Hit`。
- 仅在 `Enemy.TakeDamage` 入口屏蔽是不够的，共享血量路径会直接在 `SharedHealthGroup` 内播放音效；两条路径必须同时处理。

## 已修复：受击后攻击动画丢失

- 根因：敌人在攻击冷却态受击时，HitStop 禁用 Animator，但 `Hit` Trigger 已设置且攻击冷却仍继续；Animator 恢复的同一帧又触发 `Attack`，最终 HitFlash 抢占动画，而 DOTween 独立执行 `PerformAttack`，表现为攻击动画缺失但伤害存在。
- 修复：`Enemy.UpdateAttack` 在 `_hitStopRemaining > 0` 或 `_hitFlashRoutine != null` 时暂停攻击冷却；受击反馈完整结束后才允许启动攻击。
- 保留仅异常时输出的 `[ATTACK_ANIM_DIAG] TriggerNotEntered` 与 `DamageWithoutAttackAnimation` 警告，用于监测复发。
- 用户已复测开局受击场景，暂未再次出现该问题。

## 后续注意

- 当前 Hit Stop 仅冻结受击敌人 Animator，攻击波、DOTween 视觉和其他战斗对象仍按既有时间规则运行；后续若需要更强的全局卡肉，应另行设计战斗时间控制器。
- 命中强度默认主要由调用方显式标注；新增伤害入口必须选择合适的 `HitFeedbackSource` / `HitFeedbackStrength`，不可只靠伤害数值猜测表现等级。

## ★ 2026-11-12：方向复盘（局部优先）与两个新发现

### 1. 「全局时停」方向是错的（已撤回）

破防那一拍曾改成**全局 `Time.timeScale` 顿帧**（1→0.02 保持 0.1s 再回正），实测**读不出顿帧感**（用户：「完全没有时停感」）。原因两条：

1. **全局慢动作里没有东西在动**：攻击者敌人已被自己的 0.12s 卡肉冻住、其它敌人静止、枪只退 0.5 单位 ⇒ 没有「被拖慢」的对象。
2. **顿感来自对比**：被打的那个东西定住、其它东西还在动 ⇒ 眼睛读出「它被定住了」。所有对象一起停 ⇒ 读成掉帧/卡了一下，还会把「节奏卡顿」的错觉误传给战斗系统。

⇒ **通行做法是 hit pause（局部定格），不是 hit slow（全局慢动作）**：格斗游戏在命中帧让攻守双方停 1~6 帧（世界照常）；魂系/怪物猎人让**被打者**的受击动作定格或放慢 2~8 帧。时长随打击强度分级（轻 1~3 / 中 3~5 / 重 5~10 帧），且**只影响表演层**，不影响伤害/命中框结算；顿帧期间**输入缓冲保留**、取消窗口延后兑现。

⇒ 本项目现有档位正落在通行区间：Light 0.045s≈2.7 帧 / Standard 0.09s≈5.4 帧 / Heavy 0.14s≈8.4 帧（@60fps）。
⇒ 已实现：**玩家侧局部命中定格**（`PlayerWeaponStance.ApplyHitStop`，只停持枪姿态，世界照常）+ **破防受击慢放**（只把持枪姿态放慢到 0.35×，0.15s 真实时间）。**全局 `Time.timeScale` 时缓已删除**（`CombatTimeDilation` / `CombatTimeDilationRunner` 已删）。

### 2. ★ 发现：「闪白」在当前渲染结构下**做不到**（材质染色无法提亮）

- 敌人的「闪白」实际有两条路：① `EnemyHitFeedback.ApplyHitFlashImmediate()` 把材质 `_Color` 设成 white；② `PlayHitVisual()` 播 `HitFlash` 动画状态（用独立的 `Enemy_1011_hit1..hit5` 美术帧）。
- ⚠ **①在当前 shader 下无效**：敌人用的是 `Custom/EnemyOutline`（乘法混合），`_Color` 是**染色乘数** ⇒ white 只能「去掉染色」，**不可能把精灵变亮**；`_PierceHighlightStrength` 那条通道也是 `lerp(c.rgb, 固定3(1,0.3,0.3), s)` 的**乘法后**替换，同样无法超过原色。
- ⇒ **可见的闪完全依赖美术帧**；若 hit1..hit5 不是明显更亮的帧，就「根本没有闪白」。
- ⇒ 要做真闪白有三条路（未实施）：① 给 `EnemyOutline` 加一个 `_FlashStrength`（`lerp(c.rgb, white, s)` 后再 `*= c.a`）并用 `SetPropertyBlock` 驱动（**不污染材质实例**，shader 里已有 `_PierceHighlightStrength` 的现成范式）；② 叠一层白色 sprite/quad；③ 重画 hit 帧。

### 3. ★ 发现：镜头震动的「范围」——为什么读成地震

实测（Battle 场景，相机在 (0,3,-10)，fov 60，aspect 0.5；单位 = 屏幕宽）：

| 镜头动作 | 地形(z=0) | 天空(z=300) | 敌人(z≈9) |
|---|---|---|---|
| 平移 0.035（普通格挡档） | -0.0061 | -0.0002 | -0.0032 |
| 平移 0.075 | -0.0130 | -0.0004 | -0.0068 |
| **旋转 2.8°**（`intensity×80°`，普通格挡档） | **-0.0220** | **+0.0001** | -0.0134 |
| **旋转 6.0°** | **-0.0471** | **+0.0001** | -0.0286 |

- **「地面在晃、天空钉住」的主因是镜头的 Z 轴旋转项**（`PlayFeedback` 里 `DOShakeRotation(intensity*80°)`），不是平移：旋转把近处大平面甩出 2~5% 屏宽，而远处天空几乎不动 ⇒ 读成地震。
- ⚠ **背景补偿是死代码（双重死）**：`CameraFeedbackController.LateUpdate` 把背景位移写进 `worldBackground`，而
  ① 它指向 `Canvas/background` —— 该对象在 Battle 场景里 **`activeSelf=false`（禁用）**；
  ② **组件本身 `enabled=false`**（而且这个组件**没有落盘** —— 脚本 GUID 在任何场景/预制体里都不存在 ⇒ 是运行时/未保存的状态）
  ⇒ `LateUpdate` 根本不跑 ⇒ 补偿从来没有生效过。
  历史上 `0cdfe6d4` 引入 135× 补偿时配的是旧 Canvas 天空盒，`21a3f6fd`（Y 场景表驱动）换成 mesh 之后就失效了。
- ⇒ 传统动作游戏的做法：命中震动是**镜头的一点点位移 + 极短的时长 + 衰减**，普通命中根本不震；且镜头永远是**最后一层**，不能替代局部顿帧。

### 4. ★ 关键机制：远景天空是**钉在屏幕上的 clip 空间四边形**

`Assets/Experiments/CurvedScroll/Shaders/YSkyBackground.shader` 的顶点函数**手写 clip 空间四边形、不过投影矩阵**
（注释里写明：为了避开 RenderTexture 目标导致的 Y 翻转，2026-11-09 在设备上踩过「背景倒置」的坑）：

```hlsl
float2 clipXY = v.uv * 2 - 1;
clipXY.y *= _ProjectionParams.x;
o.vertex = float4(clipXY, z_far, 1);
```

⇒ 天空**对相机运动完全免疫**：相机平移/旋转时它一动不动，而世界在动 ⇒ 两者错位 ⇒ 「地动山摇」。
这与 `design/curved-scroll-travel-experiment.md` 的「远景层：天空…**前进时固定或弱视差**」是一致的（那是针对**推进**的设计，不是针对震动）。

### 5. ★★ D 的修法（2026-11-12 已接上）：背景只跟随相机的**滚转**

- **shader**：给 `YSkyBackground` 加一个全局 `_LocusScreenShake`（xy = clip 空间位移，z = 滚转弧度），
  在 **Y 翻转之前**应用到 `clipXY` ⇒ 与世界内容同样被镜像，两边始终一致。
- **驱动**：`CameraFeedbackController.UpdateBackgroundFollow()` 把相机的**滚转**写进该全局参数；
  ⚠ **必须挂在震动自己的 Tween 的 `OnUpdate` 上**，不能挂在 `LateUpdate` —— 组件当时是 `enabled=false`
  （旧版的背景补偿就是因此变成死代码的）。
- ★★ **只跟滚转、不跟平移**（2026-11-12 二次修正）：
  背景是**远景/无穷远层** ⇒ 相机**平移**时它本来就不该动；
  若按某个深度（如地面 D=10）去补偿，它就会与真正的远景（山/墙/树 D≈200~290）错开
  ⇒ 用户看到的现象是「**背景和场景的抖动不同步，这两者分别在动**」。
  ⇒ 现在：平移项恒为 0（背景不横移），滚转项与相机逐帧一致（整幅画面一起歪）。
- **只在相机偏离基准位时非零** ⇒ 推进/转向时远景仍然「固定或弱视差」，不破坏 Y 场景的既有设计。
- **破防镜头**：`CameraFeedbackController.RequestGuardBreak()` —— 比普通格挡**时长略长、幅度更小**（0.14s / 0.045）。
- ⚠ **新加的序列化字段在旧场景实例上是 0**（Unity 域重载会保留 managed 状态）⇒ 运行时都做了兜底。

### 5.1 ★★★ `CameraFeedbackController` 组件已正式落盘（2026-11-12）

- 之前它在 Battle 场景里是 **`enabled=false` 且未落盘**（脚本 GUID 在任何场景文件里都搜不到）⇒
  ① 它的 `LateUpdate` 从不跑 ⇒ 旧版背景补偿是**双重死代码**；
  ② **Unity 域重载会保留 managed 状态** ⇒ 改 C# 默认值对那个旧实例**无效**，
  而且**在 Inspector 上调它的参数也无效**（场景里没有这份数据）——
  本轮就踩到了：代码把倍率改成 0.35，运行时实测仍是旧的 0.5。
- ⇒ 已把它**写进 `Assets/Scenes/Battle.scene`**（`Main Camera` 上，`m_Enabled: 1`），
  参数落盘为：`shakeIntensityScale 0.35` / `rotationDegreesPerUnit 20` / `rotationVibrato 4` /
  `guardBreakDuration 0.14` / `guardBreakIntensity 0.045` / `backgroundFollowRotation 1`。
  **从此 Inspector 上的参数真正生效**（改完仍需保存场景）。
- ⇒ **以后改这类组件参数的正确姿势**：改 C# 默认值 **+ 写进场景资产**（或运行时兜底），否则旧实例吃不到。


### 6. ★★★ 震动幅度重做（2026-11-12，用户反馈：「stabC3 和格挡时依然整个画面都在动，像地震」）

**根因（两个量同时爆）**：

1. **旋转项 = 强度×80°**（重击档 0.035×80 = **2.8°**；玩家受伤档 0.07×80 = **5.6°**）
   ⇒ 竖屏 1080×2160 下，整个画面在**甩**（实测近处地面被甩出 2.2% 屏宽）。
2. **旋转的 vibrato = 14 次 / 0.11 秒 ≈ 127Hz** ⇒ 这不是「被顶了一下」，是**高频抖动** ⇒ 读成「震」。

**改法**（`CameraFeedbackController` 新增三个参数 + 去掉按档位的 vibrato）：

| 参数 | 默认 | 作用 |
|---|---|---|
| `shakeIntensityScale` | **0.5** | 所有档位的**总幅度倍率**（调 0 = 等于关掉所有震动） |
| `rotationDegreesPerUnit` | **20**（原 80） | 旋转幅度 = 强度×这个系数 ⇒ 重击档 0.7° |
| `rotationVibrato` | **4**（原 10~14） | 整段震动里来回几回 ⇒ ~36Hz 的「闷响」而不是 127Hz 的「抖」 |

**实测（改后 + 组件落盘后运行时复核）**：

| 档位 | 最大位移（世界单位） | 最大滚转 | 改前 |
|---|---|---|---|
| 格挡/重击 | **0.0118（1080宽≈4.4 像素）** | **0.64°** | 0.035 / 2.8° @127Hz |
| 破防 | ≈0.015（≈5.6 像素） | ≈0.83° | 0.045 / 3.6° @127Hz |
| 玩家受伤 | ≈0.024（≈9 像素） | ≈1.23° | 0.07 / 5.6° @127Hz |

⇒ 位移 4~9 像素、滚转 ≤1.2°、频率 ~36Hz（一次「闷响」）。

**另一个事实**：所有张飞招式的反馈强度都是 **Standard**（`Zhangfei_*.asset` 的 `stabFirstHitStrength/slashFeedbackStrength = 2`）
⇒ 配合 `heavyOnlyCameraShake=true`，**普通命中本来就不震镜头**；
会震的只有：**格挡**、**破防**、**玩家受伤**、以及被判为 **Heavy** 的命中（Launch / Displacement / Ultimate）。
⇒ 想让某一招**完全不震屏**：把该招式的反馈强度调成 Standard/Light 即可（不用改代码）。



