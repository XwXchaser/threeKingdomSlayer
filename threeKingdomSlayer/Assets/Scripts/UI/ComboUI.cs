using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 连击 UI — 显示连击数字和激活中的 Buff 图标
/// </summary>
public class ComboUI : MonoBehaviour
{
    [Header("连击数字")]
    public TMP_Text comboText;
    public GameObject comboGroup; // 连击为 0 时隐藏

    [Header("Buff 图标")]
    public Transform buffIconParent;
    public GameObject buffIconPrefab; // 带 Image + 倒计时填充的预制体

    private Dictionary<string, BuffIconEntry> _iconEntries = new Dictionary<string, BuffIconEntry>();

    private void Start()
    {
        if (comboGroup != null) comboGroup.SetActive(false);

        if (ComboManager.Instance != null)
        {
            ComboManager.Instance.OnComboUpdated += OnComboUpdated;
            ComboManager.Instance.OnComboTrigger += OnComboTriggered;
        }
    }

    private void OnDestroy()
    {
        if (ComboManager.Instance != null)
        {
            ComboManager.Instance.OnComboUpdated -= OnComboUpdated;
            ComboManager.Instance.OnComboTrigger -= OnComboTriggered;
        }
    }

    private void Update()
    {
        UpdateBuffIcons();
    }

    private void OnComboUpdated(int combo)
    {
        if (comboText != null)
            comboText.text = combo.ToString();

        if (comboGroup != null)
            comboGroup.SetActive(combo > 0);
    }

    private void OnComboTriggered(string buffId)
    {
        // 新 Buff 触发时确保图标存在
        EnsureIcon(buffId);
    }

    private void EnsureIcon(string buffId)
    {
        if (_iconEntries.ContainsKey(buffId)) return;
        if (buffIconPrefab == null || buffIconParent == null) return;

        var go = Instantiate(buffIconPrefab, buffIconParent);
        _iconEntries[buffId] = new BuffIconEntry { go = go };
    }

    private void UpdateBuffIcons()
    {
        var bm = BuffManager.Instance;
        if (bm == null) return;

        var activeBuffs = bm.ActiveBuffs;
        var activeIds = new HashSet<string>();

        float now = Time.time;
        foreach (var buff in activeBuffs)
        {
            activeIds.Add(buff.buffId);
            EnsureIcon(buff.buffId);

            if (_iconEntries.TryGetValue(buff.buffId, out var entry))
            {
                entry.go.SetActive(true);
                // 倒计时：endTime=0 表示永久
                if (buff.endTime > 0f)
                {
                    float remaining = Mathf.Max(0f, buff.endTime - now);
                    // 子类 BuffIconEntry 可覆写此逻辑
                    entry.UpdateFill(remaining);
                }
            }
        }

        // 隐藏已过期的图标
        foreach (var kv in _iconEntries)
        {
            if (!activeIds.Contains(kv.Key))
                kv.Value.go.SetActive(false);
        }
    }

    private class BuffIconEntry
    {
        public GameObject go;

        public void UpdateFill(float remaining)
        {
            // 默认不做填充动画，由具体图标预制体自行实现
        }
    }
}
