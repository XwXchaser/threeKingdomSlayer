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
    private Tween _flightProgressTween;
    private Sequence _deflectTween;
    private bool _arrived;
    private float _flightProgress;
    private float _maxDescentPitch;
    private bool _isDisposing;
    private SpriteRenderer[] _spriteRenderers;
    private Transform[] _visualTransforms;
    private Quaternion[] _visualLocalRotations;
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
    /// <param name="maxDescentPitch">下落段俯角上限（度）</param>
    public void Launch(Vector3 startPos, float endZ, float endX, float dmg, float arcH, float duration, Enemy source = null, float endY = float.MinValue, float maxDescentPitch = 89f)
    {
        _startPos = startPos;
        float targetY = endY > float.MinValue + 1f ? endY : startPos.y;
        _endPos = new Vector3(endX, targetY, endZ);
        damage = dmg;
        _sourceEnemy = source;
        arcHeight = arcH;
        flyDuration = duration;
        _maxDescentPitch = maxDescentPitch;
        _arrived = false;
        _isDisposing = false;
        _flightProgressTween?.Kill();
        _deflectTween?.Kill();
        _flightProgress = 0f;

        transform.position = startPos;
        gameObject.SetActive(true);
        _spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        CacheVisualTransforms();
        ApplyVisualForwardFlip();
        EnemyProjectileVisualPriority.Apply(gameObject);
        if (_spriteRenderers != null)
        {
            foreach (var sr in _spriteRenderers)
            {
                var c = sr.color; c.a = 1f; sr.color = c;
            }
        }

        _flightProgressTween = DOTween.To(() => _flightProgress, value =>
        {
            _flightProgress = value;
            UpdateFlight();
        }, 1f, duration).SetEase(Ease.Linear).SetUpdate(UpdateType.Normal, false);
        _flightProgressTween.OnComplete(OnArrival);

        if (_safetyTimeout != null) StopCoroutine(_safetyTimeout);
        _safetyTimeout = StartCoroutine(SafetyTimeout(duration + 3f));
    }

    /// <summary>
    /// 被 Parry 反弹 — 旋转 + 坠落
    /// </summary>
    public void Deflect()
    {
        if (!gameObject.activeSelf)
        {
            Destroy(gameObject);
            return;
        }

        if (_arrived) return;

        _arrived = true; // 阻止 OnArrival 再次触发
        _flightProgressTween?.Kill();
        _flightProgressTween = null;

        // 快速旋转 + 坠落 + 0.3s 淡出消失
        float rx = Random.Range(-500f, 500f);
        float ry = Random.Range(-400f, 400f);
        float rz = Random.Range(700f, 1200f);
        float fallY = transform.position.y - Random.Range(2f, 4f);
        float driftX = transform.position.x + Random.Range(-0.5f, 0.5f);

        _deflectTween = DOTween.Sequence().SetUpdate(UpdateType.Normal, false);
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
            DisposeProjectile();
        });
    }

    private void OnArrival()
    {
        if (_arrived) return;
        _arrived = true;

        // 玩家受到伤害
        PlayerState.Instance?.TakeDamage(damage, _sourceEnemy);

        DisposeProjectile();
    }

    private void CacheVisualTransforms()
    {
        if (_visualTransforms != null) return;

        _visualTransforms = new Transform[_spriteRenderers.Length];
        _visualLocalRotations = new Quaternion[_spriteRenderers.Length];
        for (int i = 0; i < _spriteRenderers.Length; i++)
        {
            _visualTransforms[i] = _spriteRenderers[i].transform;
            _visualLocalRotations[i] = _visualTransforms[i].localRotation;
        }
    }

    private void ApplyVisualForwardFlip()
    {
        for (int i = 0; i < _visualTransforms.Length; i++)
            _visualTransforms[i].localRotation = _visualLocalRotations[i] * Quaternion.Euler(0f, 0f, 180f);
    }

    private void DisposeProjectile()
    {
        if (_isDisposing) return;
        _isDisposing = true;

        _flightProgressTween?.Kill();
        _deflectTween?.Kill();
        _flightProgressTween = null;
        _deflectTween = null;

        if (_safetyTimeout != null)
        {
            StopCoroutine(_safetyTimeout);
            _safetyTimeout = null;
        }

        if (_spriteRenderers != null && _spriteRenderers.Length > 0 && _spriteRenderers[0].color.a > 0.05f && gameObject.activeInHierarchy)
        {
            var fade = DOTween.Sequence().SetUpdate(UpdateType.Normal, false);
            for (int i = 0; i < _spriteRenderers.Length; i++)
                fade.Join(_spriteRenderers[i].DOFade(0f, 0.25f).SetEase(Ease.InQuad));
            fade.OnComplete(() => Destroy(gameObject));
            fade.OnKill(() => Destroy(gameObject));
            return;
        }

        Destroy(gameObject);
    }

    private void UpdateFlight()
    {
        Vector3 position = EvaluatePosition(_flightProgress);
        Vector3 nextPosition = EvaluatePosition(Mathf.Min(_flightProgress + 0.01f, 1f));
        transform.position = position;

        Vector3 velocity = nextPosition - position;
        if (velocity.sqrMagnitude > 0.0001f)
        {
            float horizontalDistance = new Vector2(velocity.x, velocity.z).magnitude;
            float pitch = Mathf.Atan2(-velocity.y, horizontalDistance) * Mathf.Rad2Deg;
            pitch = Mathf.Clamp(pitch, -_maxDescentPitch, _maxDescentPitch);
            Vector3 horizontalDirection = new Vector3(velocity.x, 0f, velocity.z).normalized;
            transform.rotation = Quaternion.LookRotation(horizontalDirection, Vector3.up) * Quaternion.Euler(pitch, 0f, 0f);
        }
    }

    private Vector3 EvaluatePosition(float progress)
    {
        Vector3 position = Vector3.Lerp(_startPos, _endPos, progress);
        float arcFactor = progress <= 0.5f
            ? 1f - Mathf.Pow(1f - progress * 2f, 2f)
            : 1f - Mathf.Pow((progress - 0.5f) * 2f, 2f);
        position.y += arcHeight * arcFactor;
        return position;
    }

    private System.Collections.IEnumerator SafetyTimeout(float delay)
    {
        yield return new WaitForSeconds(delay);
        Debug.LogWarning($"[EnemyProjectile] 安全超时强制销毁");
        if (_safetyTimeout != null)
        {
            _safetyTimeout = null;
            DisposeProjectile();
        }
    }

    private void OnDestroy()
    {
        _flightProgressTween?.Kill();
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
