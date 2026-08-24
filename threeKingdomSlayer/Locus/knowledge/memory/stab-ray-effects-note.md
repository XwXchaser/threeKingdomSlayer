---
id: kd_c766e336-dafc-4c40-b7c4-86feb546f2b2
injectMode: inherit
summary: Stab 与同类伸缩武器的单射线容器实现规则。
aiEditMode: inherit
---

### Stab 等伸缩武器必须由单一射线容器驱动
- 只允许“固定容器射线方向 + 射程长度”驱动伸出/收回；range 只能改变长度，不能重新计算方向。
- Prefab 是容器子视觉，视觉局部校准不得反向或重算容器路径。
- 命中按容器沿射线的推进进度和 `rowIndex` 结算，不使用敌人实时世界位置决定视觉路径。
- 默认按当前前排阵型偏移自动计算五列射线角，并保留 Inspector 手动角度覆盖用于美术微调。
- 禁止混用目标格、世界 Z、Prefab bounds、独立 Yaw 或 180°补偿来分别控制路径/朝向；出现方向问题先检查容器路径与视觉局部坐标是否分离。
- 相关文件：`Assets/Scripts/Attack/StabSweepEffect.cs`、`Assets/Scripts/Player/AttackSystem.cs`。
