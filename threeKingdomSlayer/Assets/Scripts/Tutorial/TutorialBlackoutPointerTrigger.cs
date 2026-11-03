using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 路牌按钮的「按下即遮罩」触发器（挂在路线系统运行时生成的路牌按钮上）。
///
/// 存在原因（2026-11-01 用户反馈）：节点移动的黑幕应该是**跟着玩家的点击**出现，
/// 而不是等路线阶段切过去之后才出现 —— 否则看起来就是「移动的时候突然黑屏」。
/// 所以在按钮被按下的那一帧就先开始遮罩。
/// </summary>
public class TutorialBlackoutPointerTrigger : MonoBehaviour, IPointerDownHandler
{
    [Tooltip("要通知的黑幕（留空则自动找场景里的）")]
    public TutorialRouteBlackout blackout;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (blackout == null) blackout = FindObjectOfType<TutorialRouteBlackout>();
        if (blackout != null) blackout.ShowNow();
    }
}
