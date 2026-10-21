using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;


/// <summary>
/// Enemy 的**命中帧**：把动画帧映射成出伤时机。
///
/// 按机制从 <c>Enemy.Combat.cs</c> 拆出（2026-10-22，partial 分文件）。
/// partial class 与 Enemy.Combat.cs **同类型**，行为完全一致，只为降低单文件体量与阅读成本。
/// </summary>

public partial class Enemy : MonoBehaviour
{
    private static float GetFirstResolveHitTime(AttackStep step)
    {
        if (step.hitFrames == null)
            return -1f;

        float first = float.MaxValue;
        for (int i = 0; i < step.hitFrames.Count; i++)
        {
            AttackHitFrame frame = step.hitFrames[i];
            if (frame.resolve && frame.hitTime > 0.001f && frame.hitTime < first)
                first = frame.hitTime;
        }
        return first == float.MaxValue ? -1f : first;
    }

    private void SetupHitFrames(AttackStep step)
    {
        ClearHitFrames();
        if (step.hitFrames == null || step.hitFrames.Count == 0) return;

        for (int i = 0; i < step.hitFrames.Count; i++)
        {
            AttackHitFrame frame = step.hitFrames[i];
            if (string.IsNullOrEmpty(frame.spriteName))
            {
                DebugLog.Warning($"[Enemy] {DebugTag} 命中帧 #{i}（keyIndex={frame.keyIndex}）未填 spriteName，已忽略；" +
                                 "运行时按精灵名匹配，请用 Tools/三国杀戮/校验攻击命中帧 回填");
                continue;
            }
            _hitFrameSprites.Add(frame.spriteName);
            _hitFrameResolve.Add(frame.resolve);
            _hitFrameFired.Add(false);
        }
    }

    private void ClearHitFrames()
    {
        _hitFrameSprites.Clear();
        _hitFrameResolve.Clear();
        _hitFrameFired.Clear();
        _hitFrameResolvedOnce = false;
        _hitFrameFiredAny = false;
        _hitFrameInterrupted = false;
    }

    /// <summary>
    /// 命中帧判定：声明命中帧时，出伤（近战）/ 击发（远程）以「命中帧精灵是否已显示在屏幕上」为准。
    /// 读的是屏幕上真实显示的精灵，因此卡肉（Animator 被禁用）、暂停、动画变速都自动保持一致。
    /// </summary>
    private void UpdateAttackHitFrames()
    {
        if (_hitFrameSprites.Count == 0 || _hitFrameFired.Count != _hitFrameSprites.Count) return;
        if (_spriteRenderer == null) return;
        if (!HasPendingHitFrame()) return;

        string currentSprite = _spriteRenderer.sprite != null ? _spriteRenderer.sprite.name : null;
        if (string.IsNullOrEmpty(currentSprite)) return;

        // 打断检测：动画已离开攻击动作（HitFlash/眩晕等）→ 不再按帧结算，交给收尾兜底
        if (_animator != null && _animator.enabled)
        {
            var clipInfos = _animator.GetCurrentAnimatorClipInfo(0);
            if (clipInfos.Length > 0 && clipInfos[0].clip != null &&
                !AttackHitFrameResolver.IsAttackClipName(clipInfos[0].clip.name))
            {
                _hitFrameInterrupted = true;
            }
        }

        for (int i = 0; i < _hitFrameSprites.Count; i++)
        {
            if (_hitFrameFired[i]) continue;
            if (!AttackHitFrameResolver.MatchesSprite(_hitFrameSprites[i], currentSprite)) continue;
            ResolveHitFrame(i, currentSprite);
        }
    }

    private bool HasPendingHitFrame()
    {
        for (int i = 0; i < _hitFrameFired.Count; i++)
            if (!_hitFrameFired[i]) return true;
        return false;
    }

    /// <summary>命中帧出现在屏幕上的那一帧：结算伤害/发射，并进入收招阶段。</summary>
    private void ResolveHitFrame(int index, string currentSprite)
    {
        _hitFrameFired[index] = true;
        _hitFrameFiredAny = true;

        if (!_hitFrameResolvedOnce)
        {
            _hitFrameResolvedOnce = true;
            _attackTelegraph?.StopWarning();
            isAttackDrawPhase = true; // 命中后进入收招阶段，此后不可被招架打断
            isCFrame = false;         // 伤害帧结束霸体窗口
            UpdateOutlineState();
        }

        bool doResolve = index < _hitFrameResolve.Count && _hitFrameResolve[index];
        if (doResolve)
        {
            if (isRanged)
                SpawnProjectile();
            else
                PerformAttack();
        }

        DebugLog.Info($"[Enemy] {DebugTag} 命中帧 #{index} 触发: sprite={currentSprite} resolve={doResolve} " +
                      $"isRanged={isRanged} frame={Time.frameCount} hitStop={_hitStopRemaining:F3}");
    }

    /// <summary>
    /// 攻击动作结束时的兜底：声明了命中帧却整段都没出现（动画被受击/HitFlash 打断，或配置写错）。
    /// 兜底仍结算一次，避免敌人因配置问题变得完全无威胁；配置错误会打印告警。
    /// </summary>
    private void ResolvePendingHitFramesOnAttackEnd()
    {
        if (_hitFrameSprites.Count == 0 || _hitFrameFiredAny)
        {
            ClearHitFrames();
            return;
        }

        if (_hitFrameInterrupted)
            DebugLog.Info($"[Enemy] {DebugTag} 命中帧未出现（攻击动画被打断），按收尾兜底结算一次");
        else
            DebugLog.Warning($"[Enemy] {DebugTag} 命中帧未出现（请检查命中帧配置）: sprites=[{string.Join(",", _hitFrameSprites)}]，按收尾兜底结算一次");

        isAttackDrawPhase = true;
        isCFrame = false;
        UpdateOutlineState();
        if (isRanged)
            SpawnProjectile();
        else
            PerformAttack();

        ClearHitFrames();
    }
}