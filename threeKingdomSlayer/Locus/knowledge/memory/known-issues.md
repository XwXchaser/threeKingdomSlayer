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
updatedAt: 1783497897955
---

# known-issues

## Summary
记录特效层级排序问题（SpikeTrap 已修复，Cyclone 待修复）

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

## 6. Cyclone / SpikeTrap 等特效遮挡前方敌人 ✅ 已修复
- **现象**: CycloneEffect 生效时会错误遮���敌人；最初误判为缺少高 sortingOrder
- **实际根因**: 战斗场景依赖透视相机 + Z 位置做 2.5D 深度排序。给 Cyclone 设置高 sortingOrder 会绕过 Z 深度，导致后排特效压住前排敌人
- **修复**: CycloneEffect 保持 `sortingOrder = 0`，生成时 `pos.z -= 0.2f`，与 SpikeTrap 的 Z 前移思路一致，让目标行内显示在敌人身前，同时保留跨排前后关系
- **规则**: 战斗内敌人/地面特效遮挡优先用 Z 偏移，不要用高 sortingOrder；高 sortingOrder 只用于 overlay/描边/UI 类视觉
<!-- locus:body:end -->
