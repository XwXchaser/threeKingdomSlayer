# 三国无双割草游戏 — 模块索引

## 项目概览
一夫当关型单英雄防守游戏，5 列阵型战斗。玩家通过手势输入控制武将释放 6 种攻击（Stab/Slash/Pierce/Sweep/Launch/Parry），击退呈梯形阵型推进的敌人波次。

## 模块列表

| 序号 | 模块 | 文件数 | 职责概要 |
|---|---|---|---|
| 1 | [Attack](Attack.summary.md) | 1 | 攻击波视觉特效：沿 Z 轴旅行或原地固定命中，DOTween 驱动 |
| 2 | [Core](Core.summary.md) | 9 | 阵型数据结构、敌人/武将/关卡 ScriptableObject 配置、排位置计算、大招系统 |
| 3 | [Editor](Editor.summary.md) | 1 | StageConfig/WaveConfig 自定义 Inspector 绘制工具 |
| 4 | [Enemy](Enemy.summary.md) | 2 | 敌人状态机、移动/攻击/受击/死亡动画、按 enemyId 对象池 |
| 5 | [Managers](Managers.summary.md) | 3 | 编排层：伤害跳字、敌人生命周期协调、关卡流程（开始/胜/负/重开） |
| 6 | [Player](Player.summary.md) | 3 | 手势到攻击输入映射、攻击执行与目标选择、玩家属性与冷却 |
| 7 | [UI](UI.summary.md) | 6 | 战斗 HUD、场景转场模糊动画、浮动伤害跳字、主菜单、精灵动画、大招按钮 |
| 8 | [Wave](Wave.summary.md) | 1 | 按 StageConfig 波次生成敌人、阵型前压、波次完成监控 |

## 模块依赖关系

```
                    ┌──────────────┐
                    │ StageController│ (编排器)
                    └──────┬───────┘
           ┌───────────────┼───────────────┐
           │               │               │
    ┌──────▼──────┐ ┌─────▼──────┐ ┌──────▼──────┐
    │ WaveSpawner │ │EnemyManager│ │  PlayerState │
    └──────┬──────┘ └─────┬──────┘ └──────┬──────┘
           │               │               │
    ┌──────▼──────┐ ┌─────▼──────┐ ┌──────▼──────┐
    │  EnemyPool  │ │ColumnManager│ │ AttackSystem │
    └──────┬──────┘ └─────┬──────┘ └──────┬──────┘
           │               │               │
    ┌──────▼──────┐ ┌─────▼──────┐ ┌──────▼──────┐
    │    Enemy    │ │   Column   │ │  AttackWave  │
    └─────────────┘ └────────────┘ └──────┬──────┘
           │                               │
    ┌──────▼──────┐               ┌───────▼──────┐
    │DamageNumber │               │  InputManager │
    │  Manager    │               └──────────────┘
    └──────┬──────┘
           │
    ┌──────▼──────┐
    │DamageNumber │
    └─────────────┘
```

**大招系统**（独立，读 AttackSystem 写入，读 EnemyManager）：
```
    ┌──────────────┐
    │UltimateSystem│ (singleton)
    └──────┬───────┘
           │
    ┌──────▼──────┐
    │UltimateEffect│ (abstract)
    └──────────────┘
```

**配置层**（ScriptableObject，无运行时依赖）：
`EnemyConfig`, `AttackSkillConfig`, `UltimateSkillConfig`, `HeroConfig`, `StageConfig`(+`WaveConfig`/`RowConfig`), `RowFormationPreset`

**静态工具**（无 MonoBehaviour 依赖）：`RowFormation`

**UI 层**（读取 singleton，写入屏幕）：`BattleHUD`, `ChargeIndicatorController`, `EnemyHealthBar`, `CameraManager`, `MainMenuUI`, `PingPongAnim`

## 全局枚举位置

| 枚举 | 定义文件 |
|---|---|
| `DamageType` | `Assets/Scripts/Enemy/Enemy.cs` |
| `EnemyState` | `Assets/Scripts/Enemy/Enemy.cs` |
| `AttackType` | `Assets/Scripts/Player/PlayerState.cs` |
| `StageState` | `Assets/Scripts/Player/PlayerState.cs` |

## 关键技术栈

- **动画/动效**：DOTween (移动、缩放、淡入淡出、序列)
- **UI 文字**：TextMeshPro
- **输入**：Legacy Input Manager（鼠标 + 触摸统一处理）
- **渲染管线**：Built-in Render Pipeline (BIRP)
- **配置**：ScriptableObject（敌人/武将/关卡/阵型预设）
- **资源加载**：`Resources.Load` + `Resources.LoadAll`
- **对象池**：`EnemyPool`（敌人）、`DamageNumberManager`（跳字）

## 核心设计模式

- **Singleton**：`ColumnManager`, `EnemyPool`, `DamageNumberManager`, `EnemyManager`, `StageController`, `AttackSystem`, `InputManager`, `PlayerState`, `WaveSpawner`, `UltimateSystem`
- **Object Pool**：`EnemyPool`（按 enemyId 分池）、`DamageNumberManager`（DamageNumber 对象池）
- **Event-Driven**：C# event 贯穿全部模块，实现松耦合通信
- **Factory Method**：`AttackWave.Create()` 静态工厂
- **Chain of Responsibility**：Column 链式补齐（`OnRushMoveComplete` 事件链）
