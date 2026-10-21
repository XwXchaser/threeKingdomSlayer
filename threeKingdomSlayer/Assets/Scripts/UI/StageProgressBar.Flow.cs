using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;


/// <summary>
/// StageProgressBar 的**流程与 Boss**：波次开始/结束、玩家节点移动、可见性与 Boss 阶段的推进。
///
/// 按机制从 <c>StageProgressBar.cs</c> 拆出（2026-10-22，partial 分文件）。
/// partial class 与 StageProgressBar.cs **同类型**，行为完全一致，只为降低单文件体量与阅读成本。
/// </summary>

public partial class StageProgressBar : MonoBehaviour
{
    private void OnWaveStarted(int waveIndex)
    {
        // 波次开始不切换 UI，切换由 BOSS 交战事件驱动
    }

    private void OnWaveCompleted(int waveIndex)
    {
        if (!_initialized) return;
        if (waveIndex < 0 || waveIndex >= _totalWaves) return;

        // BOSS 波：玩家点已在 OnBossEngaged 入场动画中移动，此处跳过
        if (waveIndex == _pendingBossWaveIndex)
        {
            _pendingBossWaveIndex = -1;
            return;
        }

        TryMovePlayerRight();
    }

    // ═══════════════════════════════════════════
    //  移动逻辑
    // ═══════════════════════════════════════════

    private void TryMovePlayerRight()
    {
        int nextIndex = _playerDotIndex + 1;
        if (nextIndex > _totalWaves) return; // 已通关

        // 可以在当前窗口内右移
        if (nextIndex < _windowStartIndex + visibleDots)
        {
            AnimatePlayerTo(nextIndex);
            return;
        }

        // 需要滚动窗口
        if (_windowStartIndex + visibleDots - 1 >= _totalWaves)
        {
            // 最后一组，直接走到终点
            AnimatePlayerTo(nextIndex);
            return;
        }

        PerformScrollThenMove();
    }

    private void PerformScrollThenMove()
    {
        int nextIndex = _playerDotIndex + 1;
        if (nextIndex > _totalWaves) return;

        int newWindowStart = _windowStartIndex + scrollGroupSize;
        newWindowStart = Mathf.Min(newWindowStart, Mathf.Max(0, _totalWaves - visibleDots + 1));

        if (newWindowStart == _windowStartIndex)
        {
            AnimatePlayerTo(_totalWaves);
            return;
        }

        _windowStartIndex = newWindowStart;

        var seq = DOTween.Sequence();
        seq.Append(_contentRect.DOAnchorPosX(_contentOriginX - newWindowStart * dotSpacing, scrollDuration)
            .SetEase(scrollEase));

        int targetIndex = Mathf.Min(nextIndex, _totalWaves);
        seq.AppendCallback(() =>
        {
            _playerDotIndex = targetIndex;
            UpdatePlayerDotPosition();
        });

        seq.AppendCallback(UpdateNodeAppearance);
    }

    private void AnimatePlayerTo(int targetIndex)
    {
        _playerDotIndex = Mathf.Clamp(targetIndex, 0, _totalWaves);
        _playerDotRect.DOKill();
        _playerDotRect.DOAnchorPos(GetNodePosition(_playerDotIndex), moveDuration).SetEase(Ease.OutCubic)
            .OnComplete(UpdateNodeAppearance);
    }

    private void SnapPlayerToPosition()
    {
        _playerDotRect.anchoredPosition = GetNodePosition(_playerDotIndex);
        UpdateContentPosition();
        UpdateNodeAppearance();
    }

    private void UpdatePlayerDotPosition()
    {
        _playerDotRect.anchoredPosition = GetNodePosition(_playerDotIndex);
    }
    public void SetVisible(bool visible)
    {
        if (qteFrameRect != null)
        {
            var cg = qteFrameRect.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = qteFrameRect.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = visible ? 0f : 1f;
            cg.blocksRaycasts = !visible;
            cg.interactable = !visible;

            // QTEFrame Image 无 sprite 时不渲染，补白色 sprite 确保背景可见
            if (!visible)
            {
                var qteImg = qteFrameRect.GetComponent<Image>();
                if (qteImg != null && qteImg.sprite == null)
                {
                    EnsureTextures();
                    qteImg.sprite = Sprite.Create(_whiteTex, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
                    qteImg.type = Image.Type.Sliced;
                }
            }
        }
        gameObject.SetActive(visible);
    }

    public System.Action OnBossTransitionComplete;

    private void OnBossTransitionCompleted()
    {
        SetVisible(false);
        OnBossTransitionComplete?.Invoke();
    }
    private void OnBossEngaged(Enemy boss)
    {
        if (!_initialized) return;
        if (boss == null) return;

        bool wasZero = _activeBossCount == 0;
        _activeBossCount++;
        boss.OnDeath += OnBossDeath;

        if (wasZero)
        {
            _pendingBossWaveIndex = _waveSpawner.CurrentWaveIndex;
            AnimatePlayerForwardThenSwitch();
        }
    }

    /// <summary>
    /// BOSS 入场：先平移动画移玩家点到下一位置，完成后切 QTE 面板
    /// </summary>
    private void AnimatePlayerForwardThenSwitch()
    {
        int nextIndex = _playerDotIndex + 1;
        if (nextIndex > _totalWaves)
        {
            SetVisible(false);
            return;
        }

        // 可以在当前窗口内右移
        if (nextIndex < _windowStartIndex + visibleDots)
        {
            _playerDotIndex = nextIndex;
            _playerDotRect.DOKill();
            _playerDotRect.DOAnchorPos(GetNodePosition(_playerDotIndex), moveDuration)
                .SetEase(Ease.OutCubic)
                .OnComplete(OnBossTransitionCompleted);
            UpdateNodeAppearance();
            return;
        }

        // 需要滚动窗口
        if (_windowStartIndex + visibleDots - 1 >= _totalWaves)
        {
            // 最后一组
            _playerDotIndex = nextIndex;
            _playerDotRect.DOKill();
            _playerDotRect.DOAnchorPos(GetNodePosition(_playerDotIndex), moveDuration)
                .SetEase(Ease.OutCubic)
                .OnComplete(OnBossTransitionCompleted);
            UpdateNodeAppearance();
            return;
        }

        // 滚动 + 移动 + 切 QTE
        int newWindowStart = _windowStartIndex + scrollGroupSize;
        newWindowStart = Mathf.Min(newWindowStart, Mathf.Max(0, _totalWaves - visibleDots + 1));
        _windowStartIndex = newWindowStart;
        _playerDotIndex = Mathf.Min(nextIndex, _totalWaves);

        var seq = DOTween.Sequence();
        seq.Append(_contentRect.DOAnchorPosX(_contentOriginX - newWindowStart * dotSpacing, scrollDuration)
            .SetEase(scrollEase));
        seq.AppendCallback(() =>
        {
            UpdatePlayerDotPosition();
            UpdateNodeAppearance();
        });
        seq.AppendInterval(moveDuration);
        seq.AppendCallback(OnBossTransitionCompleted);
    }

    private void OnBossDeath(Enemy boss)
    {
        if (boss == null) return;
        boss.OnDeath -= OnBossDeath;

        _activeBossCount--;
        if (_activeBossCount <= 0)
        {
            _activeBossCount = 0;
            SetVisible(true);
        }
    }
}