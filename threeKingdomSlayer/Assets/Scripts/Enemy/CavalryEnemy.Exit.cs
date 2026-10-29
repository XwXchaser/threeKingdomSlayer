using System.Collections;
using UnityEngine;

/// <summary>
/// 109 骑兵的**教学离场**：回退途中淡出 → 从战场摘掉（**不计击杀**）。
///
/// 用途：教学关的"教防御"那一拍 —— 玩家举枪扛住冲锋后，109 自己退场离场，
/// 教学关以**胜利**收尾（不再是被 109 剧情杀）。
///
/// 与死亡流程的区别：走 <see cref="EnemyManager.DespawnEnemy"/>（非死亡移除），
/// 因此不计击杀 / 经验 / 铜钱 / 掉落。
/// </summary>
public sealed partial class CavalryEnemy : MonoBehaviour
{
    [Tooltip("教学离场：淡出时长（秒，**跟游戏时间**走，与返航速度一致）。")]
    [SerializeField, Min(0.1f)] private float exitFadeSeconds = 0.9f;

    private Coroutine _exitRoutine;
    /// <summary>正在离场：不再发起新的冲锋（否则会在淡出过程中再冲一次）。</summary>
    private bool _exiting;

    /// <summary>是否正在离场（离场中不再接受新的冲锋）。</summary>
    public bool IsExiting => _exiting || _exitRoutine != null;

    /// <summary>
    /// 请求离场：立刻开始淡出（与返航并行），等退场收尾后从战场摘掉。
    /// 幂等：重复调用只生效一次。
    /// </summary>
    public void RequestTutorialExit()
    {
        if (_exitRoutine != null) return;
        if (_enemy == null || _enemy.state == EnemyState.Dead) return;
        // 离场中：不再发起新的冲锋（当前这一次让它自己走完返航）
        _exiting = true;
        if (_visual == null) _visual = GetComponent<CavalryVisualController>();
        _visual?.CacheFadeColors();
        _exitRoutine = StartCoroutine(TutorialExitRoutine());
    }

    private IEnumerator TutorialExitRoutine()
    {
        // ── 1. 淡出（与返航并行：玩家看到"它一边退一边消失"；跟游戏时间，与返航同步）──
        float t = 0f;
        while (t < exitFadeSeconds)
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) dt = Time.unscaledDeltaTime * 0.25f;   // 冻结时不至于完全卡住
            t += dt;
            _visual?.SetFadeAlpha(1f - Mathf.Clamp01(t / exitFadeSeconds));
            yield return null;
        }
        _visual?.SetFadeAlpha(0f);

        // ── 2. 等退场收尾（返航结束 → MountedIdle），最多等 3 秒 ──
        float wait = 0f;
        while (wait < 3f)
        {
            if (_enemy == null || _enemy.state == EnemyState.Dead) break;
            if (!IsCharging && Phase == CavalryPhase.MountedIdle) break;
            wait += Time.unscaledDeltaTime;
            yield return null;
        }

        // ── 3. 离场：先还原颜色（回池前必须还原，否则下次复用隐形），再摘掉 ──
        _visual?.RestoreFadeAlpha();
        var enemy = _enemy;
        _exitRoutine = null;
        if (enemy != null && enemy.state != EnemyState.Dead)
        {
            EnemyManager.Instance?.DespawnEnemy(enemy);
        }
    }
}
