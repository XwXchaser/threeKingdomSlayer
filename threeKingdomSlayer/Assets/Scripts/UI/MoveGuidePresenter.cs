using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// **招式解锁引导的呈现**（2026-11-03）。
///
/// 复用两件现成的，不新建 UI：
///   • **手势示范** → 场景里的 <see cref="TutorialGestureHint"/>（手指 + 拖尾 + 压暗 ✅，全是代码驱动 ✅）；
///   • **一行台词** → 现成的对话层（<c>CombatPresenters.Dialogue</c> ✅ 自带压暗 + 点击继续 ✅）。
///
/// 只做「演示」不做「教学判定」：玩家点了继续就继续打 ✅（不像教学关那样必须打出这一招才算过 ✅）。
/// </summary>
public class MoveGuidePresenter : MonoBehaviour, IMoveGuidePresenter
{
    [Header("手势示范")]
    [Tooltip("场景里的手势提示组件（手指 + 拖尾 + 压暗层）")]
    public TutorialGestureHint gestureHint;

    [Tooltip("压暗层透明度（0~1）。对话层自己也会压暗 → 提示层会主动避让 ✅（不会两层叠加）。")]
    [Range(0f, 0.9f)] public float dimAlpha = 0.5f;

    [Tooltip("横向划动的起点（屏幕归一化 0~1，左下角为原点）")]
    public Vector2 swipeFrom01 = new Vector2(0.2f, 0.58f);
    [Tooltip("横向划动的终点（屏幕归一化 0~1）")]
    public Vector2 swipeTo01 = new Vector2(0.8f, 0.58f);

    [Header("节奏")]
    [Tooltip("手势至少播多久（秒）—— 台词被秒关时也保证看得到完整示范")]
    public float minGestureSeconds = 1.2f;
    [Tooltip("最多等玩家多久（秒）—— 超过就当玩家已读，继续开打（防止把战斗卡死）")]
    public float maxWaitSeconds = 20f;

    public bool IsShowing { get; private set; }

    private void Awake()
    {
        // 登记到呈现层端口（流程层只通过端口要引导 ✅）。
        CombatPresenters.MoveGuide = this;
    }

    private void OnDestroy()
    {
        if (ReferenceEquals(CombatPresenters.MoveGuide, this))
            CombatPresenters.MoveGuide = null;
    }

    public void ShowMoveGuide(MoveGuideRequest request, Action onDone)
    {
        if (request == null)
        {
            onDone?.Invoke();
            return;
        }
        StartCoroutine(Run(request, onDone));
    }

    private IEnumerator Run(MoveGuideRequest request, Action onDone)
    {
        IsShowing = true;

        // 1) 手势示范（自己的计时走 unscaled → 对话把游戏暂停时照样播 ✅）。
        PlayGesture(request.gesture);

        // 2) 一行台词（走现成对话层：压暗 + 点击继续 ✅）。
        //    forceReplay = true：引导的「只教一次」由存档管 ✅，这里要保证每次触发都演 ✅。
        var dialogue = CombatPresenters.Dialogue;
        bool queued = dialogue != null && request.dialogue != null
                      && dialogue.QueueEventAndNotify(request.dialogue, true);
        if (!queued) Debug.Log($"[MoveGuide] 无台词可播（dialogue={(request.dialogue != null)}），只演手势");

        // 3) 等台词演完（且手势至少播满 minGestureSeconds）。
        float waited = 0f;
        while (waited < maxWaitSeconds)
        {
            waited += Time.unscaledDeltaTime;
            bool dialogueBusy = dialogue != null && dialogue.IsBusy;
            if (!dialogueBusy && waited >= minGestureSeconds) break;
            yield return null;
        }

        // 超时兜底：台词可能还挂在屏幕上（玩家一直没点）→ 收掉它，
        // 不然气泡会盖在战斗上（实测过：超时后气泡残留）。
        if (dialogue != null && dialogue.IsBusy)
        {
            var view = BattleDialogueView.Instance;
            if (view != null && view.IsShowing) view.Skip();
        }

        gestureHint?.Hide();
        gestureHint?.HideDim();
        IsShowing = false;
        onDone?.Invoke();
    }

    private void PlayGesture(MoveGuideGesture gesture)
    {
        if (gestureHint == null) return;

        Vector2 from = ScreenPoint(swipeFrom01);
        Vector2 to = ScreenPoint(swipeTo01);
        switch (gesture)
        {
            case MoveGuideGesture.SwipeHorizontal:
                gestureHint.PlaySwipe(from, to);
                break;
            case MoveGuideGesture.SwipeVertical:
                gestureHint.PlaySwipe(to, from);
                break;
            case MoveGuideGesture.Hold:
                gestureHint.PlayHold(from);
                break;
            case MoveGuideGesture.Tap:
                gestureHint.PlayTap(from);
                break;
        }
        gestureHint.ShowDim(dimAlpha);
    }

    /// <summary>屏幕归一化坐标 → 屏幕像素（与教学关的 from01/to01 同一口径 ✅）。</summary>
    private static Vector2 ScreenPoint(Vector2 normalized01)
    {
        return new Vector2(normalized01.x * Screen.width, normalized01.y * Screen.height);
    }
}
