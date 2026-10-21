using UnityEngine;

/// <summary>
/// 敌人的**疾病状态**（持续伤害 DoT + 泡泡表现 + 死亡扩散）。
///
/// 迁移说明（2026-10-22，批次 C）：疾病原本由玩家侧的 <c>UpgradeEffectManager</c>
/// 用一个 <c>Dictionary&lt;Enemy, DiseaseState&gt;</c> 持有并结算——状态挂在**别人**身上，
/// 泡泡表现也由管理器按敌人建字典管理。现在疾病是**敌人自己的状态**：
/// 挂在 <c>Enemy.StatusHost</c> 上，由敌人自己每帧结算，泡泡由状态自己持有，
/// 死亡扩散由状态自己订阅死亡事件处理。
///
/// 数值口径与迁移前完全一致：
///   • 总伤害按层数摊到每层，每层再按固定间隔（<see cref="TickInterval"/>）平摊；
///   • 再次施加：层数按「叠加 / 取大」规则合并，单跳伤害取较大值，持续时间取较大值，
///     剩余跳数重置；
///   • 敌人死亡时：停掉泡泡，并按「智能扩散 / 左右随机」规则把疾病传给另一个敌人；
///   • BOSS 不扩散；BOSS 在非应战 / QTE 攻击 / 阶段切换期间暂停结算。
/// </summary>
public sealed class EnemyStatusDisease : IStatusEffect
{
    /// <summary>状态 id（与 <see cref="EnemyStatusRegistry.Disease"/> 一致）。</summary>
    public const string Id = EnemyStatusRegistry.Disease;

    /// <summary>跳字间隔（秒）。</summary>
    public const float TickInterval = 0.5f;

    private readonly Enemy _owner;
    private int _totalDamagePerLayer;
    private int _durationSeconds;
    private int _damagePerTick;
    private int _layers;
    private int _totalTicks;
    private int _ticksRemaining;
    private float _tickTimer;
    private bool _smartSpread;
    private bool _cleared;
    private DiseaseBubbleEffect _visual;

    private EnemyStatusDisease(Enemy owner)
    {
        _owner = owner;
        owner.StatusHost?.Add(this);
        owner.OnDying += HandleOwnerDying;
    }

    // ── IStatusEffect ──

    /// <summary>状态 id。</summary>
    public string StatusId => Id;

    /// <summary>是否处于疾病中（敌人已死 / 状态已结束都不算）。</summary>
    public bool IsActive => !_cleared && _owner != null && _owner.state != EnemyState.Dead && _ticksRemaining > 0;

    /// <summary>进度（0~1，血条用）。</summary>
    public float GetProgress()
    {
        if (!IsActive) return 0f;
        return Mathf.Clamp01((_ticksRemaining - 1 + Mathf.Clamp01(_tickTimer / TickInterval)) / Mathf.Max(1f, _totalTicks));
    }

    /// <summary>层数（疾病条右上角的数字）。</summary>
    public int GetStacks() => IsActive ? _layers : 0;

    /// <summary>显示优先级（高于灼烧 10，低于冻结 30）。</summary>
    public int GetPriority() => 20;

    /// <summary>每跳结算：到点按「单跳伤害 × 层数」掉血。</summary>
    public void Tick(float deltaTime)
    {
        if (_cleared) return;
        if (_owner == null || _owner.state == EnemyState.Dead) { Clear(); return; }
        if (!EnemyStatusDoTRules.IsTickingAllowed(_owner)) return;

        _tickTimer -= deltaTime;
        if (_tickTimer <= 0f)
        {
            Debug.Log($"[RouteDiag] DiseaseTick frame={Time.frameCount} enemy={_owner.DebugTag} routeCombat=" + EnemyStatusDoTRules.DescribeRouteCombat());
            int tickDmg = _damagePerTick * _layers;
            float hpBefore = _owner.currentHealth;
            Debug.Log($"[Disease] Tick: {_owner.DebugTag} dmg={tickDmg} hpBefore={hpBefore:F1} remaining={_ticksRemaining} layers={_layers}");
            _owner.TakeDamage(tickDmg, DamageType.Pierce, new Color(0.72f, 0.28f, 0.9f),
                countsForCombo: false, canInterruptAttack: false, triggerHitAnimation: false, ignoreDamageModifiers: true,
                feedbackSource: HitFeedbackSource.Dot, feedbackStrength: HitFeedbackStrength.None);
            _ticksRemaining--;
            _tickTimer += TickInterval;
        }

        if (_owner.state == EnemyState.Dead || _ticksRemaining <= 0)
        {
            StopVisual();
            Clear();
        }
    }

    /// <summary>清空（敌人回池 / 状态结束时）：停泡泡、退订死亡事件、从宿主摘除。</summary>
    public void Clear()
    {
        if (_cleared) return;
        _cleared = true;
        _ticksRemaining = 0;
        StopVisual();
        if (_owner != null)
        {
            _owner.OnDying -= HandleOwnerDying;
            _owner.StatusHost.Remove(this);
        }
    }

    // ── 死亡扩散 ──

    /// <summary>
    /// 敌人死亡：停掉泡泡，把疾病传给另一个敌人（BOSS 不扩散）。
    ///
    /// 这是**敌人自己对自己死亡的反应**，所以挂在状态上，而不是塞回玩家侧。
    /// </summary>
    private void HandleOwnerDying(Enemy enemy)
    {
        if (_cleared) return;
        if (_owner == null)
        {
            Debug.Log("[Disease] HandleOwnerDying SKIP: owner 已丢失");
            return;
        }

        int column = _owner.columnIndex;
        int row = _owner.rowIndex;
        int totalDamagePerLayer = _totalDamagePerLayer;
        int durationSeconds = _durationSeconds;
        int layers = _layers;
        bool smartSpread = _smartSpread;

        Debug.Log($"[Disease] HandleOwnerDying: {_owner.DebugTag} totalDmgPerLayer={totalDamagePerLayer} duration={durationSeconds} layers={layers} dmgPerTick={_damagePerTick} ticksRemaining={_ticksRemaining} smartSpread={smartSpread}");

        StopVisual();
        Clear(); // 退订 + 从宿主摘除（扩散要在状态摘除之后做）

        if (_owner.isBoss) return;
        var columnManager = EnemyStatusDoTRules.ResolveColumnManager();
        if (columnManager == null) return;

        if (smartSpread)
        {
            Enemy target = SelectSpreadTarget(columnManager, column, row);
            if (target != null)
                ApplyTo(target, totalDamagePerLayer, durationSeconds, layers, addToExistingLayers: false, smartSpread: true);
        }
        else
        {
            // 非智能传播：左右有则随机选一，否则选后
            var left = columnManager.GetEnemyAt(column - 1, row);
            var right = columnManager.GetEnemyAt(column + 1, row);
            bool leftValid = left != null && !left.isBoss && left.state != EnemyState.Dead;
            bool rightValid = right != null && !right.isBoss && right.state != EnemyState.Dead;

            if (leftValid && rightValid)
                SpreadToIfValid(Random.value < 0.5f ? left : right, totalDamagePerLayer, durationSeconds, layers, smartSpread);
            else if (leftValid)
                SpreadToIfValid(left, totalDamagePerLayer, durationSeconds, layers, smartSpread);
            else if (rightValid)
                SpreadToIfValid(right, totalDamagePerLayer, durationSeconds, layers, smartSpread);
            else
                SpreadToIfValid(columnManager.GetEnemyAt(column, row + 1), totalDamagePerLayer, durationSeconds, layers, smartSpread);
        }
    }

    private static void SpreadToIfValid(Enemy target, int totalDamagePerLayer, int durationSeconds, int layers, bool smartSpread)
    {
        if (target != null && !target.isBoss && target.state != EnemyState.Dead)
            ApplyTo(target, totalDamagePerLayer, durationSeconds, layers, addToExistingLayers: false, smartSpread: smartSpread);
    }

    /// <summary>智能扩散目标：优先同行（左右），其次其他排；同行内选最近，其他排内选最近。</summary>
    private static Enemy SelectSpreadTarget(ColumnManager columnManager, int column, int row)
    {
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

    // ── 泡泡表现 ──

    private void EnsureVisual()
    {
        if (_owner == null) return;
        if (_visual == null)
        {
            // 复用敌人身上已有的泡泡对象（状态被清空后重建时不重复创建）。
            var existing = _owner.transform.Find("DiseaseBubbleEffect");
            if (existing != null) _visual = existing.GetComponent<DiseaseBubbleEffect>();
        }
        if (_visual != null)
        {
            _visual.StartEmission();
            return;
        }

        var visualObject = new GameObject("DiseaseBubbleEffect");
        visualObject.transform.SetParent(_owner.transform, false);
        _visual = visualObject.AddComponent<DiseaseBubbleEffect>();
        _visual.StartEmission();
    }

    private void StopVisual()
    {
        if (_visual == null) return;
        // 只停发射，不销毁：泡泡自然淡出；下次施加时复用同一个对象（与迁移前一致）。
        _visual.StopEmission();
        _visual = null;
    }

    // ── 对外接口 ──

    /// <summary>
    /// 对敌人施加疾病：<paramref name="totalDamage"/> 是**每层在持续时间内的总伤害**，
    /// 按层数叠加；重复施加时按 <paramref name="addToExistingLayers"/> 决定层数是叠加还是取大。
    /// </summary>
    public static void ApplyTo(Enemy enemy, int totalDamage, int durationSeconds, int layers = 1,
        bool addToExistingLayers = true, bool smartSpread = false)
    {
        int tickCount = Mathf.Max(1, Mathf.CeilToInt(durationSeconds / TickInterval));
        int damagePerTick = Mathf.Max(1, totalDamage / tickCount);
        if (enemy == null || enemy.state == EnemyState.Dead || durationSeconds <= 0 || layers <= 0) return;

        var status = enemy.StatusHost.Get(Id) as EnemyStatusDisease;
        bool isNew = status == null || status._cleared;
        if (isNew) status = new EnemyStatusDisease(enemy);

        int oldLayers = status._layers;
        int oldTotalDmg = status._totalDamagePerLayer;
        status._layers = addToExistingLayers
            ? status._layers + layers
            : Mathf.Max(status._layers, layers);
        status._totalDamagePerLayer = Mathf.Max(status._totalDamagePerLayer, totalDamage);
        status._durationSeconds = Mathf.Max(status._durationSeconds, durationSeconds);
        status._damagePerTick = Mathf.Max(status._damagePerTick, damagePerTick);
        status._totalTicks = tickCount;
        status._ticksRemaining = tickCount;
        status._tickTimer = TickInterval;
        status._smartSpread = status._smartSpread || smartSpread;
        status.EnsureVisual();

        if (isNew)
            Debug.Log($"[Disease] ApplyDisease NEW: {enemy.DebugTag} totalDmg={totalDamage} ticks={tickCount} dmgPerTick={damagePerTick} layers={layers} smartSpread={smartSpread}");
        else
            Debug.Log($"[Disease] ApplyDisease UPDATE: {enemy.DebugTag} oldLayers={oldLayers}→{status._layers} oldTotalDmg={oldTotalDmg}→{status._totalDamagePerLayer} dmgPerTick={status._damagePerTick} addToExisting={addToExistingLayers}");
    }
}
