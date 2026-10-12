---
id: kd_a4421ad4-a4cf-4881-aac8-c630a043ac3b
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# Stab 3D 长枪：现状交接 —— 骨骼绑定与物理效果工作入口

## 0. 这份文档的用途

后续工作目标：**(a) 在 Blender 里给这杆枪绑定骨骼；(b) 在 Unity 或 Blender 里制作物理效果（红缨摆动、枪身动态）；(c) 完全替换原本的面片长枪 `Stab.prefab`。**

本文只记录**已实测的事实**和**尚未完成的部分**，供下一轮开工直接使用。带「待确认」标记的是我没有验证、不能当作结论的内容。

---

## 1. 一句话现状

3D 长枪已入库、材质接线完成、prefab 已建好并与 `Stab.prefab` 同姿态同长度；**戳击（Stab）这一条路径的 5 份配置已切到 3D 枪**，其余 4 种攻击和 QTE 仍是旧面片。**没有骨骼、没有蒙皮、没有动画、没有物理。** 距离「完全替换面片」还差 3 个消费方的改造（§6）。

---

## 2. 资产清单（精确路径与 GUID）

| 资产 | 路径 | GUID |
|---|---|---|
| FBX 模型 | `Assets/Models/Weapons/FantasySpear/FantasySpear.fbx` | `440a2857e305921439e1c100402226d6` |
| 材质 | `Assets/Models/Weapons/FantasySpear/FantasySpear.mat` | `a79d96e04c4a5644393463edfc2a875a` |
| BaseColor 贴图 | `Assets/Models/Weapons/FantasySpear/Textures/FantasySpear_BaseColor.png` | `b3b5ee6e49e3ed943b113bd5c66cfd06` |
| Normal 贴图 | `Assets/Models/Weapons/FantasySpear/Textures/FantasySpear_Normal.png` | `2c8229bd6a83e38479d1b01a63536088` |
| 3D prefab | `Assets/Prefabs/FantasySpear3D.prefab` | `27d8262bc67b9864ea33c9576052febc` |
| **旧面片 prefab（待替换）** | `Assets/Prefabs/Stab.prefab` | `3ebe15c25c9ebda4fb9cd0b5ea7df39c` |

FBX 与两张贴图**已在 `d9758e8`（109/J1 提交）入库**，本轮只补了材质、导入设置和 prefab。

### 模型规格

- 网格名 `fantasy spear 3d model`，**2377 顶点 / 1423 三角面**，1 个 submesh
- 有法线、有切线（Mikk 计算）、1 套 UV
- 单材质 `FantasySpear`，Standard shader，`_NORMALMAP` 已启用，`_Metallic=0.1` `_Glossiness=0.25`
- 贴图两张 1024×1024：BaseColor（sRGB）、Normal（NormalMap 类型）

### Blender 源

- 原始对象名 `fantasy spear 3d model`，709 顶点 / 1423 面
- Blender 内尺寸 `0.19317 × 0.12432 × 0.97731`，**长轴为 +Z**
- 现场备份：`Library/Locus/tmp/spear-3d-prefab/fantasy_spear_session_copy.blend`（20 MB，`save_as_mainfile(copy=True)` 存的，**不改动你的实时场景**）
- 导出脚本：`Library/Locus/tmp/spear-3d-prefab/export_spear_fbx.py`

---

## 3. prefab 当前结构（rigging 的接入点）

```
FantasySpear3D                       localPos(0,0,0)  rot(90,0,0)   scale(8.19,8.19,8.19)
└─ Model                             localPos(0,-0.49,0)  rot(270.02,0,0)  scale(100,100,100)
   MeshFilter → fantasy spear 3d model
   MeshRenderer → FantasySpear
BoxCollider                          size(0.19,0.98,0.12)  center(0,0,0)  isTrigger=false
```

**世界尺寸（实例化后，不覆盖 prefab 自身 transform）：**

| 轴 | 尺寸 |
|---|---|
| X | 1.581 |
| **Y（长轴）** | **8.000** |
| Z | 1.018 |

**轴心在武器几何中心**（`min.y = -4.000`，`max.y = +4.000`），不是底部。这一点是刻意的：`ChargeStabVisual.ApplyChargePose()` 用 `tipPosition - axis * halfLength` 定位，要求轴心居中；`Stab.prefab` 也是中心轴心。

### 轴向约定（rigging 必须先对齐这条）

- `FantasySpear3D` 根节点 `rot(90,0,0)` → **prefab-local +Y = 武器长轴**
- `Model` 子节点 `rot(270.02,0,0)` 把 Blender 的 Z 轴转到 Unity Y 轴
- 因此在 prefab 局部空间里：**+Y 沿枪身、X/Z 是截面**
- Blender 侧：长轴是 **+Z**，截面在 XY。枪尾在 z≈0，另一端 z≈0.9773

**待确认**：Blender 里 +Z 的哪一端是枪尖、哪一端是尾鐏，我没有逐顶点确认。红缨（最粗处 1.2–1.4 世界单位）位于 43%–73% 区间，枪尖（顶部 10%，0.548 宽）在远端。绑定时请自行核对，不要假定。

---

## 4. 导入设置（改动前请先读）

```yaml
ModelImporter:
  globalScale: 1
  useFileScale: true        # fileScale = 0.01
  bakeAxisConversion: false # 关键：保持 Blender 已写好的 Y-up，不要再转一次
  importNormals: Import
  importTangents: CalculateMikk
  materialImportMode: None  # 材质由我们自己的 .mat 提供
  animationType: None
  importAnimation: false
  isReadable: false         # ← 绑定时若需要运行时读网格，要改成 true
  addCollider: false
```

**`bakeAxisConversion = false` 是踩过坑的结论。** 我一度设成 `true`，结果把 Blender 已经写好的 Y-up 又绕 X 转了一次，枪变成躺倒的 Z 轴方向。最终方案见上表。

---

## 5. 游戏内驱动链路（完全替换必须覆盖这些）

### 5.1 戳击（Stab）—— 已经切到 3D 枪

**真实入口是招式表，不是技能表：**

```
MoveTable_Zhangfei  roots: gesture=Tap → Zhangfei_Jab1
                                          ├─ Tap              → Jab2
                                          └─ Hold (charge≥1)  → StabR2
                       Jab2  Tap → Jab3
                       Jab3  SwipeVertical(charge≥1) → LaunchFinisher
MoveTable_Zhangfei  states: 空
```

手势映射（`MoveGestureDefaults.ResolveAttackType`）：
`Tap→Stab` / `Hold→Pierce` / `竖滑+蓄力→Launch` / `竖滑未蓄力→Parry` / `横滑+蓄力→Sweep` / `横滑未蓄力→Slash`

**本轮已改指 3D 枪的 5 份配置**（`attackType == Stab`，其余 4 类一律未动）：

- `Assets/ScriptableObjects/Moves/Zhangfei/Zhangfei_Jab1.asset`（gameplay 主入口）
- `Assets/ScriptableObjects/Moves/Zhangfei/Zhangfei_Jab2.asset`
- `Assets/ScriptableObjects/Moves/Zhangfei/Zhangfei_Jab3.asset`
- `Assets/ScriptableObjects/Moves/Zhangfei/Zhangfei_StabR2.asset`（蓄力终结）
- `Assets/Prefabs/UI/Skills/Zhangfei_Stab.asset`（仅直通模式回退用，`moveTable` 为空时才生效）

**核心驱动脚本 `Assets/Scripts/Attack/StabSweepEffect.cs`（945 行）的层级：**

```
StabRay (空物体, LookRotation 对准本列)
└─ VisualOffsetRoot   倾角 + 蓄力指向偏摆
   └─ ShakeRoot       命中震动，独立 realtime 计时
      └─ DeformRoot   承载 prefab 原始 rot(90,0,0) 与 scale(8.19)
         └─ [prefab 实例]   localPos/Rot/Scale 被重置为 0 / identity / 1
```

代码对 prefab 的操作全部施加在**父级**，所以 prefab 自身的 90° 与缩放被完整保留。**这意味着任何骨骼层级请建在 prefab 内部（`Model` 之下或与之平级），不要依赖根节点的旋转。**

### 5.2 本轮为支持网格做的代码改动（`b6dcd8d`）

`StabSweepEffect.cs` 的视觉长度原先只认 `SpriteRenderer`，用网格时整段枪尖定位被跳过（枪尖前伸约 4 世界单位）。改为：

```csharp
private static float MeasureVisualLength(GameObject visual)
{
    var sr = visual.GetComponentInChildren<SpriteRenderer>();
    if (sr != null && sr.sprite != null)
        return sr.sprite.bounds.size.y * visual.transform.localScale.y;   // 精灵：数值与改前一致
    Bounds b = ComputeVisualLocalBounds(visual);                          // 网格：包围盒八角 → 根节点空间
    return Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z)) * visual.transform.localScale.y;
}
```

三处引用点统一：`Create()` 的枪尖对齐偏移、`Initialize()` 的半长、`LateUpdate()` 的形变补偿、`GetVisualTipPosition()` 的命中点。

**Play Mode 实测（同一 `Create` 入口、同一参数）：**

| prefab | halfVisualLength | 枪尖距 rayOrigin | deformRoot 位置 |
|---|---|---|---|
| 面片 | 4.000 | 0.500 | (−0.55, −0.60, −7.45) |
| 网格 | 4.000 | 0.500 | (−0.55, −0.60, −7.45) |

完全一致。网格版完整播放了蓄势→刺出→回收，`deform.z` 实测轨迹：−7.74 → −7.84（后座）→ −2.21（刺出最远，第 22 帧）→ −7.45（回收归位）。Console 0 错误。

### 5.3 已放弃的表现（用户明确同意）

网格下**静默跳过**、不报错（调用点都有 null 保护）：

- **运动模糊** `WeaponMotionBlurController` —— 绑定在 `Weapons/WeaponDirectionalPixelBlur` 精灵着色器上，网格套不上
- **速度帧切换** `renderer.sprite = speedSprite` —— 刺出加速帧
- **蓄力姿势档位** `ApplyChargeFrame()` —— `_renderer.sprite` 切换

如果绑骨骼后想用动画替代这三项的视觉作用，这是合理方向。

---

## 6. 完全替换面片还差什么

### 6.1 仍引用旧面片 `Stab.prefab` 的 12 处

| 文件 | 字段 | 类型 | 处理意见 |
|---|---|---|---|
| `Assets/Scenes/Battle.scene` | `stabPrefab` | `ChargeStabVisual` | **阻塞**，见下 |
| `Assets/Resources/EnemyPrefabs/Enemy_104.prefab` | `stabBlockEffectPrefab` | `QTEController` | **阻塞**，见下 |
| `Assets/Resources/EnemyPrefabs/Enemy_107.prefab` | 同上 | 同上 | **阻塞** |
| `Assets/Resources/EnemyPrefabs/Enemy_108.prefab` | 同上 | 同上 | **阻塞** |
| `Assets/Prefabs/UI/Skills/Zhangfei_Slash.asset` | `attackWavePrefab` | Slash | 有意保留 |
| `Assets/Prefabs/UI/Skills/Zhangfei_Pierce.asset` | 同上 | Pierce | 有意保留 |
| `Assets/Prefabs/UI/Skills/Zhangfei_Launch.asset` | 同上 | Launch | 有意保留 |
| `Assets/Prefabs/UI/Skills/Zhangfei_Parry.asset` | 同上 | Parry | 有意保留 |
| `Assets/ScriptableObjects/Moves/Zhangfei/Zhangfei_LaunchFinisher.asset` | 同上 | Launch | 有意保留 |
| `Assets/Experiments/CurvedScroll/{FakeRouteDataTrial,ScrollCombatTrial,ValleyCombatTrial}.unity` | `stabPrefab` | 试炼场景 | 次要 |

### 6.2 三个阻塞点的具体原因

**A. `ChargeStabVisual`（`Assets/Scripts/Effects/ChargeStabVisual.cs`）**

```csharp
_sr = _visualInstance.GetComponent<SpriteRenderer>();
if (_sr == null) _sr = _visualInstance.AddComponent<SpriteRenderer>();  // 会凭空加一个
_weaponLength = _sr.bounds.size.y;                                     // 网格时为 0
_sr.sprite = chargeSprite1;                                            // 又画出旧面片
```

后果三个：**旧面片美术与 3D 枪同屏双绘**；`_weaponLength = 0` 使枪尖定位失效；`SetAlpha()` 只改 SpriteRenderer 的 color，**退场渐隐对网格无效**。

**B. `QTEController.PlayBlockVisual`（`Assets/Scripts/QTE/QTEController.cs` L1294 起）**

```csharp
visual.transform.localScale = stabBlockScale;   // = 0.05，绝对覆盖而非相乘
```

我们的根节点 scale 是 8.19，会被替换成 0.05 → **整根枪缩到看不见**。要让世界长度匹配面片，`stabBlockScale` 需从 `0.05` 改为约 **4.1**。另外三帧精灵切换 `sr.sprite = ...` 对网格是空操作，格挡的旋转帧动画全丢。

**C. 其他 4 种攻击（Slash / Pierce / Sweep / Launch / Parry）**

`AttackWave.cs` 与 `SweepEffect.cs` 把姿态当作面片专用：`AttackWave` 的 L421/L440 用 `DORotate(new Vector3(90f, 1440f*t, 0f), ...)` 让面片**绕 X 轴翻滚**（网格会变成整根矛翻跟头）；L386 从 `Vector3.zero` 缩放到 `prefab.transform.localScale`（面片 0.1 vs 网格 8.19，尺度假设不同）。**建议这条路不接网格**，或单独重写姿态逻辑。

### 6.3 尺寸差异（决定「看起来对不对」）

我当初按 `Stab.prefab` 的**四边形高度 8.0** 对齐（也是 `ChargeStabVisual` 里当枪长用的那个值）。但面片可见美术只有 **7.288**，所以 3D 枪比可见矛身长 **约 10%**。

实测剖面对比（世界单位，网格用三角形边采样、面片用 PNG alpha 轮廓）：

| 部位 | 3D 网格 | 面片美术 | 比值 |
|---|---|---|---|
| 枪尖 顶部 10% | 0.548 | 0.494 | **1.11×** |
| 枪尖 顶部 20% | 0.806 | 0.931 | 0.87× |
| 上部 60–80% | 1.440 | 2.069 | 0.70× |
| 中部 40–60% | 1.284 | 2.006 | 0.64× |
| 枪杆 15–40% | 0.376 | 0.888 | **0.42×** |
| 最宽处 | 1.581 | 2.069 | 0.76× |

**结论：枪尖大小基本吻合（±10%），主要差异是粗细——枪杆只有面片的 42%。** 原因是面片最宽处（2.0）是**红缨飘带的像素级横向铺展**，而 3D 模型是垂落收拢的立体红缨。这是「像素画 vs 真 3D 造型」的表达差异，不是缩放错误。强行非等比拉伸只会破坏体积感，**不建议**。

若只想修长度：根节点 scale 从 `8.19` 改为 **`7.46`**（= 8.1857 × 0.9110）即可与可见矛身严格等长。当前**未改**。

---

## 7. 绑定与物理的建议接入点

### 7.1 骨骼

- 建议骨链：`Root → Shaft(1~3节) → Head`，红缨单独一条链 `TasselRoot → Tassel(2~3节)`，因为红缨是后续物理的主体
- **必须保持 prefab-local +Y 为长轴**（§3）。骨链的严格朝向取决于你最终在 Blender 里的建模轴向；导出后**务必回 Unity 核对**世界包围盒仍是 `(1.58, 8.00, 1.02)` 量级且长轴落在世界 Y
- 网格当前 `isReadable = false`，若要在 Unity 运行时读骨/网格数据需改导入设置
- 权重与归一化、骨骼父子关系、武器挂点要逐项核对；**「有 Armature Modifier」和「刚性挂载」不是一回事**

### 7.2 交付边界（沿用本项目既有纪律）

- 保存**版本化** `.blend`，渲染多角度 PNG，用 `read` **真的看图**验收，不靠成功回执
- 概念/造型未通过前，不推进骨头与动画的后续阶段
- 不清理用户历史文件；「弃用」不等于授权删除
- 每次修正要有明确假设 + 数值/图像证据；失败即更新诊断，不连续重复同一错误归因

### 7.3 物理（Unity vs Blender 未定）

**两条路线的取舍本文不预设**。但要提醒 Unity 侧的现状：

- 项目是**纯 2D BIRP**，`MeshRenderer` 在 gameplay 里几乎没有先例（全项目只有 `Enemy_109` 的 TMP 文字和这个新 prefab）
- 渲染排序只有 `Default` 和 `EnemyProjectiles` 两个 sorting layer；3D 网格走 `Standard` 不透明队列，与精灵的透明混合方式不同
- 现有 prefab 上的 `BoxCollider` **目前没有被任何逻辑使用**（`StabSweepEffect` 用射线 + `ColumnManager` 判定；`ChargeStabVisual` 会显式销毁 Collider）。它是我按包围盒建的，物理接入时长度轴向需要重新确认（prefab 根转了 90°，collider 的 `size`/`center` 是根节点局部空间的值）

---

## 8. 环境与工具

| 项 | 值 |
|---|---|
| Blender | `H:/Blender5.2/blender.exe`（API 回报 5.2.1 LTS） |
| Blender MCP 桥 | `mcp-for-blender`（`uvx`），插件在 **127.0.0.1:9876**；工具 `mcp__blender__execute_blender_code` / `look` / `get_scene_info` |
| Tripo 桥（另一件事） | `ws://127.0.0.1:60600`，插件 `%APPDATA%/Blender Foundation/Blender/5.2/scripts/addons/Tripo3d_Blender_Bridge` |
| Blender MCP 插件版本 | addon 1.7 / protocol 9，服务端期望 13；基础能力可用，高级功能降级 |
| Unity | 2022.3.62t7，BIRP，Legacy Input |

Blender MCP 接入细节见 `plan/blender-mcp-verified-workflow.md`。三视图/造型素材见 `plan/stab-3d-three-view-production-handoff.md`。

---

## 9. Git 与工作区状态

**本轮 3 个提交，均已推送，`origin/route-scroll-movement` 同步：**

```
2e56a24  feat(weapon): 3D 长枪资产接入准备 —— 材质、prefab 与导入设置
b6dcd8d  feat(stab): StabSweepEffect 视觉长度改为渲染器无关，支持 3D 网格枪
de4e3f2  feat(stab): 把 3D 长枪接到戳击观感 —— 5 份 Stab 配置改指 FantasySpear3D
```

**回退方式**：`de4e3f2` 是纯配置提交，单独 revert 即可让戳击回到面片；`b6dcd8d` 保留也不影响面片（精灵分支数值未变）。

**工作区有另一任务未提交的改动，与本工作无关，不要一起提交：**

```
Assets/Resources/EnemyPrefabs/Enemy_1/101/1011/102/1021/103/104/105/106_Fixed/107/108/109.prefab
Assets/RouteData/Stage01/N1_ArtPreview_Battle.asset
Assets/Scripts/Enemy/Enemy.cs
Assets/Scripts/UI/EnemyHealthBar.cs
Assets/Scripts/Enemy/EnemyGroundAligner.cs（新增，未入库）
Assets/Scripts/Editor/EnemyGroundAlignTool.cs（新增，未入库）
Locus/knowledge/design/visual-yoffset-system.md
Locus/workspace-trees/default.json
```

（是敌人 `visualYOffset` 地面对齐的另一件事，例如 `Enemy_104` 从 `0.36 → 1.035`。）

**`Battle.scene` 已还原**：我在测试期误让 Play Mode 写入过 6 个运行时对象（`StabRay`/`ShakeRoot`/`VisualOffsetRoot`），已 `git checkout` 清干净。同时移除了早前放在场景里的 `FantasySpear3D` 测试摆件——**当前 Battle 场景里没有这杆枪的实例**。

---

## 10. 下一轮开工检查表

1. 重新探活 Blender（9876）与 Unity 连接，不假定进程还在
2. 读 `§3` 轴向约定，在 Blender 里确认枪尖/枪尾的 Z 向归属
3. 备份当前 `.blend` 现场（`save_as_mainfile(copy=True)`）
4. 先做**骨骼 + 蒙皮**，导出后回 Unity **核对世界包围盒与长轴**，再谈动画
5. 物理效果：先定 Unity/Blender 路线，再决定是否需要改 `isReadable`
6. 完全替换面片前，先解决 §6.2 的 A（双绘）与 B（缩小 82 倍）两个阻塞点
7. 尺寸问题：确认是否接受「枪尖匹配、枪杆偏细」的现状，或改根节点 scale 到 7.46

---

## 11. 未验证 / 已知缺口

- **没有测到实际命中敌人**：Play Mode 测试时敌人池为空（场景启动于 `Defeat` 终态，`RestartStage` 也没刷出波次），所以「命中时的卡肉、伤害数字落点」未验证
- 蓄力保持（`chargeHoldYawDegrees=60` 的偏摆）、连段三击的姿态衔接**没有逐段肉眼验收**
- 网格版的运动模糊/速度帧/蓄力帧**已确认放弃**，无替代视觉
- 枪杆偏细的观感是否可接受，**待用户判断**
- 本文所有几何数字来自编辑器内实测；`Library/Locus/tmp/spear-3d-prefab/` 下有对比图（`tip_compare_full.png`、`side_by_side.png` 等）可供复核
