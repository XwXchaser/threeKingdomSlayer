using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 攻击计数触发模块 — 单例
///
/// 监听 AttackSystem.OnAttackPerformed，为每个已注册的升级维护独立计数器。
/// 计数器到达阈值时，根据 effectType 分发到自包含的效果执行器。
///
/// 效果执行不再依赖攻击上下文（_lastAttackType 等已移除），
/// 每个效果从自身每级配置读取所需的全部参数。
/// </summary>
public class PassiveTriggerModule : MonoBehaviour
{
    public static PassiveTriggerModule Instance { get; private set; }

    [Header("特效预制体")]
    [Tooltip("喷火特效 prefab（effectType=passive_timed_aoe 被攻击计数触发时使用）")]
    public GameObject fireEffectPrefab;
    [Tooltip("箭雨特效 prefab（effectType=passive_timed_arrow 被攻击计数触发时使用）")]
    public GameObject arrowEffectPrefab;
    [Tooltip("箭矢齐射精灵模板（effectType=passive_arrow_volley）")]
    [SerializeField] private SpriteRenderer _arrowVolleyTemplate;
    [Tooltip("箭矢齐射 Z 轴出生位置")]
    public float arrowVolleySpawnZ = 1.5f;

    [Header("测试开关")]
    [Tooltip("开启后所有效果每次攻击都触发（忽略配表阈值）")]
    [SerializeField] private bool _forceTriggerEveryAttack;

    private class PassiveState
    {
        public UpgradeDefinition definition;
        public int level;
        public int currentCount;
        public int threshold;
    }

    private Dictionary<string, PassiveState> _states = new Dictionary<string, PassiveState>();
    private AttackType _currentAttackType;  // 当前触发攻击类型，供 ExecuteArrowVolley 等上下文依赖效果使用

    public System.Action<string, int> OnPassiveRegistered;   // upgradeId, threshold
    public System.Action<string> OnPassiveTriggered;         // upgradeId
    public IEnumerable<string> RegisteredUpgradeIds => _states.Keys;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        if (AttackSystem.Instance != null)
            AttackSystem.Instance.OnAttackPerformed += OnAttackPerformed;
    }

    private void OnAttackPerformed(AttackType attackType, int targetColumn, bool slashLeftToRight)
    {
        _currentAttackType = attackType;
        foreach (var kv in _states)
        {
            var state = kv.Value;
            state.currentCount++;

            if (state.currentCount >= state.threshold)
            {
                state.currentCount = 0;
                DispatchEffect(state);
            }
        }
    }

    /// <summary>根据 effectType 分发到对应效果执行器</summary>
    private void DispatchEffect(PassiveState state)
    {
        switch (state.definition.effectType)
        {
            case "passive_phantom_weapon":
                StartCoroutine(ExecutePhantoms(state));
                break;
            case "passive_return_wave":
                StartCoroutine(ExecuteReturnWave(state));
                break;
            case "passive_chain_bounce":
                StartCoroutine(ExecuteChainBounce(state));
                break;
            case "passive_timed_aoe":
                ExecuteFire(state);
                break;
            case "passive_timed_arrow":
                ExecuteArrow(state);
                break;
            case "passive_arrow_volley":
                StartCoroutine(ExecuteArrowVolley(state));
                break;
            default:
                Debug.LogWarning($"[PassiveTriggerModule] 未知 effectType: {state.definition.effectType}");
                break;
        }
    }

    // ══════════════════════════════════════════
    // 注册 / 注销
    // ══════════════════════════════════════════

    public void Register(UpgradeDefinition def, int level)
    {
        if (def == null) return;

        int threshold = _forceTriggerEveryAttack ? 1 : def.GetTriggerThreshold(level);
        if (threshold <= 0)
        {
            Debug.LogWarning($"[PassiveTriggerModule] {def.upgradeId} Lv.{level} GetTriggerThreshold 返回 {threshold}，使用 intValue={def.intValue} 兜底");
            threshold = def.intValue > 0 ? def.intValue : 4;
        }

        if (_states.TryGetValue(def.upgradeId, out var existing))
        {
            existing.threshold = threshold;
            existing.level = level;
            existing.definition = def;
        }
        else
        {
            _states[def.upgradeId] = new PassiveState
            {
                definition = def,
                level = level,
                currentCount = 0,
                threshold = threshold
            };
        }

        OnPassiveRegistered?.Invoke(def.upgradeId, threshold);
        Debug.Log($"[PassiveTriggerModule] 注册: {def.displayName} Lv.{level} threshold={threshold} effectType={def.effectType}");
    }

    public void Unregister(string upgradeId)
    {
        _states.Remove(upgradeId);
    }

    public void ResetAll()
    {
        _states.Clear();
    }

    /// <summary>获取已注册被动的当前攻击计数，未注册返回 -1</summary>
    public int GetCurrentCount(string upgradeId)
    {
        return _states.TryGetValue(upgradeId, out var s) ? s.currentCount : -1;
    }

    /// <summary>获取已注册被动的触发阈值，未注册返回 -1</summary>
    public int GetThreshold(string upgradeId)
    {
        return _states.TryGetValue(upgradeId, out var s) ? s.threshold : -1;
    }

    // ══════════════════════════════════════════
    // 效果执行器（自包含，不依赖攻击上下文）
    // ══════════════════════════════════════════

    private IEnumerator ExecutePhantoms(PassiveState state)
    {
        if (AttackSystem.Instance == null)
        {
            Debug.LogWarning("[PassiveTriggerModule] AttackSystem.Instance is null");
            yield break;
        }

        var def = state.definition;
        def.GetPhantomEffectConfig(state.level, out var attackType, out var targetColumn, out var steps);

        if (steps == null || steps.Count == 0)
        {
            Debug.LogWarning($"[PassiveTriggerModule] {def.displayName} phantomSteps 为空");
            yield break;
        }

        // slashLeftToRight 根据列位置推断（col 1-2 向右，col 3 向左）
        bool slashLeftToRight = targetColumn <= 2;

        for (int i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            if (i == 0 && step.delaySeconds > 0f)
                yield return new WaitForSeconds(step.delaySeconds);
            if (i > 0)
            {
                float delay = step.delaySeconds > 0f ? step.delaySeconds : 0.15f;
                yield return new WaitForSeconds(delay);
            }

            if (AttackSystem.Instance == null) yield break;

            bool hit = AttackSystem.Instance.ExecutePhantomAttack(
                attackType, targetColumn, slashLeftToRight,
                step.damageRatio, step.alpha);

            if (!hit)
                Debug.LogWarning($"[PassiveTriggerModule] 幻影未命中 (type={attackType} col={targetColumn} ratio={step.damageRatio})");
        }

        OnPassiveTriggered?.Invoke(def.upgradeId);
        Debug.Log($"[PassiveTriggerModule] 幻影触发: {def.displayName} steps={steps.Count} type={attackType} col={targetColumn}");
    }

    private IEnumerator ExecuteReturnWave(PassiveState state)
    {
        if (AttackSystem.Instance == null) yield break;

        var def = state.definition;
        int level = state.level;
        var waveCfg = (def.returnWaveLevels != null && level <= def.returnWaveLevels.Count)
            ? def.returnWaveLevels[level - 1]
            : new ReturnWaveLevelConfig { column = 2, rangeRows = 2, damageRatio = def.floatValue };

        int column = waveCfg.column > 0 ? waveCfg.column : 2;
        int rangeRows = waveCfg.rangeRows > 0 ? waveCfg.rangeRows : 2;
        float damageRatio = waveCfg.damageRatio > 0f ? waveCfg.damageRatio : def.floatValue;
        bool slashLeftToRight = column <= 2;

        bool hit = AttackSystem.Instance.ExecuteReturnWave(
            AttackType.Pierce, column, slashLeftToRight,
            damageRatio, rangeRows);

        OnPassiveTriggered?.Invoke(def.upgradeId);
        Debug.Log($"[PassiveTriggerModule] 折返波触发: {def.displayName} col={column} rows={rangeRows} ratio={damageRatio} hit={hit}");
    }

    private IEnumerator ExecuteChainBounce(PassiveState state)
    {
        if (AttackSystem.Instance == null) yield break;

        var def = state.definition;
        int level = state.level;
        var bounceCfg = (def.chainBounceLevels != null && level <= def.chainBounceLevels.Count)
            ? def.chainBounceLevels[level - 1]
            : new ChainBounceLevelConfig { column = 2, maxBounces = 3, damageRatio = def.floatValue };

        int column = bounceCfg.column > 0 ? bounceCfg.column : 2;
        int maxBounces = bounceCfg.maxBounces > 0 ? bounceCfg.maxBounces : 3;
        float damageRatio = bounceCfg.damageRatio > 0f ? bounceCfg.damageRatio : def.floatValue;

        bool hit = AttackSystem.Instance.ExecuteChainBounce(
            AttackType.Pierce, column, damageRatio, maxBounces);

        OnPassiveTriggered?.Invoke(def.upgradeId);
        Debug.Log($"[PassiveTriggerModule] 连锁弹射触发: {def.displayName} col={column} maxBounces={maxBounces} ratio={damageRatio} hit={hit}");
    }

    private void ExecuteFire(PassiveState state)
    {
        var def = state.definition;
        if (def.timedAoeLevels == null || state.level > def.timedAoeLevels.Count) return;
        var cfg = def.timedAoeLevels[state.level - 1];
        if (cfg.columns == null || cfg.columns.Count == 0 || fireEffectPrefab == null) return;

        var instance = Instantiate(fireEffectPrefab);
        instance.GetComponent<ShootFireEffect>().Play(cfg.columns, cfg.damage);

        OnPassiveTriggered?.Invoke(def.upgradeId);
        Debug.Log($"[PassiveTriggerModule] 喷火触发: {def.displayName} damage={cfg.damage} cols=[{string.Join(",", cfg.columns)}]");
    }

    private void ExecuteArrow(PassiveState state)
    {
        var def = state.definition;
        if (def.timedArrowLevels == null || state.level > def.timedArrowLevels.Count) return;
        var cfg = def.timedArrowLevels[state.level - 1];
        if (arrowEffectPrefab == null) return;

        var instance = Instantiate(arrowEffectPrefab);
        instance.GetComponent<TimedArrowEffect>().Play(cfg.rowCount, cfg.arrowCount, cfg.damage);

        OnPassiveTriggered?.Invoke(def.upgradeId);
        Debug.Log($"[PassiveTriggerModule] 箭雨触发: {def.displayName} rows={cfg.rowCount} arrows={cfg.arrowCount} damage={cfg.damage}");
    }

    private IEnumerator ExecuteArrowVolley(PassiveState state)
    {
        var def = state.definition;
        if (def.arrowVolleyLevels == null || state.level > def.arrowVolleyLevels.Count) yield break;
        var cfg = def.arrowVolleyLevels[state.level - 1];
        if (_arrowVolleyTemplate == null)
        {
            Debug.LogWarning("[PassiveTriggerModule] _arrowVolleyTemplate 未配置");
            yield break;
        }

        var cm = AttackSystem.Instance?.columnManager;
        if (cm == null) yield break;

        // 收集所有存活敌人，按 row 升序分组
        var allEnemies = cm.GetAllEnemies();
        allEnemies.RemoveAll(e => e == null || e.state == EnemyState.Dead);
        if (allEnemies.Count == 0) yield break;

        allEnemies.Sort((a, b) => a.rowIndex.CompareTo(b.rowIndex));

        var selected = new List<Enemy>();

        // Stab 攻击时优先锁定 Stab 目标
        if (_currentAttackType == AttackType.Stab)
        {
            var stabTarget = AttackSystem.Instance.LastStabTargetEnemy;
            if (stabTarget != null && stabTarget.state != EnemyState.Dead)
            {
                selected.Add(stabTarget);
                allEnemies.Remove(stabTarget);
            }
        }

        // 从最近排开始补齐剩余目标
        int needed = cfg.targetCount - selected.Count;
        if (needed > 0)
        {
            // 按 row 分组，逐排随机选取
            var byRow = new Dictionary<int, List<Enemy>>();
            foreach (var e in allEnemies)
            {
                int row = e.rowIndex;
                if (!byRow.ContainsKey(row))
                    byRow[row] = new List<Enemy>();
                byRow[row].Add(e);
            }

            var sortedRows = new List<int>(byRow.Keys);
            sortedRows.Sort();

            foreach (int row in sortedRows)
            {
                var candidates = byRow[row];
                // 随机打乱
                for (int i = candidates.Count - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    var tmp = candidates[i];
                    candidates[i] = candidates[j];
                    candidates[j] = tmp;
                }

                int take = Mathf.Min(needed, candidates.Count);
                selected.AddRange(candidates.GetRange(0, take));
                needed -= take;
                if (needed <= 0) break;
            }
        }

        if (selected.Count == 0) yield break;

        // 计算最终伤害
        float mult = UpgradeEffectManager.Instance != null ? UpgradeEffectManager.Instance.GetDamageMultiplier() : 1f;
        int finalDamage = Mathf.RoundToInt(cfg.baseDamage * mult);

        // 获取玩家位置
        Vector3 playerPos = AttackSystem.Instance.playerState != null
            ? AttackSystem.Instance.playerState.transform.position
            : AttackSystem.Instance.transform.position;

        // 在 spawnWindow 内均匀发射所有箭矢
        int totalArrows = selected.Count * cfg.arrowCount;
        float spawnWindow = 0.5f;
        float interval = totalArrows > 1 ? spawnWindow / (totalArrows - 1) : 0f;

        int arrowIndex = 0;
        foreach (var enemy in selected)
        {
            for (int i = 0; i < cfg.arrowCount; i++)
            {
                float delay = interval * arrowIndex;
                arrowIndex++;
                StartCoroutine(FireArrow(enemy, playerPos, finalDamage, delay));
            }
        }

        OnPassiveTriggered?.Invoke(def.upgradeId);
        Debug.Log($"[PassiveTriggerModule] 箭矢齐射: {def.displayName} targets={selected.Count} arrows={cfg.arrowCount} dmg={finalDamage}");
    }

    private IEnumerator FireArrow(Enemy target, Vector3 playerPos, int damage, float delay)
    {
        // 提前捕获目标网格位置
        Vector3 targetPos;
        if (target != null && target.state != EnemyState.Dead)
            targetPos = target.transform.position;
        else
            targetPos = playerPos + Vector3.forward * 5f;

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (_arrowVolleyTemplate == null) yield break;

        var arrow = Instantiate(_arrowVolleyTemplate, transform);
        arrow.gameObject.SetActive(true);
        var sr = arrow.GetComponent<SpriteRenderer>();
        if (sr == null) { Destroy(arrow.gameObject); yield break; }
        sr.color = Color.white;

        // 出发位置：玩家前方偏上
        Vector3 startPos = new Vector3(
            playerPos.x + Random.Range(-0.15f, 0.15f),
            playerPos.y + 0.8f,
            arrowVolleySpawnZ
        );
        arrow.transform.position = startPos;

        // 单射线朝向：箭矢沿 startPos → targetPos 直线飞行
        Vector3 direction = (targetPos - startPos).normalized;
        Quaternion axisCorrection = Quaternion.Euler(-90f, 0f, 0f);
        arrow.transform.rotation = Quaternion.LookRotation(direction, Vector3.up) * axisCorrection;

        float flyDuration = 0.22f;
        bool completed = false;
        bool hasHit = false;

        var seq = DOTween.Sequence().SetUpdate(UpdateType.Normal, false);
        seq.Append(arrow.transform.DOMove(targetPos, flyDuration).SetEase(Ease.Linear));

        // 飞行途中检测命中
        seq.Join(DOTween.To(
            () => 0f,
            v =>
            {
                if (!hasHit && arrow != null && target != null && target.state != EnemyState.Dead)
                {
                    float dist = Vector3.Distance(arrow.transform.position, targetPos);
                    if (dist < 0.4f)
                    {
                        hasHit = true;
                        target.TakeDamage(damage, DamageType.Pierce);
                    }
                }
            },
            1f, flyDuration).SetEase(Ease.Linear));

        // 淡出
        if (sr != null)
            seq.Append(sr.DOFade(0f, 0.08f));

        seq.OnComplete(() =>
        {
            completed = true;
            seq = null;
            if (arrow != null) Destroy(arrow.gameObject);
        });

        seq.OnKill(() =>
        {
            if (!completed)
            {
                seq = null;
                if (arrow != null) Destroy(arrow.gameObject);
            }
        });
    }
}
