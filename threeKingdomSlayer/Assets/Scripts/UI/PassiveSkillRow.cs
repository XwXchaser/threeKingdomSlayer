using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// **被动技能排**（2026-10-29 用户提：被动技能一直没地方显示）。
///
/// 口径：
///   • 位置：**底排右侧**，**从右往左**排（最早获得的在最右，新获得的往左长）。
///     参照《上古卷轴：刀锋》里“大招就绪”那类技能位的做法（单列、紧凑、不占中央），
///     我们把它放在右侧这一条，和左侧的**黄色棱形排**（主动技能 / 道具）分开：
///     左边=**能点的**，右边=**被动生效的**。
///   • 内容：**饰品 / 机制型被动**（资产上 <c>statId</c> 为空的升级：AttackPassive / TimedPassive
///     + 机制型 Numeric），用各自 `UpgradeDefinition.icon` ✅，右下角标等级。
///     ★ **属性提升（<c>statId</c> 非空）不在这一排**（2026-11-19 用户定：“属性提升不是饰品，
///     当前右侧应该显示的是饰品”）—— 它们显示在**左侧的数值排**（BuffDisplayPanel）。
///   • 不显示：道具（Item）与主动技能（ActiveSkill）——它们各有自己的位置。
///   • 纯展示：被动没有冷却、不可点击，只告诉玩家“我现在带着哪些被动”。
///   • 超出槽位用 “+N” 折叠（最多显示 5 个）。
/// </summary>
public class PassiveSkillRow : MonoBehaviour
{
    /// <summary>一个被动槽位（图标 + 等级）。</summary>
    [Serializable]
    public sealed class SlotView
    {
        public RectTransform root;
        public Image icon;
        public SpriteNumberDisplay level;
    }

    public static PassiveSkillRow Instance { get; private set; }

    [Header("槽位")]
    [Tooltip("从右往左的槽位（索引 0 = 最右 = 最早获得）")]
    [SerializeField] private SlotView[] _slots = new SlotView[0];
    [Tooltip("溢出提示（+N）")]
    [SerializeField] private TextMeshProUGUI _overflow;

    /// <summary>本局已获得的被动（按获得顺序）。</summary>
    private readonly List<UpgradeAcquired> _passives = new List<UpgradeAcquired>();
    private int _lastSignature = -1;

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
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        Sync();
    }

    /// <summary>把被动列表同步到槽位（只在内容变化时重排）。</summary>
    private void Sync()
    {
        var player = PlayerState.Instance;
        if (player == null) return;

        int signature = ComputeSignature(player);
        if (signature == _lastSignature) return;
        _lastSignature = signature;

        CollectPassives(player);
        FillSlots();
    }

    /// <summary>内容签名（数量 + 各自等级）——变了才重排。</summary>
    private static int ComputeSignature(PlayerState player)
    {
        int hash = 17;
        var list = player.acquiredUpgrades;
        for (int i = 0; i < list.Count; i++)
        {
            var u = list[i];
            if (u == null || u.definition == null) continue;
            if (!ShowsInPassiveRow(u.definition)) continue;
            hash = hash * 31 + u.currentLevel;
        }
        return hash * 31 + list.Count;
    }

    private void CollectPassives(PlayerState player)
    {
        _passives.Clear();
        var list = player.acquiredUpgrades;
        for (int i = 0; i < list.Count; i++)
        {
            var u = list[i];
            if (u == null || u.definition == null) continue;
            if (!ShowsInPassiveRow(u.definition)) continue;
            _passives.Add(u);
        }
    }

    /// <summary>
    /// 这条升级**是不是显示在右下角那一排**（被动技能排 / 饰品排）。
    ///
    /// 口径（2026-11-19 用户定：“属性提升不是饰品，当前右侧应该显示的是饰品”）：
    ///   · **属性**（资产上填了 <c>statId</c>）⇒ **不在这一排**，它显示在左侧的数值排；
    ///   · **饰品 / 机制**（没填 <c>statId</c>）⇒ 在这一排；
    ///   · 道具（Item）与主动技能（ActiveSkill）⇒ 都不在（它们各有自己的位置）。
    ///
    /// ⚠ 旧口径是「<c>category == Numeric</c> 也算」⇒ **属性提升被塞进这一排**，
    /// 同时又被从左侧数值排排除掉 ⇒ 玩家在两处都看不到属性。
    /// 且它早就跟**被动槽上限判定**（<c>UpgradeEffectManager.PassiveSlotUserCount</c>，只数机制型被动）脱节了。
    ///
    /// ⚠️ **public**：<c>BuffDisplayPanel</c> 要用**同一个判定**来决定
    /// 「这条是不是已经在右下角显示了」（避免两边都显示 / 两边都不显示）。
    /// </summary>
    public static bool ShowsInPassiveRow(UpgradeDefinition def)
    {
        if (def == null) return false;
        if (def.category == UpgradeCategory.Item) return false;
        if (def.category == UpgradeCategory.ActiveSkill) return false;
        return string.IsNullOrEmpty(def.statId);
    }

    private void FillSlots()
    {
        int slotCount = _slots != null ? _slots.Length : 0;
        int shown = Mathf.Min(_passives.Count, slotCount);

        for (int i = 0; i < slotCount; i++)
        {
            var slot = _slots[i];
            if (slot == null || slot.root == null) continue;

            if (i < shown)
            {
                var passive = _passives[i];
                slot.root.gameObject.SetActive(true);
                if (slot.icon != null)
                {
                    slot.icon.sprite = passive.definition.icon;
                    // 空槽时图标是**禁用**的（黄色棱形槽的惯例）→ 填内容时必须打开；
                    // 但个别升级资产**没配图标** → 直接开 Image 会画成白框，宁可留空。
                    slot.icon.enabled = passive.definition.icon != null;
                }
                if (slot.level != null)
                {
                    // 等级 1 不标（默认就 1 级），升过级才显示数字（用菱形槽同款的角标）
                    if (passive.currentLevel > 1) slot.level.ShowNumber(passive.currentLevel);
                    else slot.level.Clear();
                }
            }
            else
            {
                slot.root.gameObject.SetActive(false);
            }
        }

        if (_overflow != null)
        {
            int hidden = _passives.Count - shown;
            _overflow.text = hidden > 0 ? "+" + hidden : string.Empty;
            _overflow.gameObject.SetActive(hidden > 0);
        }
    }
}

