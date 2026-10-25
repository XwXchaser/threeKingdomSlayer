using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// 关卡**埋点记录器**：把每场战斗 / 每一波的关键数据写成 JSONL，
/// 供 <c>Tools/analyze_level_telemetry.py</c> 离线分析（清场时长、掉血、等级曲线、波次强度）。
///
/// 产物目录：<c>Library/Locus/telemetry/</c>（机器本地，不入库；Library 本身被 .gitignore 忽略）。
/// 记录时机：
///   • 每波开始：波次序号、敌人构成、玩家血量/等级/已获升级数
///   • 每波结束：该波用时、**该波**掉血、击杀数、是否用过重攻击
///   • 每场战斗结束：总用时、总掉血、死亡次数、最终等级、升级数、结算铜钱、胜负
///
/// 口径说明（2026-10-25 修正）：
///   • 「一波」= 一个 <see cref="StageConfig"/> 里的一条 <see cref="WaveConfig"/>（按 <c>WaveSpawner</c> 的出波事件切）。
///   • <c>hpLost</c> 是**该波内**的掉血（波开始时快照 HP → 波结束时相减），不是整场累计。
///   • 整场累计掉血/死亡次数由 <see cref="ReportHpChanged"/> 累加（需外部接 <c>PlayerState.OnHealthChanged</c>）。
/// </summary>
public static class LevelTelemetry
{
    /// <summary>埋点输出目录（工程内 Library 下，机器本地）。</summary>
    public static string OutputDirectory => Path.Combine(Application.dataPath, "..", "Library", "Locus", "telemetry");

    private static readonly List<string> _pending = new List<string>();
    private static bool _enabled;
    private static float _battleStartTime;
    private static int _battleWaveCount;
    private static int _battleKills;
    private static float _waveStartTime;
    private static float _waveStartHp = -1f;
    private static float _battleStartHp = -1f;
    private static float _lastHp = -1f;
    private static float _battleDamageTaken;
    private static int _battleDeaths;
    private static int _battleWaveIndex;
    private static string _battleLabel = "";
    private static bool _battleActive;

    /// <summary>打开/关闭记录（默认关；由 BattleAutoPlayer 或调试菜单打开）。</summary>
    public static bool Enabled
    {
        get => _enabled;
        set
        {
            _enabled = value;
            if (!value) Flush();
        }
    }

    /// <summary>当前是否在一场战斗的埋点区间内。</summary>
    public static bool BattleActive => _battleActive;

    /// <summary>当前战斗的关卡配置名（调试用）。</summary>
    public static string BattleLabel => _battleLabel;

    /// <summary>战斗开始（一场 = 一个 StageConfig 的全部波次）。</summary>
    public static void BeginBattle(string label)
    {
        _battleLabel = label ?? "";
        _battleStartTime = Time.time;
        _battleWaveCount = 0;
        _battleKills = 0;
        _battleWaveIndex = 0;
        _battleDamageTaken = 0f;
        _battleDeaths = 0;
        _battleActive = true;
        _lastHp = CurrentHp();
        _battleStartHp = _lastHp;
        Log("battle_begin", new Dictionary<string, object>
        {
            { "label", _battleLabel },
            { "time", NowStamp() },
            { "hp", _lastHp },
            { "level", CurrentLevel() },
            { "upgrades", CurrentUpgradeCount() },
        });
    }

    /// <summary>一波开始。</summary>
    public static void BeginWave(int waveIndex, int enemyCount, string composition)
    {
        _battleWaveIndex = waveIndex;
        _waveStartTime = Time.time;
        _waveStartHp = CurrentHp();
        Log("wave_begin", new Dictionary<string, object>
        {
            { "battle", _battleLabel },
            { "wave", waveIndex },
            { "enemies", enemyCount },
            { "composition", composition ?? "" },
            { "hp", CurrentHp() },
            { "level", CurrentLevel() },
            { "upgrades", CurrentUpgradeCount() },
            { "time", NowStamp() },
        });
    }

    /// <summary>
    /// 一波结束。aliveRemaining = 波结束时剩余敌人数；usedHeavy = 本波是否用过蓄力/重招；
    /// kills = 本波击杀数。
    /// </summary>
    public static void EndWave(int aliveRemaining, bool usedHeavy, int kills = 0)
    {
        float dur = Time.time - _waveStartTime;
        float hpNow = CurrentHp();
        float waveHpLost = (_waveStartHp >= 0f && hpNow >= 0f) ? Mathf.Max(0f, _waveStartHp - hpNow) : 0f;
        Log("wave_end", new Dictionary<string, object>
        {
            { "battle", _battleLabel },
            { "wave", _battleWaveIndex },
            { "duration", Round(dur) },
            { "hpLost", Round(waveHpLost) },
            { "alive", aliveRemaining },
            { "kills", kills },
            { "usedHeavy", usedHeavy },
            { "level", CurrentLevel() },
            { "upgrades", CurrentUpgradeCount() },
            { "time", NowStamp() },
        });
        _battleWaveCount++;
        _battleKills += kills;
    }

    /// <summary>战斗结束（胜/败都记）。</summary>
    public static void EndBattle(bool victory, int coinsGained)
    {
        if (!_battleActive) return;
        _battleActive = false;
        Log("battle_end", new Dictionary<string, object>
        {
            { "battle", _battleLabel },
            { "victory", victory },
            { "duration", Round(Time.time - _battleStartTime) },
            { "waves", _battleWaveCount },
            { "kills", _battleKills },
            { "hpLost", Round(_battleDamageTaken) },
            { "deaths", _battleDeaths },
            { "level", CurrentLevel() },
            { "upgrades", CurrentUpgradeCount() },
            { "coins", coinsGained },
            { "time", NowStamp() },
        });
        Flush();
    }

    /// <summary>玩家掉血时调用（累计战斗掉血 + 死亡次数）。</summary>
    public static void ReportHpChanged(float currentHp, float maxHp)
    {
        if (!_battleActive) return;
        if (_lastHp >= 0f && currentHp < _lastHp) _battleDamageTaken += (_lastHp - currentHp);
        if (_lastHp > 0f && currentHp <= 0f) _battleDeaths++;
        _lastHp = currentHp;
    }

    private static void Log(string type, Dictionary<string, object> data)
    {
        if (!_enabled) return;
        data["type"] = type;
        var sb = new StringBuilder();
        sb.Append('{');
        bool first = true;
        foreach (var kv in data)
        {
            if (!first) sb.Append(',');
            first = false;
            sb.Append('"').Append(kv.Key).Append("\":");
            if (kv.Value is string s) sb.Append('"').Append(Escape(s)).Append('"');
            else if (kv.Value is bool b) sb.Append(b ? "true" : "false");
            else sb.Append(Convert.ToString(kv.Value, CultureInfo.InvariantCulture));
        }
        sb.Append('}');
        _pending.Add(sb.ToString());
        if (_pending.Count >= 20) Flush();
    }

    private static void Flush()
    {
        if (_pending.Count == 0) return;
        try
        {
            Directory.CreateDirectory(OutputDirectory);
            string file = Path.Combine(OutputDirectory, $"run-{DateTime.Now:yyyyMMdd-HHmmss}.jsonl");
            File.AppendAllLines(file, _pending);
            Debug.Log($"[Telemetry] 已写入 {_pending.Count} 条 → {file}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Telemetry] 写入失败：{e.Message}");
        }
        _pending.Clear();
    }

    private static string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ");
    }

    private static float CurrentHp() => PlayerState.Instance != null ? PlayerState.Instance.currentHealth : -1f;
    private static int CurrentLevel() => PlayerState.Instance != null ? PlayerState.Instance.currentLevel : -1;
    private static int CurrentUpgradeCount() => PlayerState.Instance != null && PlayerState.Instance.acquiredUpgrades != null
        ? PlayerState.Instance.acquiredUpgrades.Count : 0;
    private static float NowStamp() => (float)Time.realtimeSinceStartup;
    private static float Round(float v) => Mathf.Round(v * 100f) / 100f;
}
