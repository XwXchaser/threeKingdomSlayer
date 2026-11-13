---
id: kd_eb27bdc6-7394-445a-9faf-288073297df7
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
id: kd_config_table_excel_pipeline
injectMode: inherit
summary: 配置总表 Excel 配表管线：Unity 资产 ⇄ dump json ⇄ xlsx 工作簿（敌人/技能/大招/升级/台词），编辑→比对基线→SerializedProperty 回填；台词表按局内/局外分并过滤未部署项。
aiEditMode: inherit
---

# 配置总表 Excel 配表管线

把「数值 + 文案」配表从 Unity Inspector / 窗口搬进 Excel，一屏横向对比、直接改、统一回填。

## 组成

| 位置 | 作用 |
|---|---|
| `Assets/Scripts/Editor/ConfigTablePipeline.cs` | ①导出 dump（菜单 `Tools/三国杀戮/配置总表/① 导出 dump`）、③应用回填（`③ 应用 Excel 回填`）；模型 + 回填写回 |
| `Assets/Scripts/Editor/ConfigTableDomains.cs` | 各域定义（sheet 名 / 列 / 行采集）；新增一页 = 加一个域方法 |
| `Tools/build_config_workbook.py` | dump json → `Locus/knowledge/plan/config-tables.xlsx`（支持输出路径参数） |
| `Tools/apply_config_workbook.py` | xlsx + 基线 → `Library/Locus/tmp/config_table_edits.json`（默认自动取最新的 `config-tables*.xlsx`） |

工作簿 sheet：`00_说明` / `10_敌人数值` / `20_技能数值` / `21_大招数值` / `30_三选一升级` / `31_升级逐级数值` / `90_局内教学文案` / `91_局外教学文案` / `92_战斗与剧情台词`。

## 使用循环

1. Unity 里 `① 导出 dump`（写 `Library/Locus/tmp/config_table_dump.json`，含**原值基线**）
2. `python Tools/build_config_workbook.py` → 生成 xlsx
3. 用户在**黄底列**改（灰底=只读参考，留空=不改）
4. `python Tools/apply_config_workbook.py` → 比对基线产出 edits（只产出**真改动**的格子）
5. Unity 里 `③ 应用 Excel 回填` → 走 SerializedProperty 写回 + 打印变更报告

**没有「原值/新值」两列**：差异靠「表里的值 vs 导出时的基线」算，所见即所得；连跑两次第二次应为 0 改动（幂等）。

## 分类口径

- 数值域按**资产类型**分（敌人/技能/大招/升级/逐级），行=资产、列=字段（宽表）；逐级数值用长表（行=某级）。
- 文案域按**局内 / 局外**分（用户定的轴）：局外 = 主菜单·落地页·天赋页（教你解锁新招式那套）；局内 = 关卡内；非教学的战斗剧情台词单独一页。
- **只显示配到当前流程的内容**：教学关台词必须被教学导演的拍/槽引用，或 eventType==Tutorial 且关卡 ∈ 教学关（9000/9001/9002）、触发方式非 Manual、波次号 ≤ 该关波次数。别的关卡的、Manual 的、没人引用的都不进表。

## 坑（都踩过）

- **edits json 键名大小写**：`JsonUtility` 大小写敏感，Python 必须写 `"Edits"`（不是 `edits`），否则回填静默 0 项。
- **Excel 占用锁**：文件被 Excel 打开时写不进去 → 构建脚本自动降级为 `config-tables-<日期>.xlsx` 并打印实际路径；回填脚本默认取最新那份。
- **别关别人开着的场景**：导出器扫描场景文案时若临时 additively 打开场景，扫完**只能关自己开且没改脏的**——否则会把别人（和自己）带着未保存改动的实例一起关掉，改动直接丢失。已改为「已加载就复用、只关未改脏的」。
- **场景回填只标脏不自动存**：场景组件的字段改动必然 dirty 场景；自动存盘会把 GUID 迁移结果一起写进去且可能连带提交别人的未保存场景改动。资产类改动照常自动保存。
- **GUID 格式在迁移中**：磁盘上的文件级 GUID 扫描不可信（`.meta` 多为 base64、package 资源仍是 32 位 hex）→ 引用判定一律走编辑器依赖图 + 精确到对象字段的反向引用。
- **列名要带单位**：`移动速度` 的单位是**秒/排**（数值越小越快），差点填反 → 已改名「移动速度(秒/排)」并在域说明里注明。
- **枚举/布尔写法**：枚举填英文枚举名；布尔填 true/false、1/0 或 是/否。

## 待办

`plan/bugs-and-requests.md` 的 **REQ-005**：把这 12 条仍存在场景组件字段里的教学/引导文案搬进 ScriptableObject（`TutorialCopy`），完成后本管线的**场景写回分支可以整条删掉**。
