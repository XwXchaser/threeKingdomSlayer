using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 狂怒大招效果：持续时间内无敌 + 自动 Stab + 禁止技能输入
/// 配置数据从 UltimateSkillConfig 读取
/// </summary>
public class UltimateEffect_Berserk : UltimateEffect
{
    private UltimateSkillConfig config;
    private Coroutine berserkRoutine;
    private List<int> aliveCols = new List<int>();
    private int stabRoundIndex;


    public override void Execute()
    {
        config = PlayerState.Instance?.heroConfig?.ultimateSkillConfig;
        if (config == null)
        {
            Debug.LogError("[UltimateEffect_Berserk] 未找到 UltimateSkillConfig");
            return;
        }

        // 设置血量条为橙色（委托给 BattleHUD → HeroHUD）
        var hud = FindObjectOfType<BattleHUD>();
        if (hud != null)
            hud.SetHealthBarColor(new Color(1f, 0.5f, 0f)); // orange

        // 无敌
        if (PlayerState.Instance != null)
            PlayerState.Instance.isInvincible = true;

        // 禁技能输入
        if (InputManager.Instance != null)
            InputManager.Instance.skillInputEnabled = false;

        berserkRoutine = StartCoroutine(BerserkLoop());
    }

    private IEnumerator BerserkLoop()
    {
        float elapsed = 0f;
        float stabTimer = 0f;

        while (elapsed < config.berserkDuration)
        {
            stabTimer += Time.deltaTime;
            if (stabTimer >= config.berserkStabCooldown)
            {
                stabTimer -= config.berserkStabCooldown;
                ExecuteAutoStab();
            }

            yield return null;
            elapsed += Time.deltaTime;
        }

        Cleanup();
    }

    private void ExecuteAutoStab()
    {
        var colMgr = AttackSystem.Instance?.columnManager;
        if (colMgr == null) return;

        // 每轮重新收集存活列，保证遍历到所有有敌人的列
        aliveCols.Clear();
        for (int col = 0; col < 5; col++)
        {
            var enemy = colMgr.GetFrontEnemy(col);
            if (enemy != null && enemy.state != EnemyState.Dead)
                aliveCols.Add(col);
        }

        if (aliveCols.Count == 0) return;

        // 轮转索引：确保每列依次被戳，不遗漏
        stabRoundIndex = stabRoundIndex % aliveCols.Count;
        int targetCol = aliveCols[stabRoundIndex];
        stabRoundIndex++;

        // 伤害 = Ult 自身 damage × (1+武将加成) × 暴怒倍率
        float bonusPercent = PlayerState.Instance?.heroConfig?.damageBonusPercent ?? 0f;
        float damage = config.damage * (1f + bonusPercent) * config.berserkDamageMultiplier;
        AttackSystem.Instance?.ForceExecuteStab(targetCol, damage);
    }

    private void Cleanup()
    {
        if (PlayerState.Instance != null)
            PlayerState.Instance.isInvincible = false;

        if (InputManager.Instance != null)
            InputManager.Instance.skillInputEnabled = true;

        // 恢复血量条原始颜色
        var hud = FindObjectOfType<BattleHUD>();
        if (hud != null)
            hud.ResetHealthBarColor();
    }

    public override void Cancel()
    {
        if (berserkRoutine != null)
        {
            StopCoroutine(berserkRoutine);
            berserkRoutine = null;
        }
        Cleanup();
    }

    private void OnDestroy()
    {
        Cancel();
    }

    public override float GetLifetime()
    {
        if (config != null)
            return config.berserkDuration + 0.1f; // 多加一点确保协程跑完
        return 5.1f;
    }
}
