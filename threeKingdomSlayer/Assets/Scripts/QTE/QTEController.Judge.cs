using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
    /// QTEController 的**QTE 判定**：输入窗口、输入规则、判定结果与命中反馈。
    ///
    /// 按机制从 <c>QTEController.cs</c> 拆出（2026-10-22，partial 分文件）。
    /// partial class 与 QTEController.cs **同类型**，行为完全一致，只为降低单文件体量与阅读成本。
    /// </summary>

public partial class QTEController : MonoBehaviour
{
    #region QTE 判定

    private void UpdatePerforming()
    {
        _performingTimer += Time.deltaTime;

        if (!_qtePhaseStarted)
        {
            bool hasProjectile = _activeProjectile != null;
            bool multiPhaseReady = _currentAttack.UseMultiPhaseAnimation && _performingTimer >= _currentAttack.EffectiveLeadTime;
            bool singlePhaseReady = !_currentAttack.UseMultiPhaseAnimation && !hasProjectile && _performingTimer >= _currentAttack.EffectiveLeadTime;
            if (multiPhaseReady || singlePhaseReady)
                StartQTEPhase();
            return;
        }

        _qtePhaseTimer += Time.deltaTime;
        if (_currentQTEIndex >= _activeQTEs.Count)
        {
            if (CanEndQTEPhase())
                StartQTEEndingPhase();
            return;
        }

        var qte = _activeQTEs[_currentQTEIndex];
        if (qte.indicator == null && _qtePhaseTimer >= _nextQTEStartTime)
        {
            if (!qte.earlyInputFailed)
                SpawnQTEIndicator(qte);

            if (UsesStrictInputRule && qteDisplay != null)
            {
                // 落位前仅用于阶段2失败的预计结束时刻；实际落位时会以真实时刻重设完整窗口。
                qte.warningEndTime = _qtePhaseTimer + qteDisplay.slideInDuration;
                qte.judgeEndTime = qte.warningEndTime + qte.config.judgeWindow;
                qte.judgmentStarted = false;
            }
            else
            {
                // V1 保持图案生成即进入判定。
                qte.warningEndTime = _qtePhaseTimer;
                qte.judgeEndTime = qte.warningEndTime + qte.config.judgeWindow;
                qte.judgmentStarted = true;
            }

            // 严格模式的箭矢飞行覆盖图案入场和完整判定窗口。
            SpawnArrowWaveForSlot(_currentQTEIndex, qte.judgeEndTime - _qtePhaseTimer);
            _state = QTEState.QTEJudging;

            if (qte.earlyInputFailed)
            {
                DebugLog.Info($"[QTEController] 提前失败段按原时序演出 idx={_currentQTEIndex}, end={qte.judgeEndTime:F2}");
            }
        }
    }

    private void UpdateJudging()
    {
        _qtePhaseTimer += Time.deltaTime;
        if (_currentQTEIndex >= _activeQTEs.Count)
        {
            if (CanEndQTEPhase())
                StartQTEEndingPhase();
            return;
        }

        var qte = _activeQTEs[_currentQTEIndex];
        TryOpenStrictJudgmentWindow(qte);

        if (!_judgingSpeedApplied && _currentAttack != null && _currentAttack.UseBranchedAnimation && _animator != null && qte.judgmentStarted)
        {
            _judgingSpeedApplied = true;
            float happenLength = _currentAttack.animationLoopClip != null ? _currentAttack.animationLoopClip.length : 0.5f;
            _animator.speed = Mathf.Clamp(happenLength / qte.config.judgeWindow, 0.05f, 1f);
        }

        CheckJudgmentStart();
        if (qte.earlyInputFailed)
        {
            if (_qtePhaseTimer > qte.judgeEndTime)
                ResolveQTE(qte, false);
            return;
        }
        if (!qte.resolved && qte.IsExpired(_qtePhaseTimer))
            ResolveQTE(qte, false);
    }

    private void StartQTEEndingPhase()
    {
        _state = QTEState.QTEEnding;
        _endAnimTimer = 0f;

        // 判断玩家是否格挡成功（所有 slot 成功 = 格挡成功）
        bool playerBlocked = true;
        foreach (var qte in _activeQTEs)
        {
            if (!qte.resolved || !qte.success) { playerBlocked = false; break; }
        }

        float endClipLength = 0f;

        if (_currentAttack != null && _currentAttack.UseBranchedAnimation)
        {
            if (_animator == null) _animator = GetComponent<Animator>();
            if (_animator == null)
            {
                CompleteQTEAttack();
                return;
            }

            if (playerBlocked)
            {
                // 加速播完剩余 Happen 帧，然后切到 Blocked
                _animator.speed = 3f;
                float accelerateWindow = 0.12f; // ~3-4 帧的加速时间
                StartCoroutine(TriggerBlockedAfterAcceleration(accelerateWindow, _qteGeneration));
            }
            else
            {
                // 失败：立即切到 Hit
                _animator.speed = 1f;
                _animator.SetTrigger("QTEHit");
            }
            endClipLength = _currentAttack.branchedResultDuration > 0f ? _currentAttack.branchedResultDuration : float.MaxValue;
        }
        else
        {
            // 非 Branched 模式恢复速度
            if (_animator != null) _animator.speed = 1f;
            StopQTEAnimation();
            // BUG FIX: 单段模式未配置 animationEndClip 时回退 animationDuration，
            // 否则 endClipLength=0 导致 QTE 判定一结束就立即 CompleteQTEAttack，Boss 挥击演出被瞬间打断
            endClipLength = _currentAttack != null && _currentAttack.animationEndClip != null
                ? _currentAttack.animationEndClip.length
                : (_currentAttack != null ? _currentAttack.animationDuration : 0f);
        }

        if (endClipLength <= 0f)
        {
            CompleteQTEAttack();
            return;
        }

        DebugLog.Info($"[QTEController] 进入QTE收尾: resultDuration={endClipLength:F2}");
    }

    private System.Collections.IEnumerator TriggerBlockedAfterAcceleration(float delay, int generation)
    {
        yield return new WaitForSeconds(delay);
        if (generation != _qteGeneration || _state != QTEState.QTEEnding) yield break;
        if (_animator != null)
        {
            _animator.SetTrigger("QTEBlocked");
            _animator.speed = 1f;
        }
    }

    private void UpdateEnding()
    {
        _endAnimTimer += Time.deltaTime;

        float endClipLength;
        if (_currentAttack != null && _currentAttack.UseBranchedAnimation)
        {
            // 分支动画模式：动画链由 AnimationEvent 主导，branchedResultDuration 兜底
            endClipLength = _currentAttack.branchedResultDuration > 0f
                ? _currentAttack.branchedResultDuration
                : float.MaxValue;
        }
        else
        {
            // BUG FIX: 单段模式未配置 animationEndClip 时回退 animationDuration，
            // 与 StartQTEEndingPhase 保持一致，避免收尾时长不一致导致 QTE 判定结束立即完成
            endClipLength = _currentAttack != null && _currentAttack.animationEndClip != null
                ? _currentAttack.animationEndClip.length
                : (_currentAttack != null ? _currentAttack.animationDuration : 0f);
        }

        if (_endAnimTimer >= endClipLength)
        {
            DebugLog.Info($"[QTEController] QTE收尾兜底完成: elapsed={_endAnimTimer:F2}");
            CompleteQTEAttack();
        }
    }

    private bool TryOpenStrictJudgmentWindow(QTEInstance qte)
    {
        if (!UsesStrictInputRule || qte == null || qte.judgmentStarted || qte.indicator == null || qteDisplay == null)
            return qte != null && qte.judgmentStarted;
        if (!qteDisplay.HasLanded(qte.indicator))
            return false;

        qte.warningEndTime = _qtePhaseTimer;
        qte.judgeEndTime = _qtePhaseTimer + qte.config.judgeWindow;
        qte.judgmentStarted = true;
        DebugLog.Info($"[QTEController] 严格模式图案落位，开始判定 idx={_currentQTEIndex}, end={qte.judgeEndTime:F2}");
        return true;
    }

    public bool IsQTEActive
    {
        get
        {
            // QTE 阶段一旦开始，所有输入均被 QTE 系统拦截，防止误触普通攻击
            return _state == QTEState.QTEJudging || _state == QTEState.PerformingQTEAttack || _state == QTEState.QTEEnding;
        }
    }

    public bool TryStrictInput(Vector2 startScreenPos, Vector2 releaseScreenPos, bool isSwiped, float swipeDistance, float pressDuration)
    {
        // Strict 模式在整个 QTE 生命周期内吞掉战斗手势；收尾或已结算时不再回退为普通攻击。
        if (!IsStrictInputActive)
            return false;
        if (_currentQTEIndex >= _activeQTEs.Count)
            return true;

        var qte = _activeQTEs[_currentQTEIndex];
        if (qte.resolved || qte.earlyInputFailed) return true;

        if (!_qtePhaseStarted || qte.indicator == null)
            return true;

        if (!qte.judgmentStarted && !TryOpenStrictJudgmentWindow(qte))
        {
            qte.earlyInputFailed = true;
            DebugLog.Info($"[QTEController] 严格模式图案入场期间输入失败 idx={_currentQTEIndex}");
            ShowImmediateFailureFeedback(qte);
            return true;
        }

        if (!qte.IsInJudgeWindow(_qtePhaseTimer))
            return true;

        bool success = false;
        if (isSwiped)
        {
            Vector2 direction = releaseScreenPos - startScreenPos;
            float swipeSpeed = swipeDistance / Mathf.Max(pressDuration, 0.001f);
            success = TryQTESwipe(startScreenPos, direction, swipeSpeed, releaseScreenPos);
        }
        else
        {
            success = TryQTEClick(releaseScreenPos);
        }

        if (!success && !qte.resolved)
        {
            DebugLog.Info($"[QTEController] 严格模式错误输入锁定失败 idx={_currentQTEIndex}");
            qte.earlyInputFailed = true;
            ShowImmediateFailureFeedback(qte);
        }
        return true;
    }

    private static bool MatchesQTEGestureType(QTEConfig config, bool isSwiped)
    {
        return config != null && (config.qteType == QTEType.Swipe) == isSwiped;
    }

    private void ShowImmediateFailureFeedback(QTEInstance qte)
    {
        if (qte == null || qte.failureFeedbackShown) return;
        qte.failureFeedbackShown = true;

        if (qteDisplay == null)
            qteDisplay = QTEDisplay.Instance;
        if (qteDisplay == null) return;

        if (qte.indicator != null)
        {
            qteDisplay.FlashIndicatorFailure(qte.indicator);
            qteDisplay.ShowQTEResult(qte.indicator, false);
            qte.indicator = null;
        }
        else
        {
            qteDisplay.ShowResultFeedback(false);
        }
    }

    public bool TryQTEClick(Vector2 screenPos)
    {
        if (_state != QTEState.QTEJudging && _state != QTEState.PerformingQTEAttack) return false;
        if (!_qtePhaseStarted)
        {
            DebugLog.Info($"[QTEController] TryQTEClick 拒绝: _qtePhaseStarted=false");
            return false;
        }

        var qte = _activeQTEs[_currentQTEIndex];
        if (qte.resolved || qte.config.qteType != QTEType.Click) return false;
        if (!qte.IsInJudgeWindow(_qtePhaseTimer)) return false;

        if (IsClickInQTEArea(screenPos, qte))
        {
            DebugLog.Info($"[QTEController] 点击QTE成功 idx={_currentQTEIndex}");
            ResolveQTE(qte, true);
            return true;
        }
        DebugLog.Info($"[QTEController] TryQTEClick 未命中当前指示器 screenPos={screenPos}");
        return false;
    }

    public bool TryQTESwipe(Vector2 startScreenPos, Vector2 swipeDirection, float swipeSpeed, Vector2 releaseScreenPos)
    {
        if (_state != QTEState.QTEJudging && _state != QTEState.PerformingQTEAttack) return false;
        if (!_qtePhaseStarted) return false;

        var qte = _activeQTEs[_currentQTEIndex];
        if (qte.resolved || qte.config.qteType != QTEType.Swipe) return false;
        if (!qte.IsInJudgeWindow(_qtePhaseTimer)) return false;
        Rect? indicatorRect = GetIndicatorScreenRect(qte);
        if (indicatorRect == null || !LineIntersectsRect(startScreenPos, releaseScreenPos, indicatorRect.Value))
            return false;

        if (swipeSpeed < qte.config.swipeMinSpeed)
        {
            DebugLog.Info($"[QTEController] 划动速度不足: {swipeSpeed:F0} < {qte.config.swipeMinSpeed}");
            return false;
        }

        float targetAngle = qte.config.swipeDirection;
        float swipeAngle = Mathf.Atan2(swipeDirection.y, swipeDirection.x) * Mathf.Rad2Deg;
        if (swipeAngle < 0f) swipeAngle += 360f;
        float bestDiff = Mathf.Min(Mathf.Abs(Mathf.DeltaAngle(swipeAngle, targetAngle)), Mathf.Abs(Mathf.DeltaAngle(swipeAngle, targetAngle + 180f)));
        if (bestDiff > qte.config.swipeAngleTolerance)
        {
            DebugLog.Info($"[QTEController] 划动角度偏差过大: bestDiff={bestDiff:F1}° > tol={qte.config.swipeAngleTolerance}°");
            return false;
        }

        DebugLog.Info($"[QTEController] 划动匹配成功: idx={_currentQTEIndex}");
        PlaySwipeInputVisual(qte);
        ResolveQTE(qte, true);
        return true;
    }

    private Camera _qteCanvasCamera;
    private bool _qteCanvasCameraChecked;

    private Camera GetQTECanvasCamera()
    {
        if (!_qteCanvasCameraChecked)
        {
            _qteCanvasCameraChecked = true;
            if (qteDisplay == null) qteDisplay = QTEDisplay.Instance;
            if (qteDisplay != null)
            {
                var canvas = qteDisplay.GetComponent<Canvas>();
                if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
                    _qteCanvasCamera = canvas.worldCamera;
            }
        }
        return _qteCanvasCamera;
    }

    /// <summary>
    /// 获取 QTE 指示器在屏幕空间中的矩形
    /// </summary>
    private Rect? GetIndicatorScreenRect(QTEInstance qte)
    {
        if (qte.indicator == null) return null;
        var rt = qte.indicator.GetComponent<RectTransform>();
        if (rt == null) return null;

        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        var cam = GetQTECanvasCamera();
        if (cam != null)
        {
            for (int i = 0; i < 4; i++)
                corners[i] = cam.WorldToScreenPoint(corners[i]);
        }
        return new Rect(corners[0].x, corners[0].y,
            corners[2].x - corners[0].x,
            corners[2].y - corners[0].y);
    }

    /// <summary>
    /// 线段与矩形相交检测
    /// </summary>
    private bool LineIntersectsRect(Vector2 p1, Vector2 p2, Rect rect)
    {
        if (rect.Contains(p1) || rect.Contains(p2))
            return true;

        Vector2 bl = new Vector2(rect.xMin, rect.yMin);
        Vector2 tl = new Vector2(rect.xMin, rect.yMax);
        Vector2 tr = new Vector2(rect.xMax, rect.yMax);
        Vector2 br = new Vector2(rect.xMax, rect.yMin);

        return LinesIntersect(p1, p2, bl, tl)
            || LinesIntersect(p1, p2, tl, tr)
            || LinesIntersect(p1, p2, tr, br)
            || LinesIntersect(p1, p2, br, bl);
    }

    private bool LinesIntersect(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
    {
        Vector2 d1 = a2 - a1;
        Vector2 d2 = b2 - b1;
        float cross = d1.x * d2.y - d1.y * d2.x;
        if (Mathf.Approximately(cross, 0f)) return false;

        float t = ((b1.x - a1.x) * d2.y - (b1.y - a1.y) * d2.x) / cross;
        float u = ((b1.x - a1.x) * d1.y - (b1.y - a1.y) * d1.x) / cross;

        return t >= 0f && t <= 1f && u >= 0f && u <= 1f;
    }

    /// <summary>
    /// 检查 QTE 判定窗口开始（标记 judgmentStarted，指示器落位后自动进入判定）
    /// </summary>
    private void CheckJudgmentStart()
    {
        if (!UsesStrictInputRule)
        {
            foreach (var qte in _activeQTEs)
            {
                if (qte.resolved || qte.judgmentStarted) continue;
                if (_qtePhaseTimer >= qte.warningEndTime && qte.indicator != null)
                    qte.judgmentStarted = true;
            }
        }
    }

    private bool IsClickInQTEArea(Vector2 screenPos, QTEInstance qte)
    {
        if (qte.indicator == null) return false;

        // 使用指示器的 RectTransform 判断
        var rt = qte.indicator.GetComponent<RectTransform>();
        if (rt == null) return false;

        var cam = GetQTECanvasCamera();
        return RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, cam);
    }

    private void SpawnQTEIndicator(QTEInstance qte)
    {
        if (qteDisplay == null)
        {
            qteDisplay = QTEDisplay.Instance;
            if (qteDisplay == null)
            {
                Debug.LogWarning("[QTEController] 未找到 QTEDisplay");
                return;
            }
            DebugLog.Info($"[QTEController] 找到 QTEDisplay: {qteDisplay.gameObject.name}");
        }
        DebugLog.Info($"[QTEController] 生成指示器: type={qte.config.qteType}, prefab={qte.config.qteIndicatorPrefab?.name}, pos={qte.config.screenPosition}");
        qte.indicator = qteDisplay.SpawnIndicator(qte.config);
        if (qte.indicator != null)
            DebugLog.Info($"[QTEController] 指示器已生成: {qte.indicator.name}, active={qte.indicator.activeSelf}, parent={qte.indicator.transform.parent?.name}");
        else
            Debug.LogWarning("[QTEController] SpawnIndicator返回null!");
    }

    private void ResolveQTE(QTEInstance qte, bool success)
    {
        DebugLog.Info($"[QTE_DIAG] ResolveQTE: success={success}, indicatorId={qte.indicator?.GetInstanceID()}, remainingUnresolved={_activeQTEs.FindAll(q => !q.resolved).Count}");
        qte.resolved = true;
        qte.success = success;

        if (success)
        {
            OnQTESuccessSingle(qte);
        }
        else
        {
            OnQTEFailureSingle(qte);
            if (_currentAttack != null && !_currentAttack.isDefensiveQTE)
                _failureDamagePending = true;
            if (qteDisplay != null && qte.indicator != null)
                qteDisplay.FlashIndicatorFailure(qte.indicator);
        }

        // 通知 QTEDisplay 播放结果特效；提前/错误输入已即时播放失败反馈时不重复。
        if (qteDisplay != null && qte.indicator != null && !qte.failureFeedbackShown)
            qteDisplay.ShowQTEResult(qte.indicator, success);

        if (_currentQTEIndex < _activeQTEs.Count && _activeQTEs[_currentQTEIndex] == qte)
        {
            _currentQTEIndex++;
            if (_currentQTEIndex < _activeQTEs.Count)
            {
                _nextQTEStartTime = _qtePhaseTimer + _activeQTEs[_currentQTEIndex].spawnTime;
                _state = QTEState.PerformingQTEAttack;
            }
        }
    }

    private void OnQTESuccessSingle(QTEInstance qte)
    {
        // 防御型 QTE：弹反该 slot 对应的箭矢波，不造成 poise 伤害
        if (_currentAttack != null && _currentAttack.isDefensiveQTE)
        {
            DeflectArrowWave(_activeQTEs.IndexOf(qte));
            PlayBlockVisual();
        }
        else
        {
            if (enemy != null && _currentAttack != null)
                enemy.TakeQTEPoiseDamage(qte.config.poiseDamage, _currentAttack.interruptibleOnStun);
        }

        if (UltimateSystem.Instance != null)
            UltimateSystem.Instance.AddEnergy(qte.config.ultimateEnergyGain);

        OnQTESuccess?.Invoke();

        // QTE 格挡成功音效
        if (AudioManager.Instance != null)
            AudioManager.Instance.PostEvent("QTE_Block");
        else
            DebugLog.Info("[QTEController] AudioManager.Instance 为 null，跳过 QTE_Block 音效");

        // Handheld.Vibrate(); // 安卓端攻击震动暂关闭
    }

    private void OnQTEFailureSingle(QTEInstance qte)
    {
        // 防御型 QTE：箭矢波继续飞行（已在飞行中，无需额外操作）
        // 非防御型：失败伤害延迟到 CompleteQTEAttack 时应用
        OnQTEFailure?.Invoke();
    }

    #endregion
}