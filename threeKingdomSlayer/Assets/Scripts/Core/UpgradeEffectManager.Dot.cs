using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
    /// UpgradeEffectManager 的**持续伤害（DoT）子系统**：灼烧与疾病的施加、每跳结算、传播与状态查询。
    ///
    /// 按机制从 <c>UpgradeEffectManager.cs</c> 拆出（2026-10-22，partial 分文件）。
    /// partial class 与 UpgradeEffectManager.cs **同类型**，行为完全一致。
    /// 扩展点：新的 DoT 机制（新的状态类型）在这里新增独立状态机，不要塞进 ApplyUpgrade 的分派。
    /// </summary>

public partial class UpgradeEffectManager : MonoBehaviour, IStatModifierApplier
{
    public struct DotState
    {
        public int damagePerTick;
        public int totalTicks;
        public int ticksRemaining;
        public float tickTimer;
    }

    public struct DiseaseState
    {
        public int totalDamagePerLayer;
        public int durationSeconds;
        public int damagePerTick;
        public int layers;
        public int totalTicks;
        public int ticksRemaining;
        public float tickTimer;
        public bool smartSpread;
    }

    public struct DotStatus
    {
        public bool isBurning;
        public float burnProgress;
        public bool isDiseased;
        public float diseaseProgress;
        public int diseaseLayers;
    }
    private static bool IsDotPaused(Enemy enemy)
    {
        return enemy != null && enemy.isBoss &&
            (enemy.bossState != BossState.InCombat || enemy.state == EnemyState.QTEAttacking || enemy.isPhaseTransitioning);
    }

    public int GetBurnStateCountForDiagnostics()
    {
        return _burnStates.Count;
    }
    private void TickBurnDamage()
    {
        TickBurnStates();
        TickDiseaseStates();
    }

    private void TickBurnStates()
    {
        if (_burnStates.Count == 0) return;

        _burnUpdateBuffer.Clear();
        var keys = new Enemy[_burnStates.Count];
        _burnStates.Keys.CopyTo(keys, 0);
        for (int i = 0; i < keys.Length; i++)
        {
            var enemy = keys[i];
            if (enemy != null && _burnStates.TryGetValue(enemy, out var state))
                _burnUpdateBuffer.Add(new KeyValuePair<Enemy, DotState>(enemy, state));
        }

        _dotRemovalList.Clear();
        float dt = Time.deltaTime;
        for (int i = 0; i < _burnUpdateBuffer.Count; i++)
        {
            var enemy = _burnUpdateBuffer[i].Key;
            var state = _burnUpdateBuffer[i].Value;
            if (enemy == null || enemy.state == EnemyState.Dead)
            {
                _dotRemovalList.Add(enemy);
                continue;
            }

            if (IsDotPaused(enemy))
            {
                _burnStates[enemy] = state;
                continue;
            }

            state.tickTimer -= dt;
            if (state.tickTimer <= 0f)
            {
                Debug.Log($"[RouteDiag] BurnTick frame={Time.frameCount} enemy={enemy.DebugTag} routeCombat=" + (StageBattleState != null ? StageBattleState.IsRouteCombatActive.ToString() : "NULL"));
                enemy.TakeDamage(state.damagePerTick, DamageType.Pierce, Color.red, countsForCombo: false, canInterruptAttack: false, triggerHitAnimation: false, ignoreDamageModifiers: true, feedbackSource: HitFeedbackSource.Dot, feedbackStrength: HitFeedbackStrength.None);
                state.ticksRemaining--;
                state.tickTimer += BurnTickInterval;
            }

            if (enemy.state == EnemyState.Dead || state.ticksRemaining <= 0)
                _dotRemovalList.Add(enemy);
            else
                _burnStates[enemy] = state;
        }

        for (int i = 0; i < _dotRemovalList.Count; i++)
            _burnStates.Remove(_dotRemovalList[i]);
    }

    private void TickDiseaseStates()
    {
        if (_diseaseStates.Count == 0) return;

        _diseaseUpdateBuffer.Clear();
        var keys = new Enemy[_diseaseStates.Count];
        _diseaseStates.Keys.CopyTo(keys, 0);
        for (int i = 0; i < keys.Length; i++)
        {
            var enemy = keys[i];
            if (enemy != null && _diseaseStates.TryGetValue(enemy, out var state))
                _diseaseUpdateBuffer.Add(new KeyValuePair<Enemy, DiseaseState>(enemy, state));
        }

        _dotRemovalList.Clear();
        float dt = Time.deltaTime;
        for (int i = 0; i < _diseaseUpdateBuffer.Count; i++)
        {
            var enemy = _diseaseUpdateBuffer[i].Key;
            var state = _diseaseUpdateBuffer[i].Value;
            if (enemy == null || enemy.state == EnemyState.Dead)
            {
                _dotRemovalList.Add(enemy);
                continue;
            }

            if (IsDotPaused(enemy))
            {
                _diseaseStates[enemy] = state;
                continue;
            }

            state.tickTimer -= dt;
            if (state.tickTimer <= 0f)
            {
                Debug.Log($"[RouteDiag] DiseaseTick frame={Time.frameCount} enemy={enemy.DebugTag} routeCombat=" + (StageBattleState != null ? StageBattleState.IsRouteCombatActive.ToString() : "NULL"));
                int tickDmg = state.damagePerTick * state.layers;
                float hpBefore = enemy.currentHealth;
                Debug.Log($"[Disease] Tick: {enemy.DebugTag} dmg={tickDmg} hpBefore={hpBefore:F1} remaining={state.ticksRemaining} layers={state.layers}");
                enemy.TakeDamage(tickDmg, DamageType.Pierce, new Color(0.72f, 0.28f, 0.9f), countsForCombo: false, canInterruptAttack: false, triggerHitAnimation: false, ignoreDamageModifiers: true, feedbackSource: HitFeedbackSource.Dot, feedbackStrength: HitFeedbackStrength.None);
                state.ticksRemaining--;
                state.tickTimer += DiseaseTickInterval;
            }

            if (enemy.state == EnemyState.Dead || state.ticksRemaining <= 0)
            {
                _dotRemovalList.Add(enemy);
                StopDiseaseVisual(enemy);
            }
            else
                _diseaseStates[enemy] = state;
        }

        for (int i = 0; i < _dotRemovalList.Count; i++)
        {
            var enemy = _dotRemovalList[i];
            _diseaseStates.Remove(enemy);
            if (enemy != null)
                enemy.OnDying -= HandleDiseasedEnemyDeath;
        }
    }

    /// <summary>施加灼烧，总伤害在持续时间内按固定跳字间隔结算。</summary>
    public void ApplyBurn(Enemy enemy, int totalDamage, float burnDuration)
    {
        int tickCount = Mathf.Max(1, Mathf.CeilToInt(burnDuration / BurnTickInterval));
        int damagePerTick = totalDamage / tickCount;
        if (enemy == null || damagePerTick <= 0 || burnDuration <= 0f) return;

        if (_burnStates.TryGetValue(enemy, out var existing))
        {
            existing.totalTicks = tickCount;
            existing.ticksRemaining = tickCount;
            existing.damagePerTick = Mathf.Max(existing.damagePerTick, damagePerTick);
            existing.tickTimer = BurnTickInterval;
            _burnStates[enemy] = existing;
            return;
        }

        _burnStates[enemy] = new DotState
        {
            damagePerTick = damagePerTick,
            totalTicks = tickCount,
            ticksRemaining = tickCount,
            tickTimer = BurnTickInterval
        };
    }

    public void ApplyDisease(Enemy enemy, int totalDamage, int durationSeconds, int layers = 1, bool addToExistingLayers = true, bool smartSpread = false)
    {
        int tickCount = Mathf.Max(1, Mathf.CeilToInt(durationSeconds / DiseaseTickInterval));
        int damagePerTick = Mathf.Max(1, totalDamage / tickCount);
        if (enemy == null || enemy.state == EnemyState.Dead || durationSeconds <= 0 || layers <= 0) return;

        if (_diseaseStates.TryGetValue(enemy, out var existing))
        {
            int oldLayers = existing.layers;
            int oldTotalDmg = existing.totalDamagePerLayer;
            existing.layers = addToExistingLayers
                ? existing.layers + layers
                : Mathf.Max(existing.layers, layers);
            existing.totalDamagePerLayer = Mathf.Max(existing.totalDamagePerLayer, totalDamage);
            existing.durationSeconds = Mathf.Max(existing.durationSeconds, durationSeconds);
            existing.damagePerTick = Mathf.Max(existing.damagePerTick, damagePerTick);
            existing.totalTicks = tickCount;
            existing.ticksRemaining = tickCount;
            existing.tickTimer = DiseaseTickInterval;
            existing.smartSpread = existing.smartSpread || smartSpread;
            _diseaseStates[enemy] = existing;
            EnsureDiseaseVisual(enemy);
            Debug.Log($"[Disease] ApplyDisease UPDATE: {enemy.DebugTag} oldLayers={oldLayers}→{existing.layers} oldTotalDmg={oldTotalDmg}→{existing.totalDamagePerLayer} dmgPerTick={existing.damagePerTick} addToExisting={addToExistingLayers}");
            return;
        }

        _diseaseStates[enemy] = new DiseaseState
        {
            totalDamagePerLayer = totalDamage,
            durationSeconds = durationSeconds,
            damagePerTick = damagePerTick,
            layers = layers,
            totalTicks = tickCount,
            ticksRemaining = tickCount,
            tickTimer = DiseaseTickInterval,
            smartSpread = smartSpread
        };
        EnsureDiseaseVisual(enemy);
        enemy.OnDying += HandleDiseasedEnemyDeath;
        Debug.Log($"[Disease] ApplyDisease NEW: {enemy.DebugTag} totalDmg={totalDamage} ticks={tickCount} dmgPerTick={damagePerTick} layers={layers} smartSpread={smartSpread}");
    }

    private void HandleDiseasedEnemyDeath(Enemy enemy)
    {
        if (enemy == null || !_diseaseStates.TryGetValue(enemy, out var state))
        {
            Debug.Log($"[Disease] HandleDiseasedEnemyDeath SKIP: enemy={enemy?.DebugTag} stateFound={enemy != null && _diseaseStates.ContainsKey(enemy)}");
            return;
        }

        Debug.Log($"[Disease] HandleDiseasedEnemyDeath: {enemy.DebugTag} totalDmgPerLayer={state.totalDamagePerLayer} duration={state.durationSeconds} layers={state.layers} dmgPerTick={state.damagePerTick} ticksRemaining={state.ticksRemaining} smartSpread={state.smartSpread}");

        int column = enemy.columnIndex;
        int row = enemy.rowIndex;
        StopDiseaseVisual(enemy);
        _diseaseStates.Remove(enemy);
        enemy.OnDying -= HandleDiseasedEnemyDeath;

        if (enemy.isBoss) return;

        var columnManager = AttackSystem.Instance?.columnManager;
        if (columnManager == null) return;

        if (state.smartSpread)
        {
            Enemy target = SelectDiseaseSpreadTarget(columnManager, column, row);
            if (target != null)
                ApplyDisease(target, state.totalDamagePerLayer, state.durationSeconds, state.layers, addToExistingLayers: false, smartSpread: true);
        }
        else
        {
            // 非智能传播：左右有则随机选一，否则选后
            var left = columnManager.GetEnemyAt(column - 1, row);
            var right = columnManager.GetEnemyAt(column + 1, row);
            bool leftValid = left != null && !left.isBoss && left.state != EnemyState.Dead;
            bool rightValid = right != null && !right.isBoss && right.state != EnemyState.Dead;

            if (leftValid && rightValid)
                SpreadToIfValid(Random.value < 0.5f ? left : right, state);
            else if (leftValid)
                SpreadToIfValid(left, state);
            else if (rightValid)
                SpreadToIfValid(right, state);
            else
                SpreadToIfValid(columnManager.GetEnemyAt(column, row + 1), state);
        }
    }

    private void SpreadToIfValid(Enemy target, DiseaseState state)
    {
        if (target != null && !target.isBoss && target.state != EnemyState.Dead)
            ApplyDisease(target, state.totalDamagePerLayer, state.durationSeconds, state.layers, addToExistingLayers: false, smartSpread: state.smartSpread);
    }

    private static Enemy SelectDiseaseSpreadTarget(ColumnManager columnManager, int column, int row)
    {
        // 优先同行（左右），其次后排；同行内选最近，后排内选最近
        Enemy closestSameRow = null;
        int closestSameRowDist = int.MaxValue;
        Enemy closestOtherRow = null;
        int closestOtherRowDist = int.MaxValue;
        var allEnemies = columnManager.GetAllEnemies();
        if (allEnemies == null) { DebugLog.Info("[DiseaseSpread] GetAllEnemies返回null"); return null; }

        DebugLog.Info($"[DiseaseSpread] 扫描{allEnemies.Count}个敌人，源位置(col={column},row={row})");
        for (int i = 0; i < allEnemies.Count; i++)
        {
            var e = allEnemies[i];
            if (e == null) continue;
            if (e.isBoss || e.state == EnemyState.Dead) continue;
            if (e.columnIndex == column && e.rowIndex == row) continue;
            int colDist = Mathf.Abs(e.columnIndex - column);
            int rowDist = Mathf.Abs(e.rowIndex - row);
            int dist = colDist + rowDist;
            bool sameRow = e.rowIndex == row;
            DebugLog.Info($"[DiseaseSpread] 候选: {e.DebugTag} col={e.columnIndex} row={e.rowIndex} dist={dist} sameRow={sameRow}");
            if (sameRow)
            {
                if (dist < closestSameRowDist) { closestSameRowDist = dist; closestSameRow = e; }
            }
            else
            {
                if (dist < closestOtherRowDist) { closestOtherRowDist = dist; closestOtherRow = e; }
            }
        }
        var result = closestSameRow ?? closestOtherRow;
        DebugLog.Info($"[DiseaseSpread] 结果: {(result != null ? result.DebugTag : "null")} sameRow={closestSameRow != null} pickedSameRowDist={closestSameRowDist} pickedOtherRowDist={closestOtherRowDist}");
        return result;
    }

    public DotStatus GetDotStatus(Enemy enemy)
    {
        if (enemy == null || enemy.state == EnemyState.Dead)
            return default;

        var status = new DotStatus();
        if (_burnStates.TryGetValue(enemy, out var burn))
        {
            status.isBurning = burn.ticksRemaining > 0;
            status.burnProgress = status.isBurning
                ? Mathf.Clamp01((burn.ticksRemaining - 1 + Mathf.Clamp01(burn.tickTimer / BurnTickInterval)) / Mathf.Max(1f, burn.totalTicks))
                : 0f;
        }

        if (_diseaseStates.TryGetValue(enemy, out var disease))
        {
            status.isDiseased = disease.ticksRemaining > 0;
            status.diseaseProgress = status.isDiseased
                ? Mathf.Clamp01((disease.ticksRemaining - 1 + Mathf.Clamp01(disease.tickTimer / DiseaseTickInterval)) / Mathf.Max(1f, disease.totalTicks))
                : 0f;
            status.diseaseLayers = status.isDiseased ? disease.layers : 0;
        }

        return status;
    }

    /// <summary>检查敌人是否处于灼烧状态</summary>
    public bool IsBurning(Enemy enemy)
    {
        return enemy != null && _burnStates.TryGetValue(enemy, out var state) && state.ticksRemaining > 0;
    }
}