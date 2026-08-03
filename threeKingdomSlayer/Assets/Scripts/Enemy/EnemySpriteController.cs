using UnityEngine;

/// <summary>
/// 临时方案：根据 Enemy 状态驱动 SpriteRenderer 精灵切换。
/// 后续将改为 Animator + AnimationClip 方案。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Enemy))]
public class EnemySpriteController : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite idleSprite;
    public Sprite attack1Sprite;
    public Sprite attack2Sprite;
    public Sprite deadSprite;
    public Sprite hittedSprite;
    public Sprite knockUpSprite;

    private Enemy enemy;
    private SpriteRenderer spriteRenderer;

    // 受击闪烁
    private float hitTimer;

    // 攻击动画计时
    private float attackStartTime;
    private bool attackAnimating;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        hitTimer = 0f;
        attackAnimating = false;
        ApplySprite();
    }

    private void Update()
    {
        // Dead 优先级最高，立即切换
        if (enemy.state == EnemyState.Dead)
        {
            spriteRenderer.sprite = deadSprite;
            return;
        }

        // 受击闪烁中（0.3秒固定时长，仅 Idle/Moving 触发）
        if (hitTimer > 0f)
        {
            hitTimer -= Time.deltaTime;
            if (hitTimer <= 0f)
            {
                hitTimer = 0f;
                ApplySprite();
            }
            return;
        }

        // QTE 攻击演出期间不干预（QTEController 自行管理精灵）
        if (enemy.state == EnemyState.QTEAttacking)
            return;

        ApplySprite();
    }

    private void ApplySprite()
    {
        switch (enemy.state)
        {
            case EnemyState.Attacking:
                UpdateAttackSprite();
                break;
            case EnemyState.Launched:
                spriteRenderer.sprite = knockUpSprite;
                break;
            case EnemyState.Idle:
            case EnemyState.Moving:
            case EnemyState.Stunned:
            default:
                spriteRenderer.sprite = idleSprite;
                break;
        }
    }

    private void UpdateAttackSprite()
    {
        // 冷却阶段 / 攻击被打断 → 显示 idle
        if (!enemy.isAttackAnimating)
        {
            attackAnimating = false;
            spriteRenderer.sprite = idleSprite;
            return;
        }

        // 进入攻击动画，记录起始时间
        if (!attackAnimating)
        {
            attackStartTime = Time.time;
            attackAnimating = true;
        }

        if (!enemy.isAttackDrawPhase)
        {
            // 蓄力帧（AttackSpawn）→ 停顿（Parry 窗口，attack2 悬停姿态）→ 发生帧
            // 窗口起点 = 蓄力完成时刻；窗口内显示 attack2（即将出手），蓄力中显示 attack1
            if (enemy.IsParryWindowActive)
            {
                // Parry 窗口（停顿）期间：attack2 = 即将出手的悬停姿态
                spriteRenderer.sprite = attack2Sprite;
            }
            else
            {
                // 蓄力帧阶段：attack1（前摇）
                spriteRenderer.sprite = attack1Sprite;
            }
        }
        else
        {
            // AttackDraw 阶段：全程 attack2
            spriteRenderer.sprite = attack2Sprite;
        }
    }

    /// <summary>
    /// 触发受击精灵闪烁（仅 Idle / Moving 状态有效，持续 0.3 秒）。
    /// 由 Enemy.TakeDamage 调用。
    /// </summary>
    public void TriggerHitFlash()
    {
        // Idle / Moving / Stunned 总是允许；Attacking 仅在冷却阶段（非动画中）允许
        bool canHitFlash = enemy.state == EnemyState.Idle
                        || enemy.state == EnemyState.Moving
                        || enemy.state == EnemyState.Stunned
                        || (enemy.state == EnemyState.Attacking && !enemy.isAttackAnimating);

        if (canHitFlash)
        {
            hitTimer = 0.3f;
            spriteRenderer.sprite = hittedSprite;
        }
    }
}
