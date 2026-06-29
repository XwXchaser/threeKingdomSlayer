---
id: kd_60346f80-be29-4b07-bd09-11e68ef8ee8c
type: memory
path: unity-project-understanding/charge-shockwave.md
title: charge-shockwave
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1782654745638
updatedAt: 1782654745639
---

# charge-shockwave

## Summary
Charge shockwave timed passive skill: stack layers via timer (only when charging or layers==0), release all on charge attack with staggered waveDelay.

<!-- locus:body:start -->
# Charge Shockwave System

## Overview
蓄力冲击波（charge_shockwave）是 TimedPassiveModule 驱动的一个队列型被动技能：计时器到期后积攒层数，蓄力攻击释放时一次性打出所有冲击波。

## Key Files
- `Assets/Scripts/Core/TimedPassiveModule.cs` — 计时与叠层逻辑
- `Assets/Scripts/Core/UpgradeDefinition.cs` — `ChargeShockwaveLevelConfig` 结构体
- `Assets/Scripts/Core/UpgradeEffectManager.cs` — 3选1 描述文本生成
- `Assets/Scripts/Editor/UpgradeDefinitionEditor.cs` — Inspector 绘制
- `Assets/Scripts/Player/AttackSystem.cs` — `ReleaseChargeShockwaves()` 协程释放
- `Assets/ScriptableObjects/Upgrades/Definitions/ChargeShockwave.asset` — 配置数据

## ChargeShockwaveLevelConfig 字段
- `intervalSeconds` (float) — 攒波间隔秒数
- `shockwaveCount` (int) — 每次攒的波数
- `rangeRows` (int) — 冲击波覆盖排数（独立于攻击技能 rangeRows）
- `baseDamage` (int) — 每段基础伤害
- `stackDamageBonus` (float) — 每层增伤（小数，0.15=15%）
- `waveDelay` (float) — 每道波之间的延迟秒数

## Stacking Logic (TimedPassiveModule.Update)
```
layers == 0       → timer always ticks
layers > 0, not charging → timer paused (layers preserved)
layers > 0, charging     → timer ticks → on expire → layers += shockwaveCount, timer resets
On release attack → all layers consumed, timer resets fresh (layers=0)
```

## Release Logic (AttackSystem.ReleaseChargeShockwaves)
- Coroutine (IEnumerator), called via StartCoroutine
- Each result uses its own `rangeRows` to query targets via `columnManager.GetAllEnemiesInRange(rows)`
- Waves staggered by `WaitForSeconds(waveDelay)`
- Called from ExecuteSweep, ExecuteLaunch, ExecutePierce

## Description Template
`每{0}秒积攒{1}道射程{2}排造成{3}点伤害的冲击波，在蓄力攻击时一并释放。可在蓄力时叠加，每层伤害+{4}%`
- {0}=intervalSeconds(F1), {1}=shockwaveCount, {2}=rangeRows, {3}=baseDamage, {4}=stackDamageBonus*100(F0)

## Asset Data (ChargeShockwave.asset)
| Lv | interval | count | rows | dmg | bonus | delay |
|----|----------|-------|------|-----|-------|-------|
| 1  | 5s       | 1     | 2    | 15  | 10%   | 0.1s  |
| 2  | 7s       | 1     | 2    | 20  | 15%   | 0.1s  |
| 3  | 6s       | 2     | 3    | 25  | 20%   | 0.1s  |
| 4  | 5s       | 2     | 3    | 30  | 30%   | 0.1s  |
| 5  | 4s       | 3     | 3    | 35  | 30%   | 0.1s  |
<!-- locus:body:end -->
