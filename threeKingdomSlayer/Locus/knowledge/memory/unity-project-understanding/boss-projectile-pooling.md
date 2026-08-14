---
id: kd_e6389a40-4ec2-4542-b50f-7425fb079f27
injectMode: inherit
summary: P0 Boss 补齐死锁、P1 远程箭视觉落点、P2 对象池重复预热的结构认知与修复结论。
aiMaintained: inherit
---

# Boss、远程箭与对象池

- PerRow 补齐下 `Column.enemies` 的列表顺序不等于敌人的物理 `rowIndex`。Boss 在 `Approaching` 阶段判断前两排必须扫描全列并用 `enemy.rowIndex <= 1`；按列表前两项判断会让后排残敌永久阻塞 Boss。
- 普通远程敌人的箭路径在 `Enemy.SpawnProjectile()` 计算。保持 `endZ = cameraZ + projectileZTargetOffset`，可用 `projectileLandingXCenter` 和 `projectileLandingXSpread` 将落点收束到镜头中心；QTE 箭路径独立，不应在 `EnemyProjectile` 中全局修改。
- Enemy_105 当前推荐：中心 X=0、随机半宽 0.75、原 Z 偏移 5、飞行时长 1 秒。此改动会让箭更靠近玩家并可能改变招架距离手感，需 Battle 验收。
- StageController 原先对每个配置出现的 enemyId 都调用 `PrewarmPool(defaultPoolSize)`；EnemyPool 会无条件新增对象，导致对象池以出现次数倍增。现已按 ID 去重，一类只预热一次。
- 当前仍是“活动波次全量实例化”：后排敌人只 alpha=0，仍参与列阵、Boss、全体伤害、共享血量和波次完成。若实施只物化前五排，必须先引入逻辑槽位作为阵型真源，未物化槽位仍须占据行列。
