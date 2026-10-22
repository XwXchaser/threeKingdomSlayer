---
id: kd_136e02b7-da3b-4ee6-b95f-31e3c50d9fbe
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
summary: Y 场景校验工具化方案（2026-10-24）：比对器补 renderer.enabled、三方漂移检查（表↔规格↔段 prefab）、画面逐像素比对工具、自检降噪（sortBias 只在真会重叠时报）—— 附现状实测、逐条改法、验收口径与工作量。
injectMode: inherit
aiEditMode: inherit
---

# Y 场景校验工具化方案（2026-10-24）

> 起因：表化 N1 路旁布景那天 ✅，**「逐件差异 0」骗了我一次** ❗ —— 逐件比对不查渲染器 enabled ✅，
> 又因为 `Object.DestroyImmediate(transform)` 只毁组件不毁物体 ✅，手摆组没删掉 ✅ → 场景里两份布景 ❗ → 画面多出一堆东西 ❗。
> 结论：**"画面没变"必须有一条独立于结构比对的硬口径** ✅。
>
> 上游：§五 自检规则 ✅（`plan/y-scene-rebuild-plan-20261023.md` ✅）里的规则 7 ✅ 需要细化 ✅；
> 背景与坑：§八 §九（`plan/scene-pipeline-handoff-20261023.md` ✅）。

**明确不做** ✅：不碰美术 ✅、不碰战斗 ✅、不改任何既有资产的内容 ✅（只加**检查** ✅）、不重构生成器 ✅。

---

## 一、现状实测（2026-10-24 ✅）

| 观察 | 实测口径 | 后果 |
|---|---|---|
| 自检全是噪音 | 21 个规格资产 → **56 条问题，100% 是同一条**「同层多件共用 sortBias 0」❗ | 信号为零 ❗ |
| 比对器漏字段 | `CompareTrees` 不查 `renderer.enabled` ❗（也不查 color ✅ —— 表里没有 color 字段 ✅） | 旗子的精灵差点被点亮 ✅ |
| 没有三方校验 | 表 ↔ 规格 ↔ 段 prefab 三者对齐要手搓一遍 ✅ | 改过类字段后规格会**静默变陈旧** ✅（已知坑 ✅） |
| 没有画面比对 | 「画面没变」只能肉眼 ✅ | 结构比对通过 ≠ 画面一致 ✅（今天实证 ✅） |

**已具备的可复用件** ✅（别重造 ✅）：
- `YSceneSpecTools.Generate` ✅（按规格生成 ✅）、`CompareTrees` ✅（逐件比对 ✅）、`Validate` ✅（自检 ✅）
- `YSceneSpecTools.RenderSegmentView` ✅（把 prefab 渲成图 ✅ —— 机位套路可复用 ✅）
- `YScenePlacementImporter.ExportPlacementTable` / `Import` ✅（表 ⇄ 规格 ✅）
- 摆放表现在能记 `rendererDisabled` ✅（上一条提交 ✅）→ 比对器终于有依据可查 ✅

---

## 二、改动清单（4 刀 ✅，按依赖顺序 ✅）

### 2.1 刀一：比对器补 `renderer.enabled`（10 分钟 ✅）

- 改 `YSceneSpecTools.CompareTrees` ✅（现 `Assets/Scripts/Editor/Experiments/YSceneSpecTools.cs` ✅）：
  在 SpriteRenderer 分支里加一条 `sa.enabled != sb.enabled` ✅（MeshRenderer 分支同理 ✅）。
- **不查 color** ✅：表里没有颜色字段 ✅ → 查了也永远对不上 ❗ → 会变成新的噪音 ❗。
- 报错文案带路径 ✅ + 两边取值 ✅（和现有风格一致 ✅）。

> 为什么现在才有依据可查 ✅：摆放表直到 `6eee2a1` ✅ 才带 `rendererDisabled` ✅ —— 之前比对器无从判断 ✅。

### 2.2 刀二：自检降噪 —— 规则 7 只在**真会重叠**时报（30 分钟 ✅）

现状（`Validate` 现 `YSceneSpecTools.cs:846-848` ✅）：同组里 `sortBias` 重复就报 ❗ → 56 条 ❗。

改成：
1. 给每条条目算**世界足迹** ✅：
   - 宽 `w = sprite.bounds.size.x × |scale.x|` ✅；高 `h = sprite.bounds.size.y × |scale.y|` ✅
   - 脚点 `footY = localPosition.y − (sprite.pivot.y / rect.height − 0.5) × bounds.size.y × |scale.y|` ✅
   - 中心 `cx = localPosition.x + (sprite.pivot.x / rect.width − 0.5) × bounds.size.x × scale.x` ✅
2. 同 `sortBias` 的两条：**X 区间重叠 ✅ 且 Y 区间重叠 ✅** → 才报 ✅（并列出两条名字 ✅，可操作 ✅）。
3. 其余规则**一律不动** ✅：层级越界 ✅（组基准 + 最大 sortBias > 世界基准 ✅）、精灵/网格/材质引用为空 ✅ 照旧 ✅。
4. `containerOnly` 的条目不参与重叠计算 ✅（没有渲染器 ✅）。

> 为什么成立 ✅：本场景是 **flat world + 透视相机 + Z 深度排序** ✅（见 `memory/unity-project-understanding/combat-visual-sorting.md` ✅）→
> 同一组里的东西沿路散开 ✅ → 共用 sortBias 根本不重叠 ✅ → 报它是纯噪音 ❗。
> 计划文档 §五 规则 7 的**意图**（避免同 z 互相穿插 ✅）仍然成立 ✅，只是判据从「数值重复 ✅」换成「屏幕上真会重叠 ✅」。

### 2.3 刀三：三方漂移检查（表 ↔ 规格 ↔ 段 prefab）（1 小时 ✅）

新方法 `RunChainCheck(string tablePath, string specPath, string prefabPath, out string report)` ✅ + 菜单
`Tools/Y 场景规格/⑧ 全链路校验（表→规格→段 prefab 三方对齐 ✅）` ✅。

流程 ✅：
1. **表 → 规格**：用 `YScenePlacementImporter.Import` 把表导成 **specA** ✅（**临时新资产 ✅** —— `Import` 走 `GenerateUniqueAssetPath` ✅，**绝不能覆盖盘上的规格** ❗）。
2. **规格 A ↔ 规格 B**：盘上的规格 specB ✅ 与 specA 各生成一棵树 ✅ → `CompareTrees` ✅ → 差异数 = 表与规格是否同步 ✅。
3. **规格 → prefab**：specB 生成的树 vs 盘上的段 prefab ✅ → 差异数 = 规格与 prefab 是否同步 ✅。
4. 输出三行 ✅：表→规格 N 条 ✅｜规格→prefab N 条 ✅｜结论 ✅。
5. **跑完删掉临时资产** ✅（否则 `Assets/RouteData/YSceneSpecs/` 会积灰 ✅ —— 参考 `RunPlacementRoundTrip` 的 `keepSpec` 处理 ✅）。

> 为什么要有 ✅：`YSceneSpec` 加过字段 ✅（`rendererDisabled` ✅）→ 老规格反序列化**不报错、只静默变空** ✅（已知坑 ✅）→
> 没有这层检查，某天规格悄悄空了 ✅ 没人会发现 ❗。

### 2.4 刀四：画面逐像素比对（1~1.5 小时 ✅）

新方法 `RunPixelCheck(GameObject sceneGroup, string tablePath, string specPath, string prefabPath)` ✅ + 菜单
`Tools/Y 场景规格/⑨ 画面逐像素比对（改表前后 ✅）` ✅。

流程 ✅：
1. 渲**现状** ✅：新写 `RenderGroupView(Transform group, string outPath)` ✅（对**场景里的组**也能渲 ✅ ——
   `RenderSegmentView` ✅ 只吃 prefab ✅，今天渲 N1 时那段代码是临时写的 ✅）。
2. 由表/规格生成一份到**临时场景** ✅ → 渲第二张 ✅ → 关掉临时场景 ✅。
3. **C# 里逐像素比** ✅（`Texture2D.GetPixels32()` ✅）：输出「完全相同 ✅ / 差异像素数 ✅ / >4 级差异像素数 ✅」。
   - **不放 Python** ✅：编辑器工具不能依赖外部 Python ✅。
4. **机位约定** ✅（沿用今天渲 N1 的那套 ✅，以后统一 ✅）：眼高 1.7m ✅、FOV 60 ✅、aspect 0.5 ✅、俯角 10° ✅。

> 口径 ✅：**完全相同 = 差异 0 ✅** —— 今天实测 N1 表化前后就是 **0 / 1,573,538** ✅ → 说明这条口径可达 ✅；
> `>4 级` 单独列出来 ✅（容忍抗锯齿抖动 ✅）—— 超过 4 级的像素要逐个看 ✅。

---

## 三、验收口径（每条都要能造出「坏了」的例子 ✅）

| 改动 | 正向 | 反向（必须能抓到 ✅） |
|---|---|---|
| 比对器补 enabled ✅ | 全量跑一遍 ✅ 仍是 0 差异 ✅ | 故意关掉某个渲染器 ✅ → 必须报出 ✅ |
| 自检降噪 ✅ | 21 个规格全跑 ✅ → 问题数 56 ✅ → 个位数 ✅ | 造一对**真重叠**的同 sortBias 条目 ✅ → 必须报出 ✅ |
| 三方检查 ✅ | J1 / N1 段各跑一次 ✅ → 三行全 0 ✅ | 故意改坏表里一个数 ✅ → 表→规格那行必须非 0 ✅ |
| 逐像素比对 ✅ | N1 组跑一次 ✅ → 差异 0 ✅ | 故意挪一个物件 ✅ → 差异必须 > 0 ✅ |

> 反向用例是这次的重点 ✅ —— 今天就是**没有反向用例**才让「逐件差异 0」蒙混过去 ✅。

---

## 四、风险与回退

- 四刀都是**只读检查** ✅（不改任何资产 ✅）→ 风险极低 ✅；跑错也只是白跑 ✅。
- 自检降噪**会减少报错数** ✅ → 风险是「把真问题一起降掉」❗ → 验收里的反向用例就是防这个 ✅。
- 逐像素比对的机位是**约定** ✅ → 以后所有表都按这套机位比 ✅ → 口径统一 ✅。
- 回退 ✅：四个改动都是独立函数 ✅，删掉即回 ✅；不涉及资产格式 ✅（`rendererDisabled` 已在 `6eee2a1` ✅ 落地 ✅）。

---

## 五、顺序与工作量

| 刀 | 内容 | 预计 | 依赖 |
|---|---|---|---|
| 1 | 比对器补 `renderer.enabled` ✅ | 10 分钟 ✅ | 无 ✅ |
| 2 | 自检降噪 ✅（规则 7 换判据 ✅） | 30 分钟 ✅ | 无 ✅ |
| 3 | 三方漂移检查 ✅ | 1 小时 ✅ | 刀 1 ✅ |
| 4 | 画面逐像素比对 ✅ | 1~1.5 小时 ✅ | 刀 1 ✅（复用 CompareTrees ✅） |

合计约 **3~3.5 小时** ✅。刀 1+2 先落地 ✅（当天就能把「静默放过」堵上 ✅），刀 3+4 接着做 ✅。
