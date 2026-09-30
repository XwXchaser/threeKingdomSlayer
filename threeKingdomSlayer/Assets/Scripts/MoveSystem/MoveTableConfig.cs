using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 每武将一份的招式表：中立态入口 + 上下文状态覆盖 + 默认窗口参数。
/// 未配置（或 roots 为空）时，招式状态机进入直通模式，行为与改造前一致。
/// </summary>
[CreateAssetMenu(fileName = "MoveTableConfig", menuName = "一夫当关/招式表")]
public class MoveTableConfig : ScriptableObject
{
    [Header("中立态入口")]
    [Tooltip("中立态下各手势的入口招式。未配置的手势回落到该手势的默认攻击类型，保证招式表未填完时仍可游戏")]
    public List<MoveEntry> roots = new List<MoveEntry>();

    [Header("上下文状态")]
    [Tooltip("临时状态（如招架成功）下替换/追加的中立态入口。第一版仅预留，判定入口后续接入")]
    public List<MoveStateEntry> states = new List<MoveStateEntry>();

    [Header("默认窗口派生")]
    [Tooltip("保守模式（默认关闭）：开启后，连显式配置的窗口起点也会被抬到命中窗口结束之后。\n关闭时：显式窗口原样生效，仅【按招式类型派生的默认窗口】遵守「不早于命中窗口结束」。")]
    public bool clampWindowAfterHitWindow = false;

    [Tooltip("窗口最小绝对时长（秒），防止高攻速下窗口趋近 0")]
    public float minWindowSeconds = 0.06f;

    /// <summary>按手势查找中立态入口，未配置返回 null</summary>
    public MoveDefinition FindRoot(MoveGesture gesture)
    {
        if (roots == null) return null;
        for (int i = 0; i < roots.Count; i++)
        {
            if (roots[i] != null && roots[i].gesture == gesture && roots[i].move != null)
                return roots[i].move;
        }
        return null;
    }

    /// <summary>按状态标识查找状态覆盖表，未配置返回 null</summary>
    public MoveStateEntry FindState(string stateId)
    {
        if (states == null || string.IsNullOrEmpty(stateId)) return null;
        for (int i = 0; i < states.Count; i++)
        {
            if (states[i] != null && states[i].stateId == stateId)
                return states[i];
        }
        return null;
    }

    /// <summary>
    /// 按招式类型派生默认接续窗口（设计文档 3.7）。
    /// Stab 取命中窗口结束；Slash 取横扫后段；其余取通用兜底区间。
    /// </summary>
    public static void GetDefaultWindow(AttackType type, out float start01, out float end01)
    {
        switch (type)
        {
            case AttackType.Stab:
                start01 = 0.48f; end01 = 0.95f; break;
            case AttackType.Slash:
                start01 = 0.65f; end01 = 0.92f; break;
            case AttackType.Pierce:
            case AttackType.Sweep:
                start01 = 0.60f; end01 = 0.95f; break;
            case AttackType.Launch:
                start01 = 0.70f; end01 = 0.95f; break;
            case AttackType.Parry:
            default:
                start01 = 0.60f; end01 = 0.95f; break;
        }
    }
}

[System.Serializable]
public class MoveEntry
{
    public MoveGesture gesture;
    public MoveDefinition move;
}

[System.Serializable]
public class MoveStateEntry
{
    [Tooltip("状态标识，例如 parry_success")]
    public string stateId;
    public List<MoveEntry> roots = new List<MoveEntry>();
}
