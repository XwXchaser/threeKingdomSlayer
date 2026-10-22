using UnityEngine;

/// <summary>
/// **一条战斗状态（机制型 ✅）的定义** ✅ —— 数值写在资产里 ✅ ✅，代码只认 id ✅ ✅。
///
/// 覆盖的是**机制型状态** ✅ ✅（如「概率击飞 ✅」—— 有专属行为 ✅ ✅）；
/// **数值型状态** ✅（属性增减 ✅ / 持续时间 ✅ / 叠层 ✅）走 `ComboBuffConfig` ✅ ✅ + `StatModifier` ✅ ✅ ✅（已经是数据驱动 ✅ ✅）。
///
/// 为什么要资产化 ✅ ✅：概率 ✅ / 时长 ✅ 这类**可调数值** ✅ 不该写死在代码里 ✅ ❗
/// → 加一条状态 = **加一个资产** ✅ ✅（策划可调 ✅ ✅，不用编译 ✅ ✅）。
/// </summary>
[CreateAssetMenu(fileName = "CombatBuffDefinition", menuName = "一夫当关/战斗状态定义")]
public sealed class CombatBuffDefinition : ScriptableObject
{
    [Tooltip("状态 id ✅ —— 运行时用这个字符串认 ✅（见 `BuffIds` ✅）")]
    public string buffId = "";

    [Tooltip("显示名（调试 / 提示用 ✅）")]
    public string displayName = "";

    [Header("时长")]
    [Tooltip("默认持续秒数 ✅（0 = 永久 ✅）；运行时也可覆盖 ✅")]
    [Min(0f)] public float defaultDuration = 0f;

    [Header("机制参数")]
    [Tooltip("**概率击飞**的触发概率 ✅（0~1 ✅）—— 原先写死在代码里的 0.3 ✅ ✅，挪到这里 ✅ ✅")]
    [Range(0f, 1f)] public float launchProbability = 0.3f;

    /// <summary>这个定义的 id ✅（空 = 用不了 ✅）。</summary>
    public bool IsValid => !string.IsNullOrEmpty(buffId);
}
