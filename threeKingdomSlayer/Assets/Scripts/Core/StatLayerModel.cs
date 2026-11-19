using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// **属性转化器**：读一个源属性，按比例换算成另一个属性的加成。
///
/// 例（霸王血铠）：每 100 点生命值 → +20 攻击力。
///
/// **防环规则（写死，不允许改）**：转化器的**输入只取「基础值 + 固定值层 + 加法层」**，
/// 即**不含任何转化器产出** ⇒ 转化器之间不可能互相喂养，结构上不会成环。
/// 代价：一条「吸血 → 生命 → 攻击力」的链只读到第一跳（吸血给的生命进不了攻击力）——
/// 这是刻意选择的安全口径（见 design/weapon-system-design.md §5.3）。
/// </summary>
[Serializable]
public struct StatConverter
{
    [Tooltip("源属性 id（如 max_health）")]
    public string sourceStatId;

    [Tooltip("每多少点源属性触发一次换算（≤0 = 不触发）")]
    public float perSourceValue;

    [Tooltip("目标属性 id（如 atk）")]
    public string targetStatId;

    [Tooltip("每次触发的获得量")]
    public float gain;

    [Tooltip("gain 的含义：false = 直接加（+20 攻击）；true = 对目标基础值加百分比（+20% 攻击）")]
    public bool gainIsPercent;
}

/// <summary>
/// **属性分层乘算模型**（2026-11-16 新增）。
///
/// ## 为什么需要
/// 改造前全项目只有一个伤害乘区标量（<c>UpgradeEffectManager._damageMultiplier</c>），
/// 加法百分比与乘法百分比被压平成同一个加法数，**无法表达**：
///   · 同类相加 / 异类相乘（roguelite 防指数膨胀的基本规则）
///   · 固定值加成（"+20 攻击力"这种 flat）
///   · 转化器（"每 100 点生命 → +20 攻击力"这种读别的属性再输出）
///
/// ## 求值顺序（固定，不允许改）
/// <code>
/// 1) 基础值 + 固定值层
/// 2) × (1 + 加法层百分比)        ← 同一 statId 的百分比先相加
/// 3) + 转化器产出的固定值         ← 转化器输入只取「到第 2 步为止」的值
/// 4) × Π(1 + 乘法层乘区)         ← 异类乘区各自独立相乘
/// </code>
///
/// ## 与改造前的等价性
/// 没有固定值 / 乘区 / 转化器时，本模型 = <c>base × (1 + Σ加法)</c>，
/// 与旧的 <c>base × (_damageMultiplier + _buffDamageBonus)</c> **完全等价**（迁移期行为不变）。
/// </summary>
public sealed class StatLayerModel
{
    /// <summary>倍率上限（防止无限叠加爆 float）。</summary>
    public float maxMultiplier = 1000000f;

    private readonly Dictionary<string, float> _flat = new Dictionary<string, float>();
    private readonly Dictionary<string, float> _additive = new Dictionary<string, float>();
    private readonly Dictionary<string, List<float>> _multiplicative = new Dictionary<string, List<float>>();
    private readonly List<StatConverter> _converters = new List<StatConverter>();

    /// <summary>固定值层（flat）：+20 攻击这种，不吃百分比。</summary>
    public void AddFlat(string statId, float value)
    {
        if (string.IsNullOrEmpty(statId)) return;
        _flat[statId] = GetFlat(statId) + value;
    }

    /// <summary>加法层：百分比（0.1 = +10%）。同一 statId 的百分比先相加再乘一次。</summary>
    public void AddAdditive(string statId, float value)
    {
        if (string.IsNullOrEmpty(statId)) return;
        _additive[statId] = GetAdditiveSum(statId) + value;
    }

    /// <summary>乘法层：独立乘区（0.2 = ×1.2）。每个乘区各自相乘。</summary>
    public void AddMultiplicative(string statId, float value)
    {
        if (string.IsNullOrEmpty(statId)) return;
        if (!_multiplicative.TryGetValue(statId, out var list))
        {
            list = new List<float>();
            _multiplicative[statId] = list;
        }
        list.Add(value);
    }

    /// <summary>注册一个转化器。</summary>
    public void AddConverter(StatConverter converter)
    {
        if (string.IsNullOrEmpty(converter.sourceStatId) || string.IsNullOrEmpty(converter.targetStatId)) return;
        if (converter.perSourceValue <= 0f) return;
        _converters.Add(converter);
    }

    public float GetFlat(string statId)
    {
        if (string.IsNullOrEmpty(statId)) return 0f;
        return _flat.TryGetValue(statId, out float v) ? v : 0f;
    }

    /// <summary>加法层总和（百分比）。</summary>
    public float GetAdditiveSum(string statId)
    {
        if (string.IsNullOrEmpty(statId)) return 0f;
        return _additive.TryGetValue(statId, out float v) ? v : 0f;
    }

    /// <summary>乘法层乘积（Π(1 + v)）。</summary>
    public float GetMultiplicativeProduct(string statId)
    {
        if (string.IsNullOrEmpty(statId)) return 1f;
        if (!_multiplicative.TryGetValue(statId, out var list) || list == null || list.Count == 0) return 1f;
        float product = 1f;
        for (int i = 0; i < list.Count; i++) product *= (1f + list[i]);
        if (product > maxMultiplier) product = maxMultiplier;
        if (product < 0f) product = 0f;
        return product;
    }

    /// <summary>
    /// **倍率**（不含固定值层）= (1 + 加法层) × Π(1 + 乘区)。
    /// 改造前 <c>GetDamageMultiplier()</c> 的等价实现。
    /// </summary>
    public float EvaluateMultiplier(string statId)
    {
        float additive = 1f + GetAdditiveSum(statId);
        if (additive < 0f) additive = 0f;
        return additive * GetMultiplicativeProduct(statId);
    }

    /// <summary>
    /// **完整求值**：基础值 → 固定值层 → 加法百分比 → 转化器 → 乘法乘区。
    ///
    /// <paramref name="sourceResolver"/>：解析转化器的**源属性当前值**。
    /// 调用方必须保证它返回的是「不含转化器产出」的值（否则会成环）。
    /// </summary>
    public float Evaluate(string statId, float baseValue, Func<string, float> sourceResolver = null)
    {
        if (string.IsNullOrEmpty(statId)) return baseValue;

        float flat = GetFlat(statId);
        float convFlat = 0f;
        float convPercent = 0f;
        for (int i = 0; i < _converters.Count; i++)
        {
            StatConverter c = _converters[i];
            if (!string.Equals(c.targetStatId, statId, StringComparison.Ordinal)) continue;
            if (sourceResolver == null) continue;
            float source = sourceResolver(c.sourceStatId);
            if (source <= 0f || c.perSourceValue <= 0f) continue;
            float times = (float)Math.Floor(source / c.perSourceValue);
            if (times <= 0f) continue;
            if (c.gainIsPercent) convPercent += times * c.gain;
            else convFlat += times * c.gain;
        }

        float additive = 1f + GetAdditiveSum(statId) + convPercent;
        if (additive < 0f) additive = 0f;
        float multiplier = additive * GetMultiplicativeProduct(statId);

        float result = (baseValue + flat + convFlat) * multiplier;
        if (result < 0f) result = 0f;
        return result;
    }

    /// <summary>清空所有层（新开局）。</summary>
    public void Clear()
    {
        _flat.Clear();
        _additive.Clear();
        _multiplicative.Clear();
        _converters.Clear();
    }

    /// <summary>已注册的转化器数量（调试 / 自检用）。</summary>
    public int ConverterCount => _converters.Count;
}
