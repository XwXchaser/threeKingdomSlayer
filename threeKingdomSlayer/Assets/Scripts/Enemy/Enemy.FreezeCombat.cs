using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;


/// <summary>
/// Enemy 的**冻结结算**：冰系冻结在受击时的判定与解冻处理。
///
/// 按机制从 <c>Enemy.Combat.cs</c> 拆出（2026-10-22，partial 分文件）。
/// partial class 与 Enemy.Combat.cs **同类型**，行为完全一致，只为降低单文件体量与阅读成本。
/// </summary>

public partial class Enemy : MonoBehaviour
{
    private EnemyFreeze _freezeSys;

    /// <summary>打击反馈（卡肉 / 受击缩放 / 闪白）。实现见 EnemyHitFeedback（批次 3 刀 A）。</summary>
    private EnemyHitFeedback _hitFeedback;

    private EnemyHitFeedback HitFeedback => _hitFeedback ??= new EnemyHitFeedback(this);

    /// <summary>韧性（削韧 / 破势 / QTE 韧性伤害）。实现见 EnemyPoise（批次 3 刀 E1）。</summary>
    private EnemyPoise _poise;

    private EnemyPoise Poise => _poise ??= new EnemyPoise(this);


    /// <summary>BOSS 多阶段系统。实现见 EnemyBossPhases（批次 3 刀 B）。</summary>
    private EnemyBossPhases _bossPhases;

    private EnemyBossPhases BossPhases => _bossPhases ??= new EnemyBossPhases(this);

    /// <summary>移动订单与世界坐标。实现见 EnemyMovement（批次 3 刀 D）。</summary>
    private EnemyMovement _movement;

    private EnemyMovement Movement => _movement ??= new EnemyMovement(this);



    private EnemyFreeze FreezeSys => _freezeSys ??= new EnemyFreeze(this);

    /// <summary>当前是否处于攻击前摇播放中（与 Parry 打断同口径：收招不算前摇）。</summary>
    public bool IsInAttackWindup => state == EnemyState.Attacking && isAttackAnimating && !isAttackDrawPhase;

    /// <summary>冻结或破冰动画期间：所有自主行动与计时都暂停。</summary>
    public bool IsActionPaused => state == EnemyState.Frozen || state == EnemyState.IceBreaking || tutorialHold;

    public bool IsFrozen => FreezeSys.IsFrozen;
    public bool IsInPostFreezeRecovery => FreezeSys.IsInPostFreezeRecovery;
    public float FreezeChance => FreezeSys.FreezeChance;
    public bool CanBeFrozen => FreezeSys.CanBeFrozen;

    internal bool IsFrozenHitReactionSuppressed => FreezeSys.IsFrozenHitReactionSuppressed;

    public bool Freeze(float duration) => FreezeSys.Freeze(duration);
    public void BreakFreeze() => FreezeSys.BreakFreeze();
    public void ApplyIceHit(float chancePerHit) => FreezeSys.ApplyIceHit(chancePerHit);
    public void BreakIcePush() => FreezeSys.BreakIcePush();
    public void BeginIceBreaking() => FreezeSys.BeginIceBreaking();
    public void PlayBreakIcePushMove(Vector3 fromWorldPosition, float duration = 0.12f)
        => FreezeSys.PlayBreakIcePushMove(fromWorldPosition, duration);

    private void UpdateFrozen() => FreezeSys.UpdateFrozen();
    private void UpdateIceBreaking() => FreezeSys.UpdateIceBreaking();
    private void UpdateFreezeChanceDecay() => FreezeSys.UpdateFreezeChanceDecay();
    internal void ClearFreezeForSystemOverride() => FreezeSys.ClearFreezeForSystemOverride();
    private void ClearFreezeOnTeardown() => FreezeSys.ClearFreezeOnTeardown();
}