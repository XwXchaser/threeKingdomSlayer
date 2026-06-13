using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// QTE 状态机（由 Enemy 的 Idle 调度触发，不再自行管理冷却）
/// Idle → PerformingQTEAttack → QTEJudging → QTECompleted → Idle
///
/// QTE 阶段概念：
///   - PerformingQTEAttack: 等待 QTE 阶段开始（飞行物到达 / 动画前摇结束）
///   - QTE 阶段开始后，slot delay 相对于阶段开始计时
///   - 全部 QTE 成功 → 销毁飞行物；任一失败 → 飞行物穿过摄像机
/// </summary>
public enum QTEState
{
    Idle,
    PerformingQTEAttack,
    QTEJudging,
    QTECompleted
}

public class QTEInstance
{
    public QTEConfig config;
    public float spawnTime;       // QTE 阶段开始后的生成时间
    public float warningEndTime;  // 预警结束时间（判定窗口开始）
    public float judgeEndTime;    // 判定窗口结束时间
    public GameObject indicator;
    public bool resolved;
    public bool success;
    public bool judgmentStarted;  // 判定窗口已开始（用于通知 QTEDisplay）

    public bool IsInJudgeWindow(float phaseElapsed) => phaseElapsed >= warningEndTime && phaseElapsed <= judgeEndTime;
    public bool IsExpired(float phaseElapsed) => phaseElapsed > judgeEndTime && !resolved;
}

/// <summary>
/// QTE 控制器 — 挂载到 BOSS GameObject
/// 管理 QTE 队列状态机、冷却计时、触发时机、输入判定、成功/失败分发
/// </summary>
public class QTEController : MonoBehaviour
{
    [Header("配置")]
    public BossQTEData qteData;

    [Header("组件引用")]
    public Enemy enemy;
    public QTEDisplay qteDisplay;

    [Header("运行时状态")]
    [SerializeField] private QTEState _state = QTEState.Idle;

    private int _currentAttackIndex;
    private float _performingTimer;       // Performing 阶段的累计时间（用于动画前摇）
    private bool _qtePhaseStarted;        // QTE 阶段是否已开始（飞行物到达 / 动画前摇结束）
    private float _qtePhaseTimer;         // QTE 阶段开始后的计时器（用于 QTE spawn/judge）
    private QTEAttackConfig _currentAttack;
    private List<QTEInstance> _activeQTEs = new List<QTEInstance>();
    private GameObject _activeProjectile;

    // QTE 动画
    private Animator _animator;

    // 事件
    public System.Action OnQTETriggered;       // QTE 攻击触发
    public System.Action OnQTESuccess;         // QTE 判定成功
    public System.Action OnQTEFailure;         // QTE 判定失败
    public System.Action OnQTECompleted;       // 一轮 QTE 攻击结束
    public System.Action OnQTEAttackFinished;  // QTE 攻击完全结束，通知 Enemy 回到 Idle

    public QTEState State => _state;
    public QTEAttackConfig CurrentAttackConfig => _currentAttack;

    private void Awake()
    {
        if (enemy == null) enemy = GetComponent<Enemy>();
    }

    private void Start()
    {
        Debug.Log($"[QTEController] Start: enemy={enemy?.name}, isBoss={enemy?.isBoss}, qteData={qteData?.name}, qteAttacksCount={qteData?.qteAttacks?.Count}");
        if (enemy != null && enemy.isBoss)
        {
            // 初始化 QTE 攻击索引（由 Enemy 的 Idle 调度决定何时触发 QTE）
            if (qteData != null && qteData.qteAttacks.Count > 0)
                _currentAttackIndex = 0;
        }
    }

    private void OnDestroy()
    {
        KillProjectileSequence();
    }

    private void Update()
    {
        if (qteData == null || enemy == null) return;

        // 敌人脱离 QTE 状态（如被打入 Stun），中止 QTE
        if ((_state == QTEState.PerformingQTEAttack || _state == QTEState.QTEJudging)
            && enemy.state != EnemyState.QTEAttacking)
        {
            Debug.Log($"[QTEController] 敌人脱离QTE状态({enemy.state})，中止QTE");
            AbortQTE();
            return;
        }

        switch (_state)
        {
            case QTEState.PerformingQTEAttack:
                UpdatePerforming();
                break;
            case QTEState.QTEJudging:
                UpdateJudging();
                break;
        }
    }

    #region 状态切换

    /// <summary>
    /// 切换 QTE 数据（用于 BOSS 转阶段时更换 QTE 配置）
    /// 重置攻击索引，回到 Idle 状态
    /// </summary>
    public void SwitchQteData(BossQTEData newData)
    {
        qteData = newData;
        _currentAttackIndex = 0;
        _state = QTEState.Idle;
        _activeQTEs.Clear();
        Debug.Log($"[QTEController] 切换QTE数据: {newData?.name}, state={_state}");
    }

    /// <summary>
    /// 触发下一轮 QTE 攻击（由 Enemy 的 Idle 调度调用）
    /// 返回 false 表示当前无法触发（QTE 序列已耗尽、敌人在眩晕/击飞中等）
    /// </summary>
    public bool TriggerQTEAttack()
    {
        Debug.Log($"[QTEController] TriggerQTEAttack: attackIndex={_currentAttackIndex}, totalAttacks={qteData?.qteAttacks?.Count}");
        if (qteData == null || qteData.qteAttacks.Count == 0) return false;

        // 眩晕/击飞期间禁止触发QTE
        if (enemy.state == EnemyState.Stunned || enemy.state == EnemyState.Launched)
        {
            Debug.Log($"[QTEController] 敌人在眩晕/击飞状态({enemy.state})，跳过QTE");
            return false;
        }

        // QTE 序列已耗尽（非循环模式）
        if (_currentAttackIndex >= qteData.qteAttacks.Count)
        {
            Debug.Log("[QTEController] QTE序列已耗尽");
            return false;
        }

        _currentAttack = qteData.qteAttacks[_currentAttackIndex];
        Debug.Log($"[QTEController] 当前攻击: {_currentAttack?.name}, slots={_currentAttack?.qteSlots?.Count}");
        _state = QTEState.PerformingQTEAttack;
        _performingTimer = 0f;
        _qtePhaseStarted = false;
        _qtePhaseTimer = 0f;
        _activeQTEs.Clear();

        // 创建 QTE 实例（spawnTime/warningEndTime/judgeEndTime 相对于 QTE 阶段开始）
        foreach (var slot in _currentAttack.qteSlots)
        {
            if (slot.config == null) continue;
            _activeQTEs.Add(new QTEInstance
            {
                config = slot.config,
                spawnTime = slot.delay,
                warningEndTime = slot.delay + slot.config.warningDuration,
                judgeEndTime = slot.delay + slot.config.warningDuration + slot.config.judgeWindow,
                resolved = false,
                success = false
            });
        }

        enemy.EnterQTEAttack();
        StartQTEAnimation();
        SpawnProjectile();
        OnQTETriggered?.Invoke();
        return true;
    }

    private void SpawnProjectile()
    {
        if (_currentAttack.projectilePrefab == null) return;

        _activeProjectile = Instantiate(_currentAttack.projectilePrefab);
        _activeProjectile.transform.position = enemy.transform.position;

        var projectile = _activeProjectile.GetComponent<QTEProjectile>();
        if (projectile != null)
        {
            Vector3 targetPos = GetProjectileTargetPosition();
            projectile.Initialize(_currentAttack.projectileFlightTime, targetPos, OnProjectileReachedTarget);
        }
        else
        {
            // 无 QTEProjectile 组件的普通 prefab：直接飞过去
            Vector3 targetPos = GetProjectileTargetPosition();
            _activeProjectile.transform.DOMove(targetPos, _currentAttack.projectileFlightTime)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    OnProjectileReachedTarget();
                    if (_activeProjectile != null)
                    {
                        Destroy(_activeProjectile);
                        _activeProjectile = null;
                    }
                });
        }
    }

    private Vector3 GetProjectileTargetPosition()
    {
        float offsetZ = StageController.Instance != null ? StageController.Instance.GetFormationOffsetZ() : 0f;
        float zPos = offsetZ + _currentAttack.projectileTargetZ;
        return new Vector3(0, 1.5f, zPos);
    }

    private void OnProjectileReachedTarget()
    {
        StartQTEPhase();
    }

    private void StartQTEPhase()
    {
        if (_qtePhaseStarted) return;
        _qtePhaseStarted = true;
        _qtePhaseTimer = 0f;
    }

    private void KillProjectileSequence()
    {
        if (_activeProjectile != null)
            DOTween.Kill(_activeProjectile);
    }

    #region QTE 动画

    private void StartQTEAnimation()
    {
        if (_currentAttack == null || _currentAttack.qteAnimationClip == null)
            return;

        if (_animator == null)
            _animator = GetComponent<Animator>();
        if (_animator == null) return;

        _animator.SetTrigger("QTEAttack");
    }

    private void StopQTEAnimation()
    {
        if (_animator != null)
            _animator.Play("Idle");
    }

    #endregion

    #endregion

    #region QTE 判定

    private void UpdatePerforming()
    {
        _performingTimer += Time.deltaTime;

        if (!_qtePhaseStarted)
        {
            // 无飞行物时，等待动画前摇结束后开始 QTE 阶段
            if (_activeProjectile == null && _performingTimer >= _currentAttack.animationLeadTime)
                StartQTEPhase();
            // 有飞行物：等待 OnProjectileReachedTarget 回调
            return;
        }

        _qtePhaseTimer += Time.deltaTime;

        // 按 phase-relative 时间生成指示器
        foreach (var qte in _activeQTEs)
        {
            if (qte.indicator == null && _qtePhaseTimer >= qte.spawnTime)
                SpawnQTEIndicator(qte);
        }

        // 检查判定窗口开始（通知 QTEDisplay 触发放大闪白）
        CheckJudgmentStart();

        // 所有 QTE 已到生成时间 → 进入判定阶段
        bool allReady = true;
        foreach (var qte in _activeQTEs)
        {
            if (qte.indicator == null && _qtePhaseTimer < qte.spawnTime)
            { allReady = false; break; }
        }
        if (allReady)
            _state = QTEState.QTEJudging;
    }

    private void UpdateJudging()
    {
        _qtePhaseTimer += Time.deltaTime;

        // 检查判定窗口开始（通知 QTEDisplay 触发放大闪白）
        CheckJudgmentStart();

        bool allResolved = true;
        foreach (var qte in _activeQTEs)
        {
            if (!qte.resolved)
            {
                if (qte.IsExpired(_qtePhaseTimer))
                    ResolveQTE(qte, false);
                else
                    allResolved = false;
            }
        }

        if (allResolved)
            CompleteQTEAttack();
    }

    public bool IsQTEActive
    {
        get
        {
            // QTE 阶段一旦开始，所有输入均被 QTE 系统拦截，防止误触普通攻击
            return _state == QTEState.QTEJudging || _state == QTEState.PerformingQTEAttack;
        }
    }

    public bool TryQTEClick(Vector2 screenPos)
    {
        if (_state != QTEState.QTEJudging && _state != QTEState.PerformingQTEAttack) return false;
        if (!_qtePhaseStarted)
        {
            Debug.Log($"[QTEController] TryQTEClick 拒绝: _qtePhaseStarted=false");
            return false;
        }

        foreach (var qte in _activeQTEs)
        {
            if (qte.resolved || qte.config.qteType != QTEType.Click) continue;

            // 提早点击 → 失败
            if (!qte.IsInJudgeWindow(_qtePhaseTimer) && _qtePhaseTimer < qte.warningEndTime)
            {
                if (IsClickInQTEArea(screenPos, qte))
                {
                    Debug.Log($"[QTEController] 提早点击 → QTE失败 idx={_activeQTEs.IndexOf(qte)}");
                    ResolveQTE(qte, false, earlyFail: true);
                    return true;
                }
                continue;
            }

            if (!qte.IsInJudgeWindow(_qtePhaseTimer)) continue;

            if (IsClickInQTEArea(screenPos, qte))
            {
                Debug.Log($"[QTEController] 点击QTE成功 idx={_activeQTEs.IndexOf(qte)}");
                ResolveQTE(qte, true);
                return true;
            }
        }
        Debug.Log($"[QTEController] TryQTEClick 未命中任何指示器 screenPos={screenPos}");
        return false;
    }

    public bool TryQTESwipe(Vector2 startScreenPos, Vector2 swipeDirection, float swipeSpeed, Vector2 releaseScreenPos)
    {
        if (_state != QTEState.QTEJudging && _state != QTEState.PerformingQTEAttack) return false;
        if (!_qtePhaseStarted) return false;

        foreach (var qte in _activeQTEs)
        {
            if (qte.resolved || qte.config.qteType != QTEType.Swipe) continue;

            // 提早划动 → 失败（仅检查是否经过区域）
            if (!qte.IsInJudgeWindow(_qtePhaseTimer) && _qtePhaseTimer < qte.warningEndTime)
            {
                Rect? earlyRect = GetIndicatorScreenRect(qte);
                if (earlyRect != null && LineIntersectsRect(startScreenPos, releaseScreenPos, earlyRect.Value))
                {
                    ResolveQTE(qte, false, earlyFail: true);
                    return true;
                }
                continue;
            }

            if (!qte.IsInJudgeWindow(_qtePhaseTimer)) continue;

            if (swipeSpeed < qte.config.swipeMinSpeed)
            {
                Debug.Log($"[QTEController] 划动速度不足: {swipeSpeed:F0} < {qte.config.swipeMinSpeed}");
                continue;
            }

            // 检查划动是否经过指示器区域
            Rect? indicatorRect = GetIndicatorScreenRect(qte);
            if (indicatorRect == null) continue;

            if (!LineIntersectsRect(startScreenPos, releaseScreenPos, indicatorRect.Value))
                continue;

            float targetAngle = qte.config.swipeDirection;
            float swipeAngle = Mathf.Atan2(swipeDirection.y, swipeDirection.x) * Mathf.Rad2Deg;
            if (swipeAngle < 0f) swipeAngle += 360f;

            float diff = Mathf.Abs(Mathf.DeltaAngle(swipeAngle, targetAngle));
            if (diff <= qte.config.swipeAngleTolerance)
            {
                ResolveQTE(qte, true);
                return true;
            }
            else
            {
                Debug.Log($"[QTEController] 划动角度偏差过大: {diff:F1}° > {qte.config.swipeAngleTolerance}° (目标{qte.config.swipeDirection}°)");
            }
        }
        return false;
    }

    private Camera _qteCanvasCamera;
    private bool _qteCanvasCameraChecked;

    private Camera GetQTECanvasCamera()
    {
        if (!_qteCanvasCameraChecked)
        {
            _qteCanvasCameraChecked = true;
            if (qteDisplay == null) qteDisplay = FindObjectOfType<QTEDisplay>();
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
    /// 检查 QTE 判定窗口开始（通知 QTEDisplay 触发放大闪白）
    /// </summary>
    private void CheckJudgmentStart()
    {
        foreach (var qte in _activeQTEs)
        {
            if (qte.resolved || qte.judgmentStarted) continue;
            if (_qtePhaseTimer >= qte.warningEndTime && qte.indicator != null)
            {
                qte.judgmentStarted = true;
                if (qteDisplay != null)
                    qteDisplay.OnJudgmentStart(qte.indicator);
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
            qteDisplay = FindObjectOfType<QTEDisplay>();
            if (qteDisplay == null)
            {
                Debug.LogWarning("[QTEController] 未找到 QTEDisplay");
                return;
            }
            Debug.Log($"[QTEController] 找到 QTEDisplay: {qteDisplay.gameObject.name}");
        }
        Debug.Log($"[QTEController] 生成指示器: type={qte.config.qteType}, prefab={qte.config.qteIndicatorPrefab?.name}, pos={qte.config.screenPosition}");
        qte.indicator = qteDisplay.SpawnIndicator(qte.config);
        if (qte.indicator != null)
            Debug.Log($"[QTEController] 指示器已生成: {qte.indicator.name}, active={qte.indicator.activeSelf}, parent={qte.indicator.transform.parent?.name}");
        else
            Debug.LogWarning("[QTEController] SpawnIndicator返回null!");
    }

    private void ResolveQTE(QTEInstance qte, bool success, bool earlyFail = false)
    {
        qte.resolved = true;
        qte.success = success;

        if (success)
        {
            OnQTESuccessSingle(qte);
        }
        else
        {
            OnQTEFailureSingle(qte);
        }

        // 通知 QTEDisplay 播放结果特效
        if (qteDisplay != null && qte.indicator != null)
        {
            if (earlyFail)
                qteDisplay.CancelIndicatorEarly(qte.indicator);
            else
                qteDisplay.ShowQTEResult(qte.indicator, success);
        }
    }

    private void OnQTESuccessSingle(QTEInstance qte)
    {
        if (enemy != null && _currentAttack != null)
            enemy.TakeQTEPoiseDamage(qte.config.poiseDamage, _currentAttack.interruptibleOnStun);

        if (UltimateSystem.Instance != null)
            UltimateSystem.Instance.AddEnergy(qte.config.ultimateEnergyGain);

        OnQTESuccess?.Invoke();
    }

    private void OnQTEFailureSingle(QTEInstance qte)
    {
        // 失败伤害延迟到 CompleteQTEAttack 时应用，匹配动画时间点
        OnQTEFailure?.Invoke();
    }

    #endregion

    #region QTE 攻击收尾

    /// <summary>
    /// 中止 QTE（敌人被打入 Stun/Launch/Dead 等非 QTEAttacking 状态）
    /// </summary>
    public void AbortQTE()
    {
        // 清理飞行物
        if (_activeProjectile != null)
        {
            var proj = _activeProjectile.GetComponent<QTEProjectile>();
            if (proj != null) proj.ContinuePassThrough(0.5f, null);
            else Destroy(_activeProjectile);
            _activeProjectile = null;
        }

        StopQTEAnimation();

        if (qteDisplay != null)
            qteDisplay.ClearAllIndicators();

        _activeQTEs.Clear();
        _state = QTEState.Idle;
        OnQTEAttackFinished?.Invoke();
        Debug.Log("[QTEController] QTE已中止");
    }

    private void CompleteQTEAttack()
    {
        _state = QTEState.QTECompleted;

        // 收集 QTE 失败伤害（延迟到此时匹配动画时间点）
        float totalFailureDamage = 0f;
        bool anyFailed = false;
        foreach (var qte in _activeQTEs)
        {
            if (qte.resolved && !qte.success)
            {
                anyFailed = true;
                totalFailureDamage += qte.config.failureDamage;
            }
        }
        if (totalFailureDamage > 0f && PlayerState.Instance != null)
        {
            PlayerState.Instance.TakeDamage(totalFailureDamage);
            Debug.Log($"[QTEController] QTE失败伤害: {totalFailureDamage:F0}");
        }

        // 飞行物处理
        if (_activeProjectile != null)
        {
            var proj = _activeProjectile.GetComponent<QTEProjectile>();
            if (anyFailed && proj != null)
            {
                // 任一失败 → 飞行物穿过摄像机
                proj.ContinuePassThrough(0.8f, null);
            }
            else if (proj != null)
            {
                // 全部成功 → 销毁飞行物
                proj.DestroyOnSuccess();
            }
            else
            {
                Destroy(_activeProjectile);
            }
            _activeProjectile = null;
        }

        // 通知敌人恢复
        enemy.ExitQTEAttack();

        // 停止 QTE 精灵动画
        StopQTEAnimation();

        // 清理指示器
        if (qteDisplay != null)
            qteDisplay.ClearAllIndicators();

        // 前进到下一个 QTE 攻击
        _currentAttackIndex++;
        if (_currentAttackIndex >= qteData.qteAttacks.Count)
        {
            if (qteData.loopAttacks)
                _currentAttackIndex = 0;
            else
            {
                _state = QTEState.Idle;
                OnQTECompleted?.Invoke();
                OnQTEAttackFinished?.Invoke();
                return;
            }
        }

        _state = QTEState.Idle;
        OnQTECompleted?.Invoke();
        OnQTEAttackFinished?.Invoke();
    }

    #endregion
}
