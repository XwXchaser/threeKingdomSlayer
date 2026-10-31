using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 目标与敌人 —— 教学关导演的**目标与敌人**部分 ✅（由 TutorialStageDirector.cs 拆出 ✅ ✅）
/// </summary>
public partial class TutorialStageDirector : MonoBehaviour
{
    private static TutorialGestureTarget ToGestureTarget(GestureHintKind kind)
    {
        switch (kind)
        {
            case GestureHintKind.Tap: return TutorialGestureTarget.Tap;
            case GestureHintKind.Swipe: return TutorialGestureTarget.SwipeHorizontal;
            case GestureHintKind.ChargedSwipe: return TutorialGestureTarget.ChargedVertical;
            case GestureHintKind.Parry:
            case GestureHintKind.ParryHold: return TutorialGestureTarget.ParryVertical;
            default: return TutorialGestureTarget.None;
        }
    }

    private static AttackType ExpectedAttackFor(TutorialGestureTarget target)
    {
        switch (target)
        {
            case TutorialGestureTarget.SwipeHorizontal: return AttackType.Slash;
            case TutorialGestureTarget.ChargedVertical: return AttackType.Launch;
            case TutorialGestureTarget.ParryVertical: return AttackType.Parry;
            default: return AttackType.Stab;
        }
    }

    private static int CountAliveEnemies()
        => EnemyManager.Instance != null ? EnemyManager.Instance.GetAllAliveEnemies().Count : 0;

    private Enemy FindTargetEnemy()
    {
        if (EnemyManager.Instance == null) return null;
        Enemy best = null;
        var alive = EnemyManager.Instance.GetAllAliveEnemies();
        for (int i = 0; i < alive.Count; i++)
        {
            var e = alive[i];
            if (e == null || e.state == EnemyState.Dead || e.rowIndex != 0 || e.state == EnemyState.Moving) continue;
            if (best == null) { best = e; continue; }
            if (e.columnIndex == 2) best = e;
            else if (best.columnIndex != 2 && e.columnIndex < best.columnIndex) best = e;
        }
        return best;
    }

    /// <summary>引导指向的敌人所在列；没有敌人返回 -1。</summary>
    private int TargetColumn
    {
        get
        {
            var e = FindTargetEnemy();
            return e != null ? e.columnIndex : -1;
        }
    }

    /// <summary>取目标敌人的屏幕坐标；没有敌人就返回 false。</summary>
    /// <remarks>
    /// **锚点用敌人的可见本体中心，不是根节点**：敌人根节点在脚底，1011/1012 本体高 2.66 世界单位
    /// → 脚底到本体中心差 1.33 世界单位（≈250 px，透视相机 fov60/z-10/1080×2160）。
    /// 锚在脚底会让提示落在小腿/脚边（2026-10-29 实测）。
    /// </remarks>
    private bool TryGetTargetEnemyScreenPosition(out Vector2 screenPos)
    {
        screenPos = Vector2.zero;
        var cam = Camera.main;
        var best = FindTargetEnemy();
        if (cam == null || best == null) return false;
        Vector3 world = GetEnemyBodyCenter(best);
        screenPos = cam.WorldToScreenPoint(world);
        return screenPos.x > 0f && screenPos.x < Screen.width && screenPos.y > 0f;
    }

    /// <summary>敌人的可见本体中心（有 SpriteRenderer 用它的 bounds，否则用实测兜底高度）。</summary>
    private static Vector3 GetEnemyBodyCenter(Enemy enemy)
    {
        if (enemy == null) return Vector3.zero;
        var renderer = enemy.GetComponentInChildren<SpriteRenderer>();
        if (renderer != null) return renderer.bounds.center;
        // 兜底：1011/1012 实测本体高 2.66 世界单位 → 中心在根节点上方 1.33
        return enemy.transform.position + Vector3.up * 1.33f;
    }
}
