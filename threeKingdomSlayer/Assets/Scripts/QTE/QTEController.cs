using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// QTE 状态机
/// Idle → CoolingDown → WaitingForAttackFinish → PerformingQTEAttack → QTEJudging → QTECompleted → CoolingDown
///
/// QTE 阶段概念：
///   - PerformingQTEAttack: 等待 QTE 阶段开始（飞行物到达 / 动画前摇结束）
///   - QTE 阶段开始后，slot delay 相对于阶段开始计时
///   - 全部 QTE 成功 → 销毁飞行物；任一失败 → 飞行物穿过摄像机
/// </summary>
public enum QTEState
{
    Idle,
    CoolingDown,
    WaitingForAttackFinish,
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
    private float _qteTimer;              // 攻击开始后的通用计时器
    private bool _qtePhaseStarted;        // QTE 阶段是否已开始（飞行物到达 / 动画前摇结束）
    private float _qtePhaseTimer;         // QTE 阶段开始后的计时器（用于 QTE spawn/judge）
    private QTEAttackConfig _currentAttack;
    private List<QTEInstance> _activeQTEs = new List<QTEInstance>();
    private GameObject _activeProjectile;

    // QTE 精灵动画
    private SpriteRenderer _enemySpriteRenderer;
    private PingPongAnim _enemyPingPongAnim;
    private Sprite _originalSprite;
    private Coroutine _qteAnimCoroutine;

    // 事件
    public System.Action OnQTETriggered;       // QTE 攻击触发
    public System.Action OnQTESuccess;         // QTE 判定成功
    public System.Action OnQTEFailure;         // QTE 判定失败
    public System.Action OnQTECompleted;       // 一轮 QTE 攻击结束

    public QTEState State => _state;

    private void Awake()
    {
        if (enemy == null) enemy = GetComponent<Enemy>();
    }

    private void Start()
    {
        Debug.Log($"[QTEController] Start: enemy={enemy?.name}, isBoss={enemy?.isBoss}, bossState={enemy?.bossState}, qteData={qteData?.name}, qteAttacksCount={qteData?.qteAttacks?.Count}");
        if (enemy != null && enemy.isBoss)
        {
            enemy.OnBossEngaged += OnBossEngaged;
            Debug.Log($"[QTEController] 已订阅 OnBossEngaged, bossState={enemy.bossState}");
            // 若 Boss 在 Start() 之前已进入战斗（如 StartBossPhaseAdvance 在 RegisterEnemy 中
            // 先于本 Start 触发 OnBossEngaged），补启动 QTE 冷却
            if (enemy.bossState == BossState.InCombat)
            {
                Debug.Log("[QTEController] 补触发 OnBossEngaged（Start时已在InCombat）");
                OnBossEngaged(enemy);
            }
        }
        else
        {
            Debug.LogWarning($"[QTEController] Start跳过: enemy={(enemy==null?"null":"notBoss")}");
        }
    }

    private void OnDestroy()
    {
        if (enemy != null) enemy.OnBossEngaged -= OnBossEngaged;
        KillProjectileSequence();
    }

    private void Update()
    {
        if (qteData == null || enemy == null) return;

        switch (_state)
        {
            case QTEState.CoolingDown:
                UpdateCooldown();
                break;
            case QTEState.PerformingQTEAttack:
                UpdatePerforming();
                break;
            case QTEState.QTEJudging:
                UpdateJudging();
                break;
        }
    }

    #region 状态切换

    private void OnBossEngaged(Enemy boss)
    {
        Debug.Log($"[QTEController] OnBossEngaged: qteData={qteData?.name}, attacksCount={qteData?.qteAttacks?.Count}");
        if (qteData == null || qteData.qteAttacks.Count == 0)
        {
            Debug.LogWarning("[QTEController] OnBossEngaged 跳过: qteData为空或无qteAttacks");
            return;
        }
        _currentAttackIndex = 0;
        Debug.Log($"[QTEController] 开始QTE冷却: firstQTECooldown={qteData.firstQTECooldown}s");
        StartCooldown(qteData.firstQTECooldown);
    }

    private void StartCooldown(float duration)
    {
        Debug.Log($"[QTEController] StartCooldown: {duration}s, 状态 {_state} → CoolingDown");
        _state = QTEState.CoolingDown;
        _qteTimer = duration;
    }

    private void UpdateCooldown()
    {
        _qteTimer -= Time.deltaTime;
        if (_qteTimer <= 0f)
        {
            Debug.Log($"[QTEController] 冷却结束, isAttackAnimating={enemy.isAttackAnimating}");
            if (enemy.isAttackAnimating)
            {
                Debug.Log("[QTEController] 等待当前攻击完成...");
                _state = QTEState.WaitingForAttackFinish;
            }
            else
            {
                Debug.Log("[QTEController] 触发QTE攻击");
                TriggerQTEAttack();
            }
        }
    }

    public void OnEnemyAttackComplete()
    {
        Debug.Log($"[QTEController] OnEnemyAttackComplete: state={_state}");
        if (_state == QTEState.WaitingForAttackFinish)
        {
            Debug.Log("[QTEController] 攻击完成，触发QTE攻击");
            TriggerQTEAttack();
        }
    }

    private void TriggerQTEAttack()
    {
        Debug.Log($"[QTEController] TriggerQTEAttack: attackIndex={_currentAttackIndex}, totalAttacks={qteData.qteAttacks.Count}");
        if (qteData == null || qteData.qteAttacks.Count == 0) return;
        _currentAttack = qteData.qteAttacks[_currentAttackIndex];
        Debug.Log($"[QTEController] 当前攻击: {_currentAttack?.name}, slots={_currentAttack?.qteSlots?.Count}");
        _state = QTEState.PerformingQTEAttack;
        _qteTimer = 0f;
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

    #region QTE 精灵动画

    private void StartQTEAnimation()
    {
        if (_currentAttack == null || _currentAttack.qteAnimationFrames == null || _currentAttack.qteAnimationFrames.Length == 0)
            return;

        // 懒加载缓存
        if (_enemySpriteRenderer == null)
        {
            _enemySpriteRenderer = enemy.GetComponent<SpriteRenderer>();
            _enemyPingPongAnim = enemy.GetComponent<PingPongAnim>();
        }
        if (_enemySpriteRenderer == null) return;

        _originalSprite = _enemySpriteRenderer.sprite;

        // 暂停 PingPongAnim（避免与 QTE 动画冲突）
        if (_enemyPingPongAnim != null)
            _enemyPingPongAnim.enabled = false;

        _qteAnimCoroutine = StartCoroutine(PlayQTEAnimation());
    }

    private System.Collections.IEnumerator PlayQTEAnimation()
    {
        var frames = _currentAttack.qteAnimationFrames;
        float interval = 1f / Mathf.Max(_currentAttack.qteAnimationFPS, 1f);
        int count = frames.Length;

        for (int i = 0; i < count; i++)
        {
            if (_state != QTEState.PerformingQTEAttack && _state != QTEState.QTEJudging)
                yield break;
            _enemySpriteRenderer.sprite = frames[i];
            yield return new WaitForSeconds(interval);
        }
    }

    private void StopQTEAnimation()
    {
        if (_qteAnimCoroutine != null)
        {
            StopCoroutine(_qteAnimCoroutine);
            _qteAnimCoroutine = null;
        }

        // 恢复原始精灵
        if (_enemySpriteRenderer != null && _originalSprite != null)
            _enemySpriteRenderer.sprite = _originalSprite;

        // 恢复 PingPongAnim
        if (_enemyPingPongAnim != null)
            _enemyPingPongAnim.enabled = true;
    }

    #endregion

    #endregion

    #region QTE 判定

    private void UpdatePerforming()
    {
        _qteTimer += Time.deltaTime;

        if (!_qtePhaseStarted)
        {
            // 无飞行物时，等待动画前摇结束后开始 QTE 阶段
            if (_activeProjectile == null && _qteTimer >= _currentAttack.animationLeadTime)
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
            if (_state != QTEState.QTEJudging && _state != QTEState.PerformingQTEAttack) return false;
            return _qtePhaseStarted;
        }
    }

    public bool TryQTEClick(Vector2 screenPos)
    {
        if (_state != QTEState.QTEJudging && _state != QTEState.PerformingQTEAttack) return false;
        if (!_qtePhaseStarted) return false;

        foreach (var qte in _activeQTEs)
        {
            if (qte.resolved || qte.config.qteType != QTEType.Click) continue;
            if (!qte.IsInJudgeWindow(_qtePhaseTimer)) continue;

            if (IsClickInQTEArea(screenPos, qte))
            {
                ResolveQTE(qte, true);
                return true;
            }
        }
        return false;
    }

    public bool TryQTESwipe(Vector2 startScreenPos, Vector2 swipeDirection, float swipeSpeed, Vector2 releaseScreenPos)
    {
        if (_state != QTEState.QTEJudging && _state != QTEState.PerformingQTEAttack) return false;
        if (!_qtePhaseStarted) return false;

        foreach (var qte in _activeQTEs)
        {
            if (qte.resolved || qte.config.qteType != QTEType.Swipe) continue;
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

    private void ResolveQTE(QTEInstance qte, bool success)
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
            qteDisplay.ShowQTEResult(qte.indicator, success);
        }
    }

    private void OnQTESuccessSingle(QTEInstance qte)
    {
        if (enemy != null)
            enemy.TakePoiseDamage(qte.config.poiseDamage);

        if (UltimateSystem.Instance != null)
            UltimateSystem.Instance.AddEnergy(qte.config.ultimateEnergyGain);

        OnQTESuccess?.Invoke();
    }

    private void OnQTEFailureSingle(QTEInstance qte)
    {
        if (PlayerState.Instance != null)
            PlayerState.Instance.TakeDamage(qte.config.failureDamage);

        OnQTEFailure?.Invoke();
    }

    #endregion

    #region QTE 攻击收尾

    private void CompleteQTEAttack()
    {
        _state = QTEState.QTECompleted;

        // 检查是否有 QTE 失败
        bool anyFailed = false;
        foreach (var qte in _activeQTEs)
        {
            if (qte.resolved && !qte.success)
            { anyFailed = true; break; }
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
                return;
            }
        }

        float cooldown = _currentAttack != null ? _currentAttack.cooldownAfterQTE : qteData.baseQTECooldown;
        StartCooldown(cooldown);
        OnQTECompleted?.Invoke();
    }

    #endregion
}
