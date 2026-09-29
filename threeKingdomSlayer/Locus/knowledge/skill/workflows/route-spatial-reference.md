---
id: kd_37b635f7-acca-4810-b8a7-ae8672b9f7fe
injectMode: inherit
summary: 需要用 Blender 灰模建立可验证的道路空间关系、再制作像素关键帧与 Seedance 视频时使用；含机位规范、参考图策略与实测成功或被否决的结论。
skillEnabled: true
skillSurface: command
aiEditMode: inherit
---

# Blender MCP道路空间参考制作经验

## 目的
为长坂坡路线视频建立可验证的三维道路关系，避免Seedance把侧路瞬移生成、把汇入误读成分叉，或提前看到J3分岔。

## 接入方式
- 仓库：`C:/Users/steam/AppData/Local/locus/tools/blender-mcp`
- Blender：`C:/Program Files/Blender Foundation/Blender 5.2/blender.exe`
- addon：`C:/Users/steam/AppData/Roaming/Blender Foundation/Blender/5.2/scripts/addons/blender_mcp.py`
- Locus配置：`C:/Users/steam/AppData/Roaming/locus/mcp_servers.json`
- Blender addon启动后监听`127.0.0.1:9876`；MCP reload成功并显示28个工具后，使用`mcp__blender__get_scene_info`验证。
- managed Python缺少直接导入pywin32路径时，可在启动代码先`site.addsitedir('C:/Users/steam/AppData/Local/locus/data/managed-python/windows-x64/python-3.13/site-packages')`。

## 交付物登记

本流程产出的 Blender 工程、机位参考图、弃用文件与灰模参数统一登记在 `memory/video-generation-case-log.md`，本文不再记录路径。

## 正确道路拓扑
```text
EW来路 ─┐
        ├─ 汇合点 → 较长共同道路 → J3分叉点
另一来路 ┘                         ↙    ↘
                                  山道  曹军侧翼
```
汇合点与J3分叉点必须分开，不能只拉远分叉而让它仍出现在EW首帧视野。共同道路应足够长，并用缓弯、树林或土坡遮挡分叉。

## 相机与输出规范
- 视频关键帧统一`1024×1536`、2:3、pixel aspect 1:1。
- 视频透视相机使用独立机位，不能用俯视相机替代。
- 至少设置：EW接近、汇合、共同道路、J3抵达四个机位。
- EW/汇合/共同道路机位不得看到J3分叉；J3机位才显示远处分叉。
- 俯视图可使用其他比例，仅用于验证拓扑。

## Seedance参考图策略
- 不要直接用一张同时包含EW、汇合和J3分叉的图片作为视频首帧。
- 推荐拆段：`EW→汇合入口`、`汇合→共同道路`、`共同道路→J3`。
- 每段使用真实`first_frame + last_frame`；不要只在文字里描述尾帧。
- 灰模图用于我们核对几何和指导美术关键帧，不应把灰色平面直接当最终风格参考。
- 先用灰模确定道路，再制作对应像素风美术关键帧，最后生成视频。

### 实际制作结论补充

- Blender灰模对道路拓扑、汇合点与J3分叉间距有价值；但仅作为静态普通参考时，Seedance仍可能把道路当作后段目标元素生成。
- 严格 `first_frame + last_frame` 容易诱发尾帧贴合幻觉：道路和景物从画幅两侧向中心重排。尤其当首帧看不到目标道路、尾帧却要求完整汇入/分叉时风险很高。
- 只上传 `first_frame` 并用文字控制后段运动可以避免强制尾帧对齐，但不能保证道路拓扑；4秒版本失败，6秒版本用户验收为可用基础，随后由用户裁剪拼接完成最终视频。
- 关键词必须明确“从两条近处来路汇成一条远处共同道路”，不能只写“右侧出现道路”；同时必须明确固定相机高度、近水平视线、禁止上飘。
- 最终验收以用户实际观看的视频内容为准，不能以MP4容器校验、任务success或模型参数代替视觉验收。- 只在尾帧加入侧路，首帧没有道路入口/遮挡证据，会导致侧路在结尾瞬移出现。
- “右侧有一条路”容易被模型理解为从主路分叉；必须写成“从画面右下/侧后方进入，向中景汇入主路”，并禁止向右上背景延伸。
- “前两秒完全不变”与持续前进冲突，会造成原地踏步或后段跳跃；改为“开头短暂保持起始构图，随后稳定前进”。
- 仅堆负向词不能解决空间关系；必须明确路面、车辙、土坡、栅栏、固定树等物件的连续生命周期和遮挡顺序。
- 共同道路太短会让模型把汇合点和J3分叉合并；应增加长共同道路，并在弯道后设置J3分叉。
- 低机位中分叉可能被地平线压扁；需要独立的汇合/共同道路/J3机位，而不是只调整一台相机。

## 交接与进度

已完成/未完成状态、灰模参数、图源与弃用文件清单见 `memory/video-generation-case-log.md`；本文只保留可复用的流程与规矩。

### MCP操作纪律
1. 工具已注册时先加载`mcp__blender__get_scene_info`，确认完整链路；端口通仅证明addon一侧。不要因PATH找不到blender就判断未安装。
2. 通用接入规程：`C:/Users/steam/AppData/Local/locus/knowledge/skill/connect-software.md`。配置变更使用read/edit并保留其他server；无需反复安装已工作的包。
3. `execute_blender_code`中的`user_prompt`逐字使用用户原话，不填自己编写的子任务描述。
4. 先读当前工程、Scene、对象。新建独立Scene或Collection、另存版本，不再使用全选删除所有对象；不能覆盖用户未保存工作。
5. `hide_set`与`hide_render`不同；旧对象残留、命名前缀遗漏、共面路面导致z-fighting曾污染截图。独立Scene比反复按名称隐藏可靠。
6. 道路条带输入是中心线，不要把左右边缘点误当中心线；检查连接处、网格法线、三角缺口、侧路宽度和岛体是否压路。
7. 分辨率属于Scene渲染设置，不属于相机。切俯视图后必须恢复竖屏尺寸再保存，或独立俯视Scene。
8. 连接断开后先检查文件及现场，不能盲目重复执行创建代码；相同名称自动加后缀会制造重复网格/相机。
9. MCP的成功响应仅证明代码执行；必须读取落盘PNG看构图。视口截图不一定是渲染相机视角。
10. 曾输出Key片段或用命令行传Key的做法不应复用；不记录任何凭据。MCP遥测未在本轮验证关闭，使用前检查相关配置，不默认允许上传场景或提示词。

## 继续入口

下一步不是继续生成视频，而是以 EW 美术尾帧和灰模机位为基础，制作「汇合入口」像素风关键帧；确认其道路方向后，再制作「长共同道路」关键帧与 J3 夜间分岔美术尾帧，最后分段调用 Seedance。当前进度以 `memory/video-generation-case-log.md` 为准。
