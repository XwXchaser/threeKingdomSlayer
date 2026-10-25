using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;


public sealed partial class StabSweepEffect : MonoBehaviour
{
    private const float ThrustDuration = 0.2f;
    private const float RetractDuration = 0.3f;
    private const float WindupRatio = 0.12f;
    private const float ThrustRatio = 0.28f;
    private const float PenetrationRatio = 0.08f;
    private const float RetractRatio = 0.52f;
    private const float WindupDistance = 0.4f;
    private const float PenetrationDistance = 0.32f;
    // 蓄力：更明显压缩，长度0.92、宽度1.08
    private const float WindupLengthScale = 0.92f;
    private const float WindupWidthScale = 1.08f;
    // 高速刺出：更明显拉伸，长度1.18、宽度0.86
    private const float ThrustLengthScale = 1.18f;
    private const float ThrustWidthScale = 0.86f;
    // 首次命中：更明显压缩到长度0.90、宽度1.15，回弹至长度1.10、宽度0.92
    private const float HitCompressLength = 0.90f;
    private const float HitCompressWidth = 1.15f;
    private const float HitBounceLength = 1.10f;
    private const float HitBounceWidth = 0.92f;
    private const float SpeedFrameStartRatio = 0.1f;
    private const float SpeedFrameEndRatio = 1.0f;
    private const float HitDistanceTolerance = 0.35f;
    private const int SortingOrderWithEnemies = 0;

    private ColumnManager _columnManager;
    private int _column;
    private int _rangeRows;
    private int _visualRangeRows;
    private float _damage;
    private DamageType _damageType;
    private bool _interruptsCavalryCharge;
    /// <summary>本次招式是否“重攻击”（霸体反制：重攻击能打断霸体）</summary>
    private bool _isHeavyAttack;
    /// <summary>本次招式的削韧值（仅在霸体窗口内生效，归零 → 破势眩晕）</summary>
    private float _poiseDamage;
    private HitReactionDirection _hitReactionDirection = HitReactionDirection.None;
    private bool _suppressHitReaction;
    private readonly HashSet<Enemy> _hitEnemies = new HashSet<Enemy>();
    private readonly List<Enemy> _hitCandidates = new List<Enemy>();
    private StabMotionParams _motion = StabMotionParams.Default;
    private StabStartPose _startPose = StabStartPose.None;
    private bool _hitsEnabled;
    private Enemy _coveredBossTarget;
    private Action<Enemy> _onHit;
    private Func<Enemy, bool> _onFirstHitBeforeDamage;
    private Action _onFirstHit;
    private Action _onComplete;
    private Vector3 _rayOrigin;
    private Vector3 _rayDirection;
    private float _rayLength;
    private Vector3 _visualBaseLocalPosition;
    private Vector3 _visualBaseLocalScale;
    private Vector3 _visualTargetOffsetLocal;
    private Transform _visualTransform;
    private Transform _visualOffsetRoot;
    private Transform _deformRoot;
    private WeaponMotionBlurController _motionBlur;
    private MeshWeaponAfterimage _afterimage;
    private bool _enableWeaponAfterimage;
    private WeaponTasselDynamics _tasselDynamics;
    private Sequence _sequence;
    private Coroutine _hitStopRoutine;
    private bool _hitAny;
    private bool _usingSpeedSprite;
    /// <summary>视觉体半长（世界单位）。精灵取 sprite.bounds，网格取包围盒长轴。</summary>
    private float _halfVisualLength;
    private Coroutine _hitDeformationRoutine;
    private ChargeStabVisual _chargeVisual;
    private SpriteRenderer _renderer;
    private Sprite _baseSprite;
    private bool _chargeHoldPending;
    private bool _chargeHoldActive;
    private bool _chargeHoldFull;
    private bool _chargeFrameApplied;
    private float _chargeHoldFromRatio;
    private float _chargeHoldRatio;
    private float _chargeHoldPull;
    private float _chargeHoldHeldSeconds;
    private float _chargeHoldShakePhase;

    /// <summary>震动专用节点：只承载命中震动偏移，避免与主序列的时间线抢同一 transform</summary>
    private Transform _shakeRoot;
    /// <summary>蓄力指向：来自 AttackSystem 的「当前朝向 → 指向列」偏摆增量（度）</summary>
    private float _chargeAimYawDelta;
    /// <summary>当前实际施加的偏摆角（度），用于回收段收回</summary>
    private float _chargeAppliedYaw;
    // 命中震动：独立 realtime 计时，不受卡肉（seq.Pause）影响
    private bool _shakeActive;
    private float _shakeStartRealtime;
    private float _shakeScale = 1f;
    private float _chargeAppliedTilt;
    private bool _chargeRecoverActive;
    private float _chargeRecoverTime;
    private float _chargeRecoverDuration;
    private Vector3 _chargeRecoverFromPosition;
    private Vector3 _chargeRecoverFromOffset;
    private float _chargeRecoverFromRoll;
    private float _retractStartTime;
    private float _retractDurationCached;
    private Quaternion _deformBaseRotation;
    private Vector3 _retractFromPosition;
    private Vector3 _retractToPosition;
    private Vector3 _retractFromScale;
    private Vector3 _retractToScale;
    private Vector3 _retractFromVisualOffset;

    public static StabSweepEffect Create(GameObject prefab, Sprite speedSprite, Vector3 startPosition, Vector3 targetPosition, int column, int rangeRows, int visualRangeRows,
        float damage, DamageType damageType, ColumnManager columnManager, Enemy coveredBossTarget,
        Action<Enemy> onHit, Func<Enemy, bool> onFirstHitBeforeDamage, Action onFirstHit, Action onComplete,
        float visualReachOffset, float visualStartXOffset, float visualTargetRandomRadius, float baseRayLength,
        float targetDuration = -1f, bool interruptsCavalryCharge = false,
        HitReactionDirection hitReactionDirection = HitReactionDirection.None, bool suppressHitReaction = false,
        StabMotionParams motion = default, StabStartPose startPose = default,
        ChargeStabVisual chargeVisual = null, bool enableWeaponAfterimage = false,
        bool isHeavyAttack = false, float poiseDamage = 0f)
    {
        var ray = new GameObject("StabRay");
        ray.transform.position = startPosition;
        Vector3 rayVector = targetPosition - startPosition;
        ray.transform.rotation = Quaternion.LookRotation(rayVector.normalized, Vector3.up);

        var visual = Instantiate(prefab, ray.transform);
        visual.name = "StabVisual";
        // 枪尖定位依赖视觉长度：精灵与网格都按同一语义测量，否则用网格时这一段会被跳过。
        float visualLength = MeasureVisualLength(visual);
        visual.transform.localPosition = Vector3.back * (visualLength * 0.5f - visualReachOffset);
        visual.transform.position += Vector3.right * visualStartXOffset;

        if (motion.windupRatio <= 0f) motion = StabMotionParams.Default;
        var effect = ray.AddComponent<StabSweepEffect>();
        effect.Initialize(visual, speedSprite, targetPosition, column, rangeRows, visualRangeRows, damage, damageType,
            columnManager, coveredBossTarget, onHit, onFirstHitBeforeDamage, onFirstHit, onComplete, targetDuration,
            visualTargetRandomRadius, baseRayLength, interruptsCavalryCharge, hitReactionDirection,
            suppressHitReaction, motion, startPose, chargeVisual, enableWeaponAfterimage,
            isHeavyAttack, poiseDamage);
        return effect;
    }

    /// <summary>
    /// 视觉体沿「枪身长轴」的长度（世界单位）。
    /// 精灵沿用 sprite.bounds（保留像素长轴、含透明边距的旧行为，数值不变）；
    /// 网格则把各 Renderer 的局部包围盒八个角变换到 visual 根节点空间后取最长边，
    /// 因此结果不受父级 StabRay 朝向影响。两种 prefab 同为 (90,0,0) 姿态，语义一致。
    /// </summary>
    private static float MeasureVisualLength(GameObject visual)
    {
        var dynamics = visual.GetComponentInChildren<WeaponTasselDynamics>();
        if (dynamics != null && dynamics.WeaponLength > 0f)
            return dynamics.WeaponLength;
        var spriteRenderer = visual.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
            return spriteRenderer.sprite.bounds.size.y * visual.transform.localScale.y;

        Bounds bounds = ComputeVisualLocalBounds(visual);
        float localLength = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
        return localLength * visual.transform.localScale.y;
    }

    /// <summary>把所有子 Renderer 的包围盒并到 visual 根节点的局部空间（精灵/网格通用）</summary>
    private static Bounds ComputeVisualLocalBounds(GameObject visual)
    {
        var renderers = visual.GetComponentsInChildren<Renderer>();
        Transform root = visual.transform;
        bool hasBounds = false;
        Bounds accumulated = new Bounds(Vector3.zero, Vector3.zero);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            Bounds local = r.localBounds;
            Vector3 center = local.center;
            Vector3 extents = local.extents;
            for (int sx = -1; sx <= 1; sx += 2)
            for (int sy = -1; sy <= 1; sy += 2)
            for (int sz = -1; sz <= 1; sz += 2)
            {
                Vector3 worldCorner = r.transform.TransformPoint(
                    center + Vector3.Scale(extents, new Vector3(sx, sy, sz)));
                Vector3 rootLocal = root.InverseTransformPoint(worldCorner);
                if (!hasBounds)
                {
                    accumulated = new Bounds(rootLocal, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    accumulated.Encapsulate(rootLocal);
                }
            }
        }

        return hasBounds ? accumulated : new Bounds(Vector3.zero, Vector3.zero);
    }

    private void Initialize(GameObject visual, Sprite speedSprite, Vector3 targetPosition, int column, int rangeRows, int visualRangeRows, float damage,
        DamageType damageType, ColumnManager columnManager, Enemy coveredBossTarget,
        Action<Enemy> onHit, Func<Enemy, bool> onFirstHitBeforeDamage, Action onFirstHit,
        Action onComplete, float targetDuration, float visualTargetRandomRadius, float baseRayLength,
        bool interruptsCavalryCharge = false, HitReactionDirection hitReactionDirection = HitReactionDirection.None,
        bool suppressHitReaction = false, StabMotionParams motion = default, StabStartPose startPose = default,
        ChargeStabVisual chargeVisual = null, bool enableWeaponAfterimage = false,
        bool isHeavyAttack = false, float poiseDamage = 0f)
    {
        _column = column;
        _rangeRows = rangeRows;
        _visualRangeRows = visualRangeRows;
        _damage = damage;
        _damageType = damageType;
        _interruptsCavalryCharge = interruptsCavalryCharge;
        _isHeavyAttack = isHeavyAttack;
        _poiseDamage = poiseDamage;
        _hitReactionDirection = hitReactionDirection;
        _suppressHitReaction = suppressHitReaction;
        _motion = motion.windupRatio <= 0f ? StabMotionParams.Default : motion;
        _startPose = startPose;
        _columnManager = columnManager;
        _coveredBossTarget = coveredBossTarget;
        _onHit = onHit;
        _onFirstHitBeforeDamage = onFirstHitBeforeDamage;
        _onFirstHit = onFirstHit;
        _onComplete = onComplete;
        _chargeVisual = chargeVisual;
        _enableWeaponAfterimage = enableWeaponAfterimage;

        var renderer = visual.GetComponentInChildren<SpriteRenderer>();
        if (renderer != null)
            renderer.sortingOrder = SortingOrderWithEnemies;

        _tasselDynamics = visual.GetComponentInChildren<WeaponTasselDynamics>();
        _visualTransform = visual.transform;
        _visualBaseLocalPosition = _visualTransform.localPosition;
        _visualBaseLocalScale = _visualTransform.localScale;
        Quaternion visualBaseLocalRotation = _visualTransform.localRotation;
        Matrix4x4 visualWorldMatrix = _visualTransform.localToWorldMatrix;

        _halfVisualLength = MeasureVisualLength(visual) * 0.5f;

        _deformRoot = new GameObject("DeformRoot").transform;
        _visualOffsetRoot = new GameObject("VisualOffsetRoot").transform;
        _visualOffsetRoot.SetParent(transform, false);
        _visualOffsetRoot.localPosition = Vector3.zero;
        _visualOffsetRoot.localRotation = Quaternion.identity;
        _visualOffsetRoot.localScale = Vector3.one;
        _shakeRoot = new GameObject("ShakeRoot").transform;
        _shakeRoot.SetParent(_visualOffsetRoot, false);
        _shakeRoot.localPosition = Vector3.zero;
        _shakeRoot.localRotation = Quaternion.identity;
        _shakeRoot.localScale = Vector3.one;
        _deformRoot.SetParent(_shakeRoot, false);
        _deformRoot.localPosition = _visualBaseLocalPosition;
        _deformRoot.localRotation = visualBaseLocalRotation;
        _deformRoot.localScale = _visualBaseLocalScale;
        _visualTransform.SetParent(_deformRoot, false);
        _visualTransform.localPosition = Vector3.zero;
        _visualTransform.localRotation = Quaternion.identity;
        _visualTransform.localScale = Vector3.one;

        if (!ApproximatelyEqual(visualWorldMatrix, _visualTransform.localToWorldMatrix))
            Debug.LogError("[StabSweepEffect] DeformRoot hierarchy migration changed the visual world transform");

        _motionBlur = renderer != null
            ? new WeaponMotionBlurController(renderer, 0.4f, 0.02f, 32f)
            : null;

        _rayOrigin = transform.position;
        _rayDirection = (targetPosition - _rayOrigin).normalized;
        _rayLength = Vector3.Distance(_rayOrigin, targetPosition);
        transform.rotation = Quaternion.LookRotation(_rayDirection, Vector3.up);
        _visualTargetOffsetLocal = CreateVisualTargetOffset(visualTargetRandomRadius, baseRayLength);

        // 连段交接：从上一段当前姿态起步（位置与朝向在起手阶段过渡到本段目标列）
        if (_startPose.valid)
        {
            transform.position = _startPose.worldPosition;
            transform.rotation = _startPose.worldRotation;
            _deformRoot.localScale = GetVisualScale(1f, Mathf.Max(0.1f, _startPose.lengthScale));
        }

        Quaternion deformBaseRotation = _deformRoot.localRotation;

        float totalDuration = targetDuration > 0f ? targetDuration : ThrustDuration + RetractDuration;
        float windupDuration = totalDuration * _motion.windupRatio;
        float thrustDuration = totalDuration * _motion.thrustRatio;
        float penetrationDuration = totalDuration * _motion.penetrationRatio;
        float holdDuration = Mathf.Max(0f, _motion.windupHoldSeconds);
        float retractDuration = Mathf.Max(0.02f,
            totalDuration - windupDuration - holdDuration - thrustDuration - penetrationDuration);
        Vector3 windupPosition = _rayOrigin - _rayDirection * WindupDistance;
        Vector3 penetrationPosition = targetPosition + _rayDirection * PenetrationDistance;
        Vector3 windupScale = GetVisualScale(WindupWidthScale, WindupLengthScale);
        Vector3 thrustScale = GetVisualScale(_motion.thrustWidthScale, _motion.thrustLengthScale);
        Sprite baseSprite = renderer != null ? renderer.sprite : null;
        _renderer = renderer;
        _baseSprite = baseSprite;
        _sequence = DOTween.Sequence().SetTarget(transform);
        Quaternion aimRotation = Quaternion.LookRotation(_rayDirection, Vector3.up);
        // 画面内姿态倾角：整段保持，作为持枪姿态语言（不改变轨迹）
        if (_motion.visualTiltDegrees != 0f)
            _visualOffsetRoot.localRotation = Quaternion.Euler(0f, 0f, _motion.visualTiltDegrees);
        _sequence.Append(transform.DOMove(windupPosition, windupDuration).SetEase(Ease.OutQuad));
        _sequence.Join(_deformRoot.DOScale(GetVisualScale(WindupWidthScale, WindupLengthScale), windupDuration).SetEase(Ease.OutQuad));
        // 交接时从上一段朝向转到本段目标朝向（转向发生在起手阶段，命中前完成）
        if (_startPose.valid)
        {
            float snapRatio = Mathf.Clamp01(_motion.redirectSnapRatio);
            if (snapRatio > 0f && windupDuration > 0.01f)
            {
                // 释放指向：先用起手段的一部分「甩」过去并过冲一点，再回正到目标朝向
                float snapDuration = Mathf.Max(0.01f, windupDuration * snapRatio);
                float remainDuration = Mathf.Max(0f, windupDuration - snapDuration);
                float startYaw = _startPose.worldRotation.eulerAngles.y;
                float travelSign = Mathf.Sign(Mathf.DeltaAngle(startYaw, aimRotation.eulerAngles.y));
                if (Mathf.Approximately(travelSign, 0f)) travelSign = 1f;
                Quaternion overshootRotation = aimRotation * Quaternion.Euler(0f, _motion.redirectOvershootDegrees * travelSign, 0f);

                _sequence.Join(transform.DORotateQuaternion(overshootRotation, snapDuration).SetEase(Ease.OutQuad));
                if (remainDuration > 0.005f)
                    _sequence.Insert(snapDuration,
                        transform.DORotateQuaternion(aimRotation, remainDuration).SetEase(Ease.OutCubic));
            }
            else
            {
                _sequence.Join(transform.DORotateQuaternion(aimRotation, windupDuration).SetEase(Ease.OutQuad));
            }
        }
        if (holdDuration > 0f)
            _sequence.AppendInterval(holdDuration);

        // 命中只能在刺出阶段开始之后生效（交接时起始位置可能已经在敌人前方）
        _sequence.InsertCallback(windupDuration + holdDuration, () => _hitsEnabled = true);
        _sequence.Append(transform.DOMove(targetPosition, thrustDuration).SetEase(Ease.InCubic)
            .OnStart(() =>
            {
                _motionBlur?.SetStrength(_motion.blurThrust * _motion.motionBlurScale);
                _afterimage?.SetEmission(true);
            })
            .OnUpdate(CheckHits));
        // 绕枪身长轴自转：刺出段 0 → 设定值
        if (_motion.rollDegrees != 0f)
            _sequence.Join(DOTween.To(() => 0f, v => ApplyDeformRoll(deformBaseRotation, v), _motion.rollDegrees, thrustDuration).SetEase(Ease.OutQuad));
        _sequence.Join(_visualOffsetRoot.DOLocalMove(_visualTargetOffsetLocal, thrustDuration).SetEase(Ease.OutCubic));
        _sequence.Join(_deformRoot.DOScale(GetVisualScale(ThrustWidthScale, ThrustLengthScale), thrustDuration).SetEase(Ease.InCubic));
        if (renderer != null && baseSprite != null && speedSprite != null)
        {
            float speedFrameStart = windupDuration + holdDuration + thrustDuration * _motion.speedFrameStart01;
            float speedFrameEnd = windupDuration + holdDuration + thrustDuration * _motion.speedFrameEnd01;
            _sequence.InsertCallback(speedFrameStart, () =>
            {
                renderer.sprite = speedSprite;
                _usingSpeedSprite = true;
                _motionBlur?.SetStrength(_motion.blurSpeedFrame * _motion.motionBlurScale);
            });
            _sequence.InsertCallback(speedFrameEnd, () => RestoreBaseSprite(renderer, baseSprite));
        }
        _sequence.AppendCallback(CheckHits);
        _sequence.AppendCallback(() => _onComplete?.Invoke());
        _sequence.Append(transform.DOMove(penetrationPosition, penetrationDuration).SetEase(Ease.OutQuad)
            .OnStart(() => _motionBlur?.SetStrength(_motion.blurPenetration * _motion.motionBlurScale))
            .OnUpdate(CheckHits));
        _sequence.Join(_deformRoot.DOScale(_visualBaseLocalScale, penetrationDuration).SetEase(Ease.OutQuad));
        _sequence.Append(transform.DOMove(_rayOrigin, retractDuration).SetEase(Ease.OutCubic)
            .OnStart(() => _motionBlur?.SetStrength(0f)));
        // 自转在回收段转回 0，保证每段起步时枪身面朝一致
        if (_motion.rollDegrees != 0f)
            _sequence.Join(DOTween.To(() => _motion.rollDegrees, v => ApplyDeformRoll(deformBaseRotation, v), 0f, retractDuration).SetEase(Ease.OutCubic));
        _sequence.Join(_visualOffsetRoot.DOLocalMove(Vector3.zero, retractDuration).SetEase(Ease.OutCubic));
        _sequence.Join(_deformRoot.DOScale(_visualBaseLocalScale, retractDuration).SetEase(Ease.OutCubic));
        // 连段蓄力：按下后由本组件接管回收，把枪体「拉回蓄势位」，因此记录回收路径两端
        _retractStartTime = windupDuration + holdDuration + thrustDuration + penetrationDuration;
        _retractDurationCached = retractDuration;
        _retractFromPosition = penetrationPosition;
        _retractToPosition = _rayOrigin;
        _retractFromScale = _visualBaseLocalScale;   // 回收段起点缩放已是基准（穿入段已还原）
        _retractToScale = _visualBaseLocalScale;
        _retractFromVisualOffset = _visualTargetOffsetLocal;
        _deformBaseRotation = deformBaseRotation;
        _sequence.InsertCallback(_retractStartTime, () =>
        {
            // 回收段是「向后拉」，此时再产生残影会读成拖泥带水（与 MeshWeaponTrail 的 retractSuppression 同一意图）。
            _afterimage?.EndEmission();
            TryHoldForCharge();
        });
        _sequence.OnKill(() =>
        {
            _visualTransform?.DOKill();
            Destroy(gameObject);
        });
        _sequence.OnComplete(() =>
        {
            _visualTransform?.DOKill();
            Destroy(gameObject);
        });

        // 残影/本体涂抹：与斩击共用同一组件，只在戳击实例上 opt-in；精灵枪预制体没有该组件。
        if (_enableWeaponAfterimage)
        {
            MeshWeaponVisual meshVisual = visual.GetComponentInChildren<MeshWeaponVisual>();
            _afterimage = visual.GetComponentInChildren<MeshWeaponAfterimage>(true);
            if (meshVisual != null && _afterimage != null)
            {
                _afterimage.Begin(meshVisual, MeshWeaponAfterimage.MotionProfile.Thrust);
                // 戳击只在刺出段出残影：起手与回收段由时间线显式关闭（见下面 thrust 的 OnStart 与 _retractStartTime）。
                _afterimage.SetEmission(false);
                if (_afterimage.IsRunning && _afterimage.ReplaceTipTrail)
                {
                    MeshWeaponTrail tipTrail = visual.GetComponentInChildren<MeshWeaponTrail>(true);
                    if (tipTrail != null) tipTrail.enabled = false;
                }
            }
            else
            {
                _afterimage = null;
            }
        }
    }

    /// <summary>绕枪身长轴的自转：在形变节点的本地 Y（也就是精灵长轴）上叠加角度</summary>
    private void ApplyDeformRoll(Quaternion baseRotation, float rollDegrees)
    {
        if (_deformRoot == null) return;
        _deformRoot.localRotation = baseRotation * Quaternion.Euler(0f, rollDegrees, 0f);
    }

    private void RestoreBaseSprite(SpriteRenderer renderer, Sprite baseSprite)
    {
        if (renderer == null) return;
        renderer.sprite = baseSprite;
        _usingSpeedSprite = false;
        _visualTransform.localPosition = Vector3.zero;
        _visualTransform.localScale = Vector3.one;
    }

    private Vector3 CreateVisualTargetOffset(float baseRadius, float baseRayLength)
    {
        if (baseRadius <= 0f || baseRayLength <= 0f || Camera.main == null)
            return Vector3.zero;

        float rangeScale = _rayLength / baseRayLength;
        float radius = baseRadius * Mathf.Sqrt(Mathf.Max(rangeScale, 0f));
        radius = Mathf.Clamp(radius, baseRadius * 0.5f, baseRadius * 1.5f);
        float angle = UnityEngine.Random.value * Mathf.PI * 2f;
        float distance = radius * Mathf.Sqrt(UnityEngine.Random.value);
        Vector3 offsetWorld = Camera.main.transform.right * (Mathf.Cos(angle) * distance)
            + Camera.main.transform.up * (Mathf.Sin(angle) * distance);
        return transform.InverseTransformVector(offsetWorld);
    }

    private static bool ApproximatelyEqual(Matrix4x4 a, Matrix4x4 b)
    {
        for (int row = 0; row < 4; row++)
            for (int column = 0; column < 4; column++)
                if (Mathf.Abs(a[row, column] - b[row, column]) > 0.0001f)
                    return false;
        return true;
    }

    private Vector3 GetVisualScale(float widthMultiplier, float lengthMultiplier)
    {
        return new Vector3(
            _visualBaseLocalScale.x * widthMultiplier,
            _visualBaseLocalScale.y * lengthMultiplier,
            _visualBaseLocalScale.z);
    }

    private void Update()
    {
        if (_chargeRecoverActive)
        {
            UpdateChargeRecover();
            return;
        }

        if (!_chargeHoldActive) return;

        float held = _chargeHoldHeldSeconds;
        float pullSeconds = _motion.chargeHoldPullSeconds;
        _chargeHoldPull = Mathf.Clamp01(held / pullSeconds);

        // 阶段一：拉到蓄势位；阶段二：到位后再向后一顿（表达开始蓄力），之后才开始前后微颤
        float target = Mathf.Clamp01(_motion.chargeHoldRetractRatio);
        float ratio = Mathf.Lerp(_chargeHoldFromRatio, target, _chargeHoldPull);
        float shake = 0f;
        if (held > pullSeconds)
        {
            float settleSeconds = _motion.chargeHoldSettleSeconds;
            float settle = settleSeconds > 0f
                ? Mathf.Clamp01((held - pullSeconds) / settleSeconds)
                : 1f;
            ratio = Mathf.Lerp(target, target + _motion.chargeHoldSettleRatio, settle);
            shake = _motion.chargeHoldShakeAmplitude * settle;
        }

        _chargeHoldRatio = Mathf.Clamp01(ratio);
        _chargeHoldShakePhase += Time.deltaTime * _motion.chargeHoldShakeFrequency * Mathf.PI * 2f;

        ApplyChargeHoldPose(_chargeHoldRatio, shake);
        ApplyChargeFrame();
    }

    private void LateUpdate()
    {
        UpdateHitShake();
        if (_deformRoot == null) return;
        float lengthDelta = _deformRoot.localScale.y - _visualBaseLocalScale.y;
        _deformRoot.localPosition = _visualBaseLocalPosition
            + Vector3.up * (_halfVisualLength * lengthDelta / Mathf.Max(_visualBaseLocalScale.y, 0.0001f));
    }

    /// <summary>指向偏摆增量（度）：由 AttackSystem 按「当前朝向 → 手指指向列」给出，实际角度被资产上限夹住</summary>
}
