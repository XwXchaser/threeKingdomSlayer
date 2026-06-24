using UnityEngine;

/// <summary>
/// 调试工具：游戏开始时赋予玩家位移能力，用于测试。
/// 挂载到 Player/AttackSystem GameObject 上。
///
/// 使用 Update 等待关卡开始（ResetPlayer 之后）再设置值，
/// 避免被 StageController.StartStage() → ResetPlayer() → ResetAll() 覆盖。
/// </summary>
public class DisplacementDebugTool : MonoBehaviour
{
    [Header("击退波 (Push Wave) — Stab 专属")]
    public bool enablePushWave;
    [Range(1, 5)] public int pushDistance = 1;

    [Header("方向推 (Directional Push) — Slash 专属")]
    public bool enableDirectionalPush;
    [Range(1, 3)] public int directionalPushStep = 1;

    [Header("聚拢波 (Convergence Wave) — 独立效果")]
    public bool enableConvergenceWave;
    [Range(1, 3)] public int convergenceStep = 1;
    [Range(0f, 0.5f)] public float convergenceDamagePercent = 0.1f;

    private bool _applied;

    private void Update()
    {
        if (_applied) return;
        if (UpgradeEffectManager.Instance == null) return;

        // 等待关卡开始（ResetPlayer 之后），PlayerState 进入 InProgress
        var ps = PlayerState.Instance;
        if (ps == null || ps.stageState != StageState.InProgress) return;

        _applied = true;

        if (enablePushWave)
        {
            UpgradeEffectManager.Instance.DebugSetPushWave(pushDistance);
            Debug.Log($"[DisplacementDebug] Push wave: distance={pushDistance}");
        }

        if (enableDirectionalPush)
        {
            UpgradeEffectManager.Instance.DebugSetDirectionalPush(directionalPushStep);
            Debug.Log($"[DisplacementDebug] Directional push: step={directionalPushStep}");
        }

        if (enableConvergenceWave)
        {
            UpgradeEffectManager.Instance.DebugSetConvergenceWave(convergenceStep, convergenceDamagePercent);
            Debug.Log($"[DisplacementDebug] Convergence wave: step={convergenceStep}, dmgPct={convergenceDamagePercent}");
        }
    }
}
