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

    [Tooltip("命中后给玩家留下的毒（每次跳伤）。**防御只免直接伤害，毒照扣** —— 投壶兵的威胁来源。")]
    public float poisonDamagePerTick;
    [Tooltip("中毒持续时间（秒）。")]
    public float poisonDuration;
    [Tooltip("毒跳伤间隔（秒）。")]
    public float poisonTickInterval = 0.5f;
    [Tooltip("能否被玩家的攻击在半空**打落**（打落则既不伤害也不留毒）。默认关（由敌人配置写入）。")]
    public bool canBeShotDown = false;

    private Enemy _sourceEnemy;

    private Vector3 _startPos;
    private Vector3 _endPos;
    private Tween _flightProgressTween;
    private Sequence _deflectTween;
    /// <summary>到达后的淡出 tween（unscaled 真实时间）。存成字段才能统一管理与诊断。</summary>
    private Sequence _fadeTween;
    /// <summary>淡出是否已正常完成（= 不需要再报“被掐断”）。</summary>
    private bool _fadeCompleted;
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
        _fadeCompleted = true;      // 重用旧淡出：先标记完成，免得 Kill 时报“被掐断”
        _fadeTween?.Kill();
        _fadeTween = null;
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

        // ★ 毒**先于**伤害结算：防御只吸收直接伤害 ⇒ 陶壶即使被挡下，毒照样上（投壶兵的设计核心）。
        if (poisonDuration > 0f && poisonDamagePerTick > 0f)
            PlayerState.Instance?.ApplyPoison(poisonDamagePerTick, poisonDuration, poisonTickInterval);

        // 玩家受到伤害
        PlayerState.Instance?.TakeDamage(damage, _sourceEnemy);

        DisposeProjectile();
    }

    /// <summary>
    /// **被玩家的攻击在半空打落**（投壶兵的核心交互：把陶壶打掉 ⇒ 不伤害、不留毒）。
    /// 不造成任何伤害，只是让投掷物消失（沿用既有的淡出回收）。
    /// </summary>
    public void ShootDown()
    {
        if (!gameObject.activeSelf)
        {
            Destroy(gameObject);
            return;
        }
        if (_arrived) return;

        _arrived = true;
        _flightProgressTween?.Kill();
        _flightProgressTween = null;
        DebugLog.Info($"[EnemyProjectile] 被击落 id={GetInstanceID()} pos={transform.position}");
        DisposeProjectile();
    }

    /// <summary>
    /// 玩家挥枪时，**身边的敌方投射物会被打落**（把陶壶在半空打掉）。
    /// 规则：玩家正在出招（<see cref="AttackSystem.IsActionPlaying"/>）且投射物在
    /// <see cref="AttackSystem.ProjectileShootDownRadius"/> 半径内 ⇒ 打落。
    /// </summary>
    private void TryShootDown()
    {
        if (!canBeShotDown || _arrived) return;
        var attack = AttackSystem.Instance;
        if (attack == null || !attack.IsActionPlaying) return;
        float radius = attack.ProjectileShootDownRadius;
        if (radius <= 0f) return;
        var player = PlayerState.Instance;
        if (player == null) return;
        if (Vector3.Distance(player.transform.position, transform.position) > radius) return;
        ShootDown();
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
            // ★ **淡出改成 unscaled（真实时间 0.25s）**（2026-11-09 修）：
            //   回收是纯表现，用缩放时间会被任意暂停/卡肉冻住。
            //   实测（探针取证）：招架 hit-stop（0.12s）期间淡出被掐断在 alpha≈0.78，
            //   OnComplete 不触发 → 箭永远留在屏上；而 3 秒安全兜底已被本函数停掉 → 没人能收它。
            //   （DOTween 的 unscaled = SetUpdate(UpdateType.Normal, independentUpdate: true)）
            _fadeCompleted = false;
            _fadeTween = DOTween.Sequence().SetUpdate(UpdateType.Normal, true);
            for (int i = 0; i < _spriteRenderers.Length; i++)
                _fadeTween.Join(_spriteRenderers[i].DOFade(0f, 0.25f).SetEase(Ease.InQuad).SetUpdate(UpdateType.Normal, true));
            _fadeTween.OnComplete(() => { _fadeCompleted = true; _fadeTween = null; Destroy(gameObject); });
            // 诊断：淡出被谁 Kill（OnKill 是同步回调 → 栈里能看到凶手）
            _fadeTween.OnKill(() => LogFadeInterrupted());
            // ★ 兜底：真实时间 0.6s 后无条件销毁 → 箭**不可能**留在屏上
            StartCoroutine(ForceDestroyAfterRealtime(0.6f));
            return;
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// 淡出 tween 被掐断的诊断日志（凶手在栈里）。
    /// </summary>
    private void LogFadeInterrupted()
    {
        if (_fadeCompleted) return;
        float alpha = -1f;
        if (_spriteRenderers != null && _spriteRenderers.Length > 0 && _spriteRenderers[0] != null)
            alpha = _spriteRenderers[0].color.a;
        var st = new System.Diagnostics.StackTrace(true);
        DebugLog.Warning($"[EnemyProjectile] ⚠ 淡出 tween 被 Kill：id={GetInstanceID()} alpha={alpha:F3} pos={transform.position}\n{st}");
    }

    /// <summary>
    /// 兜底：真实时间 delay 秒后**无条件**销毁。
    ///
    /// 淡出 tween 可能被任意东西掐断（实测在招架 hit-stop 期间），一旦被掐断
    /// OnComplete 就不会触发 → 箭会永远留在屏上（3 秒安全兜底此时已被 DisposeProjectile 停掉）。
    /// 这里用**真实时间**计时（不受暂停影响）→ 箭最多留 delay 秒。
    /// </summary>
    private System.Collections.IEnumerator ForceDestroyAfterRealtime(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (this == null) yield break;
        float alpha = -1f;
        if (_spriteRenderers != null && _spriteRenderers.Length > 0 && _spriteRenderers[0] != null)
            alpha = _spriteRenderers[0].color.a;
        bool fadeAlive = _fadeTween != null && _fadeTween.IsActive();
        bool fadePlaying = _fadeTween != null && _fadeTween.IsPlaying();
        DebugLog.Warning($"[EnemyProjectile] ⚠ 兜底硬销毁（淡出未完成）：id={GetInstanceID()} alpha={alpha:F3} "
            + $"fadeAlive={fadeAlive} fadePlaying={fadePlaying} pos={transform.position}");
        Destroy(gameObject);
    }

    private void UpdateFlight()
    {
        Vector3 position = EvaluatePosition(_flightProgress);
        Vector3 nextPosition = EvaluatePosition(Mathf.Min(_flightProgress + 0.01f, 1f));
        transform.position = position;

        // ★ 玩家用攻击打落投射物（投壶兵）
        TryShootDown();
        if (_arrived) return;

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
        // ★ 改用**真实时间**：缩放时间在暂停（timeScale=0）时不走 → 兜底也跟着停 → 箭永远留在屏上。
        yield return new WaitForSecondsRealtime(delay);
        DebugLog.Warning($"[EnemyProjectile] 安全超时强制销毁 id={GetInstanceID()} prog={_flightProgress:F2} pos={transform.position}");
        if (_safetyTimeout != null)
        {
            _safetyTimeout = null;
            DisposeProjectile();
        }
    }

    private void OnEnable()
    {
        SceneRegistry<EnemyProjectile>.Register(this);
    }

    private void OnDisable()
    {
        SceneRegistry<EnemyProjectile>.Unregister(this);
    }

    private void OnDestroy()
    {
        SceneRegistry<EnemyProjectile>.Unregister(this);
        _fadeCompleted = true;      // 自己销毁时不要再报“淡出被掐断”
        _flightProgressTween?.Kill();
        _deflectTween?.Kill();
        _fadeTween?.Kill();
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
