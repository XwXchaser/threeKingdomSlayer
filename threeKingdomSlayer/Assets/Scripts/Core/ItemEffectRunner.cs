using UnityEngine;

/// <summary>道具效果执行器 — 处理道具使用时的效果触发</summary>
public class ItemEffectRunner : MonoBehaviour
{
    public static ItemEffectRunner Instance { get; private set; }

    [Header("万箭齐发")]
    [Tooltip("箭雨特效 Prefab（需挂载 TimedArrowEffect）")]
    public GameObject arrowPrefab;
    [Tooltip("箭矢伤害")]
    public int arrowDamage = 10;
    [Tooltip("波数")]
    public int arrowWaves = 3;
    [Tooltip("每波间隔秒")]
    public float arrowWaveInterval = 0.3f;
    [Tooltip("排数")]
    public int arrowRows = 3;
    [Tooltip("每波箭数")]
    public int arrowsPerWave = 5;

    [Header("火蛇机关")]
    [Tooltip("火焰特效 Prefab")]
    public GameObject firePrefab;
    [Tooltip("火焰伤害")]
    public int fireDamage = 3;
    [Tooltip("火焰排数")]
    public int fireRows = 3;
    [Tooltip("火焰持续时间")]
    public float fireDuration = 2f;

    [Header("火蛇机关覆盖")]
    [Tooltip("火蛇喷口相对 Fire Start Z 的额外偏移；负值更靠近玩家")]
    public float fireSnakeStartZOffset = -2f;

    [Header("虚幻武器")]
    [Tooltip("幻影持续时间")]
    public float phantomDuration = 5f;
    [Tooltip("幻影攻击间隔")]
    public float phantomInterval = 0.5f;
    [Tooltip("幻影伤害比例")]
    public float phantomDamageRatio = 0.5f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    public bool TryActivate(UpgradeDefinition def)
    {
        if (def == null) return false;
        switch (def.gestureId)
        {
            case "arrow_rain": ActivateArrowRain(); return true;
            case "fire_snake": ActivateFireSnake(); return true;
            case "phantom_weapon_item": ActivatePhantomWeapon(); return true;
        }
        return false;
    }

    private void ActivateArrowRain()
    {
        if (arrowPrefab == null) return;

        var arrowRain = Instantiate(arrowPrefab);
        var effect = arrowRain.GetComponent<TimedArrowEffect>();
        if (effect == null)
        {
            Destroy(arrowRain);
            return;
        }

        float itemDmgBonus = UpgradeEffectManager.Instance != null ? UpgradeEffectManager.Instance.GetItemDamageBonus() : 0f;
        float damagePerWave = arrowDamage * arrowsPerWave * (1f + itemDmgBonus);
        effect.Play(arrowRows, arrowWaves, damagePerWave, arrowsPerWave, arrowsPerWave, arrowWaveInterval);
    }

    private void ActivateFireSnake()
    {
        var colManager = AttackSystem.Instance?.columnManager;
        if (colManager == null || firePrefab == null) return;

        float itemDmgBonus = UpgradeEffectManager.Instance != null ? UpgradeEffectManager.Instance.GetItemDamageBonus() : 0f;
        int finalDamage = Mathf.RoundToInt(fireDamage * (1f + itemDmgBonus));

        var cols = new System.Collections.Generic.List<int>();
        for (int c = 0; c < 5; c++) cols.Add(c);

        var fire = Instantiate(firePrefab);
        var fireFx = fire.GetComponent<ShootFireEffect>();
        if (fireFx != null)
        {
            fireFx.PlaySweep(cols, finalDamage, fireRows, fireSnakeStartZOffset);
        }
        else
            Destroy(fire);
        Destroy(fire, fireDuration);
    }

    private void ActivatePhantomWeapon()
    {
        var attackSys = AttackSystem.Instance;
        if (attackSys == null || attackSys.columnManager == null) return;

        float itemDmgBonus = UpgradeEffectManager.Instance != null ? UpgradeEffectManager.Instance.GetItemDamageBonus() : 0f;
        float finalRatio = phantomDamageRatio * (1f + itemDmgBonus);
        StartCoroutine(PhantomRoutine(attackSys, finalRatio));
    }

    private System.Collections.IEnumerator PhantomRoutine(AttackSystem attackSys, float damageRatio)
    {
        float elapsed = 0f;
        TriggerPhantomAttack(attackSys, damageRatio);

        while (elapsed < phantomDuration)
        {
            yield return new WaitForSeconds(phantomInterval);
            elapsed += phantomInterval;
            TriggerPhantomAttack(attackSys, damageRatio);
        }
    }

    private static void TriggerPhantomAttack(AttackSystem attackSys, float damageRatio)
    {
        var colManager = attackSys.columnManager;
        var aliveCols = new System.Collections.Generic.List<int>();
        for (int c = 0; c < colManager.columnCount; c++)
        {
            var enemies = colManager.GetEnemiesInColumn(c);
            if (enemies == null) continue;
            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy != null && enemy.state != EnemyState.Dead && (!enemy.isBoss || enemy.bossState == BossState.InCombat))
                {
                    aliveCols.Add(c);
                    break;
                }
            }
        }

        if (aliveCols.Count == 0) return;
        int col = aliveCols[Random.Range(0, aliveCols.Count)];
        attackSys.ExecutePhantomAttack(AttackType.Stab, col, col <= 2, damageRatio, 0.5f);
    }
}
