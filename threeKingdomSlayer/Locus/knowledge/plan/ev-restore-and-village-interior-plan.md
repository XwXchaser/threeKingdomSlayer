---
id: kd_250df163-16a2-4df9-bd4a-426a5cf02d95
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# 恢复 EV 村口并把 N3 留在村内

## 目标

左路仍只有 J1、EV、N3 三个节点。EV 回到原来刚好看见村口的 **225 米**；N3 战斗点保持 **266.116455 米**。225 到 266 米用复制的现有房屋布置成村内道路，不新增节点，不移动 N3。

## 1. 恢复 EV

只改 EV Presentation Anchor 的 `worldDistance`：265 → **225**，再调用 `SyncTransformFromDistance()`。

它的两个子组会自动跟着回到原锚点：

- `EV Village Outskirts - Rural Entrance @ 225`
- `EV Approach - Existing Shoulder`

不要手改这两个组的世界坐标，也不要改组内已验收的房屋、柴门和树。

`Assets/RouteData/ChangbanpoScroll/Travels/Travel_J1_EV.asset` 的 `travelDistance`：75 → **35**。

`Assets/RouteData/ChangbanpoScroll/Travels/Travel_EV_N3.asset` 的 `travelDistance`：1.116455 → **41.116455**。

两段 duration 仍是 3 秒。N3、N2、J1 的距离和 `ExitTailDistance=54` 不动。

## 2. 处理被带回的过渡组

`EV Approach` 是按 EV=265 布置的。EV 回到 225 后，它会落在弯道和村口上，破坏原构图。

整组停用，并从 `scenery` 移除它的 8 个 Renderer，压缩数组，不留 null。不删除对象，以便需要时回看。当前 scenery 基线是 708；移除后应为 **700**，再加上下面的新房屋。

## 3. 村内建筑

在路线根下新建 `EV to N3 - Village Interior`，不要挂到 EV 或 N3 锚点下。复制 N3 已有房屋，不新建素材：

- `N3_House_Left_Compact_Near`，Sprite `N3_H1_CompactHouse_v1`
- `N3_House_Right_Long_Near`，Sprite `N3_H2_LongHouse_v1`
- `N3_House_Left_Damaged_Mid`，Sprite `N3_H3_DamagedHouse_v1`

材质保持 `YSampleScenery_E0Route`，局部 Y 用 -0.02，Scale 全为正数。转向只用 `flipX`，不要负 X Scale。

这些坐标使用路线根局部坐标。左路在弯后朝 -45°，所以不能只沿世界 Z 摆；按路径距离 d 转换为：

```text
arcEnd = 212.116455
tail = d - arcEnd
x = -19.91724 - 0.70710678 * tail
z = 209.91724 + 0.70710678 * tail
```

| 对象 | 复制源 | 路径距离 | 横向偏移 | Scale |
|---|---|---:|---:|---:|
| Interior_House_Left_A | Compact | 236 | 左 5.6 | 0.82 |
| Interior_House_Right_A | Long | 242 | 右 5.8 | 0.78 |
| Interior_House_Left_B | Damaged | 250 | 左 6.2 | 0.72 |
| Interior_House_Right_B | Compact | 256 | 右 6.0 | 0.68 |

横向偏移沿路径左侧的法线加到上式坐标。摆完后检查可见内缘，必须离开道路中心至少 3.6 米。

4 个 Renderer 追加到 scenery。最终应为 **704**，空项 0、重复 0。

## 验收

用 Battle Main Camera、路线根临时 Y=-1.8：

- 225 米：原 EV 村口构图恢复，不包含 `EV Approach` 的篱笆和空车。
- 236–256 米：左右只有复制房屋，中央道路为空。
- 266.116455 米：N3 战斗锚点和 `N3 Village Rescue` 组都仍在 (-46.4315, 0, 248.0956)，战斗背景不被这次复制物覆盖。
- 右路 266 米不变。

只保存 Y 场景和两份 Travel 资产。不改 N3 村庄组、EV 村口内部、道路 Shader 或共享材质。

## 执行状态（2026-10-16 已落地，待用户开游戏验收）

四步全部执行，仅保存 `Assets/Experiments/CurvedScroll/YJunctionSample.unity` 与两份 Travel 资产。

- EV：`worldDistance` 265 → 225，`SyncTransformFromDistance()` 后世界坐标 (-17.358, 0, 219.022)，与 `YScrollSample.Evaluate(225)` 精确一致。两个子组随之回到原点；`EV Village Outskirts` 内部道具未改动（处于直线段，相对道路的布局与距离无关）。
- `Travel_J1_EV` travelDistance 35、`Travel_EV_N3` 41.116455，duration 均 3 秒；两份资产已与 HEAD 一致（原先 75 / 1.116455 是本轮 EV=265 的本地改动）。
- `EV Approach - Existing Shoulder` 整组 `SetActive(false)`，8 个 Renderer 已从 `scenery` 移除并压缩；对象保留未删。
- `EV to N3 - Village Interior` 建在路线根下（局部坐标 0），4 个复制房屋坐标见下；Renderer 已追加，`scenery` = 704，空项 0、重复 0。

实际摆放（路线根局部坐标，均由 `YScrollSample.Evaluate` 得出，Y=-0.02）：

| 对象 | 局部坐标 | Scale | flipX |
|---|---|---:|---|
| Interior_House_Left_A | (-29.096, 222.840) | 0.82 | false |
| Interior_House_Right_A | (-25.277, 235.144) | 0.78 | false |
| Interior_House_Left_B | (-39.420, 232.316) | 0.72 | false |
| Interior_House_Right_B | (-35.035, 245.185) | 0.68 | true |

内缘校验（按地面 Shader 的垂直距离口径，45° 段法线 (-0.7071,-0.7071)，用 alpha 可见边界而非矩形边界）：3.651 / 4.474 / 4.332 / 4.383 米，全部 ≥ 3.6。因为 45° 段上精灵的 X 延展要乘 0.7071 才投影到法线方向，原表 5.6 / 5.8 / 6.2 / 6.0 的偏移本身已经满足要求，未做修改。

未做：未运行截图自检（按用户偏好由用户开游戏验收）。`Interior_House_Right_B` 用 `flipX` 让紧凑侧朝路，与 N3 现有右侧房屋的朝向规律一致。
