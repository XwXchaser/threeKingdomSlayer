---
id: kd_c3d3d19d-ec61-477e-bb06-72b023da4670
type: design
path: immutable-constraints.md
title: immutable-constraints
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1780218267369
updatedAt: 1780226460613
---

# immutable-constraints

## Summary
不可变设计约束：位移效果绝不导致敌人重叠、配置验证规则变更、行/列操作的硬性约束、聚拢冲突裁决规则

## Content
# 不可变设计约束

## 1. 敌人位移不可重叠（永久规则）

> **任何会让敌人排列发生更改的位移效果（击退/聚拢/牵引/挪移），绝不允许敌人重叠到同一位置。**

- 位移前必须检查目标位置是否已被占据
- 若目标位置已被占据：跳过该次位移（或根据具体设计决定：交换位置 / 将占据者继续推移）
- 此规则适用于所有位移型升级：push_wave、pull_wave、convergence_wave 及其变体
- 列（Column）中的 `List<Enemy>` 无容量上限，意味着每个 rowIndex 可以有多个敌人——但这不意味着允许重叠。同一列同一 rowIndex 不应同时存在两个活跃敌人。

## 2. 配置验证规则变更

- 旧规则「检测到排敌人编号与 stage 配置不符 = 出错」在引入位移效果后**失效**
- 新的验证逻辑：仅在**波次初始化（Spawn）时**检查生成位置有效性
- 运行时不再验证敌人排列是否"符合配置"——因为位移效果会合法改变排列

## 3. 列操作约束

- Column 使用 `List<Enemy>` 存储，无容量上限
- `RemoveEnemy` 会自动 compact 并触发 rush move chain
- `SetRowIndex` 会立即调用 `UpdateWorldPosition()` 刷新世界坐标
- 位移效果需通过 Column/ColumnManager 提供的 API 操作，不应直接修改 `rowIndex` 绕过 compact 逻辑

## 4. 聚拢冲突裁决规则

> **若多个敌人同时聚拢至同一目标列，导致目标位置冲突：冲突敌人各承受「聚拢伤害」（固定百分比HP），然后重新分配到 col=1 和 col=3（中心列 col=2 的两侧）。**

- 此规则适用于左右两侧敌人同时向中心聚拢时发生碰撞的场景
- 聚拢伤害百分比在 UpgradeDefinition 中配置
- 重新分配时仍遵循规则1（不可重叠），若 col=1/col=3 也被占满，继续向外溢出（col=0/col=4）
- BOSS 不承受聚拢伤害，但仍参与位移
