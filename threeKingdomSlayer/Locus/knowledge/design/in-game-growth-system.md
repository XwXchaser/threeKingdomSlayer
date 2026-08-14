---
id: kd_27ef541b-f3c9-4b70-9c9d-0699fc1bcb0f
injectMode: inherit
summary: 三套局内奖励系统的分期实现计划：总击杀奖励 → 连击奖励 → 经验三选一。第三期详案含等级系统、效果执行架构、连续升级流程、自适应UI、攻击解锁、前置条件系统。
aiMaintained: inherit
---

# 局内成长系统设计

## 总览

三套局内奖励系统，分期实现：
1. **总击杀奖励**（第一期，✅ 已完成）— 累计击杀里程碑 → 一次性铜钱/经验/回血
2. **连击奖励**（第二期，✅ 已完成）— 连击阈值 → 限时BUFF
3. **经验三选一**（第三期，未开始）— 经验升级 → 暂停浮窗选本局永久加成

## 系统间关系（已澄清）

- 总击杀奖励与三选一奖励**无关联**：`RewardType.GrantRandomUpgrade` 为预留值，暂不使用。总击杀里程碑不暂停游戏，仅弹即时奖励UI
- 连击的 `StatModifier` 框架与三选一的 `UpgradeEffect` 数值修正层**共享同一个攻击力计算入口**，但效果执行器**分开注册**（连击管纯数值修正，三选一管数值修正+行为注入+攻击解锁）
- 三者共用 `PlayerState` 作为运行时数据载体
- 三者共用 `SaveManager` 作为持久化出口（仅总击杀）

## 核心区别

| | 总击杀 | 连击 | 三选一 |
|---|---|---|---|
| 触发方式 | 累计击杀数 | 连续命中 | 经验满升级 |
| 奖励持续 | 即时一次性 | 限时BUFF | 本局永久 |
| 持久化 | 铜钱写存档 | 否 | 否 |
| 重置条件 | 本局不重置 | 断连归零 | 新对局重置 |
| 叠加规则 | 每里程碑一次 | 同类刷新/异类共存 | 可叠加（含等级系统） |
| 暂停游戏 | 否 | 否 | 是（连续升级逐个弹窗） |

---

## 第二期 连击奖励 实现概要

### 架构
- `ComboManager` — 单例，监听敌人受击，积累连击数，到达阈值触发 Buff
- `BuffManager` — 单例，管理限时 Buff 的添加/移除/刷新，每帧更新并广播 `StatModifier`
- `ComboBuffConfig` — ScriptableObject，配置 resetDelay、hitIncrementMode、triggers 列表
- `StatModifier` — 属性修正数据类（statId, modifierType, value）
- `ComboDisplayUI` — 连击特效显示（FillImage + StaticImage + 缩放动画），使用 Image.color.a 控制显隐
- `ComboUI` — 连击数文字显示（预留）

### 关键设计决策
- 连击倒计时使用 `Time.time`（暂停时冻结）
- 缩放动画使用 `Time.deltaTime`（暂停时冻结）
- UI 显隐使用 `Image.color.a` 方案（避免 SetActive 自引用 Bug）
- 每帧同敌人只计一次（`HitIncrementMode.PerEnemy` 模式）
- 同类 Buff 刷新时长，异类共存

### 配置
- `Assets/ScriptableObjects/Combo/ComboBuffConfig.asset` — 10 连击 → combo_atk_10（5秒 50% ATK）

### Bug 经验
- SetActive 自引用在 Play Mode 下不可靠（见 `skill/ui-visibility-patterns.md`）
- CanvasGroup 在 Canvas 子节点上可能导致全 Canvas 消失
- Image 拉伸：`preserveAspect=true` + sizeDelta 匹配 sprite 比例

---

## 第三期 经验三选一 设计详案

### 3.1 触发与流程

1. **经验来源**：仅击杀敌人获得（Enemy 预制体新增 `expReward` 字段）
2. **升级判定**：每次获得经验后检查 `currentExp >= expRequiredPerLevel[currentLevel]`
3. **连续升级**：若一次经验溢出导致多级升级，逐个弹窗处理：
   ```
   暂停游戏 → 弹出第1次三选一 → 玩家选择 → 应用效果
   → 立即检测经验是否够再升一级
   → 够则刷新弹窗内容弹出第2次（仍暂停）
   → 不够则恢复游戏
   ```
4. **经验溢出保留**：升级时扣除所需经验，溢出部分保留到下一级
5. **暂停规则**：弹窗期间 `Time.timeScale = 0`，仅响应弹窗 Canvas 的输入

### 3.2 奖励类型

| 类别 | 示例 | effectType |
|------|------|----------|
| 数值强化 | 伤害+15%、攻速+10% | `damage_multiplier`, `attack_speed` |
| 机制修饰 | 每3次攻击触发戳击 | `on_attack_trigger` |
| 资源经济 | 10%击杀掉铜钱、经验加成 | `on_kill_chance` |
| 攻击解锁 | 获得大风暴技能（屏幕画圈释放） | `unlock_attack` |

### 3.3 等级系统

- 每种 `UpgradeDefinition` 最高 10 级
- 同名奖励再次被选择时升级，数值逐级叠加
- 示例：伤害提升1级=+10%，2级=+20%（10%+10%），以此类推
- 玩家 `acquiredUpgrades` 中记录 `UpgradeDefinition` + 当前等级

### 3.4 效果文本配表

每个 `UpgradeDefinition` 需存储效果描述模板，支持动态数值替换：
- `displayName`：奖励名称（如「神力」）
- `description`：当前等级描述（如「造成伤害提升 10%」）
- 每级数值由 `UpgradeEffect` 中的 `floatValue` / `intValue` 乘以等级得出

### 3.5 效果执行架构

两层注册表，分离职责：
- **数值修正层**（共享）：连击 `StatModifier` 和三选一 `UpgradeEffect` 的数值型效果统一进入同一个攻击力/攻速/移速计算入口
- **行为执行层**（三选一专属）：`IEffectExecutor` 注册表处理 `on_attack_trigger`、`on_kill_chance`、`unlock_attack` 等行为型效果
- 连击 Buff 过期时回退数值修正，不影响三选一的永久加成

### 3.6 前置条件系统（组合解锁）

`UpgradeDefinition` 可配置前置条件：
```csharp
public List<UpgradePrerequisite> prerequisites;

[System.Serializable]
public class UpgradePrerequisite {
    public UpgradeDefinition requiredUpgrade;  // Inspector拖拽
    public int requiredLevel;                  // 最低需要几级
}
```

- 前置条件不满足的 `UpgradeDefinition` 不会出现在随机池中
- 示例：「选项A 3级 + 选项B 2级」→ 解锁选项C进入抽取池
- 已在池中的选项若已达 `maxLevel`，同样排除

### 3.7 unlock_attack 架构

`unlock_attack` 类型的效果引用现有 `AttackSkillConfig` 作为攻击骨架，等级提供伤害增量：

```
UpgradeDefinition (effectType="unlock_attack")
  ├─ AttackSkillConfig baseAttackConfig   // Inspector拖拽引用
  ├─ floatValue                           // 每级伤害增量
  └─ 运行时：伤害 = baseAttackConfig.damage + floatValue × (等级-1)
```

`AttackSystem` 需改造以支持运行时注册攻击：
- switch 改为 `Dictionary<AttackType, Func<int, bool>>` 注册表
- 新增 `UnlockedAttackRegistry`：`Dictionary<string unlockId, AttackSkillConfig>`
- 解锁攻击执行时从 `PlayerUpgradeState` 获取当前等级 → 计算最终伤害

`InputManager` 需预留手势识别器扩展点：
- 当前手势识别硬编码在 `ProcessGesture` 中
- 新增可插拔 `IGestureRecognizer` 接口列表
- 在 QTE 拦截之后、常规手势分类之前遍历识别器
- 新手势类型（如画圈）作为 `IGestureRecognizer` 实现加入列表

### 3.8 UpgradeDefinition 完整数据结构

```csharp
[CreateAssetMenu(menuName = "一夫当关/升级奖励定义")]
public class UpgradeDefinition : ScriptableObject
{
    public string upgradeId;                   // 唯一标识
    public string displayName;                 // 显示名
    public string descriptionTemplate;         // 描述模板 "造成伤害提升 {0}%"
    public UpgradeRarity rarity;               // 稀有度
    public int maxLevel = 10;                  // 最高等级
    public string effectType;                  // damage_multiplier | attack_speed | on_attack_trigger | on_kill_chance | unlock_attack

    // 数值型通用
    public float floatValue;                   // 每级叠加值
    public int intValue;                       // 整数参数
    public string stringValue;                 // 字符串参数

    // unlock_attack 专属
    public AttackSkillConfig baseAttackConfig; // Inspector拖拽

    // 前置条件
    public List<UpgradePrerequisite> prerequisites;
}
```

### 3.9 随机抽取逻辑

`UpgradePoolConfig` 抽取流程：
1. 过滤：移除前置条件不满足的选项
2. 过滤：移除已达 `maxLevel` 的选项
3. 按稀有度权重随机抽稀有度
4. 从对应稀有度池中按权重随机抽选（不重复）
5. 若池中候选不足3个，降级从其他稀有度池补足

### 3.10 UI 设计

- **独立 Canvas**：Sort Order 高于 BattleHUD，专用于暂停级弹窗
- **自适应布局**：选项数量 3/4/5 竖向堆叠排列，通过 GridLayoutGroup + ContentSizeFitter 自适应
- **卡片背景按稀有度**：普通/稀有/传说各有独立边框背景图，运行时根据 `UpgradeDefinition.rarity` 挂载
- **面板背景**：可拉伸纯色背景图，九宫格切图（9-slice）
- **间距可调**：Inspector 中暴露 spacing 参数供调整选项间距

### 3.11 数据配置

```
Assets/ScriptableObjects/Upgrades/
  Definitions/           — 每个升级一个 .asset (UpgradeDefinition)
    DamagePlus.asset
    AttackSpeed.asset
    OnAttackTrigger_Stab.asset
    Whirlwind_Unlock.asset
    ...
  UpgradePoolConfig.asset  — 池+稀有度权重+前置过滤逻辑
  DefaultExpCurve.asset    — 经验曲线
```

### 3.12 扩展接口

- 局外成长可预置 `acquiredUpgrades`（开局携带能力）
- 局外成长可增加选项数量（3选1 → 4选1 → 5选1）
- `OnBeforeUpgradeChoice` 事件可修改候选列表
- `OnUpgradeChosen` 事件供外部监听
- `IGestureRecognizer` 接口预留新攻击手势（画圈、双指等）

---

## 策划配置

- `Assets/ScriptableObjects/Combo/ComboBuffConfig.asset`
- `Assets/ScriptableObjects/KillReward/TotalKillMilestoneConfig.asset`
- `Assets/ScriptableObjects/Upgrades/` (定义 + 池 + 经验曲线)
