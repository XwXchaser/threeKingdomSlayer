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

    [Tooltip("QTE 飞行物标记：设为 true 则不会被常规 Parry 反弹")]
    public bool isQTEProjectile;

    private Enemy _sourceEnemy;

    private Vector3 _startPos;
    private Vector3 _endPos;
    private Sequence _flyTween;
    private Sequence _deflectTween;
    private bool _arrived;
    private SpriteRenderer[] _spriteRenderers;
    private Coroutine _safetyTimeout;

    /// <summary>
    /// 发射箭矢
    /// </summary>
    /// <param name="startPos">起始位置（世界坐标）</param>
    /// <param name="endZ">目标Z坐标</param>
    /// <param name="endX">目标X坐标</param>
    /// <param name="dmg">伤害值</param>
    /// <param name="arcH">抛物线最高点高度</param>
    /// <param name="duration">飞行时长</param>
    /// <param name="pitchAngle">箭矢上升段最大俯仰角（度），下降段自动取反</param>
    public void Launch(Vector3 startPos, float endZ, float endX, float dmg, float arcH, float duration, Enemy source = null, float pitchAngle = 12f, float descentPitchRatio = 0.75f)
    {
        _startPos = startPos;
        _endPos = new Vector3(endX, startPos.y, endZ);
        damage = dmg;
        _sourceEnemy = source;
        arcHeight = arcH;
        flyDuration = duration;
        // 不重置 _arrived：若此前已被 Deflect() 设为 true（stagger 延迟箭矢已在预警期被弹反），
        // 保留标志以阻止后续 OnArrival 造成二次伤害
        if (!_arrived)
            _arrived = false;

        transform.position = startPos;
        gameObject.SetActive(true);
        _spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        if (_spriteRenderers != null)
        {
            foreach (var sr in _spriteRenderers)
            {
                var c = sr.color; c.a = 1f; sr.color = c;
            }
        }

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

        // 箭矢沿抛物线切线方向俯仰：上升段上仰，下降段下俯（角度可配置）
        float descentAngle = pitchAngle * descentPitchRatio;
        _flyTween.Join(
            DOTween.Sequence()
                .Append(transform.DORotate(new Vector3(pitchAngle, 0, 7.5f), halfDuration, RotateMode.Fast).SetEase(Ease.OutQuad))
                .Append(transform.DORotate(new Vector3(-descentAngle, 0, 15), halfDuration, RotateMode.Fast).SetEase(Ease.InQuad)));

        _flyTween.OnComplete(OnArrival);

        if (_safetyTimeout != null) StopCoroutine(_safetyTimeout);
        _safetyTimeout = StartCoroutine(SafetyTimeout(duration + 3f));
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

        // 快速旋转 + 坠落 + 0.3s 淡出消失
        float rx = Random.Range(-500f, 500f);
        float ry = Random.Range(-400f, 400f);
        float rz = Random.Range(700f, 1200f);
        float fallY = transform.position.y - Random.Range(2f, 4f);
        float driftX = transform.position.x + Random.Range(-0.5f, 0.5f);

        _deflectTween = DOTween.Sequence();
        _deflectTween.Join(transform.DORotate(new Vector3(rx, ry, rz), 0.6f, RotateMode.LocalAxisAdd).SetEase(Ease.OutQuad));
        _deflectTween.Join(transform.DOMoveY(fallY, 0.6f).SetEase(Ease.InQuad));
        _deflectTween.Join(transform.DOMoveX(driftX, 0.6f).SetEase(Ease.OutQuad));

        // 并行淡出（旋转/坠落期间同步淡出，含所有子级 SpriteRenderer）
        if (_spriteRenderers != null)
        {
            foreach (var sr in _spriteRenderers)
                _deflectTween.Join(sr.DOFade(0f, 0.35f).SetEase(Ease.OutQuad));
        }

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
        PlayerState.Instance?.TakeDamage(damage, _sourceEnemy);

        ReturnToPool();
    }

    private void ReturnToPool()
    {
        _flyTween?.Kill();
        _deflectTween?.Kill();
        _flyTween = null;
        _deflectTween = null;

        if (_safetyTimeout != null)
        {
            StopCoroutine(_safetyTimeout);
            _safetyTimeout = null;
        }

        // 非 Deflect 路径（OnArrival / SafetyTimeout）：补充淡出
        // Deflect 路径已在 _deflectTween 中并行淡出，此处 alpha 已为 0，直接销毁
        if (_spriteRenderers != null && _spriteRenderers.Length > 0 && _spriteRenderers[0].color.a > 0.05f && gameObject.activeInHierarchy)
        {
            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                var t = _spriteRenderers[i].DOFade(0f, 0.25f);
                if (i == 0) t.OnComplete(() => Destroy(gameObject));
            }
        }
        else
            Destroy(gameObject);
    }

    private System.Collections.IEnumerator SafetyTimeout(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        Debug.LogWarning($"[EnemyProjectile] 安全超时强制销毁");
        if (_safetyTimeout != null)
        {
            _safetyTimeout = null;
            ReturnToPool();
        }
    }

    private void OnDestroy()
    {
        _flyTween?.Kill();
        _deflectTween?.Kill();
        if (_safetyTimeout != null)
        {
            StopCoroutine(_safetyTimeout);
            _safetyTimeout = null;
        }
    }

    /// <summary>
    /// 获取当前世界位置（供 AttackSystem Parry 扫描用）
    /// </summary>
    public Vector3 GetWorldPosition() => transform.position;
}
