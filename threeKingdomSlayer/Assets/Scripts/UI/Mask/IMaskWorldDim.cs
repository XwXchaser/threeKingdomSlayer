/// <summary>
/// **世界侧压暗后端**（遮罩系统的另一半）。
///
/// <para>为什么需要它：战场是<b>透视相机 + 世界物件</b>（长枪是 MeshRenderer、敌人是精灵），
/// <b>任何 UI 画布都必然盖在角色上面</b>，层级无解 →
/// 「背景压暗、角色与武器保持亮」只能靠给背景渲染器乘亮度系数实现。</para>
///
/// <para>所以遮罩系统是<b>同一套规则的两个落地</b>：
/// UI 侧 = <see cref="UiMask"/>；世界侧 = 本接口。
/// 规则的 <see cref="MaskRule.DimWorld"/> 为 true 时，服务会去叫后端压暗背景。</para>
///
/// <para>接口放在 Locus.UI 程序集 → 具体实现可以放在任何程序集（教学用 <c>TutorialSceneDim</c>）。</para>
/// </summary>
public interface IMaskWorldDim
{
    /// <summary>开 / 关背景压暗。<paramref name="brightness"/> = 压暗后保留的亮度（1 = 不压暗）。</summary>
    void SetDimmed(bool dimmed, float brightness);
}
