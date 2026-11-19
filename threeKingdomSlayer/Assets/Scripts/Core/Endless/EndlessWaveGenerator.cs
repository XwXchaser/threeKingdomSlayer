using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 无限模式的**波次原型**（编队语法）。生成器按原型生成行，不手写编队。
/// 见 `design/endless-mode-design.md` §3.2 第 2 层。
/// </summary>
public enum EndlessFormationKind
{
    PressureLine,  // 压迫排：成排杂兵推进
    GuardedRanged, // 护卫 + 远程：近战在前、远程在后 50%
    SideFlank,     // 侧翼包夹：两侧近战，中间留空（给玩家走位空间）
    ChargeLine,    // 冲阵：骑兵单独突前
    ShieldWall,    // 盾阵：盾卫成排 + 后排杂兵
    EliteSingle,   // 精英：单只高威胁单位收尾
    BossGuard      // BOSS 护卫 + 节奏门 + BOSS 最后一排
}

/// <summary>
/// **无限模式波次生成器**（2026-11-16 新增）。
///
/// ## 它解决什么
/// 以前关卡是手写编队（`enemyIds` 逐排填），问题：
///   · 难度被当成"HP 倍率"→ 前磨后秒；
///   · 波次之间没有威胁结构，只有点状散兵；
///   · 教学顺序与实际配置不符（机器人没有 C2 就卡死在 1012）。
/// ⇒ 本生成器把难度换成**威胁结构**：威胁配对表（教学契约） + 编队语法 + 威胁点记账。
///
/// ## 纯函数
/// <c>Generate(cfg)</c> 不依赖全局状态 ⇒ 同一 seed 两次生成完全一致。
///
/// ## 硬规则（自检内置，违反即报错）
///   · 规则九：Boss 必须在该波 <c>rows</c> 的**最后一个** RowConfig；
///   · 规则十：远程（105/106/1051/1014）必须落在**后 50%** 的排；
///   · 每排 <c>enemyIds</c> 长度 ≤ 5，下标 = 列号（0 = 空槽）；
///   · 节奏门（999）不能是最后一排；
///   · **清场时间护栏**：估算清场时长必须落在 [minClearSeconds, maxClearSeconds]
///     （防止玩家堆数值后 30 秒秒杀 → 关卡内容被架空）。
/// </summary>
public sealed class EndlessWaveGenerator
{
    /// <summary>生成参数（数值全是占位值，等实机标定）。</summary>
    public struct Config
    {
        /// <summary>随机种子（同 seed 同关卡）。</summary>
        public int seed;
        /// <summary>圈数（1 起）。</summary>
        public int circle;
        /// <summary>每圈波数。</summary>
        public int wavesPerCircle;
        /// <summary>每 N 圈一个 BOSS（1 = 每圈都有）。</summary>
        public int bossEveryNCircle;
        /// <summary>第 1 圈每波的威胁预算（威胁点）。</summary>
        public float baseThreatPerWave;
        /// <summary>每圈威胁预算增长（线性）。</summary>
        public float threatGrowthPerCircle;
        /// <summary>每波排数区间。</summary>
        public int rowsMin;
        public int rowsMax;
        /// <summary>参考 DPS（用于清场时间估算）。</summary>
        public float referenceDps;
        /// <summary>威胁点 → 估算血量的换算系数。</summary>
        public float threatToHp;
        /// <summary>清场时间护栏下限（秒）。</summary>
        public float minClearSeconds;
        /// <summary>清场时间护栏上限（秒）。</summary>
        public float maxClearSeconds;
        /// <summary>是否允许远程（教学阶段关掉）。</summary>
        public bool allowRanged;
        /// <summary>是否允许需要重攻击的威胁。</summary>
        public bool allowHeavy;

        public static Config Default(int seed, int circle)
        {
            return new Config
            {
                seed = seed,
                circle = circle,
                wavesPerCircle = 3,
                bossEveryNCircle = 1,
                // 预算量级：威胁点 1.0~2.8/只 ⇒ 预算 9 ≈ 5~8 只/波（对齐现有关卡的每波只数）
                baseThreatPerWave = 9f,
                threatGrowthPerCircle = 3.5f,
                rowsMin = 8,
                rowsMax = 11,
                referenceDps = 45f,
                threatToHp = 100f,
                minClearSeconds = 8f,
                maxClearSeconds = 55f,
                allowRanged = circle >= 2,
                allowHeavy = circle >= 2
            };
        }
    }

    /// <summary>生成一圈的全部波次。</summary>
    public List<WaveConfig> Generate(Config cfg)
    {
        var result = new List<WaveConfig>();
        if (cfg.wavesPerCircle <= 0) cfg.wavesPerCircle = 1;

        for (int w = 0; w < cfg.wavesPerCircle; w++)
        {
            bool isBossWave = (w == cfg.wavesPerCircle - 1) && (cfg.bossEveryNCircle <= 1 || cfg.circle % cfg.bossEveryNCircle == 0);
            result.Add(GenerateWave(cfg, w, w, cfg.wavesPerCircle, isBossWave));
        }
        return result;
    }

    /// <summary>
    /// 生成**单个**波次（无限模式按节点拼波次时用）。
    ///
    /// | 参数 | 含义 |
    /// |---|---|
    /// | <c>waveSeedIndex</c> | 随机种子索引（决定这一波的编队随机） |
    /// | <c>difficultyIndex</c> | 本波在整圈里的序号（决定威胁预算的递增档位，0 → 1 线性） |
    /// | <c>waveCountInCircle</c> | 整圈波数（预算递增的归一化基准） |
    /// | <c>isBossWave</c> | 是否 BOSS 波（护卫排 + 节奏门 + BOSS 最后一排） |
    /// </summary>
    public WaveConfig GenerateWave(Config cfg, int waveSeedIndex, int difficultyIndex, int waveCountInCircle, bool isBossWave)
    {
        var rng = new EndlessRandom(cfg.seed * 7919 + cfg.circle * 131 + waveSeedIndex);
        int rowCount = rng.Range(cfg.rowsMin, cfg.rowsMax + 1);
        var wave = new WaveConfig { waveId = waveSeedIndex + 1, rows = new List<RowConfig>() };

        EndlessFormationKind kind = PickFormation(rng, cfg, difficultyIndex, waveCountInCircle, isBossWave);
        float budget = ComputeBudget(cfg, difficultyIndex, waveCountInCircle, isBossWave);
        float spent = 0f;

        // BOSS 波：护卫段 → 节奏门 → BOSS 最后一排
        if (kind == EndlessFormationKind.BossGuard)
        {
            int guardRows = Mathf.Clamp(rowCount - 3, 2, 4);
            float guardBudget = budget * 0.75f;
            for (int r = 0; r < guardRows; r++)
                wave.rows.Add(BuildGuardRow(rng, cfg, guardRows, r, ref spent, guardBudget));

            // 节奏门：把"前段小怪"与"后段大阵"分开
            wave.rows.Add(MakeRow(RowConfig.RhythmGateMarker, 0));
            // BOSS 最后一排（规则九）
            wave.rows.Add(MakeRow(EndlessEnemyThreat.BossForCircle(cfg.circle).enemyId, 2));
            EnsureFrontRowOccupied(cfg, wave);
            ApplyClearTimeGuardrail(cfg, wave);
            return wave;
        }

        for (int r = 0; r < rowCount; r++)
            wave.rows.Add(BuildRow(rng, cfg, kind, rowCount, r, ref spent, budget));

        EnsureFrontRowOccupied(cfg, wave);
        TrimTrailingEmptyRows(wave);
        ApplyClearTimeGuardrail(cfg, wave);
        return wave;
    }

    /// <summary>
    /// **硬规则：波次的第 0 排必须有敌人。**
    ///
    /// 为什么（2026-11-16 实测踩到）：
    ///   · 敌人先在「逻辑目标排后方两排」生成，再靠**行前移**进场；
    ///   · 行前移的触发条件是「**某排被打空**」——空排占位符不会自己往前挪；
    ///   · 于是若某波第 0 排是空的，敌人们会永远停在 1 排，
    ///     而玩家的攻击只够得到 0 排 ⇒ **双方互相够不到 = 永久卡死**（实测卡在 row 1/5/9）。
    ///   · 现有关卡资产每一波的第 0 排都有敌人（N1/E0/N3 全部如此）——这是隐含约定。
    ///
    /// ⇒ 本方法保证第 0 排至少一只；并在 <see cref="ValidateWave"/> 里作为校验项。
    /// </summary>
    private void EnsureFrontRowOccupied(Config cfg, WaveConfig wave)
    {
        if (wave == null || wave.rows == null || wave.rows.Count == 0) return;
        var front = wave.rows[0];
        if (front == null || front.enemyIds == null) return;
        for (int c = 0; c < front.enemyIds.Length; c++)
        {
            int id = front.enemyIds[c];
            if (id > 0 && id != RowConfig.RhythmGateMarker) return;   // 已经有敌人
        }

        // 第 0 排是空的 → 补一只当前圈最便宜的敌种
        var pool = EndlessEnemyThreat.PoolForCircle(cfg.circle, false, cfg.allowHeavy);
        if (pool.Count == 0) return;
        var cheapest = pool[0];
        for (int i = 1; i < pool.Count; i++) if (pool[i].threat < cheapest.threat) cheapest = pool[i];
        front.enemyIds[2] = cheapest.enemyId;
    }

    private EndlessFormationKind PickFormation(EndlessRandom rng, Config cfg, int waveIndexInCircle, int waveCountInCircle, bool isBossWave)
    {
        if (isBossWave) return EndlessFormationKind.BossGuard;

        int waves = Mathf.Max(1, waveCountInCircle);
        if (waveIndexInCircle == 0)
        {
            // 开场波：只用压迫排（教学节奏，第一波永远简单）
            return EndlessFormationKind.PressureLine;
        }
        if (waveIndexInCircle == waves - 1)
        {
            // 收尾波：精英或压迫
            return rng.Chance(0.5f) ? EndlessFormationKind.EliteSingle : EndlessFormationKind.PressureLine;
        }

        // 中间波：按当前圈可用的威胁种类挑
        var kinds = new List<EndlessFormationKind>();
        kinds.Add(EndlessFormationKind.PressureLine);
        kinds.Add(EndlessFormationKind.SideFlank);
        if (cfg.allowRanged) kinds.Add(EndlessFormationKind.GuardedRanged);
        if (cfg.allowHeavy)
        {
            kinds.Add(EndlessFormationKind.ChargeLine);
            kinds.Add(EndlessFormationKind.ShieldWall);
        }
        return kinds[rng.Range(0, kinds.Count)];
    }

    private float ComputeBudget(Config cfg, int waveIndexInCircle, int waveCountInCircle, bool isBossWave)
    {
        float baseBudget = cfg.baseThreatPerWave + cfg.threatGrowthPerCircle * (cfg.circle - 1);
        int waves = Mathf.Max(1, waveCountInCircle);
        float waveFactor;
        if (waves == 1) waveFactor = 1f;
        else waveFactor = waveIndexInCircle / (float)(waves - 1);   // 0 → 1 线性递增
        waveFactor = 0.8f + 0.45f * waveFactor;                    // 0.8 → 1.25
        if (isBossWave) waveFactor *= 0.9f;                        // BOSS 波护卫段略收
        return baseBudget * waveFactor;
    }

    /// <summary>
    /// 按原型生成一排。
    /// **逐排摊分预算**（不是把预算从前往后吃光）⇒ 敌人铺满整波，不会前密后空。
    /// </summary>
    private RowConfig BuildRow(EndlessRandom rng, Config cfg, EndlessFormationKind kind,
                               int rowCount, int rowIndex, ref float spent, float budget)
    {
        int backHalfStart = rowCount / 2;
        bool backHalf = rowIndex >= backHalfStart;

        var row = new RowConfig { enemyIds = new int[5] };

        // 本排可以花到的上限：按排数均摊
        float rowAllowance = budget * ((rowIndex + 1) / (float)rowCount) - spent;
        rowAllowance = Mathf.Min(rowAllowance, budget - spent);
        if (rowAllowance <= 0f) return row;

        switch (kind)
        {
            case EndlessFormationKind.PressureLine:
            {
                var pool = EndlessEnemyThreat.PoolForCircle(cfg.circle, false, cfg.allowHeavy);
                if (pool.Count == 0) break;
                PlaceEnemies(rng, row, pool, rng.Range(2, 4), ref spent, budget, backHalf, false, rowAllowance);
                break;
            }
            case EndlessFormationKind.GuardedRanged:
            {
                // 近战在前，远程在后 50%（规则十）
                bool wantRanged = backHalf && cfg.allowRanged;
                var pool = EndlessEnemyThreat.PoolForCircle(cfg.circle, wantRanged, cfg.allowHeavy);
                if (pool.Count == 0) break;
                int count = wantRanged ? rng.Range(1, 3) : rng.Range(2, 4);
                PlaceEnemies(rng, row, pool, count, ref spent, budget, backHalf, wantRanged, rowAllowance);
                break;
            }
            case EndlessFormationKind.SideFlank:
            {
                var pool = EndlessEnemyThreat.PoolForCircle(cfg.circle, false, cfg.allowHeavy);
                if (pool.Count == 0) break;
                // 两侧近战 + 中间留空
                PlaceAtColumn(rng, row, pool, 0, ref spent, budget, false, 0, rowAllowance);
                PlaceAtColumn(rng, row, pool, 4, ref spent, budget, false, 0, rowAllowance);
                if (rng.Chance(0.4f)) PlaceAtColumn(rng, row, pool, 2, ref spent, budget, false, 0, rowAllowance);
                break;
            }
            case EndlessFormationKind.ChargeLine:
            {
                // 骑兵突前：放在最远排（给冲锋留出跑动空间），其余排是杂兵
                if (rowIndex == rowCount - 1 && cfg.allowHeavy)
                {
                    var cavalry = FindById(109);
                    if (cavalry.enemyId != 0 && spent + cavalry.threat <= budget)
                    {
                        row.enemyIds[2] = cavalry.enemyId;
                        spent += cavalry.threat;
                    }
                }
                else
                {
                    var pool = EndlessEnemyThreat.PoolForCircle(cfg.circle, false, cfg.allowHeavy);
                    if (pool.Count > 0)
                        PlaceEnemies(rng, row, pool, rng.Range(1, 3), ref spent, budget, backHalf, false, rowAllowance);
                }
                break;
            }
            case EndlessFormationKind.ShieldWall:
            {
                var pool = EndlessEnemyThreat.PoolForCircle(cfg.circle, false, cfg.allowHeavy);
                if (pool.Count == 0) break;
                var shield = FindById(1021);
                bool shieldAllowed = cfg.allowHeavy && shield.enemyId != 0;
                // 盾墙只占**前两排**（不是每排都排盾），其余排是杂兵
                if (shieldAllowed && rowIndex < 2)
                {
                    PlaceAtColumn(rng, row, pool, 1, ref spent, budget, false, shield.enemyId, rowAllowance);
                    PlaceAtColumn(rng, row, pool, 2, ref spent, budget, false, shield.enemyId, rowAllowance);
                }
                else
                {
                    PlaceEnemies(rng, row, pool, rng.Range(2, 4), ref spent, budget, backHalf, false, rowAllowance);
                }
                break;
            }
            case EndlessFormationKind.EliteSingle:
            {
                var pool = EndlessEnemyThreat.PoolForCircle(cfg.circle, backHalf, cfg.allowHeavy);
                if (pool.Count == 0) break;
                // 单只高威胁：取威胁最大的可用敌种
                var best = pool[0];
                for (int i = 1; i < pool.Count; i++) if (pool[i].threat > best.threat) best = pool[i];
                int col = rng.Range(0, 5);
                if (spent + best.threat <= budget)
                {
                    row.enemyIds[col] = best.enemyId;
                    spent += best.threat;
                }
                break;
            }
        }
        return row;
    }

    /// <summary>BOSS 波的护卫排（近战为主，不放远程，避免远程贴着 BOSS）。</summary>
    private RowConfig BuildGuardRow(EndlessRandom rng, Config cfg, int guardRows, int rowIndex,
                                    ref float spent, float guardBudget)
    {
        var row = new RowConfig { enemyIds = new int[5] };
        float rowAllowance = guardBudget * ((rowIndex + 1) / (float)guardRows) - spent;
        if (rowAllowance <= 0f) return row;

        var pool = EndlessEnemyThreat.PoolForCircle(cfg.circle, false, cfg.allowHeavy);
        if (pool.Count == 0) return row;

        // 第一排护卫多一点，后面递减（视觉上像"越靠近 BOSS 越少"）
        int count = rowIndex == 0 ? rng.Range(3, 5) : rng.Range(2, 4);
        PlaceEnemies(rng, row, pool, count, ref spent, guardBudget, false, false, rowAllowance);
        return row;
    }

    private void PlaceEnemies(EndlessRandom rng, RowConfig row, List<EndlessEnemyThreat.Entry> pool,
                              int count, ref float spent, float budget, bool backHalf, bool rangedOnly,
                              float rowAllowance)
    {
        var usedColumns = new List<int>();
        for (int i = 0; i < count; i++)
        {
            int col = -1;
            for (int tries = 0; tries < 8; tries++)
            {
                int c = rng.Range(0, 5);
                if (!usedColumns.Contains(c)) { col = c; break; }
            }
            if (col < 0) break;

            // 在"本排放得下"的候选里挑（避免预算被一只精英吃光）
            var affordable = new List<EndlessEnemyThreat.Entry>();
            for (int k = 0; k < pool.Count; k++)
            {
                var e = pool[k];
                if (rangedOnly && !e.ranged) continue;
                if (!rangedOnly && e.ranged && !backHalf) continue;   // 规则十：远程不进前半
                if (spent + e.threat > budget) continue;
                if (spent + e.threat > spent + rowAllowance && affordable.Count > 0) continue;
                affordable.Add(e);
            }
            if (affordable.Count == 0) continue;
            var entry = affordable[rng.Range(0, affordable.Count)];

            row.enemyIds[col] = entry.enemyId;
            usedColumns.Add(col);
            spent += entry.threat;
        }
    }

    private void PlaceAtColumn(EndlessRandom rng, RowConfig row, List<EndlessEnemyThreat.Entry> pool,
                               int column, ref float spent, float budget, bool backHalf,
                               int forcedEnemyId, float rowAllowance)
    {
        if (column < 0 || column > 4) return;
        var entry = forcedEnemyId != 0 ? FindById(forcedEnemyId) : pool[rng.Range(0, pool.Count)];
        if (entry.enemyId == 0) return;
        if (entry.ranged && !backHalf) return;
        if (spent + entry.threat > budget) return;
        if (spent + entry.threat > spent + rowAllowance) return;
        row.enemyIds[column] = entry.enemyId;
        spent += entry.threat;
    }

    private static EndlessEnemyThreat.Entry FindById(int enemyId)
    {
        for (int i = 0; i < EndlessEnemyThreat.All.Length; i++)
            if (EndlessEnemyThreat.All[i].enemyId == enemyId) return EndlessEnemyThreat.All[i];
        return new EndlessEnemyThreat.Entry();
    }

    private static RowConfig MakeRow(int enemyId, int column)
    {
        var row = new RowConfig { enemyIds = new int[5] };
        if (enemyId > 0 && column >= 0 && column < 5) row.enemyIds[column] = enemyId;
        return row;
    }

    /// <summary>去掉尾部整排空行（空排占位符只保留在波次中段，作为"队列间隔"）。</summary>
    private static void TrimTrailingEmptyRows(WaveConfig wave)
    {
        if (wave == null || wave.rows == null) return;
        while (wave.rows.Count > 1)
        {
            var last = wave.rows[wave.rows.Count - 1];
            if (last != null && !IsEmptyRow(last)) break;
            wave.rows.RemoveAt(wave.rows.Count - 1);
        }
    }

    private static bool IsEmptyRow(RowConfig row)
    {
        if (row == null || row.enemyIds == null) return true;
        for (int i = 0; i < row.enemyIds.Length; i++)
            if (row.enemyIds[i] != 0) return false;
        return true;
    }

    /// <summary>
    /// **清场时间护栏**：估算本波清场时长，落在 [min, max] 之外就增减敌人。
    /// 这是"玩家堆数值"时唯一还拦得住内容被架空的东西（PBZ 口径：难度不靠加血）。
    /// </summary>
    private void ApplyClearTimeGuardrail(Config cfg, WaveConfig wave)
    {
        if (cfg.referenceDps <= 0f || cfg.threatToHp <= 0f) return;

        float seconds = EstimateThreat(wave) * cfg.threatToHp / cfg.referenceDps;

        int guard = 0;
        while (seconds > cfg.maxClearSeconds && guard++ < 32)
        {
            if (!RemoveOneEnemy(wave)) break;
            seconds = EstimateThreat(wave) * cfg.threatToHp / cfg.referenceDps;
        }
        guard = 0;
        while (seconds < cfg.minClearSeconds && guard++ < 32)
        {
            if (!AddOneEnemy(cfg, wave)) break;
            seconds = EstimateThreat(wave) * cfg.threatToHp / cfg.referenceDps;
        }
    }

    private static float EstimateThreat(WaveConfig wave)
    {
        float total = 0f;
        if (wave == null || wave.rows == null) return 0f;
        for (int i = 0; i < wave.rows.Count; i++)
        {
            var row = wave.rows[i];
            if (row == null || row.enemyIds == null) continue;
            for (int c = 0; c < row.enemyIds.Length; c++)
            {
                int id = row.enemyIds[c];
                if (id <= 0 || id == RowConfig.RhythmGateMarker) continue;
                total += EndlessEnemyThreat.Get(id).threat;
            }
        }
        return total;
    }

    private static bool RemoveOneEnemy(WaveConfig wave)
    {
        // 从后往前找一个非 BOSS、非节奏门的怪移除（保留第 0 排：它是双方的接触面，拔掉就卡死）
        for (int r = wave.rows.Count - 1; r >= 0; r--)
        {
            if (r == 0 && wave.rows.Count > 1) continue;
            var row = wave.rows[r];
            if (row == null || row.enemyIds == null) continue;
            for (int c = row.enemyIds.Length - 1; c >= 0; c--)
            {
                int id = row.enemyIds[c];
                if (id <= 0 || id == RowConfig.RhythmGateMarker) continue;
                if (EndlessEnemyThreat.IsBoss(id)) continue;
                row.enemyIds[c] = 0;
                return true;
            }
        }
        return false;
    }

    private static bool AddOneEnemy(Config cfg, WaveConfig wave)
    {
        var pool = EndlessEnemyThreat.PoolForCircle(cfg.circle, false, cfg.allowHeavy);
        if (pool.Count == 0 || wave.rows == null) return false;
        int backHalfStart = wave.rows.Count / 2;
        for (int r = wave.rows.Count - 1; r >= 0; r--)
        {
            var row = wave.rows[r];
            if (row == null || row.enemyIds == null) continue;
            bool backHalf = r >= backHalfStart;
            for (int c = 0; c < row.enemyIds.Length; c++)
            {
                if (row.enemyIds[c] != 0) continue;
                var entry = pool[0];
                for (int i = 1; i < pool.Count; i++) if (pool[i].threat < entry.threat) entry = pool[i];
                if (entry.ranged && !backHalf) continue;
                row.enemyIds[c] = entry.enemyId;
                return true;
            }
        }
        return false;
    }

    // ───────────────────────────── 自检 ─────────────────────────────

    /// <summary>校验一个波次是否满足硬规则。返回 null = 合法。</summary>
    public static string ValidateWave(WaveConfig wave, int waveIndex = 0)
    {
        if (wave == null) return $"波次 {waveIndex} 为空";
        if (wave.rows == null || wave.rows.Count == 0) return $"波次 {waveIndex} 没有排";

        int backHalfStart = wave.rows.Count / 2;
        for (int r = 0; r < wave.rows.Count; r++)
        {
            var row = wave.rows[r];
            if (row == null) return $"波次 {waveIndex} 第 {r} 排为空";
            if (row.enemyIds == null) return $"波次 {waveIndex} 第 {r} 排没有 enemyIds";
            if (row.enemyIds.Length > 5) return $"波次 {waveIndex} 第 {r} 排长度 {row.enemyIds.Length} > 5";

            if (row.IsRhythmGate && r == wave.rows.Count - 1)
                return $"波次 {waveIndex} 的节奏门在最后一排";

            for (int c = 0; c < row.enemyIds.Length; c++)
            {
                int id = row.enemyIds[c];
                if (id <= 0) continue;
                if (id == RowConfig.RhythmGateMarker) continue;

                var entry = EndlessEnemyThreat.Get(id);
                if (entry.boss && r != wave.rows.Count - 1)
                    return $"波次 {waveIndex} 的 BOSS（{id}）不在最后一排（规则九）";
                if (entry.ranged && r < backHalfStart)
                    return $"波次 {waveIndex} 第 {r} 排出现远程（{id}），远程必须在后 50%（规则十）";
            }
        }

        bool anyEnemy = false;
        for (int r = 0; r < wave.rows.Count && !anyEnemy; r++)
        {
            var row = wave.rows[r];
            if (row == null || row.enemyIds == null) continue;
            for (int c = 0; c < row.enemyIds.Length; c++)
            {
                int id = row.enemyIds[c];
                if (id > 0 && id != RowConfig.RhythmGateMarker) { anyEnemy = true; break; }
            }
        }
        if (!anyEnemy) return $"波次 {waveIndex} 一只敌人都没有";

        // 规则十一：第 0 排必须有敌人（否则行前移不会触发，双方互相够不到 → 卡死）
        var front = wave.rows[0];
        if (front != null && front.enemyIds != null)
        {
            bool frontHasEnemy = false;
            for (int c = 0; c < front.enemyIds.Length; c++)
            {
                int id = front.enemyIds[c];
                if (id > 0 && id != RowConfig.RhythmGateMarker) { frontHasEnemy = true; break; }
            }
            if (!frontHasEnemy) return $"波次 {waveIndex} 的第 0 排没有敌人（行前移不会触发 → 卡死，规则十一）";
        }

        return null;
    }

    /// <summary>校验一整圈的所有波次，返回错误列表（空 = 全合法）。</summary>
    public static List<string> ValidateAll(List<WaveConfig> waves)
    {
        var errors = new List<string>();
        if (waves == null) return errors;
        for (int i = 0; i < waves.Count; i++)
        {
            string err = ValidateWave(waves[i], i);
            if (!string.IsNullOrEmpty(err)) errors.Add(err);
        }
        return errors;
    }

    /// <summary>统计一波的敌人数（调试 / 工具用）。</summary>
    public static int CountEnemies(WaveConfig wave)
    {
        int count = 0;
        if (wave == null || wave.rows == null) return 0;
        for (int r = 0; r < wave.rows.Count; r++)
        {
            var row = wave.rows[r];
            if (row == null || row.enemyIds == null) continue;
            for (int c = 0; c < row.enemyIds.Length; c++)
            {
                int id = row.enemyIds[c];
                if (id > 0 && id != RowConfig.RhythmGateMarker) count++;
            }
        }
        return count;
    }

    /// <summary>估算一波的清场秒数（与护栏同口径）。</summary>
    public static float EstimateClearSeconds(WaveConfig wave, float referenceDps, float threatToHp)
    {
        if (referenceDps <= 0f) return 0f;
        return EstimateThreat(wave) * threatToHp / referenceDps;
    }
}
