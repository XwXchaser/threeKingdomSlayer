using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 局外补给的**本地模拟**时刻表：按本地时钟推进轮次，规则与服务器下发的一致。
///
/// 轮次模型：
///   轮次序号 = floor(已过时间 / roundSeconds)
///   某点位的存活窗口 = [轮次起点 + roundOffset, 轮次起点 + roundOffset + lifetime]
///
/// 接后端时替换为「服务端下发轮次与存活表」的实现即可，本类只作本地模拟与离线兜底。
///
/// 注意：时间一律用 <see cref="double"/> —— Unix 秒在 float32 下的分辨率只有 128 秒，
/// 用 float 算轮次会整整偏一两轮。
/// </summary>
public sealed class LocalSupplySpawnSchedule : ISupplySpawnSchedule
{
    private readonly SupplySpawnPlan _plan;
    private readonly Func<double> _clock;
    private readonly List<SupplySpawnSlot> _slots = new List<SupplySpawnSlot>();

    /// <param name="plan">点位与轮次周期。</param>
    /// <param name="clock">服务器时间（本地模拟 = 墙钟秒，单位秒）。</param>
    public LocalSupplySpawnSchedule(SupplySpawnPlan plan, Func<double> clock)
    {
        _plan = plan;
        _clock = clock;
    }

    private double RoundSeconds => _plan != null ? Math.Max(1.0, _plan.roundSeconds) : 60.0;

    private double Now => _clock != null ? Math.Max(0.0, _clock()) : 0.0;

    public int RoundIndex => (int)Math.Floor(Now / RoundSeconds);

    public float SecondsToNextRound
    {
        get
        {
            double round = RoundSeconds;
            double remain = round - (Now % round);
            return (float)(remain <= 0 ? round : remain);
        }
    }

    public IReadOnlyList<SupplySpawnSlot> GetActiveSlots()
    {
        _slots.Clear();
        if (_plan == null || _plan.points == null) return _slots;

        double roundSeconds = RoundSeconds;
        double now = Now;
        int round = (int)Math.Floor(now / roundSeconds);
        double roundStart = round * roundSeconds;

        for (int i = 0; i < _plan.points.Length; i++)
        {
            var point = _plan.points[i];
            if (point == null) continue;
            double begin = roundStart + point.roundOffsetSeconds;
            double end = begin + Math.Max(0.0, point.lifetimeSeconds);
            if (now < begin || now >= end) continue;

            _slots.Add(new SupplySpawnSlot
            {
                point = point,
                elapsedSeconds = (float)(now - begin),
                remainSeconds = (float)(end - now),
                instanceId = $"r{round}:{point.id}"
            });
        }
        return _slots;
    }
}
