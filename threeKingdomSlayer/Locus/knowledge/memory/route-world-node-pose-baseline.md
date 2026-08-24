---
id: kd_ae0d03dd-3cef-4049-9e6b-d604e9996ff5
injectMode: inherit
aiEditMode: inherit
---

# RouteWorld 节点Pose驱动基准

## 当前结论

路线移动采用单一 `Battle.scene`，固定 Player、Enemy、Battle Camera 和战斗坐标，只移动/旋转 `RouteWorldRoot`。路线节点战斗配置由节点独立引用 `StageConfig`；整体路线由 `RouteStageConfig` 管理。

当前方案已验证：

- N0 清场后显示路线选择；
- 选择路线后 Travel 可移动 `RouteWorldRoot`；
- 目标节点可切换到自己的 `StageConfig`；
- N0 → J0 → N1 可进入后续战斗；
- Player 和 Main Camera 不被路线移动逻辑直接移动；
- 最终目标节点可对齐固定 Battle 战斗原点。

## 权威空间契约

- 节点 GameObject 的 Transform 位置和旋转是路线空间的主要编辑输入。
- RouteWorldMotion 根据源节点 Pose、目标节点 Pose 和场景配置的转向点计算 Root Pose。
- 左/右转使用三段式：源节点 → 场景中的 TurnPivot → 目标节点。
- 不能在代码中额外添加固定“前进几米”的路线语义；距离必须来自场景 Transform 点。
- `pathPoints` 和 `turnPivot` 属于边的场景空间配置，不能和节点位置混用或互相覆盖。

## 道路和层级自检规则

Unity 场景中的道路通常是父子层级，父物体位置不等于道路最终世界位置。任何道路连接检查必须读取：

1. 道路父物体 Transform；
2. 道路子物体局部 Transform；
3. 子物体 Renderer 的最终世界 Bounds；
4. 相邻节点 Arena/Junction 的 Renderer 世界 Bounds；
5. Channel 的 `sourceHead`、`turnPivot`、`targetTail` 世界坐标。

必须以 Renderer 世界 Bounds 判断是否连接，不能只看 `Edge_*` 父物体位置。子物体自身的局部 Z/X 偏移会叠加父节点变换，是本次道路断口和错位的主要排查经验。

## 当前测试切片参考

当前白盒路线曾使用：

- N0 `(0,0,0)`；
- J0 `(0,0,16)`；
- N1 `(-16,0,20)`；
- J0→N1 的转向点约在 `(0,0,20)`；
- 绿色道路连接 N0/J0；紫色道路连接 J0 转向段/N1。

这些坐标只是当前测试切片，不是不可变设计。后续调整节点和道路时，应以场景中实际 Transform 与 Renderer Bounds 为准，并重新运行路线空间自检。

## 禁止回退

- 不要只修改或检查 `RouteWorldRoot` 来“补偿”场景错位；
- 不要只读取道路父节点而忽略道路子物体；
- 不要让 `pathPoints.forward` 覆盖节点 Pose 的自动朝向规则；
- 不要移动 Player、Enemy 或 Main Camera 来伪造路线移动；
- 不要把路线整体配置误当成某一个战斗节点的 `StageConfig`。
