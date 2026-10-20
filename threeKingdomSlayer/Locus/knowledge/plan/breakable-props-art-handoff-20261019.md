---
id: kd_c3548c00-b1f3-4cb4-877d-7793470de120
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 可破坏补给道具（酒缸 / 木箱）· 美术与实现交接（2026-10-19）

> 本文是给**接手的 AI / 美术**看的。作者在本轮尝试用生图直接产出，两次都**没有达到项目画风要求**，
> 只留下负面参考；请按本文件重新产出，不要沿用旧的 `prop_wine_jar.png` / `prop_crate.png`。

## 0. 一句话现状

**需求已定、素材未成、代码未写。** 酒缸与木箱的基图和破坏序列帧都需要重新产出；
`BreakableProp` 交互组件、EV 节点摆位、高亮提示、奖励结算都还没做。

## 1. 需求（用户原话要点）

- 正式关卡路线是 **N1 → E0 → J1 → EV → N3**，其中**只有 N1 / E0 / N3 有敌人**；J1、EV 是纯路段。
- 右路 N2 **不可取**（已在 `Node_J1.asset` 里删掉 `j1_to_n2` 出口，只剩 →EV；路线校验通过，别再加回去）。
- 在 **EV（村落外围）** 摆可破坏补给：**酒缸 / 木箱**，玩家点它 → 角色对该列打一记戳击 → 打碎 → 给奖励。
- 形象要求：**显眼、但不要太直白**（不要直接贴「酒」字之类的说明性元素）。
- 交互要有**高亮提示**（建议用程序化描边脉动，和教学手势共用一套，不必另外生图）。

## 2. 已尝试的素材（都不要用，仅作反面参考）

| 文件 | 做法 | 问题 |
|---|---|---|
| `C:/Users/Administrator/Pictures/gptGen/prop_wine_jar.png`、`prop_crate.png` | 提示词写的是「厚涂偏写实」 | 插画感重，和项目「高清像素化」画风不符；不同颜色数 13.9 万 |
| `C:/Users/Administrator/Pictures/gptGen/prop_wine_jar_v2.png` | 改成像素化提示词 | 色块变平了（6.9 万色），但仍是渐变过渡，不是像素块 |
| `C:/Users/Administrator/Pictures/gptGen/prop_ui/prop_wine_jar_v2_pixel256.png` | 128 网格 + 48 色 + 硬边 alpha 的强制像素化 | 风格最接近，但用户认为方向不对，未验收 |

## 3. 画风基准（**必须按这个来，不要自创**）

权威文档：`design/art-style-guide.md`。核心：

- **高清像素化的三国战场动作美术**，不是厚涂插画、不是照片写实、不是低分辨率复古像素；
- 像素感来自**阶梯状轮廓 + 成簇明暗色块 + 清楚的受光面/暗部**，缩小后剪影仍可读；
- 场景主关系：**暖黄土 + 冷铁灰**，朱红作识别色；
- 边缘干净，**禁止柔化光晕、白边、灰边**；
- 禁止：对插画套像素滤镜、随机碎像素、用色数统计代替视觉验收。

## 4. 需要交付的素材

全部：**透明 PNG（RGBA，PNG color type 6）、256×256、Point 过滤、PPU 16**，与现有场景物件一致。

| 文件 | 内容 | 备注 |
|---|---|---|
| `Prop_WineJar.png` | 酒缸基图 | 粗陶大缸、口部蒙布+麻绳、腹部磨损高光；略歪的轮廓更真实 |
| `Prop_Crate.png` | 木箱基图 | 军需木箱、两道冷铁灰铁箍、四角包铁、裂缝与钉眼 |
| `Prop_WineJar_Break_1..6.png` | 酒缸破坏序列 6 帧 | **同机位同配色**：裂开 → 碎片外崩 → 内容物溅出 → 只剩贴地残片 |
| `Prop_Crate_Break_1..6.png` | 木箱破坏序列 6 帧 | 同上：木板炸开 → 铁箍弹飞 → 碎片散落 |

**验收口径**（缺一不可）：

1. 与已验收的场景素材（E0 残门、村落外围）放在同一画面里，**冷暖关系和像素颗粒感一致**；
2. 缩小到 128 高仍能一眼看出是缸 / 是箱，且“看起来能打碎”；
3. 破坏序列 6 帧能连成一个动作（首帧=基图，末帧=残片），不是 6 张各自独立的图；
4. 四角 alpha=0、边缘无灰边/白边/光晕；
5. 颜色数不是验收标准，但出现大面积平滑渐变即判不合格（本风格不允许插画式渐变）。

## 5. 建议的生产路径

- 生图用项目既有 skill：`skill/gpt-image-generation.md`（`gpt-image-2.5-sunburst`、`quality=max`、`background=transparent`、`output_format=png`；调用前需按用户规矩先提交提示词并获批准）。
- 破坏序列必须**以基图为首帧做图生图**（`/v1/images/edits`，`input_fidelity=high`），否则 6 帧对不上。
- 若模型产出的像素颗粒不够“块”，可在后处理里做一次**降采样到 128 网格 + 限色 + alpha 二值化 + 最近邻放大**（本轮已写过脚本思路，见 §2 第三行），但要先和用户确认网格粗细与色数，不要默认套用。
- 逐帧生成后**逐帧目视验收**，不要一次批量冲。

## 6. 代码与配置（尚未实现，接手要做）

1. **`BreakableProp` 组件**（建议 `Assets/Scripts/Prop/`）：持有基图 + 破坏帧数组 + 血量（建议 2 击）+ 奖励（铜钱 / 小额回血）+ 所在列；
   被玩家攻击命中时扣血、播帧、碎裂后给奖励并销毁；受击要能被 `AttackSystem` 的招式命中（复用敌人列/排体系或单独射线）。
2. **EV 节点摆位**：`Node_EV.asset` 目前 `battleEntries` 为空、单出口 → N3。摆位建议写进节点配置（新增字段或在场景里放固定对象），**不要运行时随机**。
3. **高亮提示**：程序化描边脉动；玩家靠近 / 首次进入 EV 时提示一次。可与 `TutorialGestureHint` 的定位逻辑共用。
4. **奖励结算**：铜钱走 `PlayerState.AddCoins`，回血走 `PlayerState.Heal`，都要能被路线快照保存。

## 7. 相关文件索引

| 内容 | 路径 |
|---|---|
| 画风基准 | `design/art-style-guide.md` |
| 生图 skill | `skill/gpt-image-generation.md`、`skill/workflows/image-asset-generation.md` |
| 路线配置 | `Assets/RouteData/ChangbanpoScroll/Changbanpo_ScrollRoute.asset` |
| EV 节点 | `Assets/RouteData/ChangbanpoScroll/Nodes/Node_EV.asset` |
| J1 节点（已删右路出口） | `Assets/RouteData/ChangbanpoScroll/Nodes/Node_J1.asset` |
| 场景美术生产经验 | `memory/scene-art-production-pipeline.md` |

## 8. 给接手者的一句提醒

本项目对**画风一致性**的容忍度极低：素材“技术上能导入”不等于通过，必须和已验收素材并排比。
本轮就是因为提示词写成「厚涂偏写实」，两张图都白生成了。先对齐 `art-style-guide.md` 的词汇（阶梯轮廓 / 色块 / 暖土冷铁），再写提示词。
