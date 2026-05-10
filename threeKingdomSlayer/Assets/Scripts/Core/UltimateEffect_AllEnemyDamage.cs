using UnityEngine;

/// <summary>
/// 示例大招效果：对全部存活敌人造成大范围伤害
/// </summary>
public class UltimateEffect_AllEnemyDamage : UltimateEffect
{
    [Header("伤害配置")]
    [Tooltip("对每个敌人的伤害量")]
    public float damageAmount = 50f;
    [Tooltip("伤害类型")]
    public DamageType damageType = DamageType.Slash;

    [Header("视觉配置")]
    [Tooltip("屏幕震动时长")]
    public float screenShakeDuration = 0.3f;
    [Tooltip("屏幕震动强度")]
    public float screenShakeIntensity = 0.5f;

    public override void Execute()
    {
        var enemyManager = EnemyManager.Instance;
        if (enemyManager == null)
        {
            Debug.LogWarning("[UltimateEffect] EnemyManager 未找到");
            return;
        }

        var enemies = enemyManager.GetAllAliveEnemies();
        if (enemies == null || enemies.Count == 0)
        {
            Debug.Log("[UltimateEffect] 没有存活敌人");
            return;
        }

        foreach (var enemy in enemies)
        {
            if (enemy != null && enemy.state != EnemyState.Dead)
            {
                enemy.TakeDamage(damageAmount, damageType);
            }
        }

        Debug.Log($"[UltimateEffect] 对 {enemies.Count} 个敌人造成 {damageAmount} 点伤害");

        // 屏幕震动（通过 AttackWave 的机制模拟或在场景中挂载震动组件）
        // 此处仅作示例，实际震动需配合 CameraShake 等组件
    }

    public override float GetLifetime() => 2f;
}
