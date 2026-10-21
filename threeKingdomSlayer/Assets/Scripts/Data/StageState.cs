/// <summary>
/// 关卡状态枚举。
///
/// 2026-10-22 从 <c>PlayerState.cs</c> 抽出，搬进 <c>Locus.Data</c> 程序集：
/// 状态枚举属于「契约层」，必须待在叶子程序集里（任何上层程序集都能引用它），
/// 否则将来的 <c>Locus.Combat</c> / <c>Locus.Route</c> 无法在不反向依赖 Application-CSharp 的前提下使用它。
/// 枚举值按 int 序列化，搬家不影响存档与场景数据。
/// </summary>
public enum StageState
{
    None,
    Starting,
    InProgress,
    Victory,
    Defeat
}
