using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// **主武器配置**（2026-11-24）：一把武器 = 一套战斗配置 + 一张招式表。
///
/// 口径（`design/main-weapon-roster.md` / `design/gameplay-progression-loop.md` §3.1）：
///   · 主武器承载玩家主要招式与战斗身份，**回局外装备**，不在本局自由切换；
///   · 武器与招式**不进天赋树、不进局内三选一**；
///   · 一部分武器初始就有，其余靠 Boss / 固定宝箱永久获得。
///
/// 招式解锁/升级（本文件 `WeaponMoveEntry`）：
///   · 每个招式可以配 **解锁价** 与 **升级价 / 每级伤害加成**；
///   · 解锁与等级都写进存档（见 `WeaponSaveSection`），**不在天赋树里卖**。
/// </summary>
[CreateAssetMenu(fileName = "NewMainWeapon", menuName = "一夫当关/主武器")]
public class MainWeaponConfig : ScriptableObject
{
    [Header("身份")]
    [Tooltip("稳定 id（写存档 / 掉落表用）。留空则用资产名。")]
    public string weaponId = "";
    public string displayName = "";
    [TextArea(2, 4)] public string description = "";
    public Sprite icon;

    [Header("战斗配置")]
    [Tooltip("装备该武器时套用的武将配置（招式表 / 技能表 / 大招 / 槽位）。\n" +
             "留空 = 该武器**尚未实装战斗配置**，UI 会标「未实装」且不允许装备。")]
    public HeroConfig heroConfig;

    [Header("获取")]
    [Tooltip("初始即拥有。取消勾选 = 只能靠 Boss / 固定宝箱永久解锁（掉落线后续实装）。")]
    public bool unlockedByDefault = true;

    [Header("招式")]
    [Tooltip("这把武器的招式表。顺序 = UI 里的显示顺序；解锁/升级价都在每条 entry 上。")]
    public List<WeaponMoveEntry> moves = new List<WeaponMoveEntry>();

    public string EffectiveWeaponId => string.IsNullOrEmpty(weaponId) ? name : weaponId;

    /// <summary>按招式资产找 entry（找不到返回 null = 该招式不归任何武器管）。</summary>
    public WeaponMoveEntry FindEntry(AttackSkillConfig move)
    {
        if (move == null || moves == null) return null;
        for (int i = 0; i < moves.Count; i++)
        {
            var e = moves[i];
            if (e != null && e.move == move) return e;
        }
        return null;
    }

    /// <summary>按招式 id 找 entry（招式树的前置解析用）。</summary>
    public WeaponMoveEntry FindEntryById(string moveId)
    {
        if (string.IsNullOrEmpty(moveId) || moves == null) return null;
        for (int i = 0; i < moves.Count; i++)
        {
            var e = moves[i];
            if (e != null && e.EffectiveMoveId == moveId) return e;
        }
        return null;
    }
}

/// <summary>
/// **一条招式**（属于某把主武器）：解锁价 + 升级价 + 每级伤害加成。
/// </summary>
[System.Serializable]
public class WeaponMoveEntry
{
    [Header("招式")]
    public AttackSkillConfig move;
    [Tooltip("稳定 id（写存档用）。留空则用招式资产名。")]
    public string moveId = "";
    public string displayName = "";
    [TextArea(1, 3)] public string description = "";
    public Sprite icon;

    [Header("解锁")]
    [Tooltip("初始即解锁。取消勾选 = 花铜钱解锁（解锁后永久有效）。")]
    public bool unlockedByDefault = false;
    [Min(0)] public int unlockCoinCost = 30;

    [Header("招式树（多线）")]
    [Tooltip("前置招式 id（要**全部**解锁才能解锁本条）。留空 = 该支线的起点。\n" +
             "节点内容只能是**招式/派生招式**，不做「某招式 +10% 伤害」这类纯数值节点。")]
    public List<string> prerequisiteIds = new List<string>();
    [Tooltip("招式树里的位置（面板像素，原点在面板左下角，x 向右、y 向上）。只影响显示。")]
    public Vector2 treePosition = Vector2.zero;

    [Header("解锁引导（教手势用）")]
    [Tooltip("配了才教：解锁后第一次进关弹一次「手势示范 + 台词」。\n" +
             "从旧天赋节点 `talent_move_*` 搬过来（那些节点随 Moves 树移出）。")]
    public MoveGuideGesture moveGuideGesture = MoveGuideGesture.None;
    public DialogueEventData moveGuideDialogue;
    public DialogueEventData moveGuidePhase2Dialogue;
    public DialogueEventData moveGuidePhase3Dialogue;

    /// <summary>写存档用的 id（留空时回落资产名）。</summary>
    public string EffectiveMoveId
    {
        get
        {
            if (!string.IsNullOrEmpty(moveId)) return moveId;
            return move != null ? move.name : "";
        }
    }
}

/// <summary>招式解锁结果（UI 直接按这个显示提示）。</summary>
public enum WeaponMoveActionResult
{
    Success,
    AlreadyUnlocked,
    PrerequisiteLocked,
    NotEnoughCoins,
    Invalid,
}
