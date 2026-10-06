/// <summary>
/// Enemy 受击动画选择。
/// Left/Right 表示要播放的受击动画方向（HitLeft / HitRight），不是攻击受力向量。
/// </summary>
public enum HitReactionDirection
{
    None = 0,
    Random = 1,
    Front = 2,
    Left = 3,
    Right = 4
}
