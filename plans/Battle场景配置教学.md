# Battle 场景配置教学

## 一、概述

Battle 场景是《一夫当关》的核心战斗场景，需要挂载 9 个管理器组件并配置多个 ScriptableObject 数据文件。本文档将手把手教你完成整个配置流程。

### 场景架构图

```
Battle场景 (GameObject层级)
├── StageController      # 关卡流程控制器（核心协调者）
├── EnemyManager         # 敌人管理器
├── EnemyPool            # 敌人对象池
├── ColumnManager        # 列管理器
├── PlayerState          # 玩家状态
├── AttackSystem         # 攻击系统
├── InputManager         # 输入管理器
├── WaveSpawner          # 波次生成器
├── BattleHUD (Canvas)   # 战斗UI
└── MainCamera           # 主摄像机
```

---

## 二、前置准备：创建 ScriptableObject 数据文件

在配置场景之前，必须先创建以下数据文件。在 Unity 菜单栏点击 **Assets → Create → 一夫当关** 即可看到所有可创建的配置类型。

### 2.1 创建敌人配置（EnemyConfig）

**路径**：`Assets/ScriptableObjects/Enemies/骷髅兵.asset`

**操作步骤**：
1. 在 Project 窗口右键 → **Create → 一夫当关 → 敌人配置**
2. 命名为 `骷髅兵`
3. 在 Inspector 中配置以下参数：

| 参数 | 类型 | 示例值 | 说明 |
|------|------|--------|------|
| `enemyName` | string | "骷髅兵" | 敌人显示名称 |
| `enemyId` | int | 1 | **唯一ID**，不可重复。用于对象池索引和波次配置引用 |
| `maxHealth` | float | 100 | 最大生命值 |
| `occupySlots` | int | 1 | 站位数（1~5）。1=占1列，2=占2列...5=占满5列 |
| `attackSpeed` | float | 1.0 | 每秒攻击次数。1.0=每秒攻击1次 |
| `attackDamage` | float | 10 | 每次攻击对玩家造成的伤害值 |
| `attackRange` | float | 1 | 攻击距离（排数）。1=只能攻击最前排 |
| `moveSpeed` | float | 1.0 | 前进速度（秒/排）。1.0=每1秒前进1排 |
| `maxPoise` | float | 50 | 最大架势值。归零时进入眩晕状态 |
| `stunDuration` | float | 1.5 | 眩晕持续时间（秒） |
| `launchDuration` | float | 2.0 | 击飞持续时间（秒） |
| `coinReward` | int | 10 | 击败后奖励铜钱数 |
| `stabDamageMultiplier` | float | 1.0 | 戳击伤害倍率。0.5=减半，2.0=翻倍 |
| `slashDamageMultiplier` | float | 1.0 | 斩击伤害倍率 |
| `pierceDamageMultiplier` | float | 1.0 | 穿刺伤害倍率 |
| `sweepDamageMultiplier` | float | 1.0 | 横扫伤害倍率 |
| `launchDamageMultiplier` | float | 1.0 | 挑飞伤害倍率 |
| `poiseDamageMultiplier` | float | 1.0 | 架势伤害倍率 |

**弱点系统设计建议**：
- 重甲兵：`stabDamageMultiplier=0.5`（戳击减半），`sweepDamageMultiplier=1.5`（横扫增伤）
- 轻骑兵：`pierceDamageMultiplier=2.0`（穿刺易伤），`slashDamageMultiplier=0.5`（斩击抗性）
- BOSS：所有倍率=1.0，但 `maxHealth` 和 `maxPoise` 大幅提高

### 2.2 创建武将配置（HeroConfig）

**路径**：`Assets/ScriptableObjects/Heroes/赵云.asset`

**操作步骤**：
1. 在 Project 窗口右键 → **Create → 一夫当关 → 武将配置**
2. 命名为 `赵云`
3. 配置参数：

| 参数 | 类型 | 示例值 | 说明 |
|------|------|--------|------|
| `heroName` | string | "赵云" | 武将名称 |
| `heroId` | int | 1 | 武将ID |
| `maxHealth` | float | 500 | 最大生命值 |
| `reviveCount` | int | 3 | 复活次数。0=不复活 |
| `reviveHealthPercent` | float | 0.5 | 复活时回复生命百分比。0.5=回复50% |
| `stabDamage` | float | 30 | 戳击伤害 |
| `stabRangeRows` | int | 1 | 戳击影响排数 |
| `stabCooldown` | float | 0.3 | 戳击冷却（秒） |
| `slashDamage` | float | 20 | 斩击伤害 |
| `slashRangeRows` | int | 2 | 斩击影响排数 |
| `slashCooldown` | float | 0.8 | 斩击冷却（秒） |
| `pierceDamage` | float | 80 | 穿刺伤害 |
| `pierceRangeRows` | int | 5 | 穿刺影响排数 |
| `pierceCooldown` | float | 1.5 | 穿刺冷却（秒） |
| `sweepDamage` | float | 40 | 横扫伤害 |
| `sweepRangeRows` | int | 3 | 横扫影响排数 |
| `sweepCooldown` | float | 1.2 | 横扫冷却（秒） |
| `launchDamage` | float | 25 | 挑飞伤害 |
| `launchRangeRows` | int | 2 | 挑飞影响排数 |
| `launchCooldown` | float | 1.0 | 挑飞冷却（秒） |
| `launchDuration` | float | 2.0 | 挑飞击飞持续时间 |
| `launchPoiseDamage` | float | 30 | 挑飞架势伤害 |
| `parryDamage` | float | 15 | 招架伤害 |
| `parryPoiseDamage` | float | 40 | 招架势伤害 |
| `damageBonusPercent` | float | 0.0 | 全局伤害加成百分比（0~2） |

### 2.3 创建关卡配置（StageConfig）

**路径**：`Assets/ScriptableObjects/Stages/第1关.asset`

**操作步骤**：
1. 在 Project 窗口右键 → **Create → 一夫当关 → 关卡配置**
2. 命名为 `第1关`
3. 配置关卡参数：

| 参数 | 类型 | 示例值 | 说明 |
|------|------|--------|------|
| `stageId` | int | 1 | 关卡ID |
| `stageName` | string | "第一关" | 关卡名称 |
| `clearCoinReward` | int | 100 | 通关奖励铜钱 |
| `killStreakThresholds` | List<int> | [10,25,50,100] | 连杀奖励触发阈值 |
| `rowAlphaFactors` | float[] | [1.0,0.8,0.6,0.4,0.2] | 每排透明度系数 |
| `maxVisibleRows` | int | 5 | 可见最大排数 |
| `formationMaxSpread` | float | 4.0 | 最前排半宽 |
| `formationMinSpread` | float | 0.5 | 最后排半宽 |
| `formationPowerCurve` | float | 1.2 | 内收曲线指数 |
| `rowSpacing` | float | 2.5 | 排间距（Z轴） |

#### 2.3.1 配置波次（Waves）

在 `StageConfig` Inspector 中，**波次配置** 区域可以添加和管理波次：

1. 设置 **波次数** 为需要的数量（例如 3）
2. 每个波次自动分配 Wave ID
3. 勾选 **BOSS波次** 标记 BOSS 波
4. 在 **敌人排配置** 列表中添加排（Rows）

#### 2.3.2 配置每排敌人（RowConfig）

每个 `RowConfig` 的 `enemyIds` 数组长度为 5，对应 5 个站位：

```
enemyIds = [1, 0, 1, 0, 1]
```
- `1` = 骷髅兵（enemyId=1）
- `0` = 空位（不生成敌人）

**敌人居中排列规则**：
- 如果 `enemyIds = [1, 1, 1, 1, 1]`（5个骷髅兵，每个占1列），总占位=5，起始列=0，填满5列
- 如果 `enemyIds = [1, 0, 1, 0, 1]`（3个骷髅兵），总占位=3，起始列=1，占据列1,2,3
- 如果有一个 `occupySlots=2` 的敌人，`enemyIds = [1, 2]`，总占位=1+2=3，起始列=1

**实战示例：第1关配置**

```
波次1（普通波）：
  排1（最远）：[1, 1, 1, 1, 1]  ← 5个骷髅兵
  排2：        [1, 0, 1, 0, 1]  ← 3个骷髅兵
  排3：        [0, 1, 0, 1, 0]  ← 2个骷髅兵

波次2（普通波）：
  排1：        [1, 1, 1, 1, 1]  ← 5个骷髅兵
  排2：        [1, 1, 1, 1, 1]  ← 5个骷髅兵
  排3：        [1, 0, 1, 0, 1]  ← 3个骷髅兵

波次3（BOSS波，勾选 isBossWave）：
  排1：        [2, 0, 0, 0, 0]  ← 1个BOSS（占5列）
```

---

## 三、创建敌人预制体

### 3.1 创建基础预制体

1. 在 Hierarchy 中右键 → **3D Object → Cube**，命名为 `Enemy_Skeleton`
2. 设置 Scale 为 (0.8, 1.0, 0.8) 使其看起来像人形
3. 添加 **Enemy** 组件
4. 添加 **Box Collider**（可选，用于点击检测）
5. 将预制体拖入 Project 窗口的 `Assets/Prefabs/` 目录

### 3.2 设置透明材质（重要）

> ⚠️ **注意**：材质颜色应保持白色（默认），不要设置为红色或其他颜色。
> 敌人的颜色由精灵图片（Sprite）本身提供，材质只负责控制透明度。
> 受伤闪白效果由代码自动处理（`Enemy.cs` 的 `UpdateAlpha()` 方法），
> 受到伤害时敌人会短暂变为白色，然后恢复。

1. 在 Project 窗口右键 → **Create → Material**，命名为 `Mat_Enemy`
2. 在 Inspector 中：
   - **Rendering Mode** → 选择 **Fade**（或 **Transparent**）
   - **Albedo** 颜色保持 **白色**（`#FFFFFF`）— 不要修改颜色
3. 将材质拖到预制体的 Renderer 组件上

### 3.3 注册预制体到 EnemyPool（自动方案，推荐）

**EnemyPool 已内置自动注册功能**，无需手动填写任何代码。

**操作步骤**：

1. 在 Project 窗口创建文件夹：`Assets/Resources/EnemyPrefabs/`
2. 将敌人预制体放入该文件夹
3. 预制体文件名必须按以下格式命名：
   ```
   Enemy_{enemyId}.prefab
   ```
   - `Enemy_1.prefab` → 自动注册为 enemyId=1
   - `Enemy_2.prefab` → 自动注册为 enemyId=2
   - `Enemy_3_Boss.prefab` → 自动注册为 enemyId=3（支持后缀）
4. 系统会在 `EnemyPool.Awake()` 时自动扫描并注册所有预制体

**验证方法**：
- 运行游戏后，在 Console 窗口查看日志：
  ```
  [EnemyPool] 自动注册敌人预制体: Enemy_1 → enemyId=1
  [EnemyPool] 自动注册敌人预制体: Enemy_2 → enemyId=2
  ```
- 如果看到 `未找到任何预制体` 的警告，说明 `Resources/EnemyPrefabs/` 文件夹为空或路径不对

**手动注册（备选方案）**：
如果不想使用自动注册，也可以在场景中选中 `EnemyPool` 对象，
在 Inspector 中取消勾选 `Auto Load From Resources`，
然后在代码中手动调用：
```csharp
EnemyPool.Instance.RegisterPrefab(1, skeletonPrefab);
```

---

## 四、配置 Battle 场景

### 4.1 创建空 GameObject 层级

在 Hierarchy 中创建以下结构：

```
Battle (空GameObject)
├── StageController
├── Managers (空GameObject)
│   ├── EnemyManager
│   ├── EnemyPool
│   ├── ColumnManager
│   └── WaveSpawner
├── Player (空GameObject)
│   ├── PlayerState
│   ├── AttackSystem
│   └── InputManager
├── UI (Canvas)
│   └── BattleHUD
└── Enemies (空GameObject，作为敌人实例的父节点)
```

### 4.2 挂载组件并配置引用

#### StageController

| 字段 | 引用目标 |
|------|----------|
| `stageConfig` | 拖入 `第1关.asset` |
| `waveSpawner` | 拖入 `WaveSpawner` 对象 |
| `enemyManager` | 拖入 `EnemyManager` 对象 |
| `playerState` | 拖入 `PlayerState` 对象 |
| `enemyPool` | 拖入 `EnemyPool` 对象 |

> **关于 `formationPreset` 字段**：
> - 此字段为 **可选配置**，默认保持 `None` 即可
> - 当 `formationPreset = None` 时，系统会自动使用 **方案 B（公式计算）**
>   根据 `formationMaxSpread`、`formationMinSpread`、`formationPowerCurve` 三个参数
>   自动计算梯形/扇形阵型
> - 如果你有自定义的阵型预设表（`RowFormationPreset` ScriptableObject），
>   可以拖入此字段，系统会优先使用预设表
> - **对于大多数关卡，保持 `None` 即可**，系统会自动生成合理的梯形阵型

#### EnemyPool

| 字段 | 引用目标 |
|------|----------|
| `poolRoot` | 拖入 `Enemies` 空对象（作为对象池父节点） |
| `defaultPoolSize` | 20（每个池默认预创建数量） |
| `autoLoadFromResources` | ✅ 勾选（默认开启，自动从 Resources 加载预制体） |
| `resourcesPrefabPath` | `EnemyPrefabs`（预制体存放路径，相对于 Resources 根目录） |

#### EnemyManager

| 字段 | 引用目标 |
|------|----------|
| `columnManager` | 拖入 `ColumnManager` 对象 |

#### WaveSpawner

| 字段 | 引用目标 |
|------|----------|
| `stageConfig` | 拖入 `第1关.asset` |
| `enemyPool` | 拖入 `EnemyPool` 对象 |
| `columnManager` | 拖入 `ColumnManager` 对象 |
| `enemyManager` | 拖入 `EnemyManager` 对象 |
| `enemyConfigs` | 拖入所有 `EnemyConfig` 文件（如 `骷髅兵.asset`） |

> **关于 `enemyConfigs` 字段**：
> - 将你创建的所有 `EnemyConfig` ScriptableObject 文件拖入此列表
> - 系统会在 `Start()` 时自动按 `enemyId` 索引缓存
> - 这样 EnemyConfig 文件可以放在 `ScriptableObjects/` 目录下，无需移动到 `Resources/` 文件夹
> - **如果不拖拽赋值**，系统会尝试从 `Resources/` 文件夹加载（需要将 EnemyConfig 放入 Resources 目录）

#### PlayerState

| 字段 | 引用目标 |
|------|----------|
| `heroConfig` | 拖入 `赵云.asset` |

#### AttackSystem

| 字段 | 引用目标 |
|------|----------|
| `columnManager` | 拖入 `ColumnManager` 对象 |
| `playerState` | 拖入 `PlayerState` 对象 |

#### InputManager

| 字段 | 引用目标 |
|------|----------|
| `attackSystem` | 拖入 `AttackSystem` 对象 |

### 4.3 配置 BattleHUD（Canvas UI）

1. 创建 **Canvas**（如果场景中没有）
2. 在 Canvas 下创建 UI 元素：

```
Canvas (Screen Space - Overlay)
├── HealthPanel (Panel)
│   ├── HealthSlider (Slider)
│   └── HealthText (Text - TMP)
├── ReviveText (Text)
├── KillCountText (Text)
├── CoinText (Text)
├── WaveText (Text)
├── CooldownPanel (Panel)
│   ├── StabCooldown (Image, Fill Method=Radial360)
│   ├── SlashCooldown (Image)
│   ├── PierceCooldown (Image)
│   ├── SweepCooldown (Image)
│   ├── LaunchCooldown (Image)
│   └── ParryCooldown (Image)
├── VictoryPanel (Panel, 初始隐藏)
│   ├── ResultCoinText (Text)
│   ├── RestartButton (Button)
│   └── MainMenuButton (Button)
└── DefeatPanel (Panel, 初始隐藏)
    ├── ResultCoinText (Text)
    ├── RestartButton (Button)
    └── MainMenuButton (Button)
```

3. 将 UI 元素拖入 `BattleHUD` 组件的对应字段

### 4.4 配置主摄像机

1. 选择 **Main Camera**
2. 设置 Position：`(0, 5, -5)`（俯视角度）
3. 设置 Rotation：`(45, 0, 0)`
4. 设置 Projection：**Orthographic**（正交投影更适合俯视游戏）
5. 设置 Size：`8`（根据实际显示范围调整）

---

## 五、配置流程检查清单

### 5.1 数据层检查

- [ ] `EnemyConfig` 已创建，`enemyId` 唯一且 > 0
- [ ] `HeroConfig` 已创建，所有攻击参数已设置
- [ ] `StageConfig` 已创建，至少包含 1 个波次
- [ ] 每个波次至少包含 1 排敌人配置
- [ ] 波次中引用的 `enemyId` 在 `EnemyConfig` 中存在

### 5.2 预制体检查

- [ ] 敌人预制体已创建，带有 `Enemy` 组件
- [ ] 材质已设置为 **Fade** 或 **Transparent** 渲染模式
- [ ] 预制体已注册到 `EnemyPool`

### 5.3 场景组件检查

- [ ] 所有 9 个管理器组件已挂载
- [ ] 所有组件引用已正确拖拽赋值
- [ ] `StageController.stageConfig` 已赋值
- [ ] `PlayerState.heroConfig` 已赋值
- [ ] `BattleHUD` 的所有 UI 引用已赋值

### 5.4 场景注册检查

- [ ] `Battle` 场景已添加到 **File → Build Settings → Scenes in Build**
- [ ] `MainMenu` 场景也已添加（用于场景切换）
- [ ] 两个场景的索引顺序正确（MainMenu=0, Battle=1）

---

## 六、常见错误及调试方法

### 6.1 编译错误

| 错误信息 | 原因 | 解决方法 |
|----------|------|----------|
| `CS0122: 'EnemyManager.OnEnemyDied(Enemy)' is inaccessible` | 访问了 private 方法 | 改为通过事件驱动，不要直接调用 |
| `CS0103: The name 'nextWaveDelay' does not exist` | Editor 脚本引用了不存在的字段 | 检查 `WaveConfig` 类，移除不存在的字段引用 |
| `CS0246: The type or namespace name 'RowFormationPreset' could not be found` | 缺少 using 或脚本顺序问题 | 确保 `RowFormation.cs` 已编译 |

### 6.2 运行时错误

| 现象 | 原因 | 调试方法 |
|------|------|----------|
| 点击屏幕无反应 | `InputManager.attackSystem` 未赋值 | 检查 Inspector 引用 |
| 敌人不移动 | `StageController.Instance` 为 null | 检查场景中是否有 `StageController` |
| 敌人不扣血 | `EnemyManager.OnEnemyAttackPlayer` 未调用 `TakeDamage` | 在 `EnemyManager.cs:121` 设断点 |
| 波次不生成 | `stageConfig.waves` 为空 | 检查 `StageConfig` 的波次配置 |
| 对象池报错 "未注册预制体" | 预制体未注册到 `EnemyPool` | 调用 `RegisterPrefab()` 或使用 Resources 自动加载 |
| 透明度不生效 | 材质不是透明模式 | 将材质 Rendering Mode 改为 Fade |
| 敌人位置错乱 | `rowIndex` 计算错误 | 在 `Enemy.UpdateWorldPosition()` 设断点查看坐标 |

### 6.3 调试技巧

**1. 启用 Gizmos 查看阵型**

在 [`RowFormation.cs`](threeKingdomSlayer/Assets/Scripts/Core/RowFormation.cs:130) 中有一个 `DrawFormationGizmos()` 方法，可以在场景中绘制阵型位置点。在 `StageController` 中添加：

```csharp
private void OnDrawGizmos()
{
    if (stageConfig != null)
    {
        RowFormation.DrawFormationGizmos(
            maxVisibleRows, rowSpacing,
            formationPreset, formationMaxSpread,
            formationMinSpread, formationPowerCurve
        );
    }
}
```

**2. 使用 Debug.Log 跟踪流程**

关键调试点：
- [`WaveSpawner.SpawnNextWave()`](threeKingdomSlayer/Assets/Scripts/Wave/WaveSpawner.cs:81) — 波次生成入口
- [`Enemy.Initialize()`](threeKingdomSlayer/Assets/Scripts/Enemy/Enemy.cs:60) — 敌人初始化
- [`Enemy.UpdateMovement()`](threeKingdomSlayer/Assets/Scripts/Enemy/Enemy.cs:166) — 敌人移动
- [`AttackSystem.TryExecuteAttack()`](threeKingdomSlayer/Assets/Scripts/Player/AttackSystem.cs:45) — 攻击执行
- [`PlayerState.TakeDamage()`](threeKingdomSlayer/Assets/Scripts/Player/PlayerState.cs:125) — 玩家受伤

**3. 使用断点调试**

在 Rider 或 Visual Studio 中：
1. 在关键方法上设置断点
2. 以 **Debug** 模式运行 Unity
3. 附加调试器到 Unity 进程
4. 触发对应操作，观察变量值

---

## 七、完整配置案例

### 案例：配置"第1关 - 长坂坡"

**需求**：3 个波次，第 3 波为 BOSS 波，使用赵云武将

#### 步骤 1：创建数据文件

```
Assets/ScriptableObjects/
├── Enemies/
│   ├── 骷髅兵.asset      (enemyId=1, maxHealth=100)
│   ├── 重甲兵.asset      (enemyId=2, maxHealth=300, stabMultiplier=0.5)
│   └── 张郃BOSS.asset    (enemyId=3, maxHealth=1000, occupySlots=5)
├── Heroes/
│   └── 赵云.asset         (heroId=1, maxHealth=500)
└── Stages/
    └── 第1关.asset        (stageId=1, waves=3)
```

#### 步骤 2：配置波次

**波次 1**（普通波，10个骷髅兵）：
```
排1: [1, 1, 1, 1, 1]  ← 5个骷髅兵
排2: [1, 1, 1, 1, 1]  ← 5个骷髅兵
```

**波次 2**（混合波，重甲兵+骷髅兵）：
```
排1: [1, 2, 1, 2, 1]  ← 3骷髅兵+2重甲兵
排2: [1, 1, 2, 1, 1]  ← 4骷髅兵+1重甲兵
排3: [1, 1, 1, 1, 1]  ← 5个骷髅兵
```

**波次 3**（BOSS波，勾选 isBossWave）：
```
排1: [3, 0, 0, 0, 0]  ← 1个BOSS（占5列）
排2: [1, 1, 1, 1, 1]  ← 5个骷髅兵护卫
```

#### 步骤 3：场景配置

1. 创建敌人预制体（材质保持白色，颜色由精灵图片提供）：
   - `Enemy_Skeleton.prefab` — Scale(0.8, 1.0, 0.8)
   - `Enemy_Heavy.prefab` — Scale(1.0, 1.2, 1.0)
   - `Enemy_Boss.prefab` — Scale(1.5, 2.0, 1.5)

2. 将预制体放入 `Assets/Resources/EnemyPrefabs/` 文件夹，按以下格式命名：
   - `Enemy_1.prefab`（骷髅兵，enemyId=1）
   - `Enemy_2.prefab`（重甲兵，enemyId=2）
   - `Enemy_3.prefab`（BOSS，enemyId=3）
   EnemyPool 会在 Awake() 时自动注册，无需手动操作

3. 挂载所有组件并拖拽引用

4. 设置摄像机为 Orthographic，Size=8

5. 在 Build Settings 中添加 MainMenu(0) 和 Battle(1) 场景

#### 步骤 4：运行测试

1. 从 MainMenu 场景开始，点击"开始游戏"
2. 观察波次 1 的 10 个骷髅兵是否正常生成
3. 测试戳击（点击）是否能击杀敌人
4. 观察敌人前进和补齐逻辑
5. 等待波次 1 清空后，波次 2 是否自动生成
6. 测试斩击（滑动）对重甲兵的伤害是否减半
7. 到达 BOSS 波后，测试挑飞和招架
8. 测试玩家死亡和复活
9. 通关后查看胜利面板和铜钱奖励

---

## 八、性能优化建议

### 8.1 对象池参数

| 参数 | 建议值 | 说明 |
|------|--------|------|
| `defaultPoolSize` | 20~50 | 每个敌人类型的预创建数量 |
| 动态扩容 | 启用 | 当池中对象不足时自动创建新实例 |

### 8.2 透明度优化

- 使用 `MaterialPropertyBlock`（已实现）避免破坏 GPU Instancing
- 超出 `maxVisibleRows` 的敌人完全透明（alpha=0），但仍在更新位置
- 优化建议：完全透明的敌人可以跳过渲染更新

### 8.3 更新频率优化

- `StageController.GetFormationOffset()` 每 0.2 秒刷新一次缓存（已实现）
- 敌人移动使用 `moveProgress` 插值，避免每帧计算复杂位置
- 冷却计时器在 `PlayerState.Update()` 中统一更新

---

## 九、扩展指南

### 9.1 添加新敌人类型

1. 创建新的 `EnemyConfig` ScriptableObject
2. 设置唯一的 `enemyId`
3. 创建对应的预制体
4. 注册到 `EnemyPool`
5. 在 `StageConfig` 的波次中引用新的 `enemyId`

### 9.2 添加新武将

1. 创建新的 `HeroConfig` ScriptableObject
2. 设置不同的攻击参数
3. 在 `PlayerState` 中替换 `heroConfig` 引用

### 9.3 添加新关卡

1. 创建新的 `StageConfig` ScriptableObject
2. 配置波次和敌人
3. 在 `StageController` 中替换 `stageConfig` 引用
4. 或在 `MainMenuUI` 中添加关卡选择功能
