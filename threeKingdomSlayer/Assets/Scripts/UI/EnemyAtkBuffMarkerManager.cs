using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// **敌人身上的「加攻」标记**（督战单位 1013 的号令可视化）。
///
/// 需求（2026-11-07 用户定）：「敌人加攻暂时在**单位左上方**用**玩家加攻的标记**表示」
/// → 不做描边、不做血条第三根条，直接在敌人**左上角**摆一个图标：
///   • 图标 = **玩家「加攻」升级的图标**（从三选一升级池里按 effectType=damage_multiplier 取，
///     不写死资源路径 → Inspector 可追踪）；
///   • 数字 = 当前加攻**层数**（×N）。
///
/// 为什么放在界面层：敌人内核不认识 UI；这里只读状态、只画表现。
/// </summary>
public class EnemyAtkBuffMarkerManager : MonoBehaviour
{
    private sealed class Marker
    {
        public GameObject root;
        public SpriteRenderer icon;
        public TextMeshPro layers;
    }

    [Header("图标")]
    [Tooltip("**玩家「加攻」的标记图标**（Inspector 直连；留空 = 回退到三选一池里的加攻升级图标）")]
    public Sprite atkUpIcon;

    [Header("样式")]
    [Tooltip("图标的世界尺寸")]
    public float iconWorldSize = 0.75f;

    [Tooltip("图标相对敌人左上角的横向偏移")]
    public float leftInset = 0.12f;

    [Tooltip("图标相对敌人顶部的纵向偏移")]
    public float topOffset = 0.12f;

    [Tooltip("图标与数字的渲染层级（敌人血条是 200）")]
    public int sortingOrder = 210;

    [Tooltip("层数文字的世界字号")]
    public float layersFontSize = 0.35f;

    private readonly Dictionary<Enemy, Marker> _markers = new Dictionary<Enemy, Marker>();
    private readonly HashSet<Enemy> _seen = new HashSet<Enemy>();
    private Sprite _atkIcon;
    private float _nextIconResolveTime;

    private void Update()
    {
        // 图标资源：Inspector 直连优先；没接就每 2s 从升级池里重试一次
        if (_atkIcon == null && Time.time >= _nextIconResolveTime)
        {
            _nextIconResolveTime = Time.time + 2f;
            _atkIcon = ResolveAtkIcon();
        }

        _seen.Clear();
        var manager = EnemyManager.Instance;
        var all = manager != null ? manager.GetAllAliveEnemies() : null;
        if (all != null)
        {
            for (int i = 0; i < all.Count; i++)
            {
                var enemy = all[i];
                if (enemy == null || enemy.state == EnemyState.Dead) continue;
                _seen.Add(enemy);
                UpdateMarker(enemy);
            }
        }

        // 清理：敌人没了（回池 / 死亡）→ 收掉它的标记
        if (_markers.Count > 0)
        {
            List<Enemy> dead = null;
            foreach (var kv in _markers)
            {
                if (_seen.Contains(kv.Key)) continue;
                if (kv.Value != null && kv.Value.root != null) Destroy(kv.Value.root);
                (dead ?? (dead = new List<Enemy>())).Add(kv.Key);
            }
            if (dead != null)
            {
                for (int i = 0; i < dead.Count; i++) _markers.Remove(dead[i]);
            }
        }
    }

    private void UpdateMarker(Enemy enemy)
    {
        int stacks = EnemyStatusAtkMod.GetUpStacks(enemy);
        Marker marker;
        if (!_markers.TryGetValue(enemy, out marker) || marker == null || marker.root == null)
        {
            if (stacks <= 0) return;             // 没被加成 → 不建标记
            marker = CreateMarker();
            _markers[enemy] = marker;
        }
        if (marker == null || marker.root == null) return;

        bool show = stacks > 0 && _atkIcon != null;
        if (marker.root.activeSelf != show) marker.root.SetActive(show);
        if (!show) return;

        if (marker.icon != null && marker.icon.sprite != _atkIcon) marker.icon.sprite = _atkIcon;
        if (marker.layers != null)
        {
            // ⚠️ 用 ASCII 的 x（× 在很多字体里没有字形 → 会整段不显示）
            string t = "x" + stacks;
            if (marker.layers.text != t) marker.layers.text = t;
        }

        // 位置：敌人**左上角**（世界坐标；不挂在敌人身上 → 不受攻击翻转影响）
        Vector3 pos = ResolveTopLeft(enemy);
        marker.root.transform.position = pos;
    }

    private Vector3 ResolveTopLeft(Enemy enemy)
    {
        var sr = enemy.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null)
            return enemy.transform.position + new Vector3(0f, 1f, 0f);

        // 头顶用**剪影顶**（与血条同一口径）：画布顶包含透明边距 → 直接用 bounds.max.y 会飘得很高。
        float headY;
        if (EnemyGroundAligner.TryGetSilhouetteBounds(sr.sprite, out float lo, out float hi))
            headY = enemy.transform.position.y + hi * enemy.transform.lossyScale.y;
        else
            headY = sr.bounds.max.y;

        var b = sr.bounds;
        return new Vector3(b.min.x + leftInset, headY + topOffset, b.min.z);
    }

    private Marker CreateMarker()
    {
        var root = new GameObject("AtkBuffMarker");
        var rootRt = root.transform as RectTransform;

        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(root.transform, false);
        var icon = iconGo.AddComponent<SpriteRenderer>();
        icon.sortingOrder = sortingOrder;
        iconGo.transform.localScale = Vector3.one * iconWorldSize;

        var textGo = new GameObject("Layers", typeof(TextMeshPro));
        textGo.transform.SetParent(root.transform, false);
        var text = textGo.GetComponent<TextMeshPro>();
        text.fontSize = layersFontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = new Color(1f, 0.86f, 0.45f);
        text.alignment = TextAlignmentOptions.Left;
        text.sortingOrder = sortingOrder + 1;
        textGo.transform.localPosition = new Vector3(iconWorldSize * 0.45f, 0f, 0f);

        return new Marker { root = root, icon = icon, layers = text };
    }

    /// <summary>
    /// 取**玩家「加攻」升级的图标**：优先用 Inspector 里直连的 <see cref="atkUpIcon"/>；
    /// 没接的话回退到三选一升级池里 effectType=damage_multiplier 的那个（不写死资源路径）。
    /// </summary>
    private Sprite ResolveAtkIcon()
    {
        if (atkUpIcon != null) return atkUpIcon;
        var ucm = UpgradeChoiceManager.Instance;
        var pool = ucm != null ? ucm.poolConfig : null;
        if (pool == null) return null;
        return FindIcon(pool.commonPool) ?? FindIcon(pool.rarePool) ?? FindIcon(pool.legendaryPool);
    }

    private static Sprite FindIcon(List<WeightedUpgrade> list)
    {
        if (list == null) return null;
        for (int i = 0; i < list.Count; i++)
        {
            var w = list[i];
            if (w == null || w.upgrade == null) continue;
            if (w.upgrade.effectType != "damage_multiplier") continue;
            if (w.upgrade.icon == null) continue;
            return w.upgrade.icon;
        }
        return null;
    }
}
