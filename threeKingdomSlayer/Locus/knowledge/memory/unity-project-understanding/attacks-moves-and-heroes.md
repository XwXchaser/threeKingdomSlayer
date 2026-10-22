---
id: kd_9cc8ccee-bfb3-4a6f-bde3-c5ae64819a3e
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
summary: 招式/连招/技能/选角的使用文档（2026-10-24）——新增一个招式或连招（如斩击后蓄力接重横扫）要照哪套流程改哪几处：手势枚举与招式表（入口表+状态机边+接续窗口）、招式数据字段与动作锁定、蓄力分支、天赋门控、主动技能层；含必踩的坑与对应代码路径，便于按需核对。
injectMode: inherit
aiEditMode: inherit
---

# 招式 / 连招 / 技能 / 选角 · 使用文档

> **这份文档的用途** ✅ ✅：**要做新招式/新连招时，照着走 ✅ ✅** —— 改哪几处 ✅ ✅、哪几处能纯数据完成 ✅ ✅、哪几处必须写代码 ✅ ✅ ✅。
> 纪律 ✅：只写「读代码得不到的东西」✅ ✅；**与代码不一致时以代码为准** ✅ ✅（末尾有代码路径 ✅ ✅✅ ✅）✅ ✅。

---

## 一、先分清四个东西 ✅ ✅

| 是什么 | 在哪 | 加一条要写代码吗 ✅ |
|---|---|---|
| **主动技能** ✅ ✅（大招/道具类 ✅ ✅） | `ActiveSkillDefinition` ✅ ✅（含等级与冷却 ✅ ✅ ✅）✅ | 视效果而定 ✅ ✅ |
| **招式（普攻/重击/挑飞/招架）** ✅ ✅ | `AttackSkillConfig` ✅ ✅（114 个字段 ✅ ✅ ✅）✅ | **否 ✅ ✅** —— 纯资产 ✅ ✅✅ ✅ |
| **连招（段位/接续）** ✅ ✅ | `MoveTableConfig.states` ✅ ✅（**状态机边** ✅ ✅✅ ✅）✅ | **否 ✅ ✅** —— 纯资产 ✅ ✅✅ ✅ |
| **手势类型** ✅ ✅ | `MoveGesture` ✅ ✅（Tap/Hold/竖滑/横滑/斜滑 ✅ ✅ ✅）✅ | **要 ✅ ✅** —— 枚举 ✅ ✅（机制 ✅ ✅ 不是内容 ✅ ✅✅ ✅）✅ ✅ |

→ **「做更多连招」✅ ✅ 基本是纯数据活 ✅ ✅**；只有**新增手势类别 ✅ ✅** 或**新增蓄力分支 ✅ ✅** 才要改代码 ✅ ✅✅ ✅。

---

## 二、招式是怎么从「玩家一抬手」走到「一刀挥出去」的 ✅ ✅

```
输入层   InputManager.Gesture ✅ ✅  →  GestureInput ✅ ✅（手势 ✅ + 蓄力信息 ✅ ✅）
          ↓
解析层   PlayerMoveStateMachine ✅ ✅（中立态查入口表 ✅；出招中按「当前节点的边 + 接续窗口」解析 ✅ ✅）
          ↓
数据     MoveTableConfig ✅ ✅（roots ✅ = 中立入口表 ✅｜states ✅ = 状态机节点 ✅ ✅ + 出手边 ✅ ✅）
          MoveGesture ✅ ✅（手势枚举 ✅ ✅）
          AttackSkillConfig ✅ ✅（这一招的全部数值 ✅ ✅）
          ↓
执行层   AttackSystem ✅ ✅（TryExecuteAttack ✅ ✅ —— **蓄力分支在这里 ✅ ✅** ✅）
          ↓
表现层   动画 ✅ ✅ + 攻击波 prefab ✅ ✅（`attackWavePrefab` ✅ ✅）✅ ✅
```

**两种模式** ✅ ✅（`PlayerMoveStateMachine` ✅ ✅）：
- **直通模式** ✅ ✅：`moveTable` 为空 ✅ ✅ → 手势直接按等价映射放招 ✅ ✅（改造前的行为 ✅ ✅✅ ✅）✅ ✅；
- **招式表模式** ✅ ✅：中立态查 `roots` ✅ ✅；**招式进行中按「当前节点的边 + 接续窗口」解析** ✅ ✅；
  **无匹配边 → 什么都不发生** ✅ ✅（不是回退中立 ✅ ✅❗）✅ ✅。

---

## 三、**加一个招式/一段连招的照做清单** ✅ ✅

> ✅ **本文防的是** ✅ ✅：**重复查找** ✅ ✅（数据在哪 ✅ / 哪条链路 ✅ ✅）与**再造一个平行模块** ✅ ❗
> （连招已经有状态机 ✅ ✅、蓄力已经有输入分支 ✅ ✅、解锁已经有天赋载荷 ✅ ✅ —— 都不要重写 ✅ ✅✅ ✅）。

### 3.1 纯数据能做完的四步 ✅ ✅
1. **招式数据** ✅ ✅ → 新建一个 `AttackSkillConfig` ✅ ✅（或复用现有类型 ✅ ✅）：
   - `attackType` ✅ ✅（决定命中窗口 ✅ ✅ 与元素反应判定 ✅ ✅✅ ✅）、`damageType` ✅ ✅
   - `damage` ✅ ✅ / `poiseDamage` ✅ ✅ / `rangeRows` ✅ ✅（影响排数 ✅ ✅）
   - **`actionDuration`** ✅ ✅（**动作锁定时长 ✅ ✅ —— 要跟动画总长对齐 ✅ ✅❗**）✅ ✅
   - `columnMode` ✅ ✅（单列 / 全列 ✅ ✅ —— 元素反应只判这个 ✅ ✅✅ ✅）✅ ✅
   - 横扫专属 ✅ ✅：`slashSweepHalfWidth` ✅ ✅ / `slashSweepAngle` ✅ ✅ / `slashSweepDirection` ✅ ✅ / `slashMovementTiltDegrees` ✅ ✅ ✅
   - `attackWavePrefab` ✅ ✅（攻击波 ✅ ✅ —— 留空用默认 Quad ✅ ✅）✅ ✅
   - `interruptsCavalryCharge` ✅ ✅（视为重攻击 ✅ ✅ —— **声明式 ✅ ✅ 不用改代码** ✅ ✅✅ ✅）✅ ✅；`isHeavy` ✅ ✅（重攻击派生 ✅ ✅ —— 元素反应用 ✅ ✅✅ ✅）✅ ✅
2. **手势入口** ✅ ✅ → `MoveTableConfig.roots` ✅ ✅ 加一条（`gesture` ✅ ✅ → `move` ✅ ✅ ✅）✅ ✅
3. **连招边** ✅ ✅ → `MoveTableConfig.states` ✅ ✅ 里给**上一个状态** ✅ ✅ 加一条边（`MoveStateEntry.roots` ✅ ✅ ✅）✅ ✅
4. **接续窗口** ✅ ✅ → `MoveTableConfig.GetDefaultWindow(AttackType, out start01, out end01)` ✅ ✅（**每种攻击类型的默认命中窗口 ✅ ✅**）✅ —— 新类型可能要加一个 case ✅ ✅；窗口太短接不上 ✅ ❗、太长会误触发 ✅ ❗✅ ✅✅ ✅

### 3.2 可能要写代码的两处 ✅ ✅
5. **蓄力分支** ✅ ✅ → 如果这一招的形态**取决于蓄满与否** ✅ ✅ → 在 `AttackSystem` ✅ ✅ 的输入分支 ✅ ✅（`isCharged` ✅ ✅✅ ✅）里区分 ✅ ✅ —— **这是最常见的"要改代码"点 ✅ ✅✅ ✅** ✅ ✅
6. **天赋门控** ✅ ✅（可选 ✅ ✅）→ 想把新招式锁在天赋后面 ✅ ✅ → 加一个天赋节点 ✅ ✅（载荷 = `moveConfig` ✅ ✅ ✅，见 `talent-system.md` ✅ ✅）✅ ✅
   ⚠️ **配了节点但没解锁 → 招式被锁** ✅ ✅（`TalentTreeState.IsMoveGatedAndLocked` ✅ ✅✅ ✅）；**没配节点的招式不受天赋管** ✅ ✅✅ ✅（避免配置缺失把招式全锁死 ✅ ✅✅ ✅）✅ ✅

### 3.3 美术那侧 ✅ ✅（本文档不覆盖 ✅ ✅）
动画 ✅ ✅ + 攻击波 prefab ✅ ✅ + 特效 ✅ ✅ —— ⚠️ 动画资产不在数据表里 ✅ ✅，**招式配置管不到** ✅ ✅❗ —— 改动画要同步改 `actionDuration` ✅ ✅✅ ✅ ✅。

---

## 四、必踩的坑 ✅ ✅
1. **段位时钟由「命中后生效的动作锁」驱动** ✅ ✅（P1 约束 ✅ ✅✅ ✅）→ **空挥不推进段位** ✅ ✅❗
   → **「没打中就接不上下一段」是设计如此 ✅ ✅**，不是 bug ✅ ✅❗——做连招时最容易误解这一点 ✅ ✅✅ ✅ ✅。
2. **无匹配边 → 什么都不发生** ✅ ✅（不回退中立 ✅ ✅）→ 连招表写错 ✅ ✅ = 玩家感觉「这招失灵了 ✅ ✅」✅ ✅。
3. **`actionDuration` = 动作锁** ✅ ✅，攻速会**缩放**它 ✅ ✅ ✅ → 与动画不匹配 ✅ ✅ = 玩家觉得「卡手 ✅ ✅」✅ ✅。
4. **接续窗口** ✅ ✅ = 手势有效的那段时间 ✅ ✅ —— 与 `actionDuration` ✅ ✅ 是两回事 ✅ ✅（一个管输入判定 ✅ ✅，一个管动作锁 ✅ ✅✅ ✅）✅ ✅。
5. **`MoveGesture` 是枚举** ✅ ✅ → **加手势类别要改代码** ✅ ✅（Tap/Hold/竖/横/斜 ✅ ✅ —— 现有 5 种大概率够用 ✅ ✅）✅ ✅。
6. **新招式可能需要配天赋** ✅ ✅ → 别忘了 `moveConfig` ✅ ✅ 载荷 ✅ ✅ ✅。
7. **同一个 `AttackSkillConfig` 可以被多个入口引用** ✅ ✅（普攻 / 连招 / 天赋 ✅ ✅）—— 改一处会同时影响多处 ✅ ✅❗✅ ✅。

---

## 五、选角（换英雄/换武器/换招式）✅ ✅
- `HeroConfig` ✅ ✅（SO ✅ ✅）：`skillConfigs` ✅ ✅（**招式列表 ✅ ✅**）/ `moveTable` ✅ ✅（**这个英雄的手势→招式表 ✅ ✅**）/ `talentTree` ✅ ✅ / `ultimateSkillConfig` ✅ ✅ / `hudSkin` ✅ ✅ / `heroHUDPrefab` ✅ ✅ / `maxHealth` ✅ ✅ / `reviveCount` ✅ ✅ / `itemSlotCount` ✅ ✅ ✅
- **换英雄** ✅ ✅ = 加一个 `HeroConfig` ✅ ✅ 资产 ✅ ✅
- **换武器 + 对应招式** ✅ ✅ = **换 `moveTable` ✅ ✅**（手势→招式 ✅ ✅）+ 换 `skillConfigs` ✅ ✅ ✅ —— **都是引用 ✅ ✅✅ ✅ 零代码** ✅ ✅✅ ✅
- 现有的移动表资产 ✅ ✅：`Assets/ScriptableObjects/Moves/MoveTable_Zhangfei.asset` ✅ ✅（+ `Moves/Zhangfei/` ✅ ✅ ✅）✅ ✅
- ⚠️ **换英雄不只是数据** ✅ ✅：动画 ✅ ✅ / 特效 ✅ ✅ / 音效 ✅ ✅ / HUD 皮肤 ✅ ✅ 都是**独立资产** ✅ ✅✅ ✅ —— 换一套 = 美术工作量 ✅ ✅✅ ✅，不是配置 ✅ ✅。

---

## 六、代码路径（改之前先核对 ✅ ✅）
- `Assets/Scripts/Data/AttackSkillConfig.cs` ✅ ✅（招式数据 ✅ ✅ 114 字段 ✅ ✅✅ ✅）✅ ✅
- `Assets/Scripts/Data/MoveTableConfig.cs` ✅ ✅（入口表 ✅ ✅ + 状态机 ✅ ✅ + 命中窗口 ✅ ✅✅ ✅）✅ ✅
- `Assets/Scripts/Data/MoveGesture.cs` ✅ ✅（手势枚举 ✅ ✅ + `GestureInput` ✅ ✅✅ ✅）✅ ✅
- `Assets/Scripts/Data/CombatEnums.cs` ✅ ✅（`AttackType` ✅ ✅ / `DamageType` ✅ ✅ ✅）✅ ✅
- `Assets/Scripts/MoveSystem/PlayerMoveStateMachine.cs` ✅ ✅（状态机 ✅ ✅ / 缓冲 ✅ ✅ / 长按等待 ✅ ✅ / `LastResolution` ✅ ✅✅ ✅）✅ ✅
- `Assets/Scripts/Player/Combat/AttackSystem*.cs` ✅ ✅（执行 ✅ ✅ / **蓄力分支 ✅ ✅** ✅ ✅）✅ ✅
- `Assets/Scripts/Data/HeroConfig.cs` ✅ ✅（选角 ✅ ✅✅ ✅）✅ ✅
- `Assets/Scripts/Data/ActiveSkillDefinition.cs` ✅ ✅（主动技能 ✅ ✅✅ ✅）✅ ✅
- `Assets/Scripts/Player/InputManager.Gesture.cs` ✅ ✅（输入层 ✅ ✅✅ ✅）✅ ✅
- `Assets/Scripts/Data/Attack/StabMotionParams.cs` ✅ ✅、`Assets/Scripts/Attack/StabSweepEffect*.cs` ✅ ✅（蓄力/扫掠的表现参数 ✅ ✅✅ ✅）✅ ✅
- `Assets/Scripts/Editor/CombatDataTableWindow*.cs` ✅ ✅（既有总表窗口 ✅ ✅ —— 看招式数值 ✅ ✅ 用它 ✅ ✅✅ ✅）✅ ✅
- 相关交接 ✅ ✅：`plan/data-modularization-handoff-20261024.md` ✅ ✅、`memory/unity-project-understanding/talent-system.md` ✅ ✅ ✅
