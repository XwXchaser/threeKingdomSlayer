using System.Collections;
using UnityEngine;


/// <summary>
/// CavalryEnemy 的**外部事件**：对玩家命中、Boss 事件等外部回调的响应。
///
/// 按机制从 <c>CavalryEnemy.cs</c> 拆出（2026-10-22，partial 分文件）。
/// partial class 与 CavalryEnemy.cs **同类型**，行为完全一致，只为降低单文件体量与阅读成本。
/// </summary>

public sealed partial class CavalryEnemy : MonoBehaviour
{
    public void OnInterruptingHit()
    {
        if (!_mounted || _enemy == null || _enemy.state == EnemyState.Dead) return;
        if (!IsCharging || _damageCommitted) return;
        _generation++;   // 让进行中的前摇/位移立即作废
        SetAnimationStage(AnimationStage.Interrupted);
        Diag("INTERRUPT", $"phase={Phase} row={_enemy.rowIndex}");
    }

    /// <summary>外部调度（SpawnEntry / WaveMarch）需要接管移动时，立刻结束骑兵动作并交还位置。</summary>
    public void AbortForExternalOrder(string reason)
    {
        if (!_mounted || _enemy == null) return;
        _generation++;
        _damageCommitted = true;
        if (_cycleRoutine != null)
        {
            StopCoroutine(_cycleRoutine);
            _cycleRoutine = null;
        }
        EnemyManager.Instance?.columnManager?.ReleaseCavalrySlot(_enemy);
        _enemy.RestoreCavalryWorldPosition();
        SetPhase(CavalryPhase.MountedIdle, reason);
        Diag("ABORT_EXTERNAL", reason);
    }

    public void NotifySpawnEntryStarted()
    {
        _spawnEntryCompleted = false;
        _spawnEntrySettleUntil = float.PositiveInfinity;
        Diag("SPAWN_ENTRY_START");
    }

    public void NotifySpawnEntryCompleted()
    {
        _spawnEntryCompleted = true;
        _spawnEntrySettleUntil = Time.time + Mathf.Max(0f, spawnEntrySettleDuration);
        Diag("SPAWN_ENTRY_COMPLETE", "combat decision delayed");
    }

    public void TraceEnemyRowChange(int from, int to, string caller)
    {
        Diag("ROW_CHANGE", $"{from}->{to} caller={caller}");
    }

    /// <summary>真正进入击飞时永久落马；Enemy.Launch 负责随后进入 Launched 状态。</summary>
    public void DismountFromLaunch() => Dismount(false, "击飞落马");

    public void Dismount(bool launchAfter = true, string reason = "击飞落马")
    {
        if (!_mounted || _enemy == null || _enemy.state == EnemyState.Dead)
            return;

        _mounted = false;
        _generation++;
        _damageCommitted = true;
        if (_cycleRoutine != null)
        {
            StopCoroutine(_cycleRoutine);
            _cycleRoutine = null;
        }
        EnemyManager.Instance?.columnManager?.ReleaseCavalrySlot(_enemy);
        _enemy.RestoreCavalryWorldPosition();
        _enemy.CancelCavalryAttackOnDismount();
        SetPhase(CavalryPhase.Dismounted, reason);
        EnemyManager.Instance?.columnManager?.RequestCavalryDismountReflow(_enemy);
        Diag("DISMOUNT", reason);

        // 落马后立即进入普通 Launched 流程；由 Enemy.Launch 调用时不再回调（避免递归）。
        if (launchAfter)
            _enemy.Launch();
    }

    private void OnDisable()
    {
        if (_cycleRoutine != null)
        {
            StopCoroutine(_cycleRoutine);
            _cycleRoutine = null;
        }
        EnemyManager.Instance?.columnManager?.ReleaseCavalrySlot(_enemy);
    }
}