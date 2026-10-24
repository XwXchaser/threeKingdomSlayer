---
id: kd_052dd7ab-1da4-441d-81d8-343b38f57656
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 落地页 UI 部署交接（2026-10-24）

> **剩余素材最新生成入口：`Locus/knowledge/plan/hub-ui-remaining-assets-generation-handoff-20261024.md`。** 已按用户要求把方案整理为S开始无字底板（1件）、T横向天赋卡底与节点（2件）、I任务与信息承托（5件）、M置灰四宫格（1件），合计四组九件。每组直接使用原始效果图的对应局部参考，避免旧组图的圆润雕花／珠宝化偏移。复用已有艺术字、长坂桥图及可算验收的铜钱图标。本轮仅准备文档，未生成、未裁切、未导入或改场景；后文部署状态仍为原会话记录。

> 状态：场景已保存，未提交。本轮在会话 `11f671b0` 中断后交接。未做截图验收，画面效果待新会话用 Game 视图确认。
>
> **【现状 2026-10-24 用户补充】** 对照参考图后，用户确认：铜钱图标可算验收；四宫格占位键、天赋 UI、开始关卡底板、任务栏相关仍需要生成。本条只记录决定，未生成、未切片、未改场景。
>
> | 项 | 用户结论 | 当前场景（Play Mode 回读） |
> | --- | --- | --- |
> | 铜钱图标 | 可算验收 | `HubCoin/CoinIcon` 与 `HubCoinDisplay.iconSprite` 仍为旧 `hub_coin`；任务奖励也用同一张。未指定替换为哪一张新图 |
> | 四宫格占位键 | 需要生成 | `HubMenuPlaceholder/Key` 为旧 `hub_fourgrid_key` |
> | 天赋 UI | 需要生成 | 仍为竖卡：`Plate`=`hub_talent_card`（260×408），`Icon`=`hub_talent_tree` |
> | 开始关卡底板 | 需要生成 | 底板仍为旧 `MM_Btn_Primary`；艺术字 `hub_start_title_zh_v2` 已在上方 |
> | 任务栏相关 | 需要生成 | 底板与名字带仍为旧 `hub_taskbar_brush`；徽章位隐藏且无图；进度槽无独立素材；文案仍为占位「本关目标」「0/0」 |
>
> 木桥图 `hub_stage_changban_bridge_v2` 仍在关卡入口，本次未被列入待生成。G1/G2 组图仍未切片，不当已部署素材。

## 1. 用户已确认的决定

| # | 决定 |
| --- | --- |
| 1 | 关卡入口用木桥图**替代**旧城门景，不并列 |
| 2 | 天赋卡横向版属于素材欠缺，需要生成 |
| 3 | 开始按钮底板保持旧 `MM_Btn_Primary`，文字改用已生成的艺术字 |
| 4 | 参考图之外的 UI 全部移除或隐藏 |
| 5 | 任务条只做占位，数据接入延后 |
| 6 | 木桥图与艺术字可以导入 `Assets/Sprites/UI/Hub/` 并生成 `.meta` |

参考图（权威布局）：`_art_process/hub_ui_reference_v2/hub_ui_reference_v2_generated.png`（1024×1536）。只保留六项：右上铜钱、下半中央开始关卡、左下任务条、右下关卡入口、右下天赋卡、左下四宫格占位。

## 2. 场景已落地的改动（`Assets/Scenes/Battle.scene`，已保存，dirty=False）

| 对象 | 改动 | 运行时回读 |
| --- | --- | --- |
| `HubStageEntry.baseSprite` | 旧 `hub_stage_entry` → `hub_stage_changban_bridge_v2` | Play Mode 中 `Base.sprite` 为木桥图 |
| `HubStageEntry/Base` 的 Image | 同步改为木桥图 | 显示框 300×210，`preserveAspect=true` |
| `StartStageButton/TitleArt` | 新增子节点，sprite=`hub_start_title_zh_v2`，358×134，锚点中心 | Play Mode 中激活 |
| `StartStageButton/Text` | TMP「开始关卡」已 `SetActive(false)` | 不显示；`BattleStartStageButton.tmpLabel` 仍指向它，不要删除 |
| `HubTabBar` | `SetActive(false)` | Play Mode 中不激活 |
| `HubTaskBar/Content/Icon` | `SetActive(false)` | 空 sprite 节点，Play Mode 中不激活 |

`HubHudModeSwitch.readyOnly` 仍为 `Side, Bottom`，与改动前一致。

## 3. 新增资产

| 文件 | 来源 | 导入设置 |
| --- | --- | --- |
| `Assets/Sprites/UI/Hub/hub_stage_changban_bridge_v2.png` | `_art_process/hub_ui_title_bridge_v1/sprites/` | Sprite、PPU100、Point、无 mipmap、未压缩、Clamp |
| `Assets/Sprites/UI/Hub/hub_start_title_zh_v2.png` | 同上 | 同上，1376×515 |

`Assets/Sprites/UI/Hub/` 整个目录在 git 中是未跟踪状态（`??`），不是本轮造成的。不要单独提交其中一部分。

## 4. 未完成

| 项 | 原因 | 下一步 |
| --- | --- | --- |
| 开始按钮底板 | A1 在 G1 组图内，未切片 | 切片并验收 `hub_start_plate_v2` 后再替换 `MM_Btn_Primary` |
| 天赋卡横向版 | 现有 `hub_talent_card` 是 260×408 竖卡 | 需要新生成横向底板，或从 G1 切片后验收 |
| 任务条底板、铜钱墨条、名字带、四宫格、天赋图案 | G1/G2 未切片 | 按 `hub-ui-sprite-breakdown-handoff-20261024.md` 切片 |
| 任务条内容 | 占位「本关目标」「0/0」 | 数据接入另开任务 |
| 画面验收 | 本会话没有用截图工具核对 | 新会话在 Game 视图确认木桥图与艺术字的位置、比例、遮挡 |

## 5. 必须避免的坑

1. **不要只改 Image.sprite。** `HubStageEntry`、`HubTalentEntry`、`HubTaskBar`、`HubCoinDisplay` 都在 `OnEnable` 用序列化字段覆盖节点图片。改图必须改对应字段（如 `baseSprite`），否则运行时会被旧图盖回。
2. **`readyOnly` 是「落地页显示」名单，不是隐藏名单。** 把节点加进去会让它在就绪态显示。本轮曾误加 `HubTabBar`，已撤销。
3. **不要删除 `BossPortrait`、`BossPortraitFrame`。** 它们在 `BattleHUD/Combat/Overlay/DialogueLayer/DialogueOverlay` 下，被 `BattleDialogueView` 和 `HeroHUDDialogue` 序列化引用，战斗对话依赖它们。落地页默认不显示。
4. **不要删除 `StartStageButton/Text`。** `BattleStartStageButton.tmpLabel` 指向它，运行时会写文字。目前只是隐藏。
5. **`CoinCounter` 不需要处理。** 它在 `BattleHUD/Combat/Feedback` 下，`Feedback` 已在 `inRunOnly`，就绪态由脚本隐藏。
6. **截图工具名称是 `unity_capture_viewport`（单下划线），属于延迟加载工具。** 使用前要 `tool_load`。本会话曾用双下划线调用失败，误判为权限问题。

## 6. 关联文档

- `plan/hub-ui-reference-generation-handoff-20261024.md`：顶部有 2026-10-24 现状快照，给生图 AI。
- `plan/hub-ui-sprite-breakdown-handoff-20261024.md`：11 件拆件方案与素材欠缺清单。
- `plan/hub-start-title-changban-bridge-generation-handoff-20261024.md`：艺术字与木桥图的生成记录。
- `_art_process/hub_ui_sprite_v2/delivery_manifest.md`：G1/G2 组图生成状态，未切片。
- `_art_process/hub_ui_title_bridge_v1/delivery_manifest.md`：艺术字与木桥图的生成记录。

## 7. 会话中断原因（供参考，不必重查）

会话 `11f671b0` 在 20:46 连续三次收到上游 `Custom(Anthropic) 400`，消息为「请求参数有误或模型不可用」。当时 `estimated_input_tokens=128423`，不是上下文撑爆，也不是 413。日志在 `C:/Users/Administrator/AppData/Roaming/Locus/logs/sessions/locus-20261024-105317-b67226ec-c5d5-469c-b12f-a10ee4bafb28.log`。
