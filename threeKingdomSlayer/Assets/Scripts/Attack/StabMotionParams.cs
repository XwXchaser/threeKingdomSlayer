using UnityEngine;

/// <summary>
/// 上一段戳击交接过来的起始姿态。连段时用它把"枪不收回、续着走"接上。
/// </summary>
public struct StabStartPose
{
    public bool valid;
    public Vector3 worldPosition;
    public Quaternion worldRotation;
    /// <summary>长度倍率（1 = 原始长度）</summary>
    public float lengthScale;

    public static StabStartPose None => new StabStartPose { valid = false, worldRotation = Quaternion.identity, lengthScale = 1f };
}

/// <summary>
/// 一次戳击的动作参数：角度与力度。默认值等于改造前写死的常量。
/// 命中判定始终沿本段目标列的射线，倾角必须在命中前收回，否则视觉与判定会错开。
/// </summary>
public struct StabMotionParams
{
    public float windupRatio;
    public float thrustRatio;
    public float penetrationRatio;
    public float windupHoldSeconds;
    public float thrustLengthScale;
    public float thrustWidthScale;
    public float motionBlurScale;
    public float visualTiltDegrees;
    public float rollDegrees;
    public float speedFrameStart01;
    public float speedFrameEnd01;
    public float blurThrust;
    public float blurSpeedFrame;
    public float blurPenetration;
    public HitFeedbackStrength firstHitStrength;
    /// <summary>连段蓄力：枪体被拉回的回收段位置（0 = 不收回，1 = 完全收回）</summary>
    public float chargeHoldRetractRatio;
    /// <summary>连段蓄力：从按下到拉满蓄势位的时长（秒）</summary>
    public float chargeHoldPullSeconds;
    /// <summary>连段蓄力：拉回时枪身额外抬起的仰角（度）</summary>
    public float chargeHoldPitchDegrees;
    /// <summary>连段蓄力：拉满后再向后一顿的时长（秒）</summary>
    public float chargeHoldSettleSeconds;
    /// <summary>连段蓄力：向后一顿的额外回收距离比例</summary>
    public float chargeHoldSettleRatio;
    /// <summary>连段蓄力：蓄势位微颤幅度（世界单位）</summary>
    public float chargeHoldShakeAmplitude;
    /// <summary>连段蓄力：微颤频率（Hz）</summary>
    public float chargeHoldShakeFrequency;

    /// <summary>蓄力保持：枪身朝「当前指向列」偏摆的上限（度）。0 = 不偏摆</summary>
    public float chargeHoldYawDegrees;
    /// <summary>释放：刺出段起点内完成朝向归零所占的比例（0 = 沿用整段起手过渡）</summary>
    public float redirectSnapRatio;
    /// <summary>释放：归零后的过冲角（度，负值 = 朝行进反方向过冲一点）</summary>
    public float redirectOvershootDegrees;
    /// <summary>命中震动：沿枪轴的轴向回弹幅度（世界单位）</summary>
    public float shakeAmplitude;
    /// <summary>命中震动：垂直于枪轴的抖动幅度（世界单位）</summary>
    public float shakeLateral;
    /// <summary>命中震动：绕枪身长轴的滚转幅度（度）</summary>
    public float shakeRollDegrees;
    /// <summary>命中震动：枪尖俯仰点头幅度（度）</summary>
    public float shakePitchDegrees;
    /// <summary>命中震动：总时长（秒）</summary>
    public float shakeDuration;
    /// <summary>命中震动：频率（Hz）</summary>
    public float shakeFrequency;
    /// <summary>命中震动：第二排及之后的倍率（第 1 排 = 1）</summary>
    public float shakeSecondRowScale;

    public static StabMotionParams Default => new StabMotionParams
    {
        windupRatio = 0.12f,
        thrustRatio = 0.28f,
        penetrationRatio = 0.08f,
        windupHoldSeconds = 0f,
        thrustLengthScale = 1.18f,
        thrustWidthScale = 0.86f,
        motionBlurScale = 1f,
        visualTiltDegrees = 0f,
        rollDegrees = 0f,
        speedFrameStart01 = 0.1f,
        speedFrameEnd01 = 1.0f,
        blurThrust = 28f,
        blurSpeedFrame = 14f,
        blurPenetration = 18f,
        firstHitStrength = HitFeedbackStrength.Standard,
        chargeHoldRetractRatio = 0.75f,
        chargeHoldPullSeconds = 0.3f,
        chargeHoldPitchDegrees = 8f,
        chargeHoldSettleSeconds = 0.12f,
        chargeHoldSettleRatio = 0.1f,
        chargeHoldShakeAmplitude = 0.06f,
        chargeHoldShakeFrequency = 14f,
        chargeHoldYawDegrees = 0f,
        redirectSnapRatio = 0f,
        redirectOvershootDegrees = -3f,
        shakeAmplitude = 0f,
        shakeLateral = 0f,
        shakeRollDegrees = 0f,
        shakePitchDegrees = 0f,
        shakeDuration = 0.1f,
        shakeFrequency = 18f,
        shakeSecondRowScale = 0.6f
    };

    /// <summary>从招式资产读取动作参数；未配置的字段回落到默认值</summary>
    public static StabMotionParams FromConfig(AttackSkillConfig cfg)
    {
        StabMotionParams p = Default;
        if (cfg == null) return p;

        if (cfg.stabWindupRatio > 0f) p.windupRatio = cfg.stabWindupRatio;
        if (cfg.stabThrustRatio > 0f) p.thrustRatio = cfg.stabThrustRatio;
        if (cfg.stabPenetrationRatio > 0f) p.penetrationRatio = cfg.stabPenetrationRatio;
        p.windupHoldSeconds = Mathf.Max(0f, cfg.stabWindupHoldSeconds);
        if (cfg.stabThrustLengthScale > 0f) p.thrustLengthScale = cfg.stabThrustLengthScale;
        if (cfg.stabThrustWidthScale > 0f) p.thrustWidthScale = cfg.stabThrustWidthScale;
        if (cfg.stabMotionBlurScale > 0f) p.motionBlurScale = cfg.stabMotionBlurScale;
        p.visualTiltDegrees = cfg.stabVisualTiltDegrees;
        p.rollDegrees = cfg.stabRollDegrees;
        p.speedFrameStart01 = Mathf.Clamp01(cfg.stabSpeedFrameStart01);
        p.speedFrameEnd01 = Mathf.Clamp01(cfg.stabSpeedFrameEnd01);
        if (cfg.stabBlurThrust > 0f) p.blurThrust = cfg.stabBlurThrust;
        if (cfg.stabBlurSpeedFrame > 0f) p.blurSpeedFrame = cfg.stabBlurSpeedFrame;
        if (cfg.stabBlurPenetration > 0f) p.blurPenetration = cfg.stabBlurPenetration;
        p.firstHitStrength = cfg.stabFirstHitStrength;
        p.chargeHoldRetractRatio = Mathf.Clamp01(cfg.chargeHoldRetractRatio);
        p.chargeHoldPullSeconds = Mathf.Max(0.05f, cfg.chargeHoldPullSeconds);
        p.chargeHoldPitchDegrees = cfg.chargeHoldPitchDegrees;
        p.chargeHoldSettleSeconds = Mathf.Max(0f, cfg.chargeHoldSettleSeconds);
        p.chargeHoldSettleRatio = Mathf.Clamp01(cfg.chargeHoldSettleRatio);
        p.chargeHoldShakeAmplitude = Mathf.Max(0f, cfg.chargeHoldShakeAmplitude);
        p.chargeHoldShakeFrequency = Mathf.Max(1f, cfg.chargeHoldShakeFrequency);
        p.chargeHoldYawDegrees = cfg.chargeHoldYawDegrees;
        p.redirectSnapRatio = Mathf.Clamp01(cfg.stabRedirectSnapRatio);
        p.redirectOvershootDegrees = cfg.stabRedirectOvershootDegrees;
        p.shakeAmplitude = Mathf.Max(0f, cfg.hitShakeAmplitude);
        p.shakeLateral = Mathf.Max(0f, cfg.hitShakeLateral);
        p.shakeRollDegrees = Mathf.Max(0f, cfg.hitShakeRollDegrees);
        p.shakePitchDegrees = Mathf.Max(0f, cfg.hitShakePitchDegrees);
        p.shakeDuration = Mathf.Max(0f, cfg.hitShakeDuration);
        p.shakeFrequency = Mathf.Max(0f, cfg.hitShakeFrequency);
        p.shakeSecondRowScale = Mathf.Clamp01(cfg.hitShakeSecondRowScale);
        return p;
    }
}
