using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// **命中反应注册表**（扩展点入口）。
///
/// 一次攻击的开始 / 结束、每次命中的反应结算，都从这里广播给已注册的处理器。
/// 新增一种元素反应或命中反应 = 写一个 <see cref="IDamageReactionHandler"/> 并注册，
/// 不用改 <c>Enemy.TakeDamage</c> 或 <c>AttackSystem</c>。
///
/// 内置处理器：冰系（<c>IceBreakReactionHandler</c>，包装原有的 <c>IceBreakRuntime</c> 静态实现），
/// 它在注册表里排第一位，行为与改造前完全一致。
/// </summary>
public static class DamageReactionRegistry
{
    private static readonly List<IDamageReactionHandler> _handlers = new List<IDamageReactionHandler>();
    private static bool _defaultsAdded;

    /// <summary>注册命中反应处理器（扩展点）。同一 id 后注册者覆盖先注册者。</summary>
    public static void Register(IDamageReactionHandler handler)
    {
        if (handler == null) return;
        EnsureDefaults();
        for (int i = 0; i < _handlers.Count; i++)
        {
            if (_handlers[i].ReactionId == handler.ReactionId) { _handlers[i] = handler; return; }
        }
        _handlers.Add(handler);
    }

    /// <summary>注销命中反应处理器。</summary>
    public static void Unregister(string reactionId)
    {
        if (string.IsNullOrEmpty(reactionId)) return;
        EnsureDefaults();
        for (int i = _handlers.Count - 1; i >= 0; i--)
            if (_handlers[i].ReactionId == reactionId) _handlers.RemoveAt(i);
    }

    /// <summary>是否已注册某个反应（诊断 / 工具用）。</summary>
    public static bool HasHandler(string reactionId)
    {
        EnsureDefaults();
        for (int i = 0; i < _handlers.Count; i++)
            if (_handlers[i] != null && _handlers[i].ReactionId == reactionId) return true;
        return false;
    }

    /// <summary>注册命中反应处理器（扩展点，委托版）：只需给出「命中时做什么」。</summary>
    public static void Register(string reactionId, System.Func<Enemy, DamageType, HitFeedbackSource, bool> resolve)
    {
        if (string.IsNullOrEmpty(reactionId)) return;
        Register(new DelegateDamageReactionHandler(reactionId, resolve));
    }

    /// <summary>广播：本次攻击开始。</summary>
    public static void BeginAttack(AttackSkillConfig cfg, float attackDuration)
    {
        EnsureDefaults();
        for (int i = 0; i < _handlers.Count; i++)
        {
            var handler = _handlers[i];
            if (handler == null) continue;
            try { handler.OnAttackBegin(cfg, attackDuration); }
            catch (System.Exception e) { Debug.LogError("[DamageReaction] " + handler.ReactionId + " OnAttackBegin 失败: " + e); }
        }
    }

    /// <summary>广播：本次攻击结束。</summary>
    public static void EndAttack()
    {
        EnsureDefaults();
        for (int i = 0; i < _handlers.Count; i++)
        {
            var handler = _handlers[i];
            if (handler == null) continue;
            try { handler.OnAttackEnd(); }
            catch (System.Exception e) { Debug.LogError("[DamageReaction] " + handler.ReactionId + " OnAttackEnd 失败: " + e); }
        }
    }

    /// <summary>
    /// 广播：这次命中的反应结算。**所有处理器都会跑**（各自可改伤害），
    /// 返回 true = 至少有一个处理器生效。
    /// </summary>
    public static bool TryResolve(Enemy target, DamageType damageType, HitFeedbackSource feedbackSource, ref float damage)
    {
        EnsureDefaults();
        bool handled = false;
        for (int i = 0; i < _handlers.Count; i++)
        {
            var handler = _handlers[i];
            if (handler == null) continue;
            try
            {
                if (handler.TryResolve(target, damageType, feedbackSource, ref damage)) handled = true;
            }
            catch (System.Exception e)
            {
                Debug.LogError("[DamageReaction] " + handler.ReactionId + " TryResolve 失败: " + e);
            }
        }
        return handled;
    }

    /// <summary>确保内置处理器已注册（惰性：任何时候调用都可用）。</summary>
    private static void EnsureDefaults()
    {
        if (_defaultsAdded) return;
        _defaultsAdded = true;
        _handlers.Add(new IceBreakReactionHandler());
    }
}

/// <summary>
/// 委托版命中反应处理器（<see cref="IDamageReactionHandler"/> 的适配器）。
///
/// 只想在命中时做点事（不改伤害）时，不必为一个反应专门写一个类：
/// <code>
/// DamageReactionRegistry.Register("crit", (enemy, type, source) =&gt; { ...; return true; });
/// </code>
/// 会话钩子（OnAttackBegin / OnAttackEnd）对委托版为空实现——需要会话状态时写自己的类。
/// </summary>
public sealed class DelegateDamageReactionHandler : IDamageReactionHandler
{
    private readonly string _reactionId;
    private readonly System.Func<Enemy, DamageType, HitFeedbackSource, bool> _resolve;

    public DelegateDamageReactionHandler(string reactionId, System.Func<Enemy, DamageType, HitFeedbackSource, bool> resolve)
    {
        _reactionId = reactionId;
        _resolve = resolve;
    }

    public string ReactionId => _reactionId;

    public void OnAttackBegin(AttackSkillConfig cfg, float attackDuration) { }

    public void OnAttackEnd() { }

    public bool TryResolve(Enemy target, DamageType damageType, HitFeedbackSource feedbackSource, ref float damage)
    {
        return _resolve != null && _resolve(target, damageType, feedbackSource);
    }
}

/// <summary>
/// 冰系命中反应的处理器（包装原有的 <c>IceBreakRuntime</c> 静态实现）。
///
/// 行为与改造前一致：破冰会话由攻击的开始 / 结束驱动，命中时按合规重击判定解冻与翻倍。
/// </summary>
public sealed class IceBreakReactionHandler : IDamageReactionHandler
{    public string ReactionId => "ice_break";

    public void OnAttackBegin(AttackSkillConfig cfg, float attackDuration)
    {
        IceBreakRuntime.BeginAttack(cfg, attackDuration);
    }

    public void OnAttackEnd()
    {
        IceBreakRuntime.EndAttack();
    }

    public bool TryResolve(Enemy target, DamageType damageType, HitFeedbackSource feedbackSource, ref float damage)
    {
        return IceBreakRuntime.TryResolve(target, damageType, feedbackSource, ref damage);
    }
}
