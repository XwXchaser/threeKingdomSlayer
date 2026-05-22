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
    }

    /// <summary>注销 statId 的应用器</summary>
    public void UnregisterApplier(string statId)
    {
        _appliers.Remove(statId);
    }

    /// <summary>
    /// 添加 Buff。同 buffId 只刷新 endTime，不叠加。
    /// </summary>
    public void AddBuff(string buffId, float duration, List<StatModifier> modifiers)
    {
        // 刷新已有的同 buffId Buff
        for (int i = 0; i < _activeBuffs.Count; i++)
        {
            if (_activeBuffs[i].buffId == buffId)
            {
                _activeBuffs[i].endTime = duration > 0f ? Time.time + duration : 0f;
                return;
            }
        }

        // 新建 Buff
        var buff = new ActiveBuff
        {
            buffId = buffId,
            endTime = duration > 0f ? Time.time + duration : 0f,
            modifiers = modifiers
        };
        _activeBuffs.Add(buff);

        // 立刻应用修正
        if (modifiers != null)
        {
            foreach (var m in modifiers)
                ApplyModifierToApplier(m);
        }
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
