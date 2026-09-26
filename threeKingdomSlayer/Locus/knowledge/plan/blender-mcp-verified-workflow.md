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
- 本会话 `mcp_reload` 返回 `not_allowed`，没有原生 Blender MCP 工具。用户提供的 Skill 明确支持现成 socket 桥接；该路径已实际工作，不需要反复要求用户重装或刷新。
- 不要把 `C:/Users/Administrator/AppData/Roaming/locus/mcp_server.json` 当作外部 MCP 客户端列表；此前读取的文件是 Locus 自身服务配置，含 token，不应输出凭据。

## 标准操作循环

在项目根目录 `H:/Project/threeKingdomSlayer/threeKingdomSlayer` 执行：

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
