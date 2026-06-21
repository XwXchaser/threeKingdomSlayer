# TODO: QTE Sweep 动画与指示器同步优化

## 问题描述

当前 QTE_Sweep 期间 BOSS 动画存在两个同步问题：

### 1. 动画帧速率过慢
- QTE 窗口期内 BOSS 的 happen 动画帧数不足，整体动画速度太慢
- 导致动画与 QTE 指示器节奏不匹配

### 2. Block 触发后动画切换延迟
- 玩家成功触发 BLOCK 后，BOSS 应**立刻**切换到 blocked 动画
- 当前行为：BOSS 继续播完当前 happen 动画帧后才切换
- 结果：音画不同步（格挡音效已播放，但格挡动画未出现）

## 预期效果

| 阶段 | 当前 | 期望 |
|------|------|------|
| happen 动画 | 帧数不足，慢 | 增加帧数 / 提高帧率，匹配 QTE 指示器节奏 |
| Block 触发 | 延迟切换（等 happen 播完） | **立即**打断 happen 动画，切换到 blocked 动画 |

## 涉及资源

- `Assets/Sprites/Enemy/BOSS1/BOSS_QTE_sweep_happen*.png` — happen 动画帧（当前 1-3）
- `Assets/Sprites/Enemy/BOSS1/BOSS_QTE_sweep_blocked*.png` — blocked 动画帧（1-4）
- `Assets/Scripts/QTE/QTEController.cs` — QTE 状态控制
- `Assets/Scripts/QTE/QTEDisplay.cs` — QTE 动画/指示器显示
- `Assets/ScriptableObjects/QTE/QTEConfig_Sweep.asset` — QTE 配置

## 状态

- [ ] happen 动画帧数 / 帧率调整
- [ ] Block 触发时立即切换动画（打断 happen）
