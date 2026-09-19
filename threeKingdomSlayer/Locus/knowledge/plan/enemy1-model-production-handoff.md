---
id: kd_4906c2d2-cdf3-435c-8ccf-42c66d9e72bd
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# Enemy1 三渲二模型制作：新对话交接与失败案例

## 先读结论

用户已认可战场组合概念图，但明确否决本轮全部程序化士兵模型。**v01–v07 全部弃用为建模基础，不在上面继续修补；保留失败记录，不擅自删除。** 本文仅交接后续任务，此轮不再生成模型。

项目根目录：`H:/Project/threeKingdomSlayer/threeKingdomSlayer`。
分支：`experiment/curved-scroll-travel`。新对话重新核对 Git 与 Blender/Unity 现场，不覆盖既有修改。

必读工作流：`plan/blender-mcp-verified-workflow.md`（物理路径 `Locus/knowledge/plan/blender-mcp-verified-workflow.md`）。
当前路线/场景进度仍以 `Locus/knowledge/plan/curved-scroll-development-handoff.md` 为准。本任务只做美术资产实验，不改路线、存档、战斗规则。

## 1. 用户方向与授权边界

- 场景以2D手绘面片构成，角色是真实3D模型。不要把参考图场景误判为全3D；最初场景截图没有角色，不能由其推断角色比例。
- 新角色是 enemy1 / enemy101 持剑杂兵，不是盾兵。用户最初提到 enemy2，随后明确更正为 **enemy1 头身比例**，以更正为准。
- 保留头盔与护颊、肩甲、胸甲、土黄短衣、橄榄裤、棕靴及右手持剑（画面左侧）。
- 短壮、大头、宽躯干、短腿，战斗向卡通3D，不是低龄吉祥物，也不是写实电影角色。
- 用户举《皇室战争》等3D战斗游戏作为方向：大体块、简洁材质、清楚剪影，不是要求照抄角色。
- 反派感通过压低眉眼、咬牙、前倾和紧张姿势表达，不靠添加骷髅、尖刺、无关装备。
- 材质干净哑光，避免划痕、污渍、布料织纹、碎褶皱和密集装饰线。
- 场景要有战场感，不是清新花园。低饱和土黄、灰绿、灰蓝，木栅、拒马、军旗、营地构成题材信息；敌人与场景光照和色块语言统一。
- Blender 数据面板必须让用户真正看到角色面数：选择角色网格＋Viewport Statistics，不能只切 DATA 页。
- 当前用户要求总结与交接，不授权本轮继续生成。后续恢复制作时先解决流程与质量门槛。付费图生3D、上传素材、新装软件等明确方案/费用后再执行。

## 2. 最重要的参考图：新对话必须实际打开

以下路径相对项目根；完整绝对路径为上述项目根加表中路径。都是本轮实际落盘文件，不是描述性占位。

### 当前认可的组合概念图（优先级最高）

`Library/Locus/tmp/enemy1-battlefield-unified-v02/enemy1-battlefield-unified-v02.png`

完整路径：`H:/Project/threeKingdomSlayer/threeKingdomSlayer/Library/Locus/tmp/enemy1-battlefield-unified-v02/enemy1-battlefield-unified-v02.png`

用户反馈：“这版感觉对了”，随后授权开始模型工作。后续批评模型时再次明确提供这张图。

重点看其中角色：下压且包覆头部的头盔、颊部轮廓、眼窝与眉毛共同表达敌意、六块简化胸甲、倾斜贴合上臂的护肩、宽松短衣及不规则衣摆、屈膝外张站姿、靴面与靴筒的连贯体积、真实握剑关系。不要把这些结构简化成直立零件清单。

这张图足够确定正面风格和主要形象。侧背存在未画出的信息，可补充设计；**不应以没有三视图为前面失败开脱，也不能承诺有三视图就自动建模成功。**

### 角色单图辅助参考

`Library/Locus/tmp/enemy1-clean-material-v06/enemy1_clean_material_v06.png`

干净材质、短壮敌意角色的单图。后续组合图已被认可，发生冲突时优先组合图；不要把单图的较强体积光照原封不动放回环境。

### 原始角色身份

`Assets/Sprites/Enemy/Enemy1/Enemy1_idle1.png`

Prefab：`Assets/Resources/EnemyPrefabs/Enemy_101.prefab`。本轮不得覆盖它。

`Assets/Sprites/Enemy/Enemy2/Enemy_2.png` 只作历史上下文；盾、头巾等不能混入 enemy1。

## 3. 生图过程与用户否决原因

| 实际图片路径 | 定位 / 反馈 |
|---|---|
| `Library/Locus/tmp/enemy1-storybook-v01/enemy1_storybook_v01.png` | 第一版非像素角色：褶皱、铆钉与线条太多 |
| `Library/Locus/tmp/enemy1-storybook-v02/enemy1_storybook_v02.png` | 简化后偏低龄绘本，被否决 |
| `Library/Locus/tmp/enemy1-battle3d-v03/enemy1_battle3d_v03.png` | 3D化但细节过多，用户要前两种路线之间的风格 |
| `Library/Locus/tmp/enemy1-balanced-v04/enemy1_balanced_v04.png` | 干净3D但太正派，头身偏长；要求保留敌军服饰和enemy1比例 |
| `Library/Locus/tmp/enemy1-villain-compact-v05/enemy1_villain_compact_v05.png` | 比例/敌意加强，但材质磨损太多 |
| `Library/Locus/tmp/enemy1-clean-material-v06/enemy1_clean_material_v06.png` | 清理磨损后的角色单图，进入场景组合探索 |
| `Library/Locus/tmp/enemy1-card-scene-v01/enemy1-card-scene-v01.png` | 场景太清新、缺战场感，角色与背景画风不一致 |
| `Library/Locus/tmp/enemy1-battlefield-unified-v02/enemy1-battlefield-unified-v02.png` | 已认可的战场组合基准 |

对应目录保存 `generate.py`、`request.json`、`response.json`，可追溯实际模型、提示词和输入图片。不要盲目重新执行脚本：会新建收费生成请求。

原始场景参考为用户本地JPEG，目录：
`E:/xwechat_files/wxid_aaujs2xb0lm822_d48a/temp/RWTemp/2026-09/9e20f478899dc29eb19741386f9343c8/`

文件：
- `eec1564ae141af960df33b4f87cc62e5.jpg`
- `bc237f79a7f5cebf4382a473f1bacbc0.jpg`
- `d071addb171e3b57fa8f5caffcdf7516.jpg`

它们仅保留面片组织与简洁语言参考。后续已调整为战场，不应回退到热带、花朵和明亮青绿天堂。

## 4. 失败模型引用：务必看图，不要拿它们当基底

| 阶段 | Blend / 实际渲染 |
|---|---|
| v01 | `Library/Locus/tmp/enemy1-blender-v01/enemy1_rig_v01.blend`；`enemy1_rig_preview.png`（同目录） |
| v02 | `Library/Locus/tmp/enemy1-blender-v02/enemy1_model_v02.blend`；`enemy1_model_preview.png` |
| v03 | `Library/Locus/tmp/enemy1-blender-v03/enemy1_model_v03.blend`；`enemy1_model_preview_v03.png` |
| v04 | `Library/Locus/tmp/enemy1-blender-v04/enemy1_model_v04.blend`；`enemy1_model_preview_v04.png` |
| v05 | `Library/Locus/tmp/enemy1-blender-v05/enemy1_model_v05.blend`；`enemy1_front.png`、`enemy1_threequarter.png` |
| v06 | `Library/Locus/tmp/enemy1-blender-v06/enemy1_model_v06.blend`；`enemy1_front_v06.png` |
| v07 | `Library/Locus/tmp/enemy1-blender-v07/enemy1_shape_v07.blend`；`front.png`、`threequarter.png` |

最少打开 v01 和 v07 正面，与认可概念图对照。v01–v04 曾导出 FBX，仅技术产物；不是可部署模型。v05–v07 不应继续绑定/导出。

### 实际失败策略

本轮通过 bridge 执行人工编写 bpy 脚本，以 sphere/cube/cylinder 和简单环形截面拼接角色。**没有调用图生3D服务，图像也没有作为建模引擎输入。** 后续虽增补自定义多边形，仍在错误剪影和体块上修补。

失败点：

1. 把角色设计误解为装备清单，没有先确定头、肩、胸、腰、膝、脚的比例与主要曲线。
2. 将“低细节”误解为方块/球体少量拼装。参考图细节少，但形体结构经过设计。
3. 头盔像帽子压脸，颊部没有向外展开；脸呈球面加外挂眼球，缺眼窝、颧面、下颌关系。
4. 肩甲像两只浮球或水平砖；胸甲像贴上去的按钮，腰带像横梁；缺少装备包裹身体的关系。
5. 袖口、手、武器、靴筒缺连续体积与动作结构，站姿直立无重心，和参考的屈膝宽站敌意不符。
6. 在美术未验收时过早建骨架、Idle和FBX导出，以技术回执代替视觉交付。
7. 曾把造型错误归因于骨骼偏移，解除挂载后图像几乎不变，已证伪该解释。
8. 有眉毛旋转轴错误、衣摆围绕原点缩放引起断层、袖子刚性挂载等工程错误。
9. 连续低质量迭代没有及时停下改变流程，浪费用户时间。

### 当前 v07 数据（历史实测，下次勿当实时值）

- 48个角色Mesh；基础5960顶点、6013多边形。
- 修改器评估后7698顶点、7827多边形、15204三角面。
- Armature Modifier 0，顶点组0；造型版未蒙皮，旧骨架不匹配当前网格。
- 面数多集中在独立球体等，而非有效轮廓。问题不是“再加面数”。
- 之前“Idle已成功验证”不能成立为完整动画验收：仅有动作数据和局部骨骼数值，未充分证明网格实际运动。头部矩阵中间帧几乎不变，部分旋转抵消。

## 5. 已验证的Blender工具链

详细见 `plan/blender-mcp-verified-workflow.md`。

快速入口：

```sh
python H:/CindyData/skills/blender-mcp/scripts/blender_bridge.py ping
python H:/CindyData/skills/blender-mcp/scripts/blender_bridge.py get_scene_info
python H:/CindyData/skills/blender-mcp/scripts/blender_bridge.py exec-file Library/Locus/tmp/<new-task>/<script>.py
```

使用实时 GUI，不启动另一个后台Blender替代。新对话先检查文件、未保存状态与对象，保存现场副本，不清空用户内容。创建独立任务集合或文件，与废弃原型隔离。

## 6. 图片生成工作流引用（必须读）

- `skill/gpt-image-generation.md` → `Locus/knowledge/skill/gpt-image-generation.md`
- `skill/workflows/image-asset-generation.md` → `Locus/knowledge/skill/workflows/image-asset-generation.md`
- 本轮实际请求脚本：`Library/Locus/tmp/enemy1-battlefield-unified-v02/generate.py`；角色清洁版：`Library/Locus/tmp/enemy1-clean-material-v06/generate.py`。

本轮成功路径为 `/v1/images/edits`、multipart同名 `image[]`、`gpt-image-2.5-sunburst`、1024×1536、high、n=1。凭据环境变量 `MUSK_API_KEY`，不记录/打印密钥。上述是历史实测，下次按服务能力核实，不盲目换模型或退回无参考文生图。

输出通常是RGB不透明PNG；不要称为可直接用的透明精灵。本轮系统Python没有PIL，实际用标准库请求、PNG签名/IHDR检查，并用read看图；需要额外图像处理先核查环境。

先保存请求与响应，再验证落盘格式、尺寸、视觉效果，最终交付真实完整地址。超时不得自动重复付费任务。本轮曾发生未调用生图却回复“已生成”，后来补生成；新对话严禁复述这种无证据完成描述。

## 7. 下一轮建议计划（待执行，不是已经实现）

### A. 锁定生产方式，而不是再次写占位几何

先实际打开认可场景、清洁角色图、原enemy1图、v07错误图，写出可检查的比例/形体差异。补充正面独立建模稿和必要侧背稿可以减少歧义，但不能随意重设计，也不应不断收费重画已认可正面。

可选择：
- 参考图驱动的专业建模/雕刻：对头、躯干、衣服制作有连续结构的网格；硬甲和武器可以合理分件，不追求全角色单网格。
- 经用户确认服务和费用的图生3D初稿：检查图片输入与服务是否真实可用，导入后再清理和重拓扑。不能承诺生成服务必定达到概念质量，也不能把 BlenderMCP 本身等同于它。

先交付一个能对齐参考的静态造型切片，判断所选方式是否可行。若只能做同类占位结果，应说明能力限制，不再以“下一版会更好”重复交付。

### B. 静态形象验收门槛

- 用相近镜头、屏幕占比、正面/三分之四视角比较，不用相机或灯光遮掩造型。
- 灰模先看剪影：约三头身只是近似指导，实际以参考头盔顶到下颌/脚底的视觉比例为准。
- 头盔、肩线、腰身、膝盖、脚尖和武器握点形成一致重心，不再直立机器人姿态。
- 脸有眼窝与下颌体积，眉眼表达敌意；没有浮球眼睛、外挂长方形嘴。
- 装备贴合、衣摆包裹、手确实握剑、靴面连贯，正侧面无明显接缝/漂浮/穿模。
- 干净色块和受控明暗匹配战场图，不靠脏旧贴图、无理由增加细节或过强高光。
- 真实渲染后由用户确认，再继续。不把“控制Blender成功”替代美术确认。

### C. 模型通过后才做

整理拓扑与UV、确定移动端预算和同屏规模、合理分材质/合批；再骨架/蒙皮、Idle/Walk/Attack。武器挂点明确，动画原地播放、关闭Root Motion，保留现有战斗时序。最后导入Unity实验目录，不替换正式Prefab；验证卡肉、受击、击飞、回池和状态描边。

## 8. 文件耐久性提醒

当前图片/模型均在 `Library/Locus/tmp/`，原始微信图片也在临时目录，清理缓存后可能丢失。本文保留准确引用但没有迁移资产。后续应在选定长期美术源文件目录后归档已认可图、必要请求和对照失败图；不要把废弃FBX误导入正式资产。
