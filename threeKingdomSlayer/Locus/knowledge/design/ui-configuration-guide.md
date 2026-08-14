---
id: kd_b24529ec-012a-4142-a00f-c963a5e24743
injectMode: inherit
summary: 战斗 UI 配置教学指南：如何为武将创建 HUD 预制体、配置 HeroConfig、接线 BattleHUD、配置 Boss 血条，以及常见错误排查
aiMaintained: inherit
---

# 战斗 UI 配置指南

本指南面向不熟悉代码的使用者，说明如何在 Unity 编辑器中为武将配置 HUD、为敌人配置血条，以及 BattleHUD 全局字段的接线方法。

---

## 一、目录结构速览

| 用途 | 文件夹 |
|------|--------|
| 武将 HUD 预制体 | `Assets/Prefabs/UI/` |
| 武将数据资产 | `Assets/ScriptableObjects/Warrior/` |
| Boss 血条预制体 | `Assets/Prefabs/UI/` |
| 敌人预制体 | `Assets/Resources/EnemyPrefabs/` |
| 武将精灵 | `Assets/Sprites/<武将名>/` |

---

## 二、为新增武将配置 HUD

### 步骤 1：准备精灵素材

在 `Assets/Sprites/` 下创建以武将名命名的文件夹（如 `Assets/Sprites/guanyu/`），放入以下精灵：

- 血量条背景图
- 血量条填充图
- 血量条底框图
- 技能冷却图标（每个技能一张，共最多 6 张）
- 技能充能填充图标（通常与冷却图标相同，但 Fill Method 设为 Radial 360）

> 所有精灵的 Texture Type 必须设为 **Sprite (2D and UI)**。

### 步骤 2：创建英雄 HUD 预制体

1. 在 `Assets/Prefabs/UI/` 右键 → **Create → Prefab**，命名为 `HeroHUD_<武将名>`（如 `HeroHUD_Guanyu`）。
2. 双击打开预制体编辑模式。
3. 在根节点上点击 **Add Component**，搜索并添加 `HeroHUD` 脚本。

### 步骤 3：搭建 HUD 子物体

在预制体根节点下创建以下子物体（右键根节点 → Create Empty，然后添加对应组件）：

#### 3a. 血量条 — Health(Slider)

- 创建空物体，重命名为 `Health(Slider)`
- 添加 **Slider** 组件
- 结构：`Background`（Image，放血量条背景精灵）、`Fill Area/Fill`（Image，放填充精灵，Image Type 设为 Filled）、`Handle Slide Area`（可删除 Handle 子物体，不需要拖拽手柄）
- Slider 组件的 Direction 设为 **Left to Right** 或 **Bottom to Top**（取决于你的血量条方向）

#### 3b. 血量文字 — Health

- 创建空物体，重命名为 `Health`
- 添加 **TextMeshPro - Text (UI)** 组件
- 设置字体大小、颜色、对齐方式
- 初始文字可留空或写 `500/500`

#### 3c. 技能冷却图标（6 个）

为每个技能创建一个 Image 子物体，命名格式见下表。每个冷却图标内部再创建一个子 Image（命名为 `Image`），作为充能指示器。

| 技能 | 冷却图标名称 | 充能指示器（子物体） |
|------|-------------|-------------------|
| 刺击 (Stab) | `StabCooldown(Image)` | `Image` |
| 挥砍 (Slash) | `SlashCooldown(Image)` | `Image` |
| 穿刺 (Pierce) | `PierceCooldown(Image)` | `Image` |
| 横扫 (Sweep) | `SweepCooldown(Image)` | `Image` |
| 挑空 (Launch) | `LunchCooldown(Image)` | `Image` |
| 格挡 (Parry) | `ParryCooldown(Image)` | `Image` |

配置规则：
- **外层冷却图标**：Image Type = Simple，放技能精灵，Color 可设为灰色
- **内层充能 Image**：Image Type = Filled，Fill Method = Radial 360，Fill Origin = Top，放充能精灵（通常与冷却图标同一张），Color 可设为亮色

> 如果该武将没有某个技能（如无格挡），可以删除对应的冷却图标物体，HeroHUD 组件上对应的字段留空即可。

### 步骤 4：接线 HeroHUD 组件

选中预制体根节点，在 Inspector 的 HeroHUD 组件中，将各子物体拖入对应字段：

| HeroHUD 字段 | 拖入的子物体 |
|-------------|------------|
| Health Slider | `Health(Slider)` 的 Slider 组件 |
| Health Text | `Health` 的 TMP_Text 组件 |
| Revive Text | （可选）复活次数文字 |
| Stab Cooldown Image | `StabCooldown(Image)` 的 Image 组件 |
| Stab Charge Fill | `StabCooldown(Image)/Image` 的 Image 组件 |
| ... （其余 5 个技能同理）| |

> 不需要复活的武将，Revive Text 留空即可。

### 步骤 5：在 HeroConfig 中挂载

1. 在 `Assets/ScriptableObjects/Warrior/` 找到该武将的资产（如 `Hero_Guanyu.asset`）。
2. 在 Inspector 中找到 **Hero HUD Prefab** 字段。
3. 将刚创建的 `HeroHUD_Guanyu` 预制体拖入。
4. 保存（Ctrl+S）。

---

## 三、BattleHUD 全局字段配置

打开 `Assets/Scenes/Battle.scene`，在 Hierarchy 中找到 `BattleHUD(Canvas)`，在 Inspector 中确认以下字段已接线：

| 字段 | 应拖入的对象 | 说明 |
|------|------------|------|
| Hero HUD Parent | `BattleHUD(Canvas)/HeroHUDParent` | 英雄 HUD 实例化的父容器 |
| Kill Count Text | `KillCount` 的 TMP_Text | 击杀计数 |
| Coin Text | `CoinCounter/TotalText` 的 TMP_Text | 铜钱显示 |
| Wave Text | `WaveText` 的 TMP_Text | 波次显示 |
| Boss Health Bar Prefab | （暂留空，等 Boss 血条预制体创建后拖入）| Boss 血条模板 |
| Boss Bars Parent | （暂留空）| Boss 血条父容器 |
| Victory Panel | `Victory(panel)` | 胜利面板 |
| Defeat Panel | `Defeat(panel)` | 失败面板 |
| Result Coin Text | 胜利/失败面板内的铜钱文字 | 结算铜钱显示 |

---

## 四、敌人 Boss 血条配置（预备）

### 敌人预制体

在 `Assets/Resources/EnemyPrefabs/` 中找到敌人预制体，Inspector 中有一个 **Boss Health Bar Prefab** 字段：

- 普通敌人：留空
- Boss 敌人：未来拖入 Boss 血条预制体（`Assets/Prefabs/UI/BossHealthBar.prefab`）

### BattleHUD 全局配置

在 `BattleHUD(Canvas)` 中：
- **Boss Health Bar Prefab**：拖入默认 Boss 血条预制体（所有 Boss 共用）
- **Boss Bars Parent**：创建一个空物体作为 Boss 血条的父容器（建议放在 Canvas 下，命名为 `BossBarsParent`）
- **Max Boss Bars**：同时显示的 Boss 血条数量上限（默认 5）

> 如果某个 Boss 需要独特的血条样式，在该敌人的预制体上单独指定 Boss Health Bar Prefab，战斗时会优先使用敌人自带的。

---

## 五、创建新的 ScriptableObject 资产

### 英雄配置 (HeroConfig)

1. 在 `Assets/ScriptableObjects/Warrior/` 右键 → **Create → HeroConfig**（如未显示此菜单项，确认 `HeroConfig.cs` 中有 `[CreateAssetMenu]` 特性）。
2. 填写字段：
   - **Hero Name**：武将显示名（如 `Guanyu`）
   - **Hero Id**：唯一数字 ID（如 `102`）
   - **Max Health**：最大生命值
   - **Revive Count**：复活次数（0 表示不可复活）
   - **Revive Health Percent**：复活后血量百分比（0.5 = 50%）
   - **Skill Configs**：拖入技能配置资产（`Assets/ScriptableObjects/Skills/` 下）
   - **Ultimate Skill Config**：拖入大招配置资产
   - **Hero HUD Prefab**：拖入对应的英雄 HUD 预制体
   - **Damage Bonus Percent**：伤害加成百分比（0 表示无加成）

### 技能配置 (AttackSkillConfig / UltimateSkillConfig)

1. 在 `Assets/ScriptableObjects/Skills/` 右键 → **Create → AttackSkillConfig** 或 **UltimateSkillConfig**。
2. 命名格式建议：`<武将名>_<技能名>`（如 `Guanyu_Slash`）。
3. 按字段说明填写伤害、冷却时间、效果参数等。

---

## 六、常见错误与注意事项

### 1. 精灵引用丢失

如果预制体中的 Image 显示为白色方块或 Missing Sprite：
- 检查精灵文件是否被删除或移动
- 确保精灵的 Texture Type 为 Sprite (2D and UI)
- 在 Image 组件的 Source Image 字段重新拖入

### 2. HeroHUD 字段未接线

战斗开始后英雄 HUD 不显示或不更新：
- 检查 HeroConfig 的 Hero HUD Prefab 是否已拖入
- 检查 HeroHUD 预制体内部的所有字段是否已拖入对应子物体
- 查看 Console 是否有报错信息

### 3. 新武将没有技能图标

- 使用通用占位精灵（如 `Assets/Sprites/skill_stab.png`）
- 也可以直接复用现有武将的精灵文件夹

### 4. 预制体修改未生效

在 Project 窗口中双击预制体进入编辑模式修改，**不要**在 Scene 中直接修改已实例化的对象（修改会被 Prefab Override 覆盖或丢失）。

### 5. 冷却指示器不显示

- 确认内层 Image 的 Image Type 为 **Filled**
- 确认 Fill Method 为 **Radial 360**
- 确认 Fill Origin 为 **Top**
- 确认有 Source Image 精灵

### 6. 不要移动 Scene 中的 HeroHUDParent

`HeroHUDParent` 是英雄 HUD 的运行时父容器。它在场景中是一个空的 RectTransform，战斗开始时由代码自动实例化 HeroHUD 预制体到其下。不要删除或重命名它。

### 7. 脚本编译后字段变空

如果修改了 `BattleHUD.cs` 或 `HeroHUD.cs` 的字段定义（增删字段、改名），Unity 重新编译后 Inspector 中的已有接线可能丢失。修改脚本字段后需要重新检查场景和预制体的接线。
