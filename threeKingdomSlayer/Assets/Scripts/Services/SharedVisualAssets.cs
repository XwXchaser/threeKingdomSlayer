using UnityEngine;

/// <summary>
/// **共享表现素材登记表**：把「项目级共享的材质 / 贴图」从「运行时按路径 Resources 加载」
/// 改成「上层用 Inspector 引用登记进来，下层按需取用」。
///
/// 为什么这么做（2026-10-23，幽灵引用清零）：
/// 之前武器方向性拖影材质是 <c>Resources.Load&lt;Material&gt;("Materials/...")</c> 拿的 ——
/// 数据来源在 Inspector 里看不见（违反 <c>design/anti-ghost-reference.md</c>），
/// 而且 Resources 目录会强制整棵依赖树进包。
///
/// 规则：
///   • **登记在持有 Inspector 引用的那一层**（现在是 <c>AttackSystem</c>，玩家的攻击表现拥有者）✅；
///   • **取用在表现层**（<c>WeaponMotionBlurController</c>）✅ —— 依赖方向朝下 ✅；
///   • 取不到时按「没有该表现」安全降级 ✅（与旧的「材质缺失」分支行为一致 ✅）。
/// </summary>
public static class SharedVisualAssets
{
    /// <summary>
    /// 武器方向性像素模糊材质（枪身 / 刀光的运动拖影）。
    /// 为 null 表示当前场景没有登记 → 拖影表现整体跳过 ✅（不影响伤害 / 命中判定 ✅）。
    /// </summary>
    public static Material DirectionalPixelBlur { get; set; }

    /// <summary>清空登记（场景重载 / 退出战斗时用）。</summary>
    public static void Clear()
    {
        DirectionalPixelBlur = null;
    }
}
