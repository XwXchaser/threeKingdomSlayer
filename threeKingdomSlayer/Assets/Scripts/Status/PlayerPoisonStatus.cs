using UnityEngine;

/// <summary>
/// **玩家的中毒**（玩家侧状态）。
///
/// 用途（2026-11-14 用户需求）：投壶兵投出的陶壶命中后留下毒 ——
/// **防御只能免掉直接伤害，毒照扣** ⇒ 防御不再是「万能保险」，
/// 远程投掷物因此变成**高优先级目标**（可以挥枪把陶壶在半空打落）。
///
/// 结算口径：**直接扣血**，**不走 <c>PlayerState.TakeDamage</c>** ⇒
///   • 不会被防御吸收（防御只挡直接伤害）；
///   • 不会被反伤护盾弹走；
///   • 不吃蓄力减伤 / 不屈天赋（那是「一次伤害」的判定，不是持续伤害）。
///
/// 归属：挂在玩家的 <see cref="PlayerState.StatusHost"/>（与反伤护盾同级）。
/// </summary>
public sealed class PlayerPoisonStatus : IStatusEffect
{
    public const string Id = "poison";

    private readonly PlayerState _player;
    private float _damagePerTick;
    private float _duration;
    private float _timer;
    private float _tickInterval;
    private float _tickTimer;

    public PlayerPoisonStatus(PlayerState player, float damagePerTick, float duration, float tickInterval)
    {
        _player = player;
        _damagePerTick = Mathf.Max(0f, damagePerTick);
        _duration = Mathf.Max(0.01f, duration);
        _tickInterval = Mathf.Max(0.05f, tickInterval);
        _timer = _duration;
        _tickTimer = _tickInterval;
    }

    public string StatusId => Id;

    public bool IsActive => _timer > 0f;

    public float GetProgress() => _duration > 0f ? Mathf.Clamp01(1f - _timer / _duration) : 0f;

    public int GetStacks() => 1;

    public int GetPriority() => 30;

    /// <summary>重新施加（同一 id 后登记者覆盖先登记者）⇒ 刷新强度与时长，不叠层。</summary>
    public void Refresh(float damagePerTick, float duration)
    {
        _damagePerTick = Mathf.Max(_damagePerTick, Mathf.Max(0f, damagePerTick));
        _duration = Mathf.Max(_duration, Mathf.Max(0.01f, duration));
        _timer = Mathf.Max(_timer, _duration);
    }

    public void Tick(float deltaTime)
    {
        if (_player == null) return;
        if (_timer <= 0f)
        {
            _player.StatusHost.Remove(this);
            return;
        }

        _timer -= deltaTime;
        _tickTimer -= deltaTime;
        if (_tickTimer > 0f) return;

        _tickTimer = _tickInterval;
        _player.TakeDamageUnblockable(_damagePerTick);
    }

    public void Clear()
    {
        _timer = 0f;
    }
}
