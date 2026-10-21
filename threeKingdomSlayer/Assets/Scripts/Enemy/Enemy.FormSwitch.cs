using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;


/// <summary>
/// Enemy 的**形态切换特例**：胆怯形态（被挑飞/破盾后的形态与表现）与骑兵控制（骑乘期间的行列与表现接管）。
///
/// 按机制从 <c>Enemy.StateSwitch.cs</c> 拆出（2026-10-22，partial 分文件）。
/// partial class 与 Enemy.StateSwitch.cs **同类型**，行为完全一致，只为降低单文件体量与阅读成本。
/// </summary>

public partial class Enemy : MonoBehaviour
{
    public bool EnterCowardFormFromPlayerLaunch()
    {
        if (enemyId != 102 || IsCoward || state == EnemyState.Dead)
            return false;
        EnterCowardFormVisual();
        return true;
    }

    /// <summary>
    /// 盾兵(1021) 破盾后进入胆怯形态。
    /// 与被挑飞进胆怯(102) 是两条独立入口，102 的判定与行为完全不受影响。
    /// </summary>
    public bool EnterCowardFormFromShieldBreak()
    {
        if (IsCoward || state == EnemyState.Dead)
            return false;
        EnterCowardFormVisual();
        return true;
    }

    /// <summary>盾牌被打掉的瞬间：减伤立即失效，进入破盾表现期（播放 ShieldBreak）。</summary>
    public bool EnterShieldBrokenForm()
    {
        if (state == EnemyState.Dead || IsCoward || formState == EnemyFormState.ShieldBroken)
            return false;

        // 攻击中被打掉盾：先复用既有打断清理（含 DOTween/形变/标志位），保持"非Boss=攻击循环"的既有语义
        if (state == EnemyState.Attacking)
            CancelAttack();

        _attackTelegraph?.StopWarning();
        transform.localScale = originalScale;
        transform.localRotation = originalRotation;
        formState = EnemyFormState.ShieldBroken;
        isAttackAnimating = false;
        isAttackDrawPhase = false;
        isCFrame = false;
        _animator?.ResetTrigger("Attack");
        _animator?.ResetTrigger("Hit");
        _animator?.ResetTrigger("Walk");
        if (_animator != null) _animator.speed = 1f;

        // 站立/移动/眩晕/攻击态播放掉盾动画；被击飞或起身中不抢表现，只标记形态
        if (state != EnemyState.Launched && state != EnemyState.GettingUp)
        {
            if (_animator != null
                && _animator.HasState(0, Animator.StringToHash("Base Layer.ShieldBreak")))
            {
                _animator.Play("ShieldBreak", 0, 0f);
            }
        }
        DebugLog.Info($"[Enemy] 破盾表现期开始: {DebugTag}");
        return true;
    }

    /// <summary>掉盾动画播完：进入胆怯（无盾、增伤生效、不再攻击）。</summary>
    public void CompleteShieldBrokenToCoward()
    {
        CompleteShieldBrokenToCowardInternal(true);
    }

    // 死亡/击飞已拥有动画时，只结束破盾形态，不递归切换 Animator。
    public void CompleteShieldBrokenToCowardWithoutVisual()
    {
        CompleteShieldBrokenToCowardInternal(false);
    }

    private void CompleteShieldBrokenToCowardInternal(bool refreshVisual)
    {
        if (state == EnemyState.Dead || IsCoward)
            return;
        if (formState != EnemyFormState.ShieldBroken)
            return;

        _attackTelegraph?.StopWarning();
        formState = EnemyFormState.Coward;
        _cowardTransitionPlaying = false;
        isAttackAnimating = false;
        isAttackDrawPhase = false;
        isCFrame = false;
        _animator?.ResetTrigger("Attack");
        _animator?.ResetTrigger("Hit");
        _animator?.ResetTrigger("Walk");
        if (refreshVisual && state == EnemyState.Launched)
            PlayLaunchVisual();
        else if (refreshVisual && state != EnemyState.GettingUp)
            PlayIdleVisual();
        DebugLog.Info($"[Enemy] 掉盾动画结束 → 胆怯形态: {DebugTag}");
    }

    private void EnterCowardFormVisual()
    {
        _attackTelegraph?.StopWarning();
        if (_attackTween != null && _attackTween.IsActive())
        {
            _attackTween.Kill();
            _attackTween = null;
        }
        transform.DOKill(false);
        StopHitScaleFeedback();
        transform.localScale = originalScale;
        transform.localRotation = originalRotation;
        formState = EnemyFormState.Coward;
        _cowardTransitionPlaying = true;
        isAttackAnimating = false;
        isAttackDrawPhase = false;
        isCFrame = false;
        attackTimer = 0f;
        _animator?.ResetTrigger("Attack");
        _animator?.ResetTrigger("Hit");
        _animator?.Play("CowardTransition", 0, 0f);
    }

    public bool OnPlayerLaunchHit() => EnterCowardFormFromPlayerLaunch();

    internal void PlayIdleVisual()
    {
        if (formState == EnemyFormState.ShieldBroken) return;
        if (_cavalryVisual != null && _cavalryVisual.TryRefreshMountedAnimation()) return;
        _animator?.Play(IsCoward ? "CowardIdle" : "Idle", 0, 0f);
    }
    private void PlayLaunchVisual()
    {
        _shieldGuard?.InterruptShieldBreakVisual();
        bool withoutShield = IsCoward || formState == EnemyFormState.ShieldBroken;
        _animator?.Play(withoutShield ? "CowardLaunched" : "Launched_Rise", 0, 0f);
    }
    internal void PlayHitVisual(HitReactionDirection direction)
    {
        if (_animator == null)
            return;

        string stateName = IsCoward
            ? "CowardHit"
            : direction switch
            {
                HitReactionDirection.Left => "HitLeft",
                HitReactionDirection.Right => "HitRight",
                _ => "HitFlash"
            };

        if (!IsCoward && !_animator.HasState(0, Animator.StringToHash("Base Layer." + stateName)))
            stateName = "HitFlash";

        _animator.Play(stateName, 0, 0f);
    }
    private void PlayGetupVisual() => _animator?.Play(IsCoward ? "CowardGetup" : "Launched_Getup", 0, 0f);

    private static HitReactionDirection ResolveHitReactionDirection(HitReactionDirection requested)
    {
        if (requested == HitReactionDirection.Front
            || requested == HitReactionDirection.Left
            || requested == HitReactionDirection.Right)
            return requested;

        return (HitReactionDirection)Random.Range(
            (int)HitReactionDirection.Front,
            (int)HitReactionDirection.Right + 1);
    }

    private bool HasGetupAnimatorState()
    {
        if (_animator == null || _animator.runtimeAnimatorController == null)
            return false;

        return _animator.HasState(0, Animator.StringToHash(IsCoward ? "Base Layer.CowardGetup" : "Base Layer.Launched_Getup"));
    }

    private void PlayDeadVisual()
    {
        _shieldGuard?.InterruptShieldBreakVisual();
        bool withoutShield = IsCoward || formState == EnemyFormState.ShieldBroken;
        _animator?.Play(withoutShield ? "CowardDead" : "Dead", 0, 0f);
    }

    public Vector3 GetCavalryRowLocalPosition(int visualRow)
    {
        var stage = StageFormation;
        float xPos = stage != null
            ? stage.GetFormationOffset(columnIndex, visualRow)
            : (columnIndex - 2) * 2f;
        float rowSpacing = stage != null ? stage.GetRowSpacing() : 2.5f;
        int maxRow = stage != null ? stage.GetMaxVisibleRows() - 1 : 4;
        float offsetZ = stage != null ? stage.GetFormationOffsetZ() : 0f;
        float zPos = (maxRow - visualRow) * (-rowSpacing) + offsetZ;
        return new Vector3(xPos, transform.localPosition.y, zPos);
    }

    public void SetCavalryChargeVisualRow(int visualRow)
    {
        transform.localPosition = GetCavalryRowLocalPosition(visualRow);
    }

    public void RestoreCavalryWorldPosition()
    {
        UpdateWorldPosition();
    }

    /// <summary>骑兵接管期间：保持 Idle，避免当次普通攻击/普通移动介入。</summary>
    public void EnterCavalryControl()
    {
        if (state == EnemyState.Dead || state == EnemyState.Launched) return;
        isAttackAnimating = false;
        isAttackDrawPhase = false;
        isCFrame = false;
        state = EnemyState.Idle;
        UpdateOutlineState();
    }

    /// <summary>骑兵动作结束：恢复 Idle 表现，交还控制权。</summary>
    public void ExitCavalryControl()
    {
        if (state == EnemyState.Dead || state == EnemyState.Launched) return;
        if (state == EnemyState.Idle)
            PlayIdleVisual();
    }

    /// <summary>骑兵命中表现：复用攻击动画（伤害与位移由 CavalryEnemy 负责）。</summary>
    public void PlayCavalryStrikeVisual()
    {
        if (_cavalryVisual != null && _cavalryVisual.TryRefreshMountedAnimation()) return;
        if (_animator == null) return;
        _animator.Play("Idle", 0, 0f);
        _animator.SetTrigger("Attack");
    }

    public void CancelCavalryAttackOnDismount()
    {
        if (state != EnemyState.Attacking) return;
        CancelAttack();
        if (state == EnemyState.Attacking)
            state = EnemyState.Idle;
        PlayIdleVisual();
    }
}