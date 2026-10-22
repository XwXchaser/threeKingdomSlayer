---
id: kd_964293ce-3063-47e7-8c8a-dc0f83e703a1
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
summary: 存档分片（ISaveSection）的做法与硬约束：格式绝不能变，分片只负责「这一片归谁管」；五类分片（道具/天赋/教学/进度/局内快照）与 SaveManager 转发式改写。
injectMode: inherit
aiEditMode: inherit
---

## 为什么要分片

存档模型 `SaveData` 曾经是「什么都往里塞」的大对象，迁移逻辑也堆在模型自己的 `MigrateIfNeeded()` 里 ✅。
加一个模块就要改中心模型 + 改中心服务 ✅ → 中心越来越难碰 ✅。

分片（2026-10-23 落地）：**存档格式一个字都不改** ✅，只是把「这一片数据归谁管」显式化 ✅。

## 硬约束（最容易踩）

1. **JSON 字段名仍归 `SaveData`** ✅ —— 改了会毁掉旧档 ✅；分片只管逻辑 ✅，不管字段名 ✅。
2. **分片迁移必须幂等** ✅（读档每次都会跑一遍 ✅）。
3. **分片只依赖 `SaveData`（纯数据）** ✅，不碰单例 / 场景 ✅ → 住在 `Locus.Data` ✅。
4. **`SaveManager` 的公开 API 一字不改** ✅ —— 18 个方法改成转发到分片 ✅，调用方零改动 ✅。
   验收入口：**读 → 写 → 再读，JSON 必须逐字相同** ✅（本轮实测 1531 字节逐字一致 ✅）。
5. **别提前重写存档格式** ✅ —— 旧档兼容是硬要求 ✅（`design/architecture-decoupling-analysis.md` §6「明确不做」✅）。

## 五类分片

| 分片 | 管的字段 | 迁移内容 |
|---|---|---|
| `PropSaveSection` | `props` + 旧字段 `coinCount` | 旧档 coinCount → props ✅ |
| `TalentSaveSection` | `unlockedTalents` | 旧档 null → 空列表 ✅ |
| `TutorialSaveSection` | `tutorialCompleted` + `completedTutorialDialogueIds` | 旧档 null → 空列表 ✅ |
| `ProgressSaveSection` | `clearedStageIds` | 旧档 null → 空列表 ✅ |
| `RunSnapshotSaveSection` | `routeStageSnapshots` + `fakeRouteSnapshots` + `activeFakeRouteStageId` | 旧档 null → 空列表 ✅ |

## 加新分片的步骤

1. 写一个 `ISaveSection` 实现（`Id` + `Migrate`）✅；
2. 在 `SaveSections.AllSections` 里登记 ✅；
3. 需要的话在该分片上加静态读写 API ✅，由 `SaveManager` 转发 ✅（**不要让业务代码直接摸 `SaveData` 字段** ✅）。
