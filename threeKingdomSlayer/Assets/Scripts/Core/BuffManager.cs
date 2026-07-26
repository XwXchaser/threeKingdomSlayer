using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 属性修正应用器接口 — 实现者能接收 StatModifier 的 Apply/Remove
/// </summary>
public interface IStatModifierApplier
{
    void ApplyModifier(StatModifier modifier);
    void RemoveModifier(StatModifier modifier);
}

/// <summary>
/// Buff 管理器 - 单例
/// 维护 IStatModifierApplier 注册表，管理 ActiveBuff 生命周期。
/// 任何系统可通过 RegisterApplier 注册属性修改能力，BuffManager 在 Buff 激活/过期时通知。
/// </summary>
public class BuffManager : MonoBehaviour
{
    public static BuffManager Instance { get; private set; }

    /// <summary>statId → 应用器 注册表</summary>
    private Dictionary<string, IStatModifierApplier> _appliers = new Dictionary<string, IStatModifierApplier>();

    /// <summary>当前激活的 Buff 列表</summary>
    private List<ActiveBuff> _activeBuffs = new List<ActiveBuff>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        UpgradeEffectManager.Instance?.RegisterBuffAppliers();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        float now = Time.time;
        for (int i = _activeBuffs.Count - 1; i >= 0; i--)
        {
            if (_activeBuffs[i].endTime > 0f && now >= _activeBuffs[i].endTime)
            {
                RemoveBuffAt(i);
            }
        }
    }

    /// <summary>注册 statId 的应用器</summary>
    public void RegisterApplier(string statId, IStatModifierApplier applier)
    {
        _appliers[statId] = applier;
        for (int i = 0; i < _activeBuffs.Count; i++)
        {
            var modifiers = _activeBuffs[i].modifiers;
            if (modifiers == null) continue;
            for (int j = 0; j < modifiers.Count; j++)
            {
                if (modifiers[j] != null && modifiers[j].statId == statId)
                    applier.ApplyModifier(modifiers[j]);
            }
        }
    }

    /// <summary>注销 statId 的应用器</summary>
    public void UnregisterApplier(string statId, IStatModifierApplier applier)
    {
        if (_appliers.TryGetValue(statId, out var registered) && registered == applier)
            _appliers.Remove(statId);
    }

    public void AddBuff(string buffId, float duration, StatModifier modifier)
    {
        AddBuff(buffId, duration, modifier != null ? new List<StatModifier> { modifier } : null);
    }

    /// <summary>
    /// 添加 Buff。不同 buffId 独立存在；相同 buffId 将新的修正叠加到已有 Buff。
    /// </summary>
    public void AddBuff(string buffId, float duration, List<StatModifier> modifiers)
    {
        for (int i = 0; i < _activeBuffs.Count; i++)
        {
            if (_activeBuffs[i].buffId != buffId) continue;

            var active = _activeBuffs[i];
            active.endTime = duration > 0f ? Time.time + duration : 0f;
            if (modifiers == null) return;
            if (active.modifiers == null)
                active.modifiers = new List<StatModifier>();

            foreach (var modifier in modifiers)
            {
                active.modifiers.Add(modifier);
                ApplyModifierToApplier(modifier);
            }
            return;
        }

        var buff = new ActiveBuff
        {
            buffId = buffId,
            endTime = duration > 0f ? Time.time + duration : 0f,
            modifiers = modifiers != null ? new List<StatModifier>(modifiers) : new List<StatModifier>()
        };
        _activeBuffs.Add(buff);

        foreach (var modifier in buff.modifiers)
            ApplyModifierToApplier(modifier);
    }

    public bool RemoveBuff(string buffId)
    {
        bool removed = false;
        for (int i = _activeBuffs.Count - 1; i >= 0; i--)
        {
            if (_activeBuffs[i].buffId != buffId) continue;
            RemoveBuffAt(i);
            removed = true;
        }
        return removed;
    }

    /// <summary>获取当前激活的 Buff 列表（供 UI 读取）</summary>
    public List<ActiveBuff> ActiveBuffs => _activeBuffs;

    private void RemoveBuffAt(int index)
    {
        var buff = _activeBuffs[index];
        _activeBuffs.RemoveAt(index);

        if (buff.modifiers != null)
        {
            foreach (var m in buff.modifiers)
                RemoveModifierFromApplier(m);
        }
    }

    private void ApplyModifierToApplier(StatModifier m)
    {
        if (_appliers.TryGetValue(m.statId, out var applier))
            applier.ApplyModifier(m);
    }

    private void RemoveModifierFromApplier(StatModifier m)
    {
        if (_appliers.TryGetValue(m.statId, out var applier))
            applier.RemoveModifier(m);
    }
}
