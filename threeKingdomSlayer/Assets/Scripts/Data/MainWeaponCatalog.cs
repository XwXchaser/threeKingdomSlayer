using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// **主武器目录**（2026-11-24）：全部武器资产的唯一入口。
///
/// 为什么要有目录：装备状态只存 `weaponId`（存档字段 `equippedWeaponId`），
/// 运行时必须能把 id 解析回资产。按项目规矩**不做 Resources.Load / 字符串查表**，
/// 目录由 `HeroConfig.weaponCatalog` 在 Inspector 上直接指过来（可追踪）。
/// </summary>
[CreateAssetMenu(fileName = "MainWeaponCatalog", menuName = "一夫当关/主武器目录")]
public class MainWeaponCatalog : ScriptableObject
{
    [Tooltip("全部武器（顺序 = UI 里的显示顺序）。未实装战斗配置的也放进来，UI 标「未实装」。")]
    public List<MainWeaponConfig> weapons = new List<MainWeaponConfig>();

    [Tooltip("没有存档 / 存档 id 失效时用哪把（留空则用列表第一个）。")]
    public string defaultWeaponId = "";

    public MainWeaponConfig Find(string weaponId)
    {
        if (string.IsNullOrEmpty(weaponId) || weapons == null) return null;
        for (int i = 0; i < weapons.Count; i++)
        {
            var w = weapons[i];
            if (w != null && w.EffectiveWeaponId == weaponId) return w;
        }
        return null;
    }

    public MainWeaponConfig Default
    {
        get
        {
            var byId = Find(defaultWeaponId);
            if (byId != null) return byId;
            if (weapons == null) return null;
            for (int i = 0; i < weapons.Count; i++)
                if (weapons[i] != null) return weapons[i];
            return null;
        }
    }
}
