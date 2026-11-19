/// <summary>
/// **可选**扩展接口：让三选一弹窗能区分「属性强化」与「装备掉落」（2026-11-17）。
///
/// 为什么单独开一个接口、而不是改 <see cref="IUpgradeChoiceView"/>：
/// <c>IUpgradeChoiceView.ShowChoices</c> 还有别的调用方
/// （如 <c>VisualLibraryCapture</c> 直接调它取截图），改签名会波及它们。
/// ⇒ 弹窗**实现**这个接口就能显示标题；没实现也照常工作（只是不显示标题）。
/// </summary>
public interface IUpgradeChoiceKindView
{
    /// <summary>告诉弹窗「这次弹的是哪一类奖励」（用于标题 / 配色）。</summary>
    void SetChoiceKind(UpgradeRewardKind kind);
}
