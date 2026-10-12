# 带红缨长枪：前／左／右／后四视图 V06

交付目录：`H:\Project\threeKingdomSlayer\threeKingdomSlayer\StabWeaponArtSource\12_AssembledFourViewV06\`

用户要求补V5左侧图并提供前、左、右、后四张单图及合图。补图已按用户批准生成一次，HTTP 200。任务中断前生成已完成；恢复时只修复本地绘图参数错误、继续对齐和合图，没有重复调用生成接口。

## 可直接使用的文件

| 文件 | 尺寸／内容 |
|---|---|
| `views/front.png` | 384×1024；V5前视原文件 |
| `views/left.png` | 384×1024；新增左视补图，整体等比缩放与白底留边后对齐 |
| `views/right.png` | 384×1024；V5右视原文件 |
| `views/back.png` | 384×1024；V5后视原文件 |
| `assembled_four_view_plain.png` | 1536×1024无字合图，从左到右为前／左／右／后 |
| `assembled_four_view_annotated.png` | 1856×1440中文标注版，视图标题、观察方向和待验收差异 |

以上均为白底RGB PNG。前／右／后三张与V5逐字节一致。四张单图与无字、中文合图中的相应区域逐像素一致。

## 左侧对齐与待验收项

新左侧原图：`left_generated_original.png`，1024×1536，原始下载副本。生成SHA-256：`2939a06dde90e95e56a35f5159c89b057eb7a258d8c766948a2bf8c96974dc08`。

对齐版使用一组全图变换：缩放0.6825396825396826、X位移−164.27777777777777、Y位移−3.984126984126984；NearestNeighbor。仅裁去画布外的空白，完整保留主体。没有水平镜像、旋转或独立拉伸零件。具体记录见 `layout_transforms.json`。

左／右的枪尖顶部、枪尾底部高度差均为0px；四张视图主体均有边距，无裁断。左側图的红缨展开比右侧更宽，远侧根部遮挡和分段结构并未严格对应，仍需用户美术验收。全图对齐不能证明各连接段或布面是精确的同一模型投影，不把补图直接声明为几何验收通过。

## 追溯文件

`trace/` 含批准规格快照、请求、状态、PNG校验、视觉检查和中英文提示词；只作追溯。原始服务响应保留在 `C:\Users\Administrator\Pictures\gptGen\stab_assembled_left_view_v06\trace\response.json`。

`delivery_verification.json` 记录四张单图与两张合图的像素一致性、来源哈希、边距和对齐结果；`source_manifest.json` 记录交付文件与原件复制校验。

本次只提交一次左侧生成；未修改Unity对象、场景、Prefab、代码或运行模式。V5五视图和所有旧版本保留。本次主交付仅前／左／右／后，未混入顶底候选。
