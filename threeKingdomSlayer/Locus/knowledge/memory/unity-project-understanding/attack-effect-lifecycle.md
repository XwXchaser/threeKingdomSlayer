---
id: kd_09680b90-1f07-4c54-adae-2eb875647450
injectMode: inherit
aiEditMode: inherit
---

# 攻击特效生命周期诊断工具

## 概述

为排查 `AttackWave` / `SweepEffect` 攻击特效残留 Bug 而建立的一套轻量级诊断系统。核心思路：**不逐条打印创建/销毁日志（避免刷屏），改用静态计数器 + 定期快照 + DOTween Kill 兜底**。

## 涉及文件

| 文件 | 角色 |
|------|------|
| `Assets/Scripts/Attack/AttackWave.cs` | 攻击波特效，维护 `AliveCount` 静态计数器 |
| `Assets/Scripts/Attack/SweepEffect.cs` | 斩击扫掠特效，维护 `AliveCount` 静态计数器 |
| `Assets/Scripts/Wave/WaveSpawner.cs` | 5 秒快照日志 + `CleanupLingeringEffects` 清理 |

## 诊断机制

### 1. 静态存活计数器

```csharp
public static int AliveCount { get; private set; }
```

- `AttackWave.AliveCount` / `SweepEffect.AliveCount`
- `Create` / `CreateInternal` 时 +1，`OnDestroy` 时 -1
- **不在创建/销毁时打印日志**，避免高频攻击刷屏

### 2. 定期快照（每 5 秒）

`WaveSpawner.Update()` 中实现：
```
[EffectLeak] AttackWave alive=3, SweepEffect alive=1, frame=xxxx
```
- 仅在至少一个计数器 > 0 时输出
- 正常情况攻击结束后数字回落；残留则持续挂起

### 3. DOTween OnKill 兜底 + 探针

两类特效的 DOTween Sequence 都加了 `OnKill` 回调：

```csharp
seq.OnKill(() =>
{
    if (!_completed)  // 未走 OnComplete = 被外部 Kill
    {
        Debug.Log($"[SweepEffect] OnKill (premature): id={_instanceId}, ...");
        seq = null;
        Destroy(gameObject);  // 兜底清除，防止残留
    }
});

seq.OnComplete(() =>
{
    _completed = true;
    seq = null;
    Destroy(gameObject);
});
```

- **正常完成**：OnComplete → `_completed=true` → Destroy → OnKill 检测到 `_completed` 已 true，跳过
- **外部 Kill**：OnKill → `_completed` 仍 false → 打印探针日志 + 执行 Destroy 兜底

### 4. 波次切换清理（CleanupLingeringEffects）

`WaveSpawner.CleanupLingeringEffects()` 在每次生成新波次前调用：
- 清理 `AttackWave`（原有）
- 清理 `SweepEffect`（**新增**，之前漏掉导致残留无法被清理）
- 清理 `DamageNumber`（原有）
- 每个被清理对象打印详细信息：name, InstanceID, damageType, survival 时长

## 排查流程

```
1. 进战斗 → 观察 [EffectLeak] snapshot（每5秒）
2. 正常攻击期间数字波动正常；波次清空后应归零
3. 不归零 → 等待 CleanupLingeringEffects 触发（下一波生成时）
   → 查看 cleanup 日志，获取残留对象的 damageType / survival
4. 若看到 [AttackWave/SweepEffect] OnKill (premature) 日志
   → 说明 DOTween Sequence 被外部 Kill
   → 根据 frame 定位是谁 Kill 的
```

## 战斗结束时序边界（2026-03）

- `CleanupLingeringEffects` 仅用于开始下一波或玩家确认离开战场后的硬清理，不能在路线奖励等待时调用。
- 三选一只暂停 `Time.timeScale`：已开始的攻击特效、死亡动画与 ULT 均冻结，恢复后继续。
- 战斗真正结束时只强制取消 ULT；路线奖励等待仍应让死亡与普通特效自然播放完。
- 玩家确认切换节点、重开或回主菜单，才允许批量终止 Tween、回收敌人和移除残留视觉。

## 已知问题与修复

| 日期 | 问题 | 修复 |
|------|------|------|
| 2025-07 | SweepEffect 残留（DOTween Sequence 被外部 Kill 后未自毁） | OnKill 兜底 + CleanupLingeringEffects 补上 SweepEffect |

## 清理残留时注意事项

- 静态计数器 `AliveCount` 在 **Domain Reload 关闭**时不会自动重置，旧运行残留的值可能影响新运行的 snapshot。建议启动后无视前几秒的 snapshot，以第一次 Cleanup 后的值为准。
- `CleanupLingeringEffects` 使用 `FindObjectsOfType<>`，范围是整个场景。正常情况应该找不到任何对象（因为已自毁）。
