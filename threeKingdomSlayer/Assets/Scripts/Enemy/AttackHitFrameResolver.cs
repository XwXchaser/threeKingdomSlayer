using System;
using UnityEngine;

/// <summary>
/// 攻击命中帧解析工具（运行时安全，不使用 UnityEditor API）。
///
/// 运行时出伤判定按「屏幕上的精灵名」匹配（见 Enemy.UpdateAttackHitFrames），
/// 所以这里只负责两件事：把「攻击步骤的 trigger」解析到攻击 clip、以及判断某 clip 是否属于攻击动作。
/// 编辑器侧的命中帧校验（读关键帧时间）见 Assets/Scripts/Editor/AttackHitFrameValidator.cs。
/// </summary>
public static class AttackHitFrameResolver
{
    /// <summary>
    /// 按命名约定解析该步骤会播放的攻击 clip：
    ///   trigger == "CAttack" → 名字含 "_CAttack" 的 clip
    ///   其它（含默认 "Attack"）→ 名字含 "_Attack" 且不含 "_CAttack" 的 clip
    /// </summary>
    public static AnimationClip ResolveClipForTrigger(RuntimeAnimatorController controller, string trigger)
    {
        if (controller == null) return null;

        AnimationClip normalClip = null;
        bool wantCAttack = trigger == "CAttack";
        foreach (var clip in controller.animationClips)
        {
            if (clip == null) continue;

            if (wantCAttack)
            {
                if (clip.name.IndexOf("_CAttack", StringComparison.Ordinal) >= 0)
                    return clip;
            }
            else if (normalClip == null
                     && clip.name.IndexOf("_Attack", StringComparison.Ordinal) >= 0
                     && clip.name.IndexOf("_CAttack", StringComparison.Ordinal) < 0)
            {
                normalClip = clip;
            }
        }

        return normalClip;
    }

    /// <summary>
    /// 同一攻击家族（_Attack 或 _CAttack）里的 clip 数量；>1 表示多段连招（如 Enemy_103_CAttack1/2/3）。
    /// 多段连招的整段时长无法从单个 clip 推导，需要单独约定。
    /// </summary>
    public static int CountFamilyClips(RuntimeAnimatorController controller, AnimationClip clip)
    {
        if (controller == null || clip == null) return 0;

        bool cAttack = clip.name.IndexOf("_CAttack", StringComparison.Ordinal) >= 0;
        int count = 0;
        foreach (var other in controller.animationClips)
        {
            if (other == null) continue;
            bool sameFamily = cAttack
                ? other.name.IndexOf("_CAttack", StringComparison.Ordinal) >= 0
                : other.name.IndexOf("_Attack", StringComparison.Ordinal) >= 0
                  && other.name.IndexOf("_CAttack", StringComparison.Ordinal) < 0;
            if (sameFamily) count++;
        }
        return count;
    }

    /// <summary>clip 名是否属于攻击动作（命中帧只在攻击动作上结算，用于识别被打断）。</summary>
    public static bool IsAttackClipName(string clipName)
    {
        if (string.IsNullOrEmpty(clipName)) return false;
        return clipName.IndexOf("_Attack", StringComparison.Ordinal) >= 0
            || clipName.IndexOf("_CAttack", StringComparison.Ordinal) >= 0;
    }

    /// <summary>声明的命中帧精灵名与当前屏幕上的精灵名是否匹配（精确优先，其次后缀）。</summary>
    public static bool MatchesSprite(string declared, string current)
    {
        if (string.IsNullOrEmpty(declared) || string.IsNullOrEmpty(current)) return false;
        if (declared == current) return true;
        return current.EndsWith(declared, StringComparison.Ordinal);
    }
}
