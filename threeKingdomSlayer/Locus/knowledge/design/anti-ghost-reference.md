---
id: kd_10290628-4da5-4830-b7c8-6bb44b2ae599
injectMode: inherit
summary: 禁止「幽灵引用」的架构规范：所有运行时数据引用必须从 Inspector 可追踪，禁止 Resources.Load/静态缓存/字符串 ID 查找，数据应直接在 Prefab 或通过 Inspector 拖拽的 ScriptableObject 引用上。
aiEditMode: inherit
---

## 核心原则

**所有运行时数据引用必须可从 Inspector 追踪到源头，禁止通过 Resources.Load / 静态缓存 / 字符串 ID 查找等间接方式获取配置数据。**

违反此原则的引用称为「幽灵引用」——在 Inspector 中无法看到实际使用的数据来源，配置时无法判断游戏运行时实际读取的是哪个数据。

## 禁止的幽灵引用模式

### 1. Resources.Load 动态加载配置
```csharp
// ❌ 禁止：通过 enemyId 去 Resources 里查找 ScriptableObject
EnemyConfig config = Resources.Load<EnemyConfig>($"EnemyConfigs/Enemy_{enemyId}");
```

### 2. 静态字典缓存 + 字符串/ID 查找
```csharp
// ❌ 禁止：静态缓存让数据来源不可见
private static Dictionary<int, EnemyConfig> cache = new();
EnemyConfig config = cache[enemyId];
```

### 3. ScriptableObject 作为中间数据层（配置应直接在 Prefab 上）
```csharp
// ❌ 禁止：Prefab 持有 Enemy 组件但数据在另一个 ScriptableObject 里
public EnemyConfig config;  // 运行时动态赋值，Inspector 看不到真实数据

// ✅ 正确：所有字段直接序列化在 Prefab 的 Enemy 组件上
[SerializeField] float maxHealth;
[SerializeField] int enemyId;
```

### 4. Scene GameObject 字段被外部 Asset 静默覆盖
```csharp
// ❌ 禁止：Inspector 填了值，但运行时被某个 ScriptableObject 覆盖
// 导致策划在 Inspector 看到的值和实际运行值不一致

// ✅ 正确：要么用 Inspector 值，要么用 Asset 引用，二选一且可追溯
[SerializeField] FormationConfig formationConfig;  // 直接拖拽引用，一目了然
```

## 允许的引用模式

### 方案 A：数据直接在 Prefab/GameObject 上
- 适合：每个实例数据不同的情况（如 Enemy 属性）
- 优势：打开 Prefab 就能看到全部数据，无间接层

### 方案 B：ScriptableObject 通过 Inspector 直接引用
- 适合：多个对象共享同一配置（如 FormationConfig）
- 要求：必须通过 `[SerializeField]` 字段 + Inspector 拖拽赋值
- 优势：引用链完全可追踪（GameObject → 字段 → Asset 文件）

## 检查清单

开发新功能前逐项确认：

1. 运行时获取配置数据的入口是否来自 Inspector 直接赋值的字段？
2. 是否存在 `Resources.Load` 动态加载配置？→ 必须消除
3. 是否存在 `static Dictionary` 缓存配置？→ 必须消除
4. 打开 Prefab/Scene GameObject 的 Inspector，能否直接看到或追踪到所有运行时数据？
5. 是否存在「Inspector 填了值但运行时被其他东西覆盖」的情况？→ 必须消除

## 历史教训

### 2026-10-23 全工程清零（可作为后续新代码的参照）

本工程最后一处 `Resources.Load` 真实代码（5 处）已全部改成 Inspector 引用 ✅，
四类做法各有适用场景 ✅：

| 场景 | 做法 |
|---|---|
| **对象池要一批预制体**（`EnemyPool`） | 组件上加 `List<GameObject>` ✅，Inspector 里逐个拖 ✅（enemyId 仍从名字解析 ✅，行为不变 ✅） |
| **项目级共享素材**（拖影材质） | 新增 `SharedVisualAssets`（服务层登记表 ✅），**登记在持有 Inspector 引用的那一层**（`AttackSystem` ✅），表现层只读登记表 ✅；取不到时安全降级 ✅ |
| **弹窗素材**（弃置弹窗三张图） | 挂在**弹窗 prefab 自己**的组件上（`UpgradeChoicePopup` 的字段 ✅），运行时读同 prefab 的组件 ✅（`ItemDiscardPopup` 是运行时 AddComponent 的 ✅，自己身上存不住序列化字段 ✅） |
| **完全没人用的类** | 直接删（`StageRegistry` ✅ —— `AssetDatabase` 实测引用数 0 ✅ 才删 ✅） |

> ⚠️ 两个坑：
> ① 改字段类型 / 删字段后**保存场景会把旧字段的值清空** ✅（Unity 重新序列化时按新类定义来 ✅）
>    → 搬数据前先把原值从 git 里取出来 ✅，搬完**从磁盘回读确认** ✅；
> ② 改 `MonoBehaviour` 字段的类型（`FakeRouteRuntime` → `MonoBehaviour`）**不丢引用** ✅
>    （场景里存的是 fileID ✅），但 Inspector 上会变成通用 Object 字段 ✅（类型安全靠代码里 `as` 转换 ✅）。

## 历史教训（EnemyConfig 合并）

本次 EnemyConfig 合并就是典型的幽灵引用清理：
- 敌人数据存放在 `EnemyConfig` ScriptableObject 中
- 运行时通过 `WaveSpawner.GetEnemyConfig(enemyId)` + 静态缓存查找
- Prefab 上的 Enemy 组件不持有真实数据，Inspector 中不可见
- 3 个 EnemyConfig.asset 与 3 个 Enemy_*.prefab 之间存在隐式关联（通过 enemyId 匹配）
- 合并后：所有数据直接在 Prefab 的 Enemy 组件上，打开即见

同理 FormationConfig 的清理：
- `DefaultFormation.asset` 通过 Resources 隐式加载
- StageController 上 `stageConfig` 的 `formation` 字段写入数据但被覆盖
- 清理后：FormationConfig 直接挂载到 StageController 的 Inspector 字段中
