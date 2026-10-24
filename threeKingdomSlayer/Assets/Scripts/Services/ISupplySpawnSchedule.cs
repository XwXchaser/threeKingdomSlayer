using System.Collections.Generic;

/// <summary>
/// 局外补给的「生成时刻表」接口。
///
/// 语义：服务器（或本地模拟）按**轮次**决定「此刻哪些点位上应该有物件」。
/// 客户端只负责按这份表刷新画面 —— 玩法代码不关心这份表是服务器下发的还是本地推的。
///
/// 接真后端时的替换点：实现这个接口即可（拉取服务端轮次 + 服务器时间）。
/// </summary>
public interface ISupplySpawnSchedule
{
    /// <summary>当前这一轮里「应当存在」的生成项（点位 + 剩余存活时间）。</summary>
    IReadOnlyList<SupplySpawnSlot> GetActiveSlots();

    /// <summary>距离下一轮开始的秒数（供 UI 倒计时）。</summary>
    float SecondsToNextRound { get; }

    /// <summary>当前轮次序号。</summary>
    int RoundIndex { get; }
}

/// <summary>一个应当存在的局外补给生成项。</summary>
public struct SupplySpawnSlot
{
    /// <summary>点位定义（位置 / 预制体 / 金币 / 存活时长）。</summary>
    public SupplySpawnPoint point;

    /// <summary>本轮里该点位已经存在了多久（秒）。</summary>
    public float elapsedSeconds;

    /// <summary>本轮里该点位还剩多久消失（秒）。</summary>
    public float remainSeconds;

    /// <summary>实例标识：轮次 + 点位 id（用于领取去重与调试）。</summary>
    public string instanceId;
}
