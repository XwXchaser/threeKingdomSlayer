using System;

/// <summary>
/// 可破坏补给物（箱 / 桶）的**存档条目**：哪个道具、被破坏没、发了什么奖励。
///
/// 2026-10-22（asmdef 分层第二批）：原本是 <c>BreakableProp</c> 里的嵌套结构体，
/// 但它要进存档快照（<c>FakeRouteStageSaveSnapshot</c>）—— 存档数据模型在数据层，
/// 嵌套在 MonoBehaviour 里的类型就下不去，所以提成顶层类型。
/// 序列化字段名不变 → 旧存档照读。
/// </summary>
[Serializable]
public struct PropSaveEntry
{
    public string propId;
    public bool destroyed;
    public int grantedCoins;
    public int grantedUltimateEnergy;
}
