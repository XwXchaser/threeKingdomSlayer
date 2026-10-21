using System.Collections;
using UnityEngine;


/// <summary>
/// CavalryEnemy 的**位移**：骑乘期间的行列接管与位置还原。
///
/// 按机制从 <c>CavalryEnemy.cs</c> 拆出（2026-10-22，partial 分文件）。
/// partial class 与 CavalryEnemy.cs **同类型**，行为完全一致，只为降低单文件体量与阅读成本。
/// </summary>

public sealed partial class CavalryEnemy : MonoBehaviour
{
    private IEnumerator MoveVisualToRow(int row, int generation)
    {
        return MoveVisualToRow(row, generation, chargeStepDuration);
    }

    /// <summary>按指定时长把视觉位置移到目标排。冲锋用 ChargeStepDuration，返航用 RetreatStepDuration。</summary>
    private IEnumerator MoveVisualToRow(int row, int generation, float stepDuration)
    {
        if (_enemy == null) yield break;
        Vector3 start = _enemy.transform.localPosition;
        Vector3 end = _enemy.GetCavalryRowLocalPosition(row);
        float duration = Mathf.Max(0.02f, stepDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (OwnerGone) yield break;
            if (IsStale(generation)) yield break;
            if (StepPaused()) { yield return null; continue; }
            elapsed += Time.deltaTime;
            _enemy.transform.localPosition = Vector3.Lerp(start, end, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        if (!OwnerGone)
            _enemy.transform.localPosition = end;
    }

    /// <summary>把视觉位置连续拉回当前物理排（收招用，不改变 rowIndex）。</summary>
    private IEnumerator RestoreVisualToRow(float duration)
    {
        if (_enemy == null) yield break;
        Vector3 start = _enemy.transform.localPosition;
        Vector3 end = _enemy.GetCavalryRowLocalPosition(_enemy.rowIndex);
        if (duration <= 0f)
        {
            _enemy.transform.localPosition = end;
            yield break;
        }
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (OwnerGone || !_mounted) yield break;
            if (StepPaused()) { yield return null; continue; }
            elapsed += Time.deltaTime;
            _enemy.transform.localPosition = Vector3.Lerp(start, end, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        if (!OwnerGone)
            _enemy.transform.localPosition = end;
    }
}