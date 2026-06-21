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
        // 全局设置：所有 DOTween 动画默认不受 Time.timeScale 影响
        // 防止 UpgradeChoiceManager 设 timeScale=0 时特效/弹丸残留
        // 暂停菜单通过 DOTween.PauseAll()/PlayAll() 单独控制
        DOTween.defaultTimeScaleIndependent = true;
    }
}
