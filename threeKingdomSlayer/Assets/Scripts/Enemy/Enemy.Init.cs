using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;


/// <summary>
/// Enemy 的**生命周期与初始化**：Awake/OnEnable、初始值快照与还原、Initialize（入列）与波次缩放。
///
/// 按机制从 <c>Enemy.cs</c> 拆出（2026-10-22，partial 分文件）。
/// partial class 与 Enemy.cs **同类型**，行为完全一致，只为降低单文件体量与阅读成本。
/// </summary>

public partial class Enemy : MonoBehaviour
{
    private void Awake()
    {
        RefreshRendererCache();

        // 快照 prefab 原始值，供池复用 / 多波次重置
        SnapshotOriginalValues();
        originalScale = transform.localScale;
        originalRotation = transform.localRotation;

        // 缓存 prefab 原始值，供波次强化（ApplyWaveScaling）使用
        _prefabMaxHealth = maxHealth;
        _prefabAttackSpeed = attackSpeed;
        _prefabAttackDamage = attackDamage;
        if (_spriteRenderer != null)
            _prefabColor = _spriteRenderer.color;
        else if (renderers != null && renderers.Length > 0)
            _prefabColor = renderers[0].sharedMaterial.color;
    }

    private void SnapshotOriginalValues()
    {
        _originalAttackSequence = attackSequence != null ? new List<AttackStep>(attackSequence) : null;
        _originalStabMult = stabDamageMultiplier;
        _originalSlashMult = slashDamageMultiplier;
        _originalPierceMult = pierceDamageMultiplier;
        _originalSweepMult = sweepDamageMultiplier;
        _originalLaunchMult = launchDamageMultiplier;
        _originalPoiseMult = poiseDamageMultiplier;
        _originalIsSuperArmor = isSuperArmor;
        var qte = GetComponent<QTEController>();
        _originalQteData = qte != null ? qte.qteData : null;
    }

    private void RestoreOriginalValues()
    {
        if (_originalAttackSequence != null)
            attackSequence = new List<AttackStep>(_originalAttackSequence);
        stabDamageMultiplier = _originalStabMult;
        slashDamageMultiplier = _originalSlashMult;
        pierceDamageMultiplier = _originalPierceMult;
        sweepDamageMultiplier = _originalSweepMult;
        launchDamageMultiplier = _originalLaunchMult;
        poiseDamageMultiplier = _originalPoiseMult;
        isSuperArmor = _originalIsSuperArmor;
    }

    /// <summary>
    /// 对象从池中重新激活时重建 Renderer 缓存
    /// Awake 只在首次创建时执行，池复用不会重新 Awake
    /// </summary>
    private void OnEnable()
    {
        RefreshRendererCache();
    }

    /// <summary>
    /// 缓存敌人主体 Renderer。
    /// AttackTelegraph 的 SpriteRenderer 独立管理透明度和材质，不参与敌人主体闪白/波次染色。
    /// </summary>
    private void RefreshRendererCache()
    {
        var allRenderers = GetComponentsInChildren<Renderer>(true);
        var filtered = new List<Renderer>(allRenderers.Length);
        for (int i = 0; i < allRenderers.Length; i++)
        {
            var renderer = allRenderers[i];
            if (renderer == null || renderer.GetComponent<EnemyAttackTelegraph>() != null)
                continue;
            filtered.Add(renderer);
        }

        renderers = filtered.ToArray();
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// 初始化敌人（从对象池取出时调用）
    /// 敌人生成时直接站在对应排位置，不自动开始移动。
    /// 只有当前排出现空位时（通过 Column.RemoveEnemy()），
    /// 后方敌人才会向前补齐。
    /// </summary>
    public void Initialize(int col, int row)
    {
        instanceId = _nextInstanceId++;
        _cavalry = GetComponent<CavalryEnemy>();
        _cavalryVisual = GetComponent<CavalryVisualController>();
        _cavalry?.ResetCavalry();
        _shieldGuard = GetComponent<ShieldGuardEnemy>();
        _shieldGuard?.ResetShieldGuard();
        columnIndex = col;
        rowIndex = row;

        currentHealth = maxHealth;
        currentPoise = maxPoise;
        state = EnemyState.Idle;
        formState = EnemyFormState.Normal;
        bossState = BossState.None;
        isSuperArmor = false;
        isPhaseTransitioning = false;
        deferredStun = false;
        _healthLocked = false;
        _deferredPhaseTransition = null;
        currentBossPhase = 0;
        if (_phaseTransitionRoutine != null) { StopCoroutine(_phaseTransitionRoutine); _phaseTransitionRoutine = null; }
        stunTimer = 0f;
        _isStunExiting = false;
        _resumeStunAfterLaunch = false;
        _getupInterrupted = false;
        if (_getupRoutine != null)
        {
            StopCoroutine(_getupRoutine);
            _getupRoutine = null;
        }
        if (_stunExitRoutine != null)
        {
            StopCoroutine(_stunExitRoutine);
            _stunExitRoutine = null;
        }
        launchTimer = 0f;
        _remainingStunOnLaunch = 0f;
        // BUG FIX: attackTimer 初始化为一个正数，避免第一次进入 UpdateAttack() 时
        // 立即触发冷却结束（attackTimer <= 0f），导致攻击动画被跳过
        attackTimer = 1f;
        attackAnimTimer = 0f;
        isAttackAnimating = false;
        isAttackDrawPhase = false;
        isCFrame = false;
        _pierceHighlighted = false;
        UpdateOutlineState();
        _currentAttackStep = 0;
        ClearHitFrames();

        // 恢复转阶段覆写的字段为 prefab 原始值
        RestoreOriginalValues();
        moveProgress = 0f;
        isMovingToNextRow = false;
        isRushMove = false;
        pendingRushMove = false;
        _rushMoveOrderOwner = RushMoveOrderOwner.None;
        _rushMoveOrderGeneration = 0;
        _rushMoveMode = RushMoveMode.Step;
        rushMoveDelayTimer = 0f;
        rushMoveChainTriggered = false;
        targetRow = -1;
        actionCooldownTimer = 0f;
        bounceYOffset = 0f;
        _attackTween = null;
        _attackTelegraph = GetComponentInChildren<EnemyAttackTelegraph>(true);
        _attackTelegraph?.StopWarning();
        transform.localScale = originalScale;
        transform.localRotation = originalRotation;
        // 创建或恢复每个 Renderer 的专属材质实例，并恢复对象池残留的视觉状态。
        CreateFlashMaterials();
        ResetWaveTint();

        initialized = true;

        gameObject.SetActive(true);

        // 缓存 Animator 和 SpriteController 引用
        _animator = GetComponent<Animator>();
        _spriteCtrl = GetComponent<EnemySpriteController>();
        _attackTelegraph = GetComponentInChildren<EnemyAttackTelegraph>(true);
        _cowardTransitionPlaying = false;
        PlayIdleVisual();

        // 订阅 QTE 完成事件（BOSS 由 Idle 调度，QTE 完成后不依赖 QTEController 自管理冷却）
        if (isBoss)
        {
            var qte = GetQTEController();
            if (qte != null) qte.OnQTEAttackFinished += OnQTEAttackFinished;
        }

        // 恢复 QTE 数据（QTEController 的 qteData 可能在转阶段时被 SwitchQteData 覆写）
        // 始终调用 SwitchQteData，即使 _originalQteData 为 null（prefab 无 QTE 但转阶段获得了）
        var qteCtrl = GetQTEController();
        if (qteCtrl != null)
            qteCtrl.SwitchQteData(_originalQteData);

        // 应用当前阶段的 QTE 数据（覆盖 prefab 默认值，确保初始阶段走正确的 QTE 配置）
        if (isBoss)
        {
            var currentPhase = GetCurrentPhaseData();
            if (currentPhase?.qteData != null)
                qteCtrl.SwitchQteData(currentPhase.qteData);
        }

        // 缓存 Attack AnimationClip（用于远程 DOTween 时长同步）
        _attackClip = null;
        if (_animator != null && _animator.runtimeAnimatorController != null)
        {
            foreach (var clip in _animator.runtimeAnimatorController.animationClips)
            {
                if (clip.name.Contains("_Attack") && !clip.name.Contains("_CAttack"))
                {
                    _attackClip = clip;
                    break;
                }
            }
        }

        // 原始姿态仅在 Awake 从预制体快照；不能在池复用时用残留动画状态覆盖。

        UpdateWorldPosition();

        // 如果敌人已在攻击范围内（由 EnemyConfig.attackRange 决定），直接进入攻击状态
        int atkRange = (int)Mathf.Max(1, attackRange);
        if (rowIndex < atkRange)
        {
            StartAttacking();
        }
    }

    /// <summary>
    /// 应用波次敌人强化（血量 / 攻速 / 伤害倍率 + 颜色叠加）
    /// 在 Initialize() 之后、共享血量组创建之前调用
    /// 始终基于 _prefab* 原始值计算，不累积
    /// </summary>
    public void ApplyWaveScaling(float hpMult, Color tint, float atkSpdMult = 1f, float dmgMult = 1f, float bossHpMult = 0f)
    {
        if (_prefabMaxHealth <= 0f) return;

        float effectiveHpMult = (isBoss && bossHpMult > 0f) ? bossHpMult : hpMult;
        maxHealth = _prefabMaxHealth * effectiveHpMult;
        currentHealth = maxHealth;

        attackSpeed = _prefabAttackSpeed * atkSpdMult;
        attackDamage = _prefabAttackDamage * dmgMult;

        Color finalColor = _prefabColor * tint;
        finalColor.a = _prefabColor.a;

        // 每波都显式写入（包括白色），避免对象池复用保留上一波染色。
        if (_spriteRenderer != null)
            _spriteRenderer.color = finalColor;

        // MeshRenderer: 使用独立 MaterialPropertyBlock 设 _Color，避免与描边 _propBlock 冲突
        if (renderers == null || renderers.Length == 0) return;

        if (_tintPropBlock == null)
            _tintPropBlock = new MaterialPropertyBlock();
        _tintPropBlock.SetColor("_Color", finalColor);

        foreach (var r in renderers)
        {
            if (r is SpriteRenderer) continue; // 已处理
            r.SetPropertyBlock(_tintPropBlock);
        }
    }
}