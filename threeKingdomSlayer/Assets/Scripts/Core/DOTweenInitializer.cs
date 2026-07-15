using UnityEngine;
using DG.Tweening;

/// <summary>
/// 在游戏启动时初始化 DOTween 容量，防止高频率战斗中 Tween 创建失败导致特效残留。
/// </summary>
public static class DOTweenInitializer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        DOTween.SetTweensCapacity(500, 100);
        // 战斗 Tween 默认使用游戏时间，确保升级弹窗和暂停菜单冻结战斗逻辑。
        // 需要在暂停期间运行的 UI 动画必须在调用处显式 SetUpdate(UpdateType.Normal, true)。
        DOTween.defaultTimeScaleIndependent = false;
    }
}
