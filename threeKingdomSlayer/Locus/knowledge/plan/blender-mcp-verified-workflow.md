---
id: kd_a670aea0-16a3-43a7-bfa9-cf93d4626a3d
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# BlenderMCP 本机工作流：已验证路径与安全检查

## 适用范围与证据边界

本对话已验证：通过用户提供的现成桥接脚本，控制正在运行的 Blender GUI，读取场景、执行 Python、创建网格/骨架/动作数据、保存 blend、导出 FBX、渲染 PNG。**连接链路成功不代表模型美术质量、蒙皮、动画或 Unity 接入成功。** 本轮敌人模型被用户明确否决。

本文是执行记录，不修改外部 Skill。模型任务另见 `plan/enemy1-model-production-handoff.md`。

## 本机入口

- Blender：`H:/Blender5.2/blender.exe`，实际 API 回报 `5.2.1 LTS`。
- 用户提供的技能目录：`H:/CindyData/skills/blender-mcp/`。
- 必读：`H:/CindyData/skills/blender-mcp/SKILL.md`。
- 桥接：`H:/CindyData/skills/blender-mcp/scripts/blender_bridge.py`。
- 配套：`references/bridge-protocol.md`、`references/api-gotchas.md`、`references/rigging-and-animation.md`、`references/preview-without-ffmpeg.md`，均位于上述技能目录。
- GUI 插件：`C:/Users/Administrator/AppData/Roaming/Blender Foundation/Blender/5.2/scripts/addons/blender_mcp.py`。
- 已验证插件地址：`127.0.0.1:9876`。下次先重新探活，不假定进程一直存在。
- **2026-10-11 起原生 MCP 工具已接入**（见下节）。`mcp_reload` 在 Agent 工具列表里仍可能显示 not_allowed，但可通过 Python SDK 调用：`await locus.call_tool("mcp_reload", {})`。
- 用户提供的 Skill（`H:/CindyData/skills/blender-mcp/`）socket 桥接仍可用，作为 MCP 不可用时的回退路径。
- 不要把 `C:/Users/Administrator/AppData/Roaming/locus/mcp_server.json` 当作外部 MCP 客户端列表；该文件是 Locus 自身对外服务配置，含 token，不应输出凭据。外部 MCP 客户端列表另有其名，见下节。

## 原生 Blender MCP 接入（已验收）

配置文件：`C:/Users/Administrator/AppData/Roaming/locus/mcp_servers.json`（复数 `mcp_servers`，与 Locus 自身服务配置 `mcp_server.json` 单数区分开）。

- `command` 用绝对路径 `C:\\Users\\Administrator\\.local\\bin\\uvx.exe`：GUI 启动的进程不继承终端 PATH，写裸 `uvx` 会 `spawn uvx ENOENT`。
- `args`：`["--python", "3.11", "mcp-for-blender"]`，`env` 里 `UV_PYTHON_PREFERENCE=only-managed` 避免 uv 抓到系统/conda 解释器。
- 包已从 `blender-mcp` 更名为 `mcp-for-blender`（同一个包，旧名仍可运行）。
- `callTimeoutMs` 240000；`loadMode` lazy。
- 首次 `mcp_reload` 会因下载包+Python 超过 10s 初始化超时而报 `initialize timed out`。先手动预热再 reload：`UV_PYTHON_PREFERENCE=only-managed uvx --python 3.11 mcp-for-blender --help`。
- 重载成功后 9 个工具：`get_addon_status`、`get_scene_info`、`execute_blender_code`、`look`、`generate_3d`、`search_assets`、`import_asset`、`disable_telemetry`、`record_trajectory_feedback`，线名形如 `mcp__blender__<tool>`，用 `tool_load` / `tool_call` 调用。
- 验证调用：`mcp__blender__get_addon_status`、`mcp__blender__get_scene_info`。
- **插件版本落后**：Blender 侧 `blender_mcp.py` 为 addon 1.7 / protocol 9，服务端期望 13；基础工具可用（有 fallback），但轨迹等高级能力降级。升级需 `uvx mcp-for-blender install-addon` 后在 Blender 里重启用插件或重启 Blender，属用户侧操作。
- 该 MCP 桥与 Tripo Bridge（`ws://127.0.0.1:60600`）无关：Tripo Bridge 只做 Tripo 网站推模型导入，无 MCP 协议；两者端口不冲突，可共存。

## Tripo Bridge 安装与网页导出口（2026-10-12 已修复并验证）

**症状**：Tripo 网页（studio.tripo3d.ai）点“开启/导出到 Blender” 提示未安装 DCC 插件。

**根因**：插件根本没装。Blender 用户 addons 目录当时只有 `blender_mcp.py`；60600 无监听，网页就是靠这个端口探活判定“未安装”。

**安装步骤**（源包：`C:/Users/Administrator/Downloads/Tripo3d_Blender_Bridge-latest/Tripo3d_Blender_Bridge`）：

1. 整目录复制到用户 addons：`%APPDATA%/Blender Foundation/Blender/5.2/scripts/addons/Tripo3d_Blender_Bridge`，删除复制的 `__pycache__`。
2. 启用并持久化（不要只 `enable` 不存偏好，否则重启丢失）：`addon_utils.enable(name, default_set=True, persistent=True)` 然后 `bpy.ops.wm.save_userpref()`。注意 `addon_utils.enable()` 返回 module 但 `addon_utils.check()` 才是启用依据。
3. 插件 register 时自动起 60600 服务（`core/task.py`），无需手动点按钮。N 面板标签：3D 视图 → 侧边栏 → `Tripo`。

**兼容性实测**（重要，避免误判为版本不兼容）：
- 插件内置 websockets 15.1，其中 `websockets.server.serve` 是指向 `legacy.server` 的 deprecation shim，**旧式双参 handler `handle(websocket, path)` 仍可用**（已用 Blender 5.2 / Python 3.13.13 实跑起 60600 验证）。
- 插件 `bl_info` 写 `blender: (4,1,0)`，实测在 Blender 5.2.1 LTS 正常加载。

**端到端验证方式**（比看面板文字更硬）：用 Blender 自带 Python 跑 `Library/Locus/tmp/tripo-bridge-check/handshake_probe.py`，向 `ws://127.0.0.1:60600` 发 `{"type":"handshake","payload":{"clientName":...,"protocolVersion":"1.0.0"}}`，应收到 `handshake_ack`（含 `dccVersion` 与 `pluginVersion`），再发 `ping` 应收到 `pong`。两者都有 = 网页能识别到 Blender。

**注意**：网页端提示消失的前提是 **Blender 进程在跑且插件已启用**；网页与插件同机、走 127.0.0.1。若网页仍不识别，先查 60600 是否 LISTENING，再看日志 `%LOCALAPPDATA%/Temp/tripo3d_blender_bridge.log`。

## 标准操作循环

首选原生 MCP：`mcp__blender__get_scene_info`（只读探活）、`mcp__blender__execute_blender_code`（执行 bpy）、`mcp__blender__look`（截图验收）。MCP 工具不可用时回退到用户 Skill 的 socket 桥接，在项目根目录 `H:/Project/threeKingdomSlayer/threeKingdomSlayer` 执行：

```sh
python H:/CindyData/skills/blender-mcp/scripts/blender_bridge.py ping
python H:/CindyData/skills/blender-mcp/scripts/blender_bridge.py get_scene_info
python H:/CindyData/skills/blender-mcp/scripts/blender_bridge.py exec "import bpy; print(bpy.data.filepath)"
python H:/CindyData/skills/blender-mcp/scripts/blender_bridge.py exec-file Library/Locus/tmp/<task>/build.py
```

1. 读取 Skill 和相关脚本；确认桥接程序行为。
2. ping、场景查询，再检查 `bpy.data.filepath`、`bpy.data.is_dirty`、所有场景/对象和当前模式。
3. 保留用户内容。优先独立 Scene/Collection 和版本化文件；不能因对象名是 Cube 就推断没有用户修改。备份磁盘文件不等于保存当前未保存状态，需时用 `save_as_mainfile(..., copy=True)` 保存现场副本。
4. 脚本放 `Library/Locus/tmp/<task>/`，通过 write/edit 保存。所有输出使用正确的绝对项目路径；本项目路径有两层 `threeKingdomSlayer`。
5. 小步执行、检查 JSON 的 `status` 与报错。执行错误前可能已有部分修改；不要把错误当成事务回滚。
6. 保存版本化 `.blend`，渲染 PNG，用 `read` 真正看图。成功响应不等于文件/效果验收成功。
7. 每次修正要有明确假设、数值/图像证据；失败即更新诊断，不能连续重复错误归因。
8. 概念造型未通过前，不反复导出 FBX，不推进骨骼与动画。交付时提供完整真实路径。

## 权限与状态

- `ping`、场景检查、静态数据统计用 readonly=true。
- 修改对象、切帧、选择对象、改变面板、保存、导出、渲染写文件都用 readonly=false。
- 不重启或清空用户 GUI，不在未核实时执行全场景删除。
- 单次任务先读实际现场；失败原型可以归档，但用户“弃用”不等于授权删除历史文件。

## 实测 API 注意

- 对象 `display_type` 支持 `SOLID` 等，不支持 `BBONE`。骨骼显示参数应在 Armature 数据上核对。
- 当前构建渲染器枚举是 `BLENDER_EEVEE`，不是 `BLENDER_EEVEE_NEXT`。
- 中文界面按节点 `type == 'BSDF_PRINCIPLED'` 找材质节点，不能只按英文显示名。
- `view_settings.view_transform='Standard'` 是可用的预览选择；仍需实际看图校验色彩。
- 骨骼旋转是局部坐标。前视相机朝 +Y 看时，画面内倾斜需要绕 Y 轴；不能用 Z 轴旋转替代眉毛在画面内的斜率。
- `pose_bone.rotation_mode` 与插入关键帧属性必须一致。Action 存在不证明动作有效。
- 4.4+ Action 数据结构需按配套 Skill 核对，不盲用旧 `Action.fcurves`。
- 修改网格原点处的 scale 会移动其世界高度范围：上一轮衣摆按世界原点缩短导致腰部断裂。

## 正确显示面数，而不是只切属性页

用户所说数据面板包含查看面数的需求。`PROPERTIES.context='DATA'` 不是面数统计开关；选中灯光时它显示的是灯光数据。

实际应：

```python
bpy.ops.object.select_all(action='DESELECT')
meshes = [o for o in bpy.context.scene.objects
          if o.type == 'MESH' and o.name.startswith('Enemy1_')]
for o in meshes:
    o.select_set(True)
bpy.context.view_layer.objects.active = bpy.data.objects['Enemy1_Head']
for area in bpy.context.screen.areas:
    if area.type == 'VIEW_3D':
        area.spaces.active.overlay.show_overlays = True
        area.spaces.active.overlay.show_stats = True
    elif area.type == 'PROPERTIES':
        area.spaces.active.context = 'DATA'
```

对象名只是本轮示例，下次按新资产真实集合选择。统计应排除地面、预览场景和旧版本。视图统计包含选中/全场景语义，最好同时输出角色精确统计。

- 基础数据：`len(o.data.vertices)`、`len(o.data.polygons)`。
- 修改器后：`ev=o.evaluated_get(depsgraph)` → `m=ev.to_mesh()` → `m.calc_loop_triangles()` → 统计 `vertices/polygons/loop_triangles` → `ev.to_mesh_clear()`。
- 区分多边形数与三角面数，区分修改器前后。
- UI 修改后回读 active object、overlay 和 context；必要时截图验收，不只打印“已打开”。

## 绑定与动画验证底线

检查 Armature Modifier、顶点组、权重归一化、骨骼父子关系及武器挂点。刚性骨骼挂载与蒙皮不是同一回事；没有 Modifier 不一定代表没有刚性挂载，必须结合 parent_type/parent_bone 检查。

动作验收要比较实际受驱动网格的世界矩阵或评估顶点，不只看骨骼 keyframe 值。检查首尾循环、多个中间帧、脚底高度、武器与手的关系。上一轮 Root/Spine 反向旋转抵消、Head 通道设置等存在问题，历史“Idle 验证成功”表述不能复用。

## 当前结论

- Blender 控制、保存、导出、渲染链路可用。
- 美术造型没有通过，生产骨骼/蒙皮与Unity接入未交付。
- 不能把桥接工具当成图生3D模型服务：实际执行的是人工编写 bpy 几何脚本，没有调用任何图生3D服务。二维生图 API 与 Blender 桥接分别可用，不意味着两者组合成图生3D引擎；当前没有已连接、已验证可用的图生3D服务。

## 续轮验证与失败防线

- 后续 `enemy1-model-build-v01–v04` 保存、渲染成功，但造型均未通过。最后保存路径为 `Library/Locus/tmp/enemy1-model-build-v04/enemy1_static_v04.blend`，活动场景 `Enemy1_Rebuild_v04`；下次重新探活、核对状态，不把本文当实时状态。完整产物索引见模型交接第9节。
- 保存现场使用了 `save_as_mainfile(copy=True)`；不要仅凭 is_dirty=False 推断不存在用户改动，也不要以备份为理由清理旧内容。
- World 背景节点同样应按 `type == 'BACKGROUND'` 找。续轮使用英文 `nodes.get('Background')` 得到 None，已实测失败。
- NumPy 算术会将 float32 图像提升为 float64；`image.pixels.foreach_set` 曾报 `incorrect sequence item type: d`。写入前用 `.astype(np.float32)` 并检查尺寸；失败时先查网格/图像是否已创建，只恢复剩余步骤，不能重跑全部几何变换。
- 通用面板函数只能接收沿外边界顺序排列的轮廓点；不能将两排网格顶点直接作为一个 n-gon。v04 胸甲将网格顶点列表当轮廓传入，渲染出现交叉破面；应使用明确的四边面索引并检查自交、退化面及法线。
- 截面放样和管道拼接不自动形成正确的人体/服装。前后连接、衣摆包裹、膝踝重心必须在正/侧/背均检查；三分之四图不代替侧面或背面。
- 五官最终确认是“立体脸＋专用表情贴图”，禁止裁切概念图整张脸作为贴图，禁止把此要求误解为全部实体五官。独立鼻/脸颊块也不能冒充合格的连续面部结构。
- 不再连续交付同类失败模型并口头承诺下一版。当前无合格3D角色、骨架、动画或Unity接入；静态造型验收仍为硬门槛。
