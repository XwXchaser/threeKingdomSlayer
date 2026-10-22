using System.Collections.Generic;

/// <summary>
/// **道具分片**：铜钱 / 钥匙等道具（<c>SaveData.props</c> + 旧字段 <c>coinCount</c>）。
///
/// 旧档兼容：老存档只有 <c>coinCount</c>，迁移时并进 <c>props</c> ✅（迁移逻辑跟着分片走 ✅）。
/// </summary>
public sealed class PropSaveSection : ISaveSection
{
    public string Id => "prop";

    public void Migrate(SaveData data)
    {
        if (data.props == null) data.props = new List<PropData>();

        // 旧档迁移：coinCount → props
        if (data.coinCount > 0 && data.props.GetPropAmount(PropType.Coin) == 0)
        {
            data.props.SetPropAmount(PropType.Coin, data.coinCount);
            data.coinCount = 0;
        }
    }

    /// <summary>读取铜钱数（优先 props，旧档回落到 coinCount ✅）。</summary>
    public static int GetCoins(SaveData data) => data.GetCoinCount();

    /// <summary>设置铜钱数。</summary>
    public static void SetCoins(SaveData data, int amount) => data.props.SetPropAmount(PropType.Coin, amount);

    /// <summary>增减铜钱，返回增量后的总量。</summary>
    public static int AddCoins(SaveData data, int delta) => data.props.AddPropAmount(PropType.Coin, delta);

    /// <summary>读取指定道具数量。</summary>
    public static int GetProp(SaveData data, PropType type) => data.props.GetPropAmount(type);

    /// <summary>设置指定道具数量。</summary>
    public static void SetProp(SaveData data, PropType type, int amount) => data.props.SetPropAmount(type, amount);

    /// <summary>增减指定道具，返回增量后的总量。</summary>
    public static int AddProp(SaveData data, PropType type, int delta) => data.props.AddPropAmount(type, delta);
}
