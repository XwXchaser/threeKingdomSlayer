using UnityEngine;
using DG.Tweening;

/// <summary>
/// QTE 飞行物 — 用于 BOSS QTE 攻击中的飞行物演出
/// 从 BOSS 位置沿 DOTween 路径飞向目标坐标
/// </summary>
public class QTEProjectile : MonoBehaviour
{
    [Header("飞行参数")]
    [Tooltip("飞行过程中是否面向飞行方向")]
    public bool faceDirection = true;

    private System.Action _onReachTarget;
    private System.Action _onPassThrough;
    private Sequence _flightSequence;

    /// <summary>
    /// 初始化飞行物
    /// </summary>
    /// <param name="flightTime">飞到目标坐标的时间（秒）</param>
    /// <param name="targetPos">目标世界坐标</param>
    /// <param name="onReachTarget">到达目标时的回调</param>
    public void Initialize(float flightTime, Vector3 targetPos, System.Action onReachTarget)
    {
        _onReachTarget = onReachTarget;

        // 创建飞行序列
        _flightSequence = DOTween.Sequence();
        _flightSequence.SetTarget(transform);
        _flightSequence.SetId("QTEProjectile");

        // 飞向目标
        _flightSequence.Append(transform.DOMove(targetPos, flightTime).SetEase(Ease.Linear));
        _flightSequence.AppendCallback(() =>
        {
            _onReachTarget?.Invoke();
        });
    }

    /// <summary>
    /// 继续飞行穿过屏幕（QTE 失败时调用）
    /// </summary>
    /// <param name="passThroughTime">穿过屏幕的时间（秒）</param>
    /// <param name="onPassThrough">穿过屏幕时的回调</param>
    public void ContinuePassThrough(float passThroughTime, System.Action onPassThrough)
    {
        _onPassThrough = onPassThrough;

        // 继续沿 Z 方向前飞穿出屏幕
        if (_flightSequence != null && _flightSequence.IsActive())
        {
            _flightSequence.Kill();
        }

        var camForward = Camera.main != null ? Camera.main.transform.forward : Vector3.forward;
        Vector3 passThroughTarget = transform.position + camForward * 30f;

        _flightSequence = DOTween.Sequence();
        _flightSequence.SetTarget(transform);
        _flightSequence.SetId("QTEProjectile");
        _flightSequence.Append(transform.DOMove(passThroughTarget, passThroughTime).SetEase(Ease.InQuad));
        _flightSequence.AppendCallback(() =>
        {
            _onPassThrough?.Invoke();
            Destroy(gameObject);
        });
    }

    /// <summary>
    /// 销毁飞行物（QTE 成功时调用）
    /// </summary>
    public void DestroyOnSuccess()
    {
        if (_flightSequence != null && _flightSequence.IsActive())
        {
            _flightSequence.Kill();
        }
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (_flightSequence != null && _flightSequence.IsActive())
        {
            _flightSequence.Kill();
        }
    }
}
