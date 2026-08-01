using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 共享血量组：同行相邻同ID敌人共享一个血量池。
/// 攻击任一成员时伤害扣除共享池，池归零时所有成员同时死亡。
/// </summary>
public class SharedHealthGroup
{
    public float currentHealth;
    public float maxHealth;
    public List<Enemy> members = new List<Enemy>();
    public List<GameObject> chainObjects = new List<GameObject>();

    public GameObject chainPrefab;
    public Vector3 chainScale = Vector3.one;
    public float chainYOffset = 0f;

    public SharedHealthGroup(GameObject chainPrefab, Vector3 chainScale, float chainYOffset)
    {
        this.chainPrefab = chainPrefab;
        this.chainScale = chainScale;
        this.chainYOffset = chainYOffset;
    }

    public void AddMember(Enemy enemy)
    {
        members.Add(enemy);
        enemy.sharedHealthGroup = this;
        maxHealth += enemy.maxHealth;
        currentHealth += enemy.currentHealth;
    }

    /// <summary>
    /// 在所有成员之间生成铁链连接
    /// </summary>
    public void SpawnChains()
    {
        if (chainPrefab == null || members.Count < 2) return;

        for (int i = 0; i < members.Count - 1; i++)
        {
            var chain = Object.Instantiate(chainPrefab);
            chain.transform.localScale = chainScale;
            chainObjects.Add(chain);
            UpdateChainPosition(i);
        }
    }

    /// <summary>
    /// 更新所有铁链位置
    /// </summary>
    public void UpdateAllChainPositions()
    {
        for (int i = 0; i < chainObjects.Count; i++)
            UpdateChainPosition(i);
    }

    private void UpdateChainPosition(int chainIndex)
    {
        if (chainIndex < 0 || chainIndex >= chainObjects.Count) return;
        if (chainIndex >= members.Count - 1) return;

        var a = members[chainIndex];
        var b = members[chainIndex + 1];
        var chain = chainObjects[chainIndex];
        if (a == null || b == null || chain == null) return;

        Vector3 mid = (a.transform.position + b.transform.position) * 0.5f;
        mid.y += chainYOffset;
        chain.transform.position = mid;
    }

    /// <summary>
    /// 受到伤害 — 扣除共享池HP，池归零时全部死亡
    /// </summary>
    public void TakeDamage(float rawDamage, DamageType damageType, Enemy hitMember, Color? damageNumberColor = null, bool triggerHitAnimation = true, bool countsForCombo = true, bool canInterruptAttack = true, bool ignoreDamageModifiers = false)
    {
        if (members.Count == 0) return;

        float multiplier = hitMember != null && !ignoreDamageModifiers ? hitMember.GetDamageMultiplier(damageType) : 1f;
        float finalDamage = rawDamage * multiplier;

        currentHealth -= finalDamage;
        AudioManager.Instance?.PostEvent("Enemy_Hit");

        // 受伤跳字
        if (hitMember != null && DamageNumberManager.Instance != null)
            DamageNumberManager.Instance.Spawn(hitMember.transform.position, finalDamage, damageNumberColor);

        if (countsForCombo && hitMember != null)
            hitMember.OnDamageTaken?.Invoke(hitMember);

        // 触发受伤闪白 + 血条更新（对所有成员）
        foreach (var m in members)
        {
            if (m == null || m.state == EnemyState.Dead) continue;

            if (triggerHitAnimation)
                m.ApplyDamageFeedback();

            // 更新血条
            var bar = m.GetComponent<EnemyHealthBar>();
            if (bar == null) bar = m.gameObject.AddComponent<EnemyHealthBar>();
            if (currentHealth > 0f) bar.Show(currentHealth / maxHealth);
        }

        // 同步更新每个成员的 currentHealth（供外部读取）
        foreach (var m in members)
        {
            if (m != null) m.currentHealth = Mathf.Max(0, currentHealth);
        }

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            KillAll();
        }
    }

    /// <summary>
    /// 全部击杀 — 每个成员独立触发死亡事件/掉落
    /// </summary>
    public void KillAll()
    {
        var membersCopy = new List<Enemy>(members);
        members.Clear();

        DestroyChains();

        foreach (var enemy in membersCopy)
        {
            if (enemy != null && enemy.state != EnemyState.Dead)
            {
                enemy.currentHealth = 0f;
                enemy.sharedHealthGroup = null;
                enemy.Die();
            }
        }

        EnemyManager.Instance?.RemoveGroup(this);
    }

    /// <summary>
    /// 解散组 — 剩余HP平分给成员，不再共享
    /// </summary>
    public void Disband()
    {
        if (members.Count == 0) return;

        float hpPerMember = currentHealth / members.Count;

        var membersCopy = new List<Enemy>(members);
        members.Clear();

        DestroyChains();

        foreach (var enemy in membersCopy)
        {
            if (enemy != null && enemy.state != EnemyState.Dead)
            {
                enemy.sharedHealthGroup = null;
                enemy.currentHealth = Mathf.Min(hpPerMember, enemy.maxHealth);
            }
        }

        EnemyManager.Instance?.RemoveGroup(this);
    }

    private void DestroyChains()
    {
        foreach (var chain in chainObjects)
        {
            if (chain != null) Object.Destroy(chain);
        }
        chainObjects.Clear();
    }
}
