using UnityEngine;

/// <summary>
/// **战斗状态 id** ✅ —— 取代旧的 <c>BuffType</c> 枚举 ✅ ✅。
///
/// 为什么不用枚举 ✅：枚举是**写死**的 ✅ —— 加一种状态 = 改代码 + 重新编译 ✅ ❗
/// → 与项目已有的**内容驱动**范式（招式 ✅ / 技能 ✅ / 天赋 ✅ / 关卡 ✅ 都是资产 ✅ ✅）不一致 ✅ ✅。
/// **id 用字符串** ✅ → 状态可以由**资产**描述 ✅ ✅（`ComboBuffConfig` ✅ ✅ 已经是这个路子 ✅ ✅）。
///
/// 这里是**机制 id** ✅ 的词表 ✅ ✅（击飞 ✅ 这类**行为** ✅）——
/// **数值参数**（概率 ✅ / 时长 ✅）放在 `CombatBuffDefinition` ✅ ✅ 资产里 ✅ ✅。
/// </summary>
public static class BuffIds
{
    /// <summary>强制击飞：无视条件的可击飞标记 ✅。</summary>
    public const string ForceLaunch = "force_launch";

    /// <summary>概率击飞：命中时按 `CombatBuffDefinition.launchProbability` ✅ 强制击飞 ✅。</summary>
    public const string ProbabilityLaunch = "probability_launch";
}
