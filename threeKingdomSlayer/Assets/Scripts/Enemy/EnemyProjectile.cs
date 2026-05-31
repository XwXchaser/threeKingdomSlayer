using UnityEngine;
using DG.Tweening;

/// <summary>
/// 敌人远程飞行物 — 箭矢抛物线飞行、Parry反弹、到达后伤害
/// 飞行物一旦射出即独立于敌人状态（死亡/受击不影响已飞出的箭）
/// </summary>
public class EnemyProjectile : MonoBehaviour
{
    [Header("飞行参数（由 Enemy 设置）")]
    public float damage;
    public float arcHeight = 3f;
    public float flyDuration = 1f;

    private Vector3 _startPos;
    private Vector3 _endPos;
    private Sequence _flyTween;
    private Sequence _deflectTween;
    private bool _arrived;

    /// <summary>
    /// 发射箭矢
    /// </summary>
    /// <param name="startPos">起始位置（世界坐标）</param>
    /// <param name="endZ">目标Z坐标</param>
    /// <param name="endX">目标X坐标</param>
    /// <param name="dmg">伤害值</param>
    /// <param name="arcH">抛物线最高点高度</param>
    /// <param name="duration">飞行时长</param>
    public void Launch(Vector3 startPos, float endZ, float endX, float dmg, float arcH, float duration)
    {
        _startPos = startPos;
        _endPos = new Vector3(endX, startPos.y, endZ);
        damage = dmg;
        arcHeight = arcH;
        flyDuration = duration;
        _arrived = false;

        transform.position = startPos;
        gameObject.SetActive(true);

        // DOTween 抛物线: Z/X 线性插值, Y 用两个 Ease 做抛物线（总时长=duration）
        _flyTween = DOTween.Sequence();
        _flyTween.Append(transform.DOMoveX(endX, duration).SetEase(Ease.Linear));
        _flyTween.Join(transform.DOMoveZ(endZ, duration).SetEase(Ease.Linear));
        float peakY = startPos.y + arcH;
        float halfDuration = duration * 0.5f;
        _flyTween.Join(
            DOTween.Sequence()
                .Append(transform.DOMoveY(peakY, halfDuration).SetEase(Ease.OutQuad))
                .Append(transform.DOMoveY(startPos.y, halfDuration).SetEase(Ease.InQuad)));

        // X轴俯仰（模拟重力）+ Z轴自旋（模拟空气动力学）
        _flyTween.Join(
            DOTween.Sequence()
                .Append(transform.DORotate(new Vector3(-25, 0, 7.5f), halfDuration, RotateMode.Fast).SetEase(Ease.OutQuad))
                .Append(transform.DORotate(new Vector3(30, 0, 15), halfDuration, RotateMode.Fast).SetEase(Ease.InQuad)));

        _flyTween.OnComplete(OnArrival);
    }

    /// <summary>
    /// 被 Parry 反弹 — 旋转 + 坠落
    /// </summary>
    public void Deflect()
    {
        if (_arrived) return;

        _arrived = true; // 阻止 OnArrival 再次触发
        _flyTween?.Kill();
        _flyTween = null;

        // 三轴随机旋转 + 随机坠落（模拟死亡坠落效果）
        float rx = Random.Range(-300f, 300f);
        float ry = Random.Range(-200f, 200f);
        float rz = Random.Range(500f, 900f);
        float fallY = transform.position.y - Random.Range(3f, 6f);
        float driftX = transform.position.x + Random.Range(-1f, 1f);

        _deflectTween = DOTween.Sequence();
        _deflectTween.Join(transform.DORotate(new Vector3(rx, ry, rz), 1.5f, RotateMode.LocalAxisAdd).SetEase(Ease.OutQuad));
        _deflectTween.Join(transform.DOMoveY(fallY, 1.5f).SetEase(Ease.InQuad));
        _deflectTween.Join(transform.DOMoveX(driftX, 1.5f).SetEase(Ease.OutQuad));
        _deflectTween.OnComplete(() =>
        {
            ReturnToPool();
        });
    }

    private void OnArrival()
    {
        if (_arrived) return;
        _arrived = true;

        // 玩家受到伤害
        PlayerState.Instance?.TakeDamage(damage);

        ReturnToPool();
    }

    private void ReturnToPool()
    {
        _flyTween?.Kill();
        _deflectTween?.Kill();
        _flyTween = null;
        _deflectTween = null;

        // 简单销毁（后续可改为对象池）
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        _flyTween?.Kill();
        _deflectTween?.Kill();
    }

    /// <summary>
    /// 获取当前世界位置（供 AttackSystem Parry 扫描用）
    /// </summary>
    public Vector3 GetWorldPosition() => transform.position;
}
