using UnityEngine;

/// <summary>
/// 「可被点中的补给物件」接口。
///
/// 放在数据层的原因：输入层（Locus.Flow）、招式层（Locus.Combat）、道具层（Locus.Combat）
/// 三边都要用它，而具体的 <c>BreakableProp</c> 在 Combat 层 —— Data 不能反向引用 Combat，
/// 所以这里只定义契约，实现放在 Combat。
/// </summary>
public interface ISupplyPropTarget
{
    /// <summary>当前是否可被打：未破坏 + 在可交互距离内 + 可见 + 属于当前可交互的层。</summary>
    bool IsSupplyInteractable { get; }

    /// <summary>被戳中：走道具自己的耐久 / 破坏 / 奖励流程。</summary>
    void ApplySupplyHit(AttackType attackType, float damage);
}
