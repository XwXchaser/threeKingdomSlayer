using UnityEngine;

public enum HitFeedbackSource
{
    BasicAttack,
    ActiveSkill,
    Passive,
    Item,
    Phantom,
    Dot,
    Ultimate,
    Displacement,
    SpikeTrap
}

public enum HitFeedbackStrength
{
    None,
    Light,
    Standard,
    Heavy
}

public readonly struct HitFeedbackContext
{
    public readonly Enemy enemy;
    public readonly DamageType damageType;
    public readonly HitFeedbackSource source;
    public readonly HitFeedbackStrength strength;
    public readonly float damage;
    public readonly bool isSharedHealth;
    public readonly bool causesDisplacement;
    public readonly bool hasImpactPosition;
    public readonly Vector3 worldPosition;
    public readonly Vector3 impactDirection;

    public HitFeedbackContext(Enemy enemy, DamageType damageType, HitFeedbackSource source,
        HitFeedbackStrength strength, float damage, bool isSharedHealth, bool causesDisplacement,
        Vector3 worldPosition, Vector3 impactDirection = default, bool hasImpactPosition = false)
    {
        this.enemy = enemy;
        this.damageType = damageType;
        this.source = source;
        this.strength = strength;
        this.damage = damage;
        this.isSharedHealth = isSharedHealth;
        this.causesDisplacement = causesDisplacement;
        this.hasImpactPosition = hasImpactPosition;
        this.worldPosition = worldPosition;
        this.impactDirection = impactDirection;
    }
}

public static class HitFeedbackManager
{
    public const bool EnableDebugLogs = true;

    private const float LightHitStopDuration = 0.045f;
    private const float StandardHitStopDuration = 0.09f;
    private const float HeavyHitStopDuration = 0.14f;

    public static HitFeedbackContext CreateDamageContext(Enemy enemy, DamageType damageType,
        HitFeedbackSource source, HitFeedbackStrength strength, float damage, bool isSharedHealth = false,
        Vector3? impactPosition = null, Vector3 impactDirection = default)
    {
        bool causesDisplacement = damageType == DamageType.Launch || source == HitFeedbackSource.Displacement;
        return new HitFeedbackContext(enemy, damageType, source, strength, damage, isSharedHealth,
            causesDisplacement, impactPosition ?? (enemy != null ? enemy.transform.position : Vector3.zero),
            impactDirection, impactPosition.HasValue);
    }

    public static float GetHitStopDuration(HitFeedbackStrength strength)
    {
        return strength switch
        {
            HitFeedbackStrength.Light => LightHitStopDuration,
            HitFeedbackStrength.Standard => StandardHitStopDuration,
            HitFeedbackStrength.Heavy => HeavyHitStopDuration,
            _ => 0f
        };
    }

    public static void Trigger(HitFeedbackContext context)
    {
        if (context.enemy == null || context.strength == HitFeedbackStrength.None)
            return;

        float duration = GetHitStopDuration(context.strength);

        if (EnableDebugLogs && context.strength >= HitFeedbackStrength.Standard)
        {
            Debug.Log($"[HitFeedback] Trigger enemy={context.enemy.DebugTag} source={context.source} type={context.damageType} strength={context.strength} duration={duration:F3}s shared={context.isSharedHealth} displacement={context.causesDisplacement} frame={Time.frameCount}");
        }

        context.enemy.ApplyHitStop(duration);
        CameraFeedbackController.Instance?.RequestHit(context);
        if (context.source != HitFeedbackSource.SpikeTrap)
            PixelHitEffectManager.Instance?.RequestHit(context);
    }

    public static HitFeedbackStrength ResolveStrength(DamageType damageType,
        HitFeedbackSource source, float damage, bool isSharedHealth = false)
    {
        if (damage <= 0f || source == HitFeedbackSource.Dot)
            return HitFeedbackStrength.None;
        if (source == HitFeedbackSource.Phantom)
            return HitFeedbackStrength.Light;
        if (source == HitFeedbackSource.Passive || source == HitFeedbackSource.Item)
            return HitFeedbackStrength.Light;
        if (source == HitFeedbackSource.Ultimate)
            return HitFeedbackStrength.Heavy;
        if (source == HitFeedbackSource.Displacement || damageType == DamageType.Launch)
            return HitFeedbackStrength.Heavy;
        if (isSharedHealth)
            return HitFeedbackStrength.Light;
        return HitFeedbackStrength.Standard;
    }
}
