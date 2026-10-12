using System.Collections;
using UnityEngine;

/// <summary>
/// 盾兵（enemyId 1021）盾牌判定组件 —— 最小可玩版本。
///
/// 规则（权威版见 design/enemy-1021-shield-guard.md §3.2）：
///   盾侧(Left，力来自画面左) / 正面(Front) → 普通与重攻击都被格挡：扣固定减伤（下限 minDamage）
///   反侧(Right)                          → 普通全额；重攻击破盾
///   未声明方向(None / Random)             → 不进盾判定（全额）
///   DoT / ignoreDamageModifiers           → 整段跳过
///   破盾：减伤立即失效 → 播放 ShieldBreak → 动画期间普通受击不打断（死亡/击飞可打断）→ 播完进入胆怯（增伤）
///
/// 借用说明（本版本）：
///   - "重攻击"暂用既有 AttackSkillConfig.interruptsCavalryCharge 透传到 TakeDamage 的标记（现役可达路径 = 蓄力斩击/横划）。
///     设计上后续会换成独立的 breaksGuard 字段，见设计文档 §8。
///   - 掉盾动画复用 102 的 CowardTransition（本体 + ShieldVisual 双目标）；盾的分离层/掉落/层级按 D5 属后续美术重做项。
/// </summary>
[RequireComponent(typeof(Enemy))]
public class ShieldGuardEnemy : MonoBehaviour
{
    public enum DamageSide
    {
        Unspecified, // 未声明方向（None / Random）→ 不进盾判定
        ShieldSide,  // 盾侧（力来自画面左）
        OffSide,     // 反侧（暴露侧）
        Front        // 正面
    }

    public struct HitResult
    {
        public float finalDamage;
        public bool blocked;
        public bool broke;
    }

    [Header("减伤")]
    [Tooltip("盾侧/正面被格挡时扣减的固定伤害")]
    public float flatDamageReduction = 10f;

    [Tooltip("格挡后的最低伤害（防止被完全免疫）")]
    public float minDamage = 1f;

    [Tooltip("盾侧/正面的重攻击是否也能破盾（默认关闭：按设计只有反侧重攻击破盾）")]
    public bool heavyFromShieldSideBreaks;

    [Header("破盾与胆怯")]
    [Tooltip("掉盾动画时长（秒）。当前复用 Enemy_102_CowardTransition，其 stopTime = 0.46s")]
    public float breakAnimationDuration = 0.46f;

    [Tooltip("掉盾动画的状态名（1021 controller 中新增，绑定 CowardTransition 同一 clip）")]
    public string breakAnimatorState = "ShieldBreak";

    [Tooltip("胆怯形态下受到伤害的倍率（越小越接近现状；与击飞倍率取较大者，不叠乘）")]
    public float cowardDamageTakenMultiplier = 1.5f;

    [Header("反馈")]
    [Tooltip("格挡命中时伤害数字使用的颜色（区别于普通命中，让减伤可读）")]
    public Color blockDamageNumberColor = new Color(0.62f, 0.70f, 0.86f, 1f);

    [Tooltip("格挡时的专属动画状态名（不存在时回落到 HitFlash）")]
    public string blockAnimatorState = "ShieldGuardHit";

    [Header("调试")]
    public bool logShieldEvents = true;

    private Enemy _enemy;
    private Animator _animator;
    private Coroutine _breakRoutine;

    public Color BlockDamageNumberColor => blockDamageNumberColor;

    /// <summary>盾是否仍在（破盾前）。</summary>
    public bool IsShieldActive => _enemy != null && _enemy.formState == EnemyFormState.Normal;

    private void Awake()
    {
        _enemy = GetComponent<Enemy>();
        _animator = GetComponent<Animator>();
    }

    /// <summary>对象池复用 / 波次重置：中断未完成的破盾流程。</summary>
    public void ResetShieldGuard()
    {
        if (_breakRoutine != null)
        {
            StopCoroutine(_breakRoutine);
            _breakRoutine = null;
        }
    }

    /// <summary>
    /// 伤害结算前的盾牌判定。damage 为已应用弱点倍率后的伤害。
    /// </summary>
    public HitResult ProcessHit(float damage, DamageType damageType, HitReactionDirection rawDirection, bool isHeavy)
    {
        var result = new HitResult { finalDamage = damage };

        // 非 HP 伤害（架势/聚拢冲突）不参与盾判定
        if (damageType == DamageType.Poise || damageType == DamageType.Convergence)
            return result;
        if (damage <= 0f)
            return result;

        // 未破盾 → 无盾形态：胆怯形态增伤
        if (_enemy != null && _enemy.formState != EnemyFormState.Normal)
        {
            if (_enemy.formState == EnemyFormState.Coward)
            {
                float multiplier = cowardDamageTakenMultiplier;
                // 与击飞倍率取较大者（不叠乘）
                if (_enemy.state == EnemyState.Launched
                    && _enemy.launchedDamageTakenMultiplier >= multiplier)
                {
                    multiplier = 1f;
                }
                result.finalDamage = damage * multiplier;
            }
            return result;
        }

        DamageSide side = ClassifySide(rawDirection);
        switch (side)
        {
            case DamageSide.ShieldSide:
            case DamageSide.Front:
                if (isHeavy && heavyFromShieldSideBreaks)
                {
                    result.broke = true;
                }
                else
                {
                    result.blocked = true;
                }
                break;

            case DamageSide.OffSide:
                if (isHeavy)
                    result.broke = true;
                break;

            default: // Unspecified → 不进盾判定
                break;
        }

        if (result.blocked)
            result.finalDamage = Mathf.Max(minDamage, damage - flatDamageReduction);
        // 破盾瞬间减伤即失效：本次按全额结算（D4）

        return result;
    }

    /// <summary>盾侧 / 反侧 / 正面的判定：沿用工程既有 HitReactionDirection 语义（Left = 力来自画面左 = 盾侧）。</summary>
    public static DamageSide ClassifySide(HitReactionDirection rawDirection)
    {
        switch (rawDirection)
        {
            case HitReactionDirection.Left: return DamageSide.ShieldSide;
            case HitReactionDirection.Right: return DamageSide.OffSide;
            case HitReactionDirection.Front: return DamageSide.Front;
            default: return DamageSide.Unspecified;
        }
    }

    /// <summary>格挡表现：播放专属状态（缺失时回落 HitFlash）。</summary>
    public void PlayBlockVisual()
    {
        if (_animator == null)
            _animator = GetComponent<Animator>();
        if (_animator == null)
            return;

        string state = blockAnimatorState;
        if (!_animator.HasState(0, Animator.StringToHash("Base Layer." + state)))
            state = "HitFlash";
        _animator.Play(state, 0, 0f);
    }

    /// <summary>破盾（伤害已结算完成）：进入破盾表现期并启动掉盾动画计时。</summary>
    public void OnShieldBroken()
    {
        if (_enemy == null || _enemy.state == EnemyState.Dead)
            return;
        if (_enemy.formState != EnemyFormState.Normal)
            return;
        if (!_enemy.EnterShieldBrokenForm())
            return;

        if (logShieldEvents)
            Debug.Log($"[ShieldGuard] 破盾: {_enemy.DebugTag} → 减伤失效，播放掉落动画 {breakAnimationDuration:F2}s");

        if (_breakRoutine != null)
            StopCoroutine(_breakRoutine);
        _breakRoutine = StartCoroutine(BreakRoutine());
    }

    private IEnumerator BreakRoutine()
    {
        float elapsed = 0f;
        while (elapsed < breakAnimationDuration)
        {
            if (_enemy == null || _enemy.state == EnemyState.Dead)
            {
                _breakRoutine = null;
                yield break;
            }
            // 被击飞（或其它会夺走表现的状态）→ 立即完成破盾，后续表现走无盾形态
            if (_enemy.state == EnemyState.Launched)
                break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        _breakRoutine = null;
        if (_enemy == null || _enemy.state == EnemyState.Dead)
            yield break;

        _enemy.CompleteShieldBrokenToCoward();
        if (logShieldEvents)
            Debug.Log($"[ShieldGuard] 掉盾动画结束 → 胆怯: {_enemy.DebugTag}");
    }
}
