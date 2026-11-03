---
id: kd_c56aa77b-ec2a-4554-a7ff-ddd1918cde55
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
summary: 天赋界面三分类（基础数值/招式解锁/三选一解锁）× 二维分页（左右划切类、上下划类内翻页）的实现方案稿（未动代码）：与局内的耦合面（3 个只读点＋页 1 是唯一侵入点）、现状锚点、TalentCatalog 与 nodeId 稳定规则、自动切页算法（合法断点＝不跨边、单页 8 行、无解即报错）、存档过滤未知 id 策略、8 个组件拆分、Pager 手势状态机与误购抑制、程序化美术逐件方案、6 条实测风险、B0–B5 分批验收。
---

# 天赋界面「三分类 × 二维分页」实现方案

> ⚠️ **状态更新（2026-11-02）。**
> **数据层大部分已落地并保留**（`TalentCatalog` / `upgradeConfig` 载荷 / `TalentNodeInfo` / `TalentPaginator` / 环检测 DFS 修复 / `nodeId` 规则 / 存档策略 / 3 树拆分）—— 裁决见 `plan/talent-scheme-alignment-20261102.md`。
> **但方案稿里的 `col` / `row` 整数格点、`statId` / `statAdd` 通用载荷** —— **未实现**，仍需补（见裁决文档 §2.2 修正 ② ③）。
> **表现层（面板 / 二维分页 / 美术）全部重做。**
>
> **仍可参考**：§0 耦合面分析（天赋数据被局内读的 3 个只读点）、§2.3 `nodeId` 规则、§2.5 存档策略、§2.4 分页推导规则（「不跨边」这条硬约束成立）。

> 状态：方案稿（未动任何代码与资产）。范围仅界面层 + 数据契约 + 存档过滤 + 校验器 + 程序化美术。
> 前置结论（已与用户确认）：三类（基础数值 / 招式解锁 / 三选一解锁）；**类间不做前置**；**布尔节点（无等级）**；**自动切页且每页数量不固定**；**左右划 = 切类，上下划 = 类内翻页**；存档**不保留**已失效节点、匹配不上即清掉。

---

## 0. 与局内（战斗）的耦合面

**界面层与局内零关联**：天赋是 additive overlay 场景（`SceneFlowManager.SwitchOverlay("TalentTree")`，`Assets/Scripts/Managers/SceneFlowManager.cs:134`），只在打开时加载，不暂停 `Time.timeScale`（同文件 195–207 的 serialized 开关，默认不暂停）。三页改造全部发生在 `Assets/Scripts/UI/Locus.UI.asmdef` 内。

**但天赋数据被局内读**，现有 3 个读点（都是只读，本次不新增写）：

| 读点 | 位置 | 作用 |
|---|---|---|
| 招式门 | `Assets/Scripts/MoveSystem/PlayerMoveStateMachine.cs:326–344` | 招式表里有节点指向该招式但未解锁 → 不许出招 |
| 招架门 | `Assets/Scripts/Player/Combat/AttackSystem.cs:390–400` | 手势招架（当前 `parryGestureEnabled=false` 已下线，链路保留） |
| 三选一入池 | `Assets/Scripts/Core/TalentUpgradeRequirementProvider.cs` → `UpgradePoolSampler`（`UpgradeChoiceManager.cs:97` 注册） | 升级条目的 `requiredTalentIds` 未满足则不进池 |

→ 因此：**面板/分页/美术改动对局内零风险**；唯一会侵入局内的是「页 1 基础数值」需要的**读值消费点**（现无任何代码读天赋改玩家数值），单独放到最后一批。

---

## 1. 现状锚点（改造起点）

| 事实 | 位置 |
|---|---|
| 节点数据 = `nodeId/displayName/description/coinCost/moveConfig/flagId/prerequisiteIds/treePosition/icon/hidden` | `Assets/Scripts/Data/TalentTreeConfig.cs` |
| 前置是 List，`IsAvailable` 要求**全部**前置已解锁（AND，"两边都点上"已成立） | `Assets/Scripts/Core/TalentTreeState.cs:33–40` |
| 连线按「每节点→它的每个前置」逐边画（多入边汇聚已成立） | `Assets/Scripts/UI/TalentTreePanel.cs:204–219` |
| 面板零场景运行时生成（标题/铜钱/说明栏/返回键都是 `CreateImage/CreateText`） | 同文件 `Build()` |
| 三态贴图与配色已接 | 场景 `TalentTree.scene`（`nodeSize 132`、`connectorThickness 14`） |
| 参考分辨率 1080×1920，Match 0.5 | 场景 CanvasScaler |
| 存档 = `List<string> unlockedTalents`（存 nodeId，非下标） | `Assets/Scripts/Data/SaveData.cs:26` |
| 迁移钩子 = `SaveSections.MigrateAll` → `TalentSaveSection.Migrate` | `Assets/Scripts/Data/Save/ISaveSection.cs:37–50` |
| 项目内 0 处 `ScrollRect`、0 个滑动组件 | 全 Assets 搜索 |
| 现有程序化贴图先例（可照抄写法） | `Assets/Scripts/Core/PixelHitEffectManager.Sprites.cs` |

---

## 2. 数据契约

### 2.1 新增 `TalentCatalog`（ScriptableObject，`Locus.Data`）

```
public class TalentCatalog : ScriptableObject
{
    public TalentTreeConfig[] categories;   // 长度 3：基础数值 / 招式 / 三选一
}
```

用途（三处）：面板取类、跨资产 id 唯一性校验、读档过滤时的"合法 id 集合"。Inspector 拖 3 个引用，符合 `design/anti-ghost-reference.md`。

### 2.2 `TalentNodeConfig` 字段增补

| 字段 | 类型 | 说明 |
|---|---|---|
| `col` | int | 0/1/2（左中右）。与 `row` 一起取代手填坐标 |
| `row` | int | 行号，0 = 第一行（自上而下） |
| `treePosition` | Vector2 | **保留**，语义改为「相对行列格点的像素微调」，默认 (0,0) |
| `statId` / `statAdd` | string / float | **仅页 1 用**（新载荷）。页 2/3 留空 |

布尔模型下不需要 rank 字段、不需要每级费用。

### 2.3 `nodeId` 规则（存档稳定性的根）

1. id 跟**内容**走，不跟位置走（"挪位置不改 id，换语义换新 id"）
2. **禁位置派生**：`TalentTreeConfig.OnValidate()` 现在会在 id 为空时写 `"node_" + i`（`TalentTreeConfig.cs:160–173`）→ 改为**留空即报错**，不再自动补
3. id 在 `TalentCatalog` 的 3 个资产间**全局唯一**

### 2.4 分页完全运行时推导（不入档）

页 = 「前置闭合」的行区间装箱结果。推导规则：

- 合法断点：相邻两行之间**没有边跨越**（父/子分列两侧）
- 每页总行数 ≤ `rowsPerPage`（SerializedField，默认 **8**；内容区 ~1300 设计像素 ÷ 行距 150）
- 贪心：自顶部行累加，超限时回退到最近合法断点
- 无解（某棵子树根→叶路径 > `rowsPerPage`）→ 校验器报错，要求数据拆成多个独立子树（用户已选这条）

产物：`pageIndex → (rowStart, rowEnd, nodes[])`，每次打开面板重算。

### 2.5 存档策略（用户口径：不保留）

在 `TalentSaveSection.Migrate()` 里加一步（幂等，现有管线已每次读档跑）：

```
合法集合 = TalentCatalog 三资产的全部 nodeId
unlockedTalents = unlockedTalents.Where(id => 合法集合.Contains(id)).Distinct().ToList()
```

- 无迁移表、无退役黑名单（死 id 当场清掉，复用也不会白送）
- 若用户要的是「有一条对不上就整份清空」，把上面改成 `if (any invalid) Clear()` 即可（一行差别）

---

## 3. 组件拆分（都在 `Locus.UI`）

| 组件 | 职责 | 与现状关系 |
|---|---|---|
| `TalentPageHost` | 持 `TalentCatalog`、当前 `(category, pageIndex)`、切页重建 | 现 `TalentTreePanel` 的 `tree` 单字段 + `_built` 守卫要拆开 |
| `TalentPager` | 两轴拖拽 → 主轴锁定 → 网格吸附；对外暴露 `IsDragging`、`CurrentPage` | 新增（项目无滑动组件） |
| `TalentGraphRenderer` | 按 `(col,row)` 生成节点与连线（逐边） | 从 `BuildNodes()` 抽出 |
| `TalentNodeView` | 单节点的贴图/名称/价格/选中态 | 替代现在 5 个并列 `List` 按下标硬指 |
| `TalentDetailPanel` | 详情 + 确认按钮 | 替代现在一行 `_descText` |
| `TalentPageIndicator` | 横向 3 类页签 + 纵向页码点 | 新增 |
| `ITalentPayloadPresenter` + 3 实现 | 按载荷（数值 / 招式 / 池条目）喂详情卡文案 | 新增；三页唯一的分支点 |
| `ProceduralUiSprites` | 程序化贴图生成 + 静态缓存 | 新增（见 §5） |

关键实现约定：
- 切页 = 销毁节点层全部子对象后重建（并顺手修 §6 的第 1 条缺陷）
- 所有节点状态一次性从 `SaveManager.Load()` 取已解锁集合，避免每节点各 `Load()` 一次
- 面板贴图统一走 SerializedField 槽位；程序化生成只做**槽位为空时**的兜底，美术随时可替换

---

## 4. 手势与交互

### 4.1 状态机

```
Idle
 └─ BeginDrag: 记 anchor、内容起始 anchoredPosition
Dragging
 ├─ 未锁轴：|delta| > 12（设计像素）时锁定主轴 = |dx|>|dy| ? 横 : 纵
 ├─ 按主轴跟手，另一轴死锁
 └─ EndDrag:
     主轴位移 > 页尺寸 × 0.25  或  甩动速度 > 阈值 → 翻页
     否则 → 回弹
Snap: DOTween 到目标页（仓库已有 DOTween）
```

- 边缘（第一页/最后一页/第一类/最后一类）→ 阻尼或直接挡住
- 横向与纵向是两个独立索引：横向 = `category`，纵向 = `pageIndex`（每类各自记忆，切回时回到原页码）

### 4.2 与节点点击的冲突

- `TalentPager` 挂在**节点层的父级**（Unity 的 drag 事件会沿层级向上找到第一个 `IDragHandler`），不必给每个节点加拖拽脚本
- 节点 `Button.onClick` 在抬手时仍会触发 → 节点点击前判 `pager.IsDragging || 位移超阈值` 直接吞掉，否则一次滑动会误购
- 拖拽接收层必须压在 `dim` 遮罩之上、节点层之下（现在 dim 铺满且 `raycastTarget=true`）

### 4.3 两段式点击

点节点 = 选中（高亮 + 详情卡填充），详情卡里的「解锁」按钮才真正扣钱；点空白清除选中。原因：现在 `OnNodeClicked` 点即购买，二维分页后误触概率显著上升。

---

## 5. 美术：先用程序化绘制自用（可行，且有先例）

参考图本身不是重美术（金属框 + 蚀刻图标 + 折线），节点/框/连线全部可用程序化生成近似，美术后期按槽位替换即可，不改代码。项目里已有同款做法：`PixelHitEffectManager.Sprites.cs`。

### 5.1 逐件方案

| 件 | 现状 | 程序化方案 | 将来替换槽位 |
|---|---|---|---|
| 节点框 | `TT_Node_*` 454×457 三态已有，实际够用 | 若需新规格：圆角方 + 竖向渐变 + 内描边 + 四角亮线，生成为 **9-slice**（`Sprite.Create(..., border)`） | `nodeLocked/Available/Unlocked` |
| 连线 | `TT_Link` 1091×144 已有 | 无需新做；备选：1D 渐变条横向拉伸 | `connector` |
| 类页签（3 个） | **缺** | 圆角条 + 选中态加下划线/提亮，9-slice，2 张（常态/选中） | 新增 `tabNormal/tabActive` |
| 页码点 | **缺** | 12px 圆点，2 张（当前/其它） | 新增 `dotOn/dotOff` |
| 详情卡底 | 现在只有 `_descText` | 圆角底板 + 内描边（同节点框生成器换参数） | 新增 `detailPanel` |
| 返回/确认按钮 | 现在是纯色块 + 文字 | 圆角底板 9-slice，文字已有 TMP | 新增 `buttonNormal/buttonPressed` |
| 背景 | `TT_Panel` 681×667 被拉伸到 1000×1840（`spriteBorder` 全 0，无九宫格） | 给 `TT_Panel` 设边距改 `Image.Type = Sliced`，或程序化生成一块大底板 | `panelBackground` |

### 5.2 生成规范（避免踩坑）

- 以 **2× 设计像素**生成（如 132×132 节点 → 264×264 纹理），`filterMode = Bilinear`、`wrapMode = Clamp`、`mipmap = false`
- 每个件只生成一次，`static` 缓存（照 `TalentTreePanel.GetCircleSprite()` 的写法），不随切页重建
- 圆角/描边/渐变参数全部 SerializedField（或 `ProceduralUiSprites` 的配置结构），便于美术验证数值后出正式图
- 生成器只做「槽位为空」的兜底 → 美术拖图进来即覆盖，符合项目 anti-ghost-reference

---

## 6. 风险与已知坑（都有实测证据）

1. **切页必踩**：`BuildNodes()` 只 `Clear()` 5 个 List、**不 Destroy 旧 GameObject**（`TalentTreePanel.cs:192–265`）；页数一多每次切页叠加整套节点+连线并挡点击 → 第一批先修
2. **位置派生 id**：`OnValidate` 写 `"node_" + i`（`TalentTreeConfig.cs:160–173`）→ 挪顺序即换身份
3. **环检测不完整**：只沿第一个前置回溯（`cur = ps[0]`，`TalentTreeConfig.cs:136–148`）→ "两边都点上"的树会漏检
4. **overlay 不拦战斗输入**：`InputBlockReason` 无「UI 打开」类，`SceneFlowManager` 也不申请拦截 → 战斗态下打开页签，滑动会同时被 `InputManager` 原始触摸读到（落地页态是否已被拦需装配时验证）
5. **跨页边 = 悬空节点**：禁令已定，靠自动切页 + 校验器保证；无解时必须改数据
6. **单页行容量 8 行**：一条 12 行链无解；"汇合节点靠下"会把左右两支绑进同一页
7. **三类共用 id 空间**：目前每个资产各自 `Validate()`，无跨资产唯一性检查

---

## 7. 分批实施与验收

| 批 | 内容 | 验收 |
|---|---|---|
| **B0** ✅ | 修 `BuildNodes` 不 Destroy；新增 `TalentCatalog`；面板改「多资产 + 当前索引」（不改视觉） | 编译无错、Console 0 error/warn；catalog 资产 slot0 = 主树、`CollectNodeIds` 回 9 个 id；场景未改（`tree` 仍生效，行为与改造前一致） |
| **B1** ◐ | `TalentPager`（两轴 + 主轴锁定 + 吸附 + 抑制点击）+ 类页签 + 页码点 | ✅ 三页**并排**在 `Viewport`（RectMask2D 裁切）里，拖拽时整体**跟手位移**、松手按 ease-out 吸附到整页（中间采样 -635/-840/-917/-934/-938）；✅ 边缘 0.35 阻尼、主轴锁定、拖后 0.25s 内忽略点击；✅ 点页签也走同一吸附动画；⬜ 上下翻页（需自动切页）、页码点、参考图造型 |
| **B2** | 自动切页算法 + 校验器 4 条（id 全局唯一 / 禁自动 id / 最长路径 ≤ rowsPerPage / 页内前置闭合）+ 环检测补全 | 造一条 9 行链能在编辑器里报出"无合法断点" |
| **B3** ◐ | `TalentDetailPanel` + 两段式点击 + 「缺哪边没解锁」提示 | ✅ 点节点只选中 → 底部详情卡（图标/名称/描述/花费/前置缺口/确认按钮）→ 按钮才扣钱；✅ 提示会点名缺哪个节点；⬜ 参考图造型未做 |
| **B4** ◐ | 程序化美术替换（节点框/页签/详情卡/按钮/页码点）+ `TT_Panel` 改九宫格 | ✅ 新增 `ProceduralUiSprites`（圆角板/圆点/圆环，带九宫格 border，白色可染色，静态缓存）：每页底板、页签常态/选中、详情卡、确认按钮、节点兜底圆环已接入；页码点多页时才显示。⬜ 节点外框仍用 `TT_Node_*` 现有素材；截图 `_art_process/Authoring/talent_ui/talent_page_procedural_v1.png` |
| **B5**（可选，最后） | 页 1 数值载荷 + 局内读值消费点 | 解锁前后玩家属性有变化；仅这一批碰战斗代码 |

B0–B4 全部不碰战斗代码、不改现有节点配置。

---

## 7.1 B0 之后的三处修复（2026-11-01，已实测）

用户报“点不了天赋按钮”，实测三处问题（前两处是既有缺陷，不是三页改造引入）：

| 问题 | 根因 | 修法 |
|---|---|---|
| Hub 两张入口卡（天赋/选关）点不到 | 卡内所有 Graphic 的 `raycastTarget=false`，Button 的 `targetGraphic` 还指向不可射线的 Icon | 把底板 `Plate`/`Base` 的 `raycastTarget=true` 并把 `targetGraphic` 指回底板（`Assets/Scenes/Battle.scene`） |
| 天赋页一个节点都没有 | 建树资产时漏了 `EditorUtility.SetDirty`，`SaveAssets` 只保存了空壳（磁盘 `nodes: []`） | 重新写入并 SetDirty，回读校验 3/5/6 个节点 |
| 点面板「返回」后卡在空 overlay | `Close()` 只 `SetActive(false)`，不关 overlay 场景 | `Close()` 同时 `SceneFlowManager.RequestCloseOverlay()` |

Play Mode 实测：点卡 → overlay 打开（Stats 页 5 节点 + 4 连线）→ 三页签切换（Stats/Moves/Pools）→ 点根节点扣 1 铜钱并解锁、重复点击不重复扣 → 返回关闭 overlay。测试消耗已还原（0 已解锁 / 300 铜钱）。

## 8. 未决

1. **页 1 数值消费点落在哪**（`HeroConfig.maxHealth` / `damageBonusPercent` 现在是静态值，需新增一个读天赋的入口）——唯一会侵入局内的一处，放到 B5
2. **`rowsPerPage` 固定 8 还是按屏幕/设备自适应**（影响是否要按安全区算高度）
