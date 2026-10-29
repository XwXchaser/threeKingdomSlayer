using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
    /// InputManager 的**坐标映射与输入过滤**：屏幕坐标→阵型列、QTE 输入、教学手势过滤。
    ///
    /// 按机制从 <c>InputManager.cs</c> 拆出（2026-10-22，partial 分文件）。
    /// partial class 与 InputManager.cs **同类型**，行为完全一致，只为降低单文件体量与阅读成本。
    /// </summary>

public partial class InputManager : MonoBehaviour
{
    #region 屏幕坐标映射

    public int GetPierceColumnFromScreenPosition(Vector2 screenPos)
    {
        return GetStabColumnFromScreenPosition(screenPos);
    }

    private int GetStabColumnFromScreenPosition(Vector2 screenPos)
    {
        int targetedColumn = GetColumnFromScreenPosition(screenPos);
        return targetedColumn >= 0 ? targetedColumn : FallbackGetColumn(screenPos);
    }

    /// <summary>
    /// 根据屏幕X坐标映射到列索引（0~4）
    /// 将敌人投影到屏幕空间，直接比较屏幕X距离。
    /// 如果最近匹配超过半列宽度，返回 -1 阻断攻击（防止空列自动跳转到邻列）。
    /// </summary>
    private int GetColumnFromScreenPosition(Vector2 screenPos)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return FallbackGetColumn(screenPos);

        int bestColumn = -1;
        float bestDistance = float.MaxValue;

        if (AttackSystem.Instance != null && AttackSystem.Instance.columnManager != null)
        {
            for (int col = 0; col < 5; col++)
            {
                Enemy frontEnemy = AttackSystem.Instance.columnManager.GetFrontEnemy(col);
                if (frontEnemy != null && frontEnemy.state != EnemyState.Dead)
                {
                    Vector3 enemyScreenPos = mainCamera.WorldToScreenPoint(frontEnemy.transform.position);
                    float dist = Mathf.Abs(enemyScreenPos.x - screenPos.x);
                    if (dist < bestDistance)
                    {
                        bestDistance = dist;
                        bestColumn = col;
                    }
                }
            }
        }

        // 阈值：半列宽度。超过此距离说明点击位置与存活的敌人列偏差过大，阻断攻击
        float halfColumnWidth = Screen.width / 10f;
        if (bestColumn >= 0 && bestDistance <= halfColumnWidth)
            return bestColumn;

        // 没有足够近的存活敌人 → 返回 -1，攻击不会触发
        return -1;
    }

    /// <summary>
    /// 回退方案：将屏幕X坐标均匀映射到5列
    /// </summary>
    private int FallbackGetColumn(Vector2 screenPos)
    {
        float screenWidth = Screen.width;
        float normalizedX = screenPos.x / screenWidth; // 0~1
        int column = Mathf.FloorToInt(normalizedX * 5f);
        return Mathf.Clamp(column, 0, 4);
    }

    #endregion

    #region QTE 输入

    private QTEController _cachedQTEController;

    private bool IsAnyQTEActive()
    {
        if (_cachedQTEController == null)
            _cachedQTEController = FindObjectOfType<QTEController>();
        return _cachedQTEController != null && _cachedQTEController.IsQTEActive;
    }

    private bool TryConsumeStrictQTEInput(Vector2 releasePos, bool isSwiped, float swipeDistance, float pressDuration)
    {
        if (_cachedQTEController == null)
            _cachedQTEController = FindObjectOfType<QTEController>();
        if (_cachedQTEController == null || !_cachedQTEController.IsStrictInputActive)
            return false;

        return _cachedQTEController.TryStrictInput(touchStartPos, releasePos, isSwiped, swipeDistance, pressDuration);
    }

    /// <summary>
    /// 尝试将当前手势作为 QTE 输入消费
    /// 若 QTE 活跃且手势匹配成功，返回 true（阻止普通攻击处理）
    /// </summary>
    private bool TryConsumeQTEInput(Vector2 releasePos, bool isSwiped, float swipeDistance, float pressDuration)
    {
        if (_cachedQTEController == null)
            _cachedQTEController = FindObjectOfType<QTEController>();
        if (_cachedQTEController == null || !_cachedQTEController.IsQTEActive)
            return false;

        if (isSwiped)
        {
            Vector2 direction = releasePos - touchStartPos;
            float swipeSpeed = swipeDistance / Mathf.Max(pressDuration, 0.001f);
            if (_cachedQTEController.TryQTESwipe(touchStartPos, direction, swipeSpeed, releasePos))
            {
                DebugLog.Info($"[InputManager] QTE 划动成功 speed={swipeSpeed:F0}");
                return true;
            }
        }
        else
        {
            if (_cachedQTEController.TryQTEClick(releasePos))
            {
                DebugLog.Info("[InputManager] QTE 点击成功");
                return true;
            }
        }

        return false;
    }

    #endregion

    #region 教学手势过滤

    /// <summary>教学拍是否允许「点击」成立：只教点按时，滑动/蓄力都不算。</summary>
    private bool TutorialAllowsTapGesture(int chargeLevel)
    {
        if (tutorialGestureTarget == TutorialGestureTarget.None) return true;
        return tutorialGestureTarget == TutorialGestureTarget.Tap && chargeLevel < 1;
    }

    /// <summary>教学拍是否允许这次滑动成立：横滑教学只收未蓄力横滑，上挑教学只收已蓄力竖滑，招架教学只收未蓄力竖滑。</summary>
    private bool TutorialAllowsSwipeGesture(MoveGesture directionGesture, int chargeLevel)
    {
        switch (tutorialGestureTarget)
        {
            case TutorialGestureTarget.None:
                return true;
            case TutorialGestureTarget.SwipeHorizontal:
                return chargeLevel < 1 && directionGesture == MoveGesture.SwipeHorizontal;
            case TutorialGestureTarget.ChargedVertical:
                return chargeLevel >= 1 && directionGesture == MoveGesture.SwipeVertical;
            case TutorialGestureTarget.ParryVertical:
                // 招架 = 不蓄力的竖滑（与游戏映射一致：ResolveAttackType(SwipeVertical, false) == Parry）
                return chargeLevel < 1 && directionGesture == MoveGesture.SwipeVertical;
            default:
                return false;   // 目标是点按时，任何滑动都不成立
        }
    }

    #endregion
}