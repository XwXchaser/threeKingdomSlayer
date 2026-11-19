using System.Collections.Generic;

/// <summary>
/// **确定性随机源**（xorshift32）—— 无限模式生成器专用。
///
/// 为什么不用 <c>UnityEngine.Random</c>：它是全局状态，不可复现。
/// 生成器必须是**纯函数** <c>Generate(seed, circle, …)</c> ⇒ 同一 seed 两次生成完全一致
/// （无尽模式没有种子就无法复现 bug、无法分享、无法做"今日一局"）。
/// </summary>
public struct EndlessRandom
{
    private uint _state;

    public EndlessRandom(int seed)
    {
        _state = seed == 0 ? 0x9E3779B9u : (uint)seed;
        // 预热几步，避免相邻 seed 的前几个输出过于接近
        for (int i = 0; i < 4; i++) NextUInt();
    }

    public uint NextUInt()
    {
        _state ^= _state << 13;
        _state ^= _state >> 17;
        _state ^= _state << 5;
        return _state;
    }

    /// <summary>
    /// [minInclusive, maxExclusive)
    ///
    /// ⚠ **必须用高位取模**：xorshift32 的**低位周期极短**（bit0 的周期是 2），
    /// 直接 <c>NextUInt() % span</c> 会让连续几次取到高度相关的数
    /// （实测症状：每一排的敌人都落在同样几列，整波偏向一侧）。
    /// </summary>
    public int Range(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive) return minInclusive;
        int span = maxExclusive - minInclusive;
        return minInclusive + (int)((NextUInt() >> 16) % (uint)span);
    }

    public float Value01() => (NextUInt() & 0xFFFFFF) / (float)0x1000000;

    public bool Chance(float probability) => Value01() < probability;
}

/// <summary>
/// **敌人威胁 / 分数表**（无限模式用）。
///
/// 设计口径（见 `design/endless-mode-design.md` §3.2 第 1 层）：
///   · 每个敌种有**基础威胁点**（难度记账的单位，不是血量）；
///   · `minCircle` = 最早可以出现的圈数 → **教学契约**：没到圈数不出场；
///   · `ranged` = 必须排在波次**后 50%**（规则十）；
///   · `needsHeavy` = 需要重攻击/招架才能解的威胁（1012 霸体 / 109 冲锋 / 1021 盾卫）；
///   · `scoreValue` = 击杀分基础值。
///
/// ⚠ 这是一张**规则表**（敌种 id 是项目常量，与 `BuffIds` 同类），不是运行时数据引用；
/// 若后续要做策划可调的表，应升级为 ScriptableObject 资产（保持 Inspector 可追踪）。
/// </summary>
public static class EndlessEnemyThreat
{
    public struct Entry
    {
        public int enemyId;
        public string displayName;
        /// <summary>基础威胁点（难度记账单位）。</summary>
        public float threat;
        /// <summary>最早出现的圈数（教学契约）。</summary>
        public int minCircle;
        /// <summary>是否远程（必须排在该波后 50%）。</summary>
        public bool ranged;
        /// <summary>是否需要重攻击 / 招架才能解。</summary>
        public bool needsHeavy;
        /// <summary>是否是 BOSS。</summary>
        public bool boss;
        /// <summary>击杀分基础值。</summary>
        public int score;
    }

    /// <summary>
    /// 全部在用敌种（敌种 id 与 `Assets/Resources/EnemyPrefabs/Enemy_{id}.prefab` 一致）。
    /// 威胁点与分数是**占位值**，等实机标定（数值工作慢慢打磨）。
    /// </summary>
    public static readonly Entry[] All =
    {
        new Entry { enemyId = 1011, displayName = "持剑杂兵",   threat = 1.0f, minCircle = 1, ranged = false, needsHeavy = false, boss = false, score = 10 },
        new Entry { enemyId = 1012, displayName = "刀兵",       threat = 2.2f, minCircle = 2, ranged = false, needsHeavy = true,  boss = false, score = 15 },
        new Entry { enemyId = 1013, displayName = "督战旗手",   threat = 1.6f, minCircle = 3, ranged = false, needsHeavy = false, boss = false, score = 15 },
        new Entry { enemyId = 105,  displayName = "弓箭手",     threat = 2.0f, minCircle = 2, ranged = true,  needsHeavy = false, boss = false, score = 15 },
        new Entry { enemyId = 106,  displayName = "固定落点弓", threat = 2.2f, minCircle = 3, ranged = true,  needsHeavy = false, boss = false, score = 15 },
        new Entry { enemyId = 1051, displayName = "弓手",       threat = 2.0f, minCircle = 2, ranged = true,  needsHeavy = false, boss = false, score = 15 },
        new Entry { enemyId = 1021, displayName = "盾卫",       threat = 2.6f, minCircle = 3, ranged = false, needsHeavy = true,  boss = false, score = 20 },
        new Entry { enemyId = 109,  displayName = "骑兵",       threat = 2.8f, minCircle = 3, ranged = false, needsHeavy = true,  boss = false, score = 25 },
        new Entry { enemyId = 1014, displayName = "投壶兵",     threat = 2.4f, minCircle = 4, ranged = true,  needsHeavy = false, boss = false, score = 20 },
        new Entry { enemyId = 104,  displayName = "BOSS·两阶段", threat = 0f,  minCircle = 1, ranged = false, needsHeavy = true,  boss = true,  score = 300 },
        new Entry { enemyId = 108,  displayName = "BOSS·双阶段", threat = 0f,  minCircle = 3, ranged = false, needsHeavy = true,  boss = true,  score = 260 },
        new Entry { enemyId = 107,  displayName = "BOSS·单阶段", threat = 0f,  minCircle = 2, ranged = false, needsHeavy = true,  boss = true,  score = 220 },
    };

    public static Entry Get(int enemyId)
    {
        for (int i = 0; i < All.Length; i++)
            if (All[i].enemyId == enemyId) return All[i];
        return new Entry { enemyId = enemyId, displayName = "未知", threat = 1f, minCircle = 1, score = 10 };
    }

    public static bool IsBoss(int enemyId)
    {
        for (int i = 0; i < All.Length; i++)
            if (All[i].enemyId == enemyId) return All[i].boss;
        return false;
    }

    /// <summary>该圈可用的**非 BOSS**敌种（教学契约过滤）。</summary>
    public static List<Entry> PoolForCircle(int circle, bool includeRanged, bool includeHeavy)
    {
        var list = new List<Entry>();
        for (int i = 0; i < All.Length; i++)
        {
            var e = All[i];
            if (e.boss) continue;
            if (e.minCircle > circle) continue;
            if (e.ranged && !includeRanged) continue;
            if (e.needsHeavy && !includeHeavy) continue;
            list.Add(e);
        }
        return list;
    }

    /// <summary>该圈可用的 BOSS（按圈数挑一个）。</summary>
    public static Entry BossForCircle(int circle)
    {
        for (int i = 0; i < All.Length; i++)
        {
            if (!All[i].boss) continue;
            if (All[i].minCircle <= circle) return All[i];
        }
        return All[All.Length - 1];
    }
}
