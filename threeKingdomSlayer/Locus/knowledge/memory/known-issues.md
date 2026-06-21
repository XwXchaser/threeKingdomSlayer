---
id: kd_c2de3458-57af-4257-9475-96d4662f67f2
type: memory
path: known-issues.md
title: known-issues
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1782017132257
updatedAt: 1782017132258
---

# known-issues

## Summary
记录当前已知的 5 个待修复问题：QTE 受击打断、连续 QTE 无动画、特效残留、BOSS 墙壁、敌人超范围攻击。

<!-- locus:body:start -->
# 已知问题

## 1. QTE 动画期间受道具伤害播放 Hit 动画
- **现象**: BOSS 在 QTE 动画时若受到道具等其他伤害，会播放受击(hit)动画打断 QTE 动画
- **预期**: 应继续 QTE 动画（死亡等不可逆状态除外）
- **关联**: 问题 2 可能由此引起

## 2. BOSS 连续使用 QTE 且无对应动画
- **现象**: BOSS 连续触发 QTE 但不播放对应 QTE 动画
- **可能原因**: QTE 动画被其他伤害打断（见问题 1），导致状态机异常循环

## 3. 攻击特效/伤害残留
- **现象**: 攻击特效或伤害数字未消失时进入 BOSS 战，这些特效会残留在 BOSS 战中
- **重现**: 经常反复出现

## 4. BOSS 未变成"墙壁"导致敌人被击退到身后
- **现象**: 普通敌人被击退到 BOSS 身后，导致玩家因攻击不到普通敌人而卡关
- **预期**: BOSS 应作为墙壁阻挡敌人后退

## 5. 被击退超出攻击范围的敌人仍在超范围攻击
- **现象**: 如 101 敌人被击退到 row4 BOSS 身后时，直接在 row4 开始攻击且不会补齐
- **重现**: 经常反复出现
<!-- locus:body:end -->
