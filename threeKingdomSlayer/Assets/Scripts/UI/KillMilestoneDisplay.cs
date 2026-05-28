using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 全局击杀数展示 — 到达阈值时显示图片，停留后消失
/// 与关卡配置解耦，通过 GlobalKillDisplayConfig 配置
/// 显隐使用 Image.color.a（参考 ComboDisplayUI 模式），避免 SetActive 自引用
/// </summary>
public class KillMilestoneDisplay : MonoBehaviour
{
    [Header("配置")]
    [Tooltip("全局击杀展示配置资产")]
    public GlobalKillDisplayConfig config;
    [Header("闪烁动效")]
    [Tooltip("闪烁次数（一亮一灭算 1 次）")]
    public int flashCount = 3;
    [Tooltip("每次亮/灭间隔（秒）")]
    public float flashInterval = 0.06f;

    private struct Slot
    {
        public GameObject go;
        public Image image;
        public int threshold;
    }

    private List<Slot> _slots = new List<Slot>();
    private HashSet<int> _shownThresholds = new HashSet<int>();
    private int _builtEntriesVersion;

    private void Start()
    {
        BuildImages();

        if (PlayerState.Instance != null)
            PlayerState.Instance.OnKillCountChanged += OnKillCountChanged;
    }

    private void OnDestroy()
    {
        if (PlayerState.Instance != null)
            PlayerState.Instance.OnKillCountChanged -= OnKillCountChanged;
    }

    /// <summary>
    /// 构建 Image 池。每次 config.entries 数量变化时重建。
    /// </summary>
    private void BuildImages()
    {
        int count = config != null && config.entries != null ? config.entries.Count : 0;
        if (count == _builtEntriesVersion && _slots.Count == count) return;
        _builtEntriesVersion = count;

        // 清理旧池
        foreach (var slot in _slots)
        {
            if (slot.go != null) Destroy(slot.go);
        }
        _slots.Clear();

        for (int i = 0; i < count; i++)
        {
            var entry = config.entries[i];
            var imgGO = new GameObject($"KillDisplay_{entry.killThreshold}", typeof(RectTransform), typeof(Image));
            imgGO.transform.SetParent(transform, false);

            var rt = imgGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = entry.displaySize;
            rt.anchoredPosition = entry.displayPosition;

            var img = imgGO.GetComponent<Image>();
            img.sprite = entry.displaySprite;
            img.preserveAspect = true;
            img.raycastTarget = false;

            // 始终 active，用 alpha 控制显隐
            SetAlpha(img, 0f);

            _slots.Add(new Slot { go = imgGO, image = img, threshold = entry.killThreshold });
        }
    }

    private void OnKillCountChanged(int killCount)
    {
        BuildImages();

        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            if (killCount >= slot.threshold && _shownThresholds.Add(slot.threshold))
            {
                float duration = config.entries[i].displayDuration > 0f ? config.entries[i].displayDuration : 2f;
                StartCoroutine(ShowAndHide(slot, duration));
            }
        }
    }

    private IEnumerator ShowAndHide(Slot slot, float totalDuration)
    {
        if (slot.image == null) yield break;

        var wait = new WaitForSeconds(flashInterval);

        // 闪烁阶段：快速亮灭
        for (int i = 0; i < flashCount; i++)
        {
            SetAlpha(slot.image, 1f);
            yield return wait;
            SetAlpha(slot.image, 0f);
            yield return wait;
        }

        // 最后亮起，停留剩余时间
        float flashTime = flashCount * flashInterval * 2f;
        float holdTime = Mathf.Max(0f, totalDuration - flashTime);
        SetAlpha(slot.image, 1f);
        yield return new WaitForSeconds(holdTime);
        SetAlpha(slot.image, 0f);
    }

    private static void SetAlpha(Image img, float a)
    {
        if (img == null) return;
        var c = img.color;
        c.a = a;
        img.color = c;
    }
}
