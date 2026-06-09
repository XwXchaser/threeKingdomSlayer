---
id: kd_b43ac30e-4b29-4d38-b5da-ec51a24091ef
type: design
path: push-back-compaction.md
title: push-back-compaction
inheritInjectMode: true
summaryEnabled: false
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1780824594062
updatedAt: 1780824594063
---

# push-back-compaction

## Content
# 击退（Push Wave）机制设计

## 概述

击退效果（push_wave）将命中敌人向后推移 pushAmount 排，推移后通过逐列紧凑使敌人阵型保持紧密。

---

## 规则

### 1. 栈式阻塞检测（CanPushColumn）

- **规则1（尾部阻塞）**：最深命中敌人身后的 [maxHitRow+1, maxHitRow+pushAmount] 区间存在非命中敌人 → 整列阻塞，不执行击退
- **规则2（重叠检测）**：每个命中敌人的目标排 (rowIndex+pushAmount) 被非命中敌人占据 → 整列阻塞
- **规则3（Boss墙壁）**：任何命中敌人的目标排 ≥ Boss 所在排 → 整列阻塞。Boss 免疫击退，不参与判断，不阻塞判断

### 2. 击退执行（ExecutePush）

- 从列表中移除命中敌人 → 更新 rowIndex → 按 rowIndex 升序重新插入
- 新 rowIndex 上限为 bossRow-1（Boss 是不可逾越的墙壁）
- **不触发 RecheckAttackRange**：击退后敌人留在新位置，不 rush 回攻击范围

### 3. 逐列紧凑（CompactColumn）

- 所有位移完成后统一执行一次 CompactAllColumns
- 每列独立紧凑：存活敌人按 writeIdx 顺序分配 row 0,1,2...
- Boss 在紧凑时视为墙壁：身后敌人紧凑到 bossRow+1，不能越过 Boss
- Launched 敌人与普通存活敌人同等对待，占据其排位，参与紧凑

### 4. Launched 敌人规则（重要变更）

- **Launched 敌人占位不视为空排**：与 Idle/Stunned 敌人走同一补齐分支
- 删除了所有 `if (e.state == EnemyState.Launched) { SilentFillToTargetRow(); continue; }` 特殊分支
- 击飞落地后由 TryStartRushMove 自然衔接补齐

---

## 执行顺序

```
ApplyDisplacementEffects:
  1. ApplyPushWave    — 击退（各列独立，阻塞则跳过该列）
  2. ApplyConvergenceWave — 聚拢（使用 originalRows 快照，不受 Push 影响）
  3. CompactAllColumns — 逐列紧凑（所有位移的最终结果）
```

---

## 案例对比

### 案例1：单敌人击退

初始：A(row=1,col=2), B(row=2,col=0), C(row=2,col=1), D(row=2,col=3), E(row=2,col=4)

```
     col0 col1 col2 col3 col4
row0  -    -    -    -    -
row1  -    -    A    -    -
row2  B    C    -    D    E
```

A 被击退1次(pushAmount=1)：

**改动前**：ExecutePush → A.rowIndex=2 → RecheckAttackRange(A rush回row=0) → RowBasedFillUp(全体紧凑到row=0)。结果5敌人都在row=0，但 A rush 过程中可能与其他敌人动画冲突。

**改动后**：ExecutePush → A.rowIndex=2 → CompactAllColumns(逐列紧凑到row=0)。结果5敌人都在row=0，无 rush 冲突。

```
改动后:
row0  B    C    A    D    E
```

### 案例2：不同列不同排位击退

初始：B(row=0,col=0), A(row=1,col=2), C(row=2,col=1), D(row=2,col=3), E(row=2,col=4)

```
     col0 col1 col2 col3 col4
row0  B    -    -    -    -
row1  -    -    A    -    -
row2  -    C    -    D    E
```

A 被击退1次：

ExecutePush：A.rowIndex=1→2
```
row0  B    -    -    -    -
row1  -    -    -    -    -
row2  -    C    A    D    E
```

**改动前 RowBasedFillUp**：clearRows[1]=true，row2 敌人各减1→row=1。B 留在 row=0，其余在 row=1 → 产生不对齐的排布。

**改动后 CompactAllColumns**：每列独立紧凑
```
row0  B    C    A    D    E
```
所有敌人紧凑到 row=0，更密集。

### 案例3：Boss墙壁

初始：A(row=0,col=2), Boss(row=2,col=2), C(row=3,col=2)

```
     col0 col1 col2 col3 col4
row0  -    -    A    -    -
row1  -    -    -    -    -
row2  -    -   Boss  -    -
row3  -    -    C    -    -
```

A 被击退1次：CanPushColumn 规则3 → A 目标排 row=1 < bossRow=2 → 通过。

ExecutePush：A.rowIndex=0→1
```
row0  -    -    -    -    -
row1  -    -    A    -    -
row2  -    -   Boss  -    -
row3  -    -    C    -    -
```

CompactAllColumns（col=2）：A→row=0, Boss→row=1(不越过), C→row=2(紧贴Boss身后)
```
row0  -    -    A    -    -
row1  -    -   Boss  -    -
row2  -    -    C    -    -
```

### 案例4：击退阻塞

初始：A(row=1,col=2), B(row=2,col=2)

A 被击退1次，B 未命中：CanPushColumn 规则2 → A目标排 row=2 被 B 占据 → 阻塞，不执行击退。
