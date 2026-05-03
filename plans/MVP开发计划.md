# 《一夫当关》MVP 开发计划

## 概述
基于已创建的团结引擎1.8.5（Unity 2022.3.62t7）3D项目，实现核心战斗系统（5列行列战斗 + 6种手势攻击 + 波次生成 + 敌人AI）+ 1个关卡 + 1个武将，达到可玩状态。

## 技术栈
- **引擎**: 团结引擎 1.8.5 (基于 Unity 2022.3.62t7)
- **渲染管线**: 内置渲染管线（Built-in Render Pipeline，3D项目默认）
- **语言**: C#
- **目标平台**: PC (后续可扩展移动端)

## 项目路径
- 团结引擎项目已位于: `C:/threeKingdomSlayer/threeKingdomSlayer/`
- 所有Assets目录下的操作均在此路径内

---

## 阶段一：项目初始化与环境搭建

### 1.1 项目已存在，无需创建
- ✅ 团结引擎1.8.5 3D项目已创建
- ✅ 项目路径：`C:/threeKingdomSlayer/threeKingdomSlayer/`

### 1.2 创建Assets目录结构
在Unity编辑器中或手动创建以下目录结构：
  ```
  Assets/
  ├── Scenes/           # 场景文件
  ├── Scripts/          # C#脚本
  │   ├── Core/         # 核心系统
  │   ├── Enemy/        # 敌人系统
  │   ├── Player/       # 玩家系统
  │   ├── Wave/         # 波次系统
  │   ├── UI/           # UI系统
  │   └── Managers/     # 管理器
  ├── Prefabs/          # 预制体
  ├── ScriptableObjects/# 可配置数据
  ├── Sprites/          # 精灵图
  ├── Animations/       # 动画
  └── Materials/        # 材质
  ```

### 1.3 安装必要包
- TextMeshPro (已自带 `com.unity.textmeshpro: 3.0.9`)
- 需要添加：`com.unity.inputsystem` (手势输入支持)
- 需要添加：`com.unity.render-pipelines.universal` (可选，如需URP)

### 1.4 创建基础场景
- 创建 `Scenes/MainMenu` 场景（主菜单）
- 创建 `Scenes/Battle` 场景（战斗场景）
- 在 `EditorBuildSettings` 中注册这两个场景
- 创建场景切换逻辑

---

## 阶段二：数据层 - ScriptableObject 配置系统

### 2.1 敌人配置 (EnemyConfig)
- 创建 `EnemyConfig` ScriptableObject
- 包含属性：生命值、站位数、攻击速度、攻击力、攻击距离、前进速度、架势值、眩晕时间、击飞时间
- 包含奖励配置（铜钱数量）

### 2.2 关卡配置 (StageConfig)
- 创建 `StageConfig` ScriptableObject
- 包含：关卡ID、关卡名称、波次列表、连杀奖励阈值、通关奖励

### 2.3 波次配置 (WaveConfig / RowConfig)
- 创建 `WaveConfig`：waveId、nextWaveDelay、isBossWave、rows列表
- 创建 `RowConfig`：enemyIds数组（长度5）
- **每波敌人上限**：1000个敌人（由策划在配置中决定具体数量）
- 波次配置示例：每波可配置10~200排（每排5个敌人），由策划自由设定

### 2.4 武将配置 (HeroConfig)
- 创建 `HeroConfig` ScriptableObject
- 包含：武将名称、所有玩家属性数值（生命值、复活次数、6种攻击属性等）

### 2.5 创建示例配置数据
- 创建1个敌人配置（如"骷髅兵"）
- 创建1个武将配置（如"赵云"）
- 创建1个关卡配置（第1关，含3~5波敌人）

---

## 阶段三：核心战斗系统 - 行列结构

### 3.1 列管理器 (ColumnManager)
- 实现 `Column` 类：维护每列的 `List<Enemy>`，index 0为最前排
- 实现 `ColumnManager`：管理5列，提供添加/移除/查询接口
- 实现补齐逻辑：当某列前排死亡，后排自动前移

### 3.2 敌人实体 (Enemy)
- 实现 `Enemy` MonoBehaviour
- 属性：当前生命值、最大生命值、当前架势值、状态机（Idle/Moving/Attacking/Stunned/Dead）
- 方法：TakeDamage()、TakePoiseDamage()、Stun()、Launch()、Die()
- 前进移动：根据排索引计算世界坐标，使用 MoveTowards 平滑移动

### 3.3 敌人透明度渐变系统（新增 - 根据用户补充需求）
- **核心规则**：玩家只能看到最近5排的敌人，越远的敌人透明度越高
- **透明度衰减配置**：在 `EnemyManager` 或 `StageController` 中提供一个可配置的 `float[] rowAlphaFactors` 字段参数，供策划自由设置每排的透明度系数
  - 示例配置：`[1.0f, 0.8f, 0.6f, 0.4f, 0.2f]`
    - 第1排（最前排）：100% 不透明
    - 第2排：80% 不透明
    - 第3排：60% 不透明
    - 第4排：40% 不透明
    - 第5排：20% 不透明
    - 第6排及以后：完全透明（不可见）
- **实现方式**：
  - 每帧根据敌人的当前排索引（相对于最前排的偏移量）计算目标透明度
  - 使用 `MaterialPropertyBlock` 或直接修改 `Renderer.material.color.a` 来设置透明度
  - 注意：使用 `MaterialPropertyBlock` 可以避免破坏GPU Instancing合批
  - 需要将材质设置为透明渲染模式（Fade/Transparent）
- **排索引计算**：敌人逻辑排索引 = 在所属列 `List<Enemy>` 中的 index
  - index 0 = 最前排 = 透明度系数 `rowAlphaFactors[0]`
  - index 1 = 第二排 = 透明度系数 `rowAlphaFactors[1]`
  - 以此类推，超出数组长度的排索引使用最后一个值或0

### 3.3 敌人管理器 (EnemyManager)
- 单例模式，管理所有存活的敌人
- 提供获取所有存活敌人列表的接口
- 处理敌人死亡事件

### 3.4 对象池 (EnemyPool)
- 实现通用对象池 `ObjectPool<T>`
- 按敌人ID分池管理
- 支持预创建和动态扩容

---

## 阶段四：玩家攻击系统（6种手势）

### 4.1 输入管理器 (InputManager)
- 使用 Unity Input System
- 检测点击、长按、滑动等手势
- 区分不同手势区域（屏幕左侧/右侧/中间）

### 4.2 攻击系统 (AttackSystem)
- 实现6种攻击类型：
  1. **戳击**：点击任意列 → 对该列前N排造成伤害
  2. **斩击**：划动屏幕 → 对所有列前N排造成伤害
  3. **穿刺**：长按某列后松开 → 对该列造成高额伤害
  4. **横扫**：从屏幕一侧长按后划向另一侧松开 → 对所有列造成伤害
  5. **挑飞**：在屏幕中间区域向上滑动 → 对所有列造成挑飞伤害+架势伤害
  6. **招架**：在红光提示时反方向划动 → 招架BOSS攻击
- 每种攻击独立计算冷却时间
- 攻击范围判定：根据配置的rangeRow决定影响多少排

### 4.3 玩家状态 (PlayerState)
- 管理玩家属性（生命值、复活次数、6种攻击属性等）
- 处理玩家受伤
- 处理复活逻辑

---

## 阶段五：波次生成系统

### 5.1 波次生成器 (WaveSpawner)
- 实现波次生成逻辑
- 清空条件：当前波次所有敌人死亡后，等待 nextWaveDelay 秒生成下一波
- 生成方式：在现有敌人列表最后追加新的排
- 最后一波清空后触发关卡胜利

### 5.2 关卡流程控制 (StageController)
- 管理关卡开始、进行中、胜利、失败状态
- 协调 WaveSpawner、EnemyManager、PlayerState 之间的交互

---

## 阶段六：UI系统

### 6.1 主菜单UI
- 新游戏按钮 → 进入战斗场景
- 游戏标题

### 6.2 战斗HUD
- 玩家生命值显示
- 连杀计数显示
- 当前波次显示
- 铜钱数量显示

### 6.3 战斗结果UI
- 胜利界面（显示奖励）
- 失败界面（重新开始按钮）

---

## 阶段七：美术资源（占位）

### 7.1 敌人占位资源
- 使用Unity基本几何体或免费资源作为占位
- 不同颜色区分不同类型

### 7.2 玩家/武将占位资源
- 简单角色占位

### 7.3 UI占位资源
- 使用TextMeshPro文字按钮和面板

---

## 阶段八：整合与测试

### 8.1 场景整合
- 将主菜单场景与战斗场景串联
- 确保场景切换时数据正确传递

### 8.2 战斗流程测试
- 测试完整战斗流程：开始 → 生成敌人 → 玩家攻击 → 敌人死亡 → 波次推进 → 关卡胜利
- 测试6种攻击手势的识别和伤害判定
- 测试敌人前进补齐逻辑
- 测试玩家死亡和复活

### 8.3 性能测试
- 测试同屏200~500个敌人时的帧率
- 优化对象池和更新频率

---

## 开发优先级与依赖关系

```
阶段一 (项目初始化)
    ↓
阶段二 (数据配置) ──────────────────────┐
    ↓                                     ↓
阶段三 (行列+敌人) ──→ 阶段五 (波次生成) ──→ 阶段八 (整合测试)
    ↓                                     ↑
阶段四 (玩家攻击) ────────────────────────┘
    ↓
阶段六 (UI) ──────────────────────────────┘
    ↓
阶段七 (美术占位) ──→ 可穿插在任何阶段
```

## 关键设计决策

1. **行列数据结构**：使用 `List<Enemy>` 按列存储，index 0为最前排，死亡时RemoveAt(0)实现逻辑前移
2. **攻击判定**：根据攻击类型获取目标列和影响排数，遍历对应敌人造成伤害
3. **波次生成**：采用"清空条件"策略，每波敌人全部死亡后才生成下一波
4. **每波上限**：1000个敌人，由策划在配置中自由设定每波的具体排数
5. **性能优化**：同屏上限500个敌人，使用对象池+GPU Instancing
6. **手势识别**：使用Unity Input System的触摸/鼠标事件，结合位置和持续时间判断手势类型
7. **透明度渐变**：玩家只能看到最近5排，提供 `float[] rowAlphaFactors` 字段供策划配置每排透明度系数
8. **透明度实现**：使用 `MaterialPropertyBlock` 设置材质透明度，避免破坏合批

---

## 项目任务清单 (Todo List)

### 已完成 ✅

- [x] 阶段一：项目初始化与环境搭建
- [x] 阶段二：数据层 ScriptableObject 配置系统
  - [x] EnemyConfig.cs — 敌人配置
  - [x] HeroConfig.cs — 武将配置
  - [x] StageConfig.cs — 关卡配置（含阵型参数）
  - [x] WaveConfig / RowConfig — 波次/排配置
  - [x] RowFormation.cs — 阵型计算器 + 预设表
- [x] 阶段三：核心战斗系统脚本
  - [x] Column.cs — 列数据结构
  - [x] ColumnManager.cs — 5列管理器
  - [x] Enemy.cs — 敌人实体（状态机/伤害/阵型位置/透明度）
  - [x] EnemyPool.cs — 对象池
  - [x] EnemyManager.cs — 敌人管理器（单例）
  - [x] StageController.cs — 关卡流程控制器（单例）
- [x] 阶段四：玩家攻击系统
  - [x] PlayerState.cs — 玩家状态（单例）
  - [x] AttackSystem.cs — 6种攻击实现
  - [x] InputManager.cs — 手势输入检测
- [x] 阶段五：波次生成系统
  - [x] WaveSpawner.cs — 协程驱动波次生成
- [x] 阶段六：UI系统
  - [x] BattleHUD.cs — 战斗HUD
  - [x] MainMenuUI.cs — 主菜单
- [x] BUG修复（3个）
  - [x] EnemyPool — enemyId在ResetEnemy后丢失
  - [x] EnemyManager — PlayerState伤害引用未连接
  - [x] Enemy — StageController null引用添加警告日志
- [x] 阵型系统（梯形/扇形内收）
- [x] Git仓库建立并推送至 GitHub
- [x] 13个 .meta 文件已添加并推送
- [x] 编译错误修复：Enemy.Die() 访问权限

### 待办（需手动操作） 🔴

**高优先级：**
- [ ] 创建 `MainMenu` 和 `Battle` 场景
- [ ] 在 `EditorBuildSettings` 中注册场景
- [ ] 创建 ScriptableObject 实例：
  - [ ] 骷髅兵 (EnemyConfig)
  - [ ] 赵云 (HeroConfig)
  - [ ] 第1关 (StageConfig，含3~5波敌人配置)
- [ ] 创建敌人预制体（带 Enemy 组件和 Renderer）
- [ ] 注册敌人预制体到 EnemyPool
- [ ] 在 Battle 场景中挂载所有管理器组件：
  - [ ] StageController
  - [ ] EnemyManager
  - [ ] EnemyPool
  - [ ] ColumnManager
  - [ ] PlayerState
  - [ ] AttackSystem
  - [ ] InputManager
  - [ ] WaveSpawner
  - [ ] BattleHUD
- [ ] 在 MainMenu 场景中挂载 MainMenuUI
- [ ] 设置材质为透明渲染模式（Fade/Transparent）

**中优先级：**
- [ ] 阶段七：美术资源占位（几何体/颜色区分）
- [ ] 创建 RowFormationPreset 实例（可选）

**低优先级：**
- [ ] 阶段八：整合与测试
  - [ ] 场景切换串联
  - [ ] 完整战斗流程测试
  - [ ] 6种攻击手势测试
  - [ ] 敌人前进补齐逻辑测试
  - [ ] 玩家死亡和复活测试
  - [ ] 同屏200~500敌人性能测试
