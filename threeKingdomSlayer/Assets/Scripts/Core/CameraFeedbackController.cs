using UnityEngine;
using DG.Tweening;

public sealed class CameraFeedbackController : MonoBehaviour
{
    public static CameraFeedbackController Instance { get; private set; }

    [Header("破防镜头")]
    [Tooltip("破防时的镜头震动时长（秒）：比普通格挡略长（破防是更大的冲击），但**幅度更小**。\n" +
             "⚠ 幅度 ≫ 时长容易读成「地动山摇」；宁可短而轻。")]
    [SerializeField] private float guardBreakDuration = 0.14f;
    [Tooltip("破防时的镜头震动强度（世界单位）。")]
    [SerializeField] private float guardBreakIntensity = 0.045f;

    [Header("震动参数")]
    [Tooltip("**整体震动强度倍率**（所有档位通用，默认 0.35）。调 0 = 等于关掉所有震动。\n" +
             "⚠ 2026-11-12 实测（竖屏第一人称）：原来的幅度会读成「地震」——\n" +
             "   ① 旋转项 = 强度×80°（重击档 2.8°）把整个画面甩出 ~2% 屏宽；\n" +
             "   ② 旋转的 vibrato 14 次 / 0.11s ≈ 127Hz 的高频抖动 ⇒ 读成「震」而不是「顶了一下」。\n" +
             "⇒ 现在：幅度砍到 1/3 + 旋转变成 ~20°/单位 + 抖动次数降到 4 ⇒ 一次「闷响」。\n" +
             "   格挡档现在约 2 像素位移 / 0.5° 倾斜；打击感主要由局部的定格 + 慢放承担。")]
    [SerializeField] private float shakeIntensityScale = 0.35f;
    [Tooltip("**旋转项的度数/单位强度**（默认 20）：旋转幅度 = 强度×这个系数。\n" +
             "原实现是 ×80°（重击档 2.8°）⇒ 整个画面在甩。")]
    [SerializeField] private float rotationDegreesPerUnit = 20f;
    [Tooltip("**旋转的抖动次数**（整段震动里来回几回，默认 4）。\n" +
             "⚠ 原来用 10~14 ⇒ 127Hz 的高频抖动，正是「地震感」的来源；次数少 = 一次闷响。")]
    [SerializeField] private int rotationVibrato = 4;

    [Header("镜头反馈")]
    [SerializeField] private float standardDuration = 0.07f;
    [SerializeField] private float standardIntensity = 0.012f;
    [SerializeField] private float heavyDuration = 0.11f;
    [SerializeField] private float heavyIntensity = 0.035f;
    [SerializeField] private float playerDamageDuration = 0.15f;
    [SerializeField] private float playerDamageIntensity = 0.07f;
    [SerializeField] private float requestCooldown = 0.12f;
    [Tooltip("背景（钉在屏幕上的 clip 空间四边形）跟随相机**滚转**的比例（默认 1）。\n" +
             "远景在无穷远 ⇒ 相机**平移**时它本来就不该动（动了就会与远景错位 ⇒ 「背景和场景不同步」）；\n" +
             "但相机**滚转**时它必须跟着转（否则整幅画面只有世界在歪 ⇒ 像地动山摇）。\n" +
             "0 = 完全不跟（回到「天空钉死」的老行为）。")]
    [Range(0f, 2f)] private float backgroundFollowRotation = 1f;
    [Tooltip("关闭背景跟随（回到「天空钉在屏幕上」的老行为）。\n" +
             "⚠ 勾上会恢复「地动山摇」读法（世界在歪、天空不动）。")]
    [SerializeField] private bool disableBackgroundFollow;
    [Tooltip("敌人受击时的相机旋转震动倍率。1 = 原有表现；0 = 只保留位移型轻微反馈，不产生整屏旋转。")]
    [SerializeField, Range(0f, 2f)] private float hitRotationScale = 1f;
    [Tooltip("玩家受击时的相机旋转震动倍率。1 = 原有表现。")]
    [SerializeField, Range(0f, 2f)] private float playerDamageRotationScale = 1f;
    [Tooltip("**只有重击才震镜头**。\n" +
             "2026-10-26：普通敌人受击时整个场景都在震（背景也跟着位移），\n" +
             "但设计要求是「**敌人自己震**」（受击缩放/闪白），镜头只给重击/格挡这类关键帧。")]
    [SerializeField] private bool heavyOnlyCameraShake = true;

    private Sequence _feedbackSequence;
    private Sequence _fovTween;
    private float _lastRequestTime = -10f;
    private HitFeedbackStrength _activeStrength = HitFeedbackStrength.None;
    private Vector3 _baseLocalPosition;
    private Quaternion _baseLocalRotation;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        _baseLocalPosition = transform.localPosition;
        _baseLocalRotation = transform.localRotation;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        DOTween.Kill(transform, false);
        if (_feedbackSequence != null && _feedbackSequence.IsActive())
            _feedbackSequence.Kill(false);
        _feedbackSequence = null;
        // 背景跟随用的是全局着色器参数 ⇒ 组件销毁时必须清零，否则背景会永远偏着。
        Shader.SetGlobalVector(ScreenShakeId, Vector4.zero);
    }

    public void RequestHit(HitFeedbackContext context)
    {
        if (context.strength < HitFeedbackStrength.Standard)
            return;
        if (context.source == HitFeedbackSource.Dot)
            return;
        // 普通命中不震镜头（敌人自己震）；只有重击才给镜头反馈
        if (heavyOnlyCameraShake && context.strength < HitFeedbackStrength.Heavy)
            return;
        if (GameplayPause.UnscaledTime - _lastRequestTime < requestCooldown
            && context.strength <= _activeStrength)
            return;

        _lastRequestTime = GameplayPause.UnscaledTime;
        bool heavy = context.strength == HitFeedbackStrength.Heavy;
        Vector3 direction = GetImpactDirection(context.worldPosition);
        PlayFeedback(heavy ? heavyDuration : standardDuration,
            heavy ? heavyIntensity : standardIntensity,
            direction,
            hitRotationScale);
        _activeStrength = context.strength;
    }

    /// <summary>
    /// **格挡成功的反馈**（2026-10-26）：玩家用防御姿态挡下敌人攻击时，
    /// 镜头给一下重击级震动 —— 与「普通命中不震镜头」区分开，
    /// 让「挡住了」这个瞬间有明确的画面反馈。
    /// </summary>
    public void RequestGuardBlock(Vector3 worldPosition)
    {
        _lastRequestTime = GameplayPause.UnscaledTime;
        PlayFeedback(heavyDuration, heavyIntensity,
            GetImpactDirection(worldPosition), hitRotationScale);
        _activeStrength = HitFeedbackStrength.Heavy;
    }

    public void RequestPlayerDamage()
    {
        _lastRequestTime = GameplayPause.UnscaledTime;
        PlayFeedback(playerDamageDuration, playerDamageIntensity,
            Vector3.zero, playerDamageRotationScale);
        _activeStrength = HitFeedbackStrength.Heavy;
    }

    /// <summary>
    /// **背景跟随**（2026-11-12）：远景天空是**钉在屏幕上的 clip 空间四边形**
    /// （<c>YSkyBackground.shader</c>，不过投影矩阵）⇒ 相机一震、世界在动、天空钉住
    /// ⇒ 读成「地动山摇」。
    /// ⇒ 把相机相对基准位的**屏幕位移与滚转**写进全局着色器参数，让背景跟着一起动
    /// ⇒ 整帧一起抖 ⇒ 读成「镜头被顶了一下」。
    /// （只在相机偏离基准位时非零 ⇒ 推进/转向时远景仍然「固定或弱视差」）
    ///
    /// ⚠ **不放在 LateUpdate 里**：本组件在 Battle 场景里是 **enabled=false**（未落盘的旧状态），
    ///   组件的 Update/LateUpdate 根本不跑（旧版的背景补偿就是这么变成死代码的）。
    ///   改为挂在**震动自己的 Tween** 的 OnUpdate 上 ⇒ DOTween 与组件 enabled 无关。
    /// </summary>
    private void UpdateBackgroundFollow()
    {
        Vector4 shake = Vector4.zero;
        if (!disableBackgroundFollow)
        {
            // ★ 只跟**滚转**，不跟平移：背景是远景（无穷远）⇒ 相机平移时它本就不该动；
            //   若按某个深度去补偿，它就会与真正的远景（山/墙/树）错开 ⇒「背景和场景不同步」。
            //   滚转必须跟：否则整幅画面只有世界在歪 ⇒ 像地动山摇。
            Quaternion dq = transform.localRotation * Quaternion.Inverse(_baseLocalRotation);
            float roll = dq.eulerAngles.z;
            if (roll > 180f) roll -= 360f;
            float rotScale = backgroundFollowRotation > 0f ? backgroundFollowRotation : 1f;
            shake.z = -roll * Mathf.Deg2Rad * rotScale;
        }
        Shader.SetGlobalVector(ScreenShakeId, shake);
    }

    private static readonly int ScreenShakeId = Shader.PropertyToID("_LocusScreenShake");

    /// <summary>
    /// **破防的镜头反馈**（2026-11-12）：比普通格挡更长一点，但**幅度更小** ——
    /// 破防是「被顶开」，要读得出这一下，但幅度大 + 时长长容易读成「地动山摇」。
    /// 方向沿攻击来源（把镜头从攻击者那一侧顶开），与普通格挡同一套方向解算。
    /// </summary>
    public void RequestGuardBreak(Vector3 worldPosition)
    {
        _lastRequestTime = GameplayPause.UnscaledTime;
        // 破防用旧实例上没有的字段 ⇒ 值为 0 ⇒ 运行时兜底
        float dur = guardBreakDuration > 0f ? guardBreakDuration : 0.14f;
        float inten = guardBreakIntensity > 0f ? guardBreakIntensity : 0.045f;
        PlayFeedback(dur, inten,
            GetImpactDirection(worldPosition), hitRotationScale);
        _activeStrength = HitFeedbackStrength.Heavy;
    }

    private Vector3 GetImpactDirection(Vector3 worldPosition)
    {
        Vector3 viewport = Camera.main != null
            ? Camera.main.WorldToViewportPoint(worldPosition)
            : new Vector3(0.5f, 0.5f, 0f);
        float horizontal = Mathf.Clamp((viewport.x - 0.5f) * 2f, -1f, 1f);
        float vertical = Mathf.Clamp((viewport.y - 0.5f) * 2f, -1f, 1f);
        return new Vector3(-horizontal, -vertical, 0f);
    }

    private void PlayFeedback(float duration, float intensity, Vector3 direction, float rotationScale)
    {
        _feedbackSequence?.Kill(false);
        transform.localPosition = _baseLocalPosition;
        transform.localRotation = _baseLocalRotation;

        // ⚠ 旧场景实例上没有这三个字段 ⇒ 值是 0 ⇒ 运行时兜底
        float scale = shakeIntensityScale > 0f ? shakeIntensityScale : 0.35f;
        float rotPerUnit = rotationDegreesPerUnit > 0f ? rotationDegreesPerUnit : 20f;
        int vib = rotationVibrato > 0 ? rotationVibrato : 4;

        Vector3 offset = direction.sqrMagnitude > 0.0001f
            ? direction.normalized * (intensity * scale)
            : new Vector3(intensity * 0.35f * scale, -intensity * 0.2f * scale, 0f);
        // 镜头反馈是游戏内容：登记到暂停门，暂停时镜头不再抖
        _feedbackSequence = GameplayPause.Bind(DOTween.Sequence().SetTarget(transform).SetUpdate(UpdateType.Normal, true));
        _feedbackSequence.Append(transform.DOLocalMove(_baseLocalPosition + offset, duration * 0.35f).SetEase(Ease.OutQuad));
        _feedbackSequence.Append(transform.DOLocalMove(_baseLocalPosition, duration * 0.65f).SetEase(Ease.OutCubic));
        if (rotationScale > 0f)
        {
            // 旋转幅度：强度×度数/单位（默认 20 ⇒ 重击档 0.7°），抖动次数默认 4（一次「闷响」）
            _feedbackSequence.Join(transform.DOShakeRotation(duration,
                new Vector3(0f, 0f, intensity * rotPerUnit * rotationScale), vib, 90f, false).SetEase(Ease.OutQuad));
        }
        // ★ 背景跟随挂在**震动自己的 Tween** 上（不能用 LateUpdate：本组件在 Battle 场景里是 enabled=false）
        _feedbackSequence.OnUpdate(UpdateBackgroundFollow);
        _feedbackSequence.OnComplete(() =>
        {
            transform.localPosition = _baseLocalPosition;
            transform.localRotation = _baseLocalRotation;
            UpdateBackgroundFollow();     // 归位 ⇒ 全局参数清零
            _feedbackSequence = null;
            _activeStrength = HitFeedbackStrength.None;
        });
        _feedbackSequence.OnKill(() =>
        {
            transform.localPosition = _baseLocalPosition;
            transform.localRotation = _baseLocalRotation;
            UpdateBackgroundFollow();     // 归位 ⇒ 全局参数清零
            _feedbackSequence = null;
            _activeStrength = HitFeedbackStrength.None;
        });
    }

    /// <summary>
    /// 破防的**镜头推近**（REQ-006 · ⑤层）：FOV 脉冲（推近再回），**不是抖**。
    /// PBZ 的「成功瞬间」靠极短推近制造时间感；幅度必须小（≤2°）否则会动构图。
    /// ⚠ 推近幅度影响构图 ⇒ 参数进配置、默认保守，随时可关（幅度 = 0 即关闭）。
    /// ★ 2026-11-13：**推进后要有一段保持**（hold）⇒ 镜头在整个慢放窗口里持续“压着”，
    ///   慢放才读得出来（一次性推完就回 ⇒ 只剩一下普通抖动）。
    /// </summary>
    public void RequestGuardBreakPush(float fovDegrees, float holdSeconds = 0.55f)
    {
        if (fovDegrees <= 0f) return;
        var cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        _fovTween?.Kill(false);
        float baseFov = cam.fieldOfView;
        float pushedFov = baseFov - fovDegrees;
        _fovTween = GameplayPause.Bind(DOTween.Sequence()
            .Append(DOTween.To(() => cam.fieldOfView, v => cam.fieldOfView = v, pushedFov, 0.08f)
                .SetEase(Ease.OutQuad))
            .Append(DOTween.To(() => cam.fieldOfView, v => cam.fieldOfView = v, pushedFov, Mathf.Max(0f, holdSeconds)))
            .Append(DOTween.To(() => cam.fieldOfView, v => cam.fieldOfView = v, baseFov, 0.3f)
                .SetEase(Ease.OutCubic))
            .SetUpdate(UpdateType.Normal, true));
        _fovTween.OnComplete(() => { cam.fieldOfView = baseFov; _fovTween = null; });
        _fovTween.OnKill(() => { cam.fieldOfView = baseFov; _fovTween = null; });
    }

    private void StopFeedback()
    {
        _feedbackSequence?.Kill(false);
        _feedbackSequence = null;
        _activeStrength = HitFeedbackStrength.None;
        transform.localPosition = _baseLocalPosition;
        transform.localRotation = _baseLocalRotation;
    }
}
