using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 斩击（Slash）扇形扫掠特效：prefab 沿 X 轴水平移动 + Z 轴旋转，模拟挥刀弧线。
/// L→R: X 从 -halfWidth → +halfWidth，Z 旋转从 -fanAngle → +fanAngle
/// R→L: 完全镜像（坐标和旋转方向均反转）
/// </summary>
public class SweepEffect : MonoBehaviour
{
    private struct TargetEntry
    {
        public Enemy enemy;
        public float xThreshold;
    }

    private float damage;
    private DamageType damageType;
    private System.Action<Enemy> onHit;
    private System.Action onFirstHit;
    private bool _hasHit;
    private System.Action onAllHit;
    private bool _onAllHitInvoked;
    private bool canInterruptCFrame;
    private List<TargetEntry> targets = new List<TargetEntry>();
    private int nextIndex;
    private bool leftToRight;
    private Material mat;
    private WeaponMotionBlurController motionBlur;
    private Color waveColor;
    private Color? damageNumberColor;
    private Sequence seq;
    private Coroutine _hitStopRoutine;
    private float _creationTime;
    private int _instanceId;
    private bool _completed;
    public static int AliveCount { get; private set; }
    public float CreationTime => _creationTime;
    public DamageType DamageType => damageType;

    public static void Create(Vector3 centerPos, DamageType damageType, float damage,
        List<Enemy> targets, bool leftToRight, float halfWidth, float fanAngle, float duration,
        System.Action<Enemy> onHit = null, GameObject prefab = null, float? alphaOverride = null,
        Color? damageNumberColor = null, bool canInterruptCFrame = false,
        Material materialOverride = null, System.Action onFirstHit = null, System.Action onAllHit = null, float targetDuration = -1f,
        Sprite rotateSprite1 = null, Sprite rotateSprite2 = null, float angleOffset = 0f, float movementTilt = 0f,
        float additionalWeaponRotation = 0f, bool useEnhancedSlashMotion = false)
    {
        float startX = leftToRight ? -halfWidth : halfWidth;
        float endX = leftToRight ? halfWidth : -halfWidth;
        float startAngle = (leftToRight ? fanAngle : -fanAngle) + angleOffset;
        float endAngle = (leftToRight ? -fanAngle : fanAngle) + angleOffset + additionalWeaponRotation;

        Vector3 localStart = new Vector3(startX, 0f, 0f);
        Vector3 localEnd = new Vector3(endX, 0f, 0f);
        Quaternion pathRotation = Quaternion.Euler(0f, 0f, movementTilt);
        Vector3 spawnPos = centerPos + pathRotation * localStart;
        Vector3 endPos = centerPos + pathRotation * localEnd;
        GameObject obj;
        Material material = null;
        Color color;

        if (materialOverride != null)
        {
            material = new Material(materialOverride);
            color = material.color;
            color.a = alphaOverride ?? materialOverride.color.a;
            material.color = color;
        }
        else
        {
            color = GetSlashColor(damageType);
            color.a = alphaOverride ?? 1.0f;
        }

        if (prefab != null)
        {
            obj = Object.Instantiate(prefab, spawnPos, prefab.transform.rotation);
            obj.name = $"Slash_{damageType}";

            if (materialOverride != null)
            {
                Renderer r = obj.GetComponentInChildren<Renderer>();
                if (r != null) { r.material = material; }
            }
            else
            {
                // 正常路径：保持原图颜色和透明度
                color = Color.white;
                Renderer r = obj.GetComponentInChildren<Renderer>();
                if (r != null) { material = r.material; material.color = color; }
            }
        }
        else
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            obj.name = $"Slash_{damageType}";
            obj.transform.position = spawnPos;
            obj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            obj.transform.localScale = new Vector3(12f, 2.5f, 1f);
            Renderer renderer = obj.GetComponent<Renderer>();
            if (materialOverride != null)
            {
                renderer.material = material;
            }
            else
            {
                material = new Material(Shader.Find("Sprites/Default"));
                material.color = color;
                renderer.material = material;
            }
        }

        // L→R 时翻转 prefab X 使枪头朝向运动方向（必须在视觉分层前做）
        if (leftToRight)
        {
            Vector3 s = obj.transform.localScale;
            s.x = -Mathf.Abs(s.x);
            obj.transform.localScale = s;
        }

        Transform visualTransform = obj.transform;
        if (useEnhancedSlashMotion)
        {
            GameObject visualObject = obj;
            var motionRoot = new GameObject($"SlashMotion_{damageType}");
            motionRoot.transform.SetPositionAndRotation(visualObject.transform.position, visualObject.transform.rotation);
            motionRoot.transform.localScale = Vector3.one;
            visualObject.transform.SetParent(motionRoot.transform, true);
            visualObject.name = "SlashVisual";
            obj = motionRoot;
            visualTransform = visualObject.transform;
        }

        SweepEffect effect = obj.AddComponent<SweepEffect>();
        effect._creationTime = Time.unscaledTime;
        effect._instanceId = obj.GetInstanceID();
        AliveCount++;
        effect.mat = material;
        effect.waveColor = color;
        effect.damage = damage;
        effect.damageType = damageType;
        effect.onHit = onHit;
        effect.onFirstHit = onFirstHit;
        effect.leftToRight = leftToRight;
        effect.damageNumberColor = damageNumberColor;
        effect.canInterruptCFrame = canInterruptCFrame;
        effect.onAllHit = onAllHit;
        if (useEnhancedSlashMotion)
        {
            var blurRenderer = visualTransform.GetComponentInChildren<SpriteRenderer>();
            effect.motionBlur = blurRenderer != null
                ? new WeaponMotionBlurController(blurRenderer, 0.45f, 0.04f, 36f)
                : null;
        }

        // 按 X 排序：L→R 升序，R→L 降序
        List<Enemy> sorted = new List<Enemy>(targets);
        if (leftToRight)
            sorted.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
        else
            sorted.Sort((a, b) => b.transform.position.x.CompareTo(a.transform.position.x));

        foreach (var enemy in sorted)
            effect.targets.Add(new TargetEntry { enemy = enemy, xThreshold = enemy.transform.position.x });

        Vector3 targetScale = useEnhancedSlashMotion ? visualTransform.localScale : obj.transform.localScale;
        if (useEnhancedSlashMotion)
            visualTransform.localScale = Vector3.zero;
        else
            obj.transform.localScale = Vector3.zero;
        float scaleInDuration = useEnhancedSlashMotion ? 0.02f : 0.05f;
        Tween scaleIn = useEnhancedSlashMotion
            ? visualTransform.DOScale(targetScale, scaleInDuration).SetEase(Ease.OutQuad)
            : obj.transform.DOScale(targetScale, scaleInDuration).SetEase(Ease.OutQuad);

        Vector3 initEuler = obj.transform.eulerAngles;
        obj.transform.eulerAngles = new Vector3(initEuler.x, initEuler.y, initEuler.z + startAngle);
        Vector3 targetEuler = new Vector3(initEuler.x, initEuler.y, initEuler.z + endAngle);

        effect.seq = DOTween.Sequence();
        effect.seq.SetTarget(obj.transform);
        effect.seq.SetUpdate(UpdateType.Normal, false);

        float spriteTimelineOffset = 0f;
        float spriteSwingDuration = duration;
        if (useEnhancedSlashMotion)
        {
            float totalDuration = targetDuration > 0f ? targetDuration : 0.5f;
            float durationScale = totalDuration / 0.5f;
            float catchUpDuration = 0.02f * durationScale;
            float inertiaDuration = 0.06f * durationScale;
            float fadeDuration = 0.09f * durationScale;
            float swingDuration = Mathf.Max(0.1f, totalDuration - inertiaDuration - fadeDuration);
            Vector3 visualBasePosition = visualTransform.localPosition;
            float windupOffset = leftToRight ? -0.18f : 0.18f;
            float inertiaOffset = leftToRight ? 0.12f : -0.12f;
            visualTransform.localPosition = visualBasePosition + Vector3.right * windupOffset;

            var move = obj.transform.DOMove(endPos, swingDuration).SetEase(Ease.InOutCubic);
            move.OnUpdate(() =>
            {
                effect.UpdateMotionBlur(1f);
                effect.CheckHitThresholds();
            });
            effect.seq.Append(move);
            effect.seq.Join(obj.transform.DORotate(targetEuler, swingDuration, RotateMode.Fast)
                .SetEase(Ease.InOutCubic));
            effect.seq.Insert(0f, DOTween.To(
                () => 0f,
                value => effect.motionBlur?.SetStrength(value),
                30f,
                swingDuration * 0.22f).SetEase(Ease.OutQuad));
            effect.seq.Insert(swingDuration * 0.72f, DOTween.To(
                () => 30f,
                value => effect.motionBlur?.SetStrength(value),
                0f,
                swingDuration * 0.28f).SetEase(Ease.InQuad));
            effect.seq.Insert(0f, visualTransform.DOLocalMove(visualBasePosition, catchUpDuration)
                .SetEase(Ease.OutQuad));

            effect.motionBlur?.SetStrength(0f);
            effect.seq.Append(visualTransform.DOLocalMove(
                visualBasePosition + Vector3.right * inertiaOffset, inertiaDuration)
                .SetEase(Ease.OutCubic));

            if (material != null)
                effect.seq.Append(material.DOFade(0f, fadeDuration).SetEase(Ease.InQuad));
            else
                effect.seq.AppendInterval(fadeDuration);

            spriteTimelineOffset = 0f;
            spriteSwingDuration = swingDuration;
        }
        else
        {
            var move = obj.transform.DOMove(endPos, duration).SetEase(Ease.InOutQuad);
            move.OnUpdate(() =>
            {
                effect.UpdateMotionBlur(1f);
                effect.CheckHitThresholds();
            });
            effect.seq.Append(move);
            effect.seq.Join(obj.transform.DORotate(targetEuler, duration, RotateMode.Fast)
                .SetEase(Ease.InOutQuad));

            if (material != null)
            {
                effect.seq.AppendInterval(0.03f);
                effect.seq.Append(material.DOFade(0f, 0.25f).SetEase(Ease.InQuad));
            }
        }

        // Stab sprite 三帧动画：stab → rotate1 → rotate2
        if (rotateSprite1 != null && rotateSprite2 != null)
        {
            SpriteRenderer sr = visualTransform.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                Sprite orig = sr.sprite;
                effect.seq.InsertCallback(0f, () => sr.sprite = orig);
                effect.seq.InsertCallback(spriteTimelineOffset + spriteSwingDuration * 0.10f, () => sr.sprite = rotateSprite1);
                effect.seq.InsertCallback(spriteTimelineOffset + spriteSwingDuration * 0.30f, () => sr.sprite = rotateSprite2);
            }
        }

        effect.seq.OnKill(() =>
        {
            if (!effect._completed)
            {
                Debug.Log($"[SweepEffect] OnKill (premature): id={effect._instanceId}, name={effect.gameObject.name}, damageType={effect.damageType}, frame={Time.frameCount}");
                effect.seq = null;
                scaleIn.Kill();
                Destroy(effect.gameObject);
            }
        });

        effect.seq.OnComplete(() =>
        {
            if (!effect._onAllHitInvoked)
            {
                effect._onAllHitInvoked = true;
                effect.onAllHit?.Invoke();
            }
            effect._completed = true;
            effect.seq = null;
            scaleIn.Kill();
            Destroy(effect.gameObject);
        });

        // Enhanced Slash already builds its exact target-duration timeline; legacy callers keep timeScale matching.
        if (!useEnhancedSlashMotion && targetDuration > 0f)
        {
            float naturalDuration = effect.seq.Duration();
            if (naturalDuration > 0f)
                effect.seq.timeScale = Mathf.Clamp(naturalDuration / targetDuration, 0.1f, 10f);
        }
    }


    private void UpdateMotionBlur(float multiplier)
    {
        if (motionBlur == null)
            return;
        Vector3 fallbackDirection = leftToRight ? Vector3.right : Vector3.left;
        motionBlur.UpdateMotion(transform.position, transform.eulerAngles.z,
            fallbackDirection, multiplier, 12f, Time.deltaTime);
    }

    private void CheckHitThresholds()
    {
        if (leftToRight)
        {
            while (nextIndex < targets.Count)
            {
                if (transform.position.x < targets[nextIndex].xThreshold) break;
                HitTarget(targets[nextIndex].enemy);
                nextIndex++;
            }
        }
        else
        {
            while (nextIndex < targets.Count)
            {
                if (transform.position.x > targets[nextIndex].xThreshold) break;
                HitTarget(targets[nextIndex].enemy);
                nextIndex++;
            }
        }

        if (!_onAllHitInvoked && nextIndex >= targets.Count)
        {
            _onAllHitInvoked = true;
            onAllHit?.Invoke();
        }
    }

    private void HitTarget(Enemy enemy)
    {
        if (enemy != null && enemy.state != EnemyState.Dead)
        {
            bool isFirstHit = !_hasHit;
            _hasHit = true;
            Vector3 impactDirection = leftToRight ? Vector3.right : Vector3.left;
            Vector3 impactPosition = new Vector3(transform.position.x, enemy.transform.position.y + 0.8f,
                enemy.transform.position.z);
            enemy.TakeDamage(damage, damageType, damageNumberColor, canInterruptCFrame,
                feedbackStrength: isFirstHit ? HitFeedbackStrength.Standard : HitFeedbackStrength.Light,
                impactPosition: impactPosition, impactDirection: impactDirection);
            if (isFirstHit)
                PauseSequenceForHitStop(HitFeedbackStrength.Standard);
            if (isFirstHit)
                onFirstHit?.Invoke();
            onHit?.Invoke(enemy);
        }
    }

    private void PauseSequenceForHitStop(HitFeedbackStrength feedbackStrength)
    {
        if (seq == null || !seq.IsActive()) return;
        if (_hitStopRoutine != null)
            StopCoroutine(_hitStopRoutine);
        _hitStopRoutine = StartCoroutine(HitStopRoutine(HitFeedbackManager.GetHitStopDuration(feedbackStrength)));
    }

    private System.Collections.IEnumerator HitStopRoutine(float duration)
    {
        seq.Pause();
        yield return new WaitForSecondsRealtime(duration);
        if (seq != null && seq.IsActive())
            seq.Play();
        _hitStopRoutine = null;
    }

    private void OnDestroy()
    {
        if (_hitStopRoutine != null)
        {
            StopCoroutine(_hitStopRoutine);
            _hitStopRoutine = null;
        }
        motionBlur?.Dispose();
        AliveCount--;
        float alive = Time.unscaledTime - _creationTime;
        Debug.Log($"[SweepEffect] OnDestroy: {gameObject.name}, alive={alive:F2}s, frame={Time.frameCount}");
        if (seq != null && seq.IsActive())
            seq.Kill();
        seq = null;
    }

    private static Color GetSlashColor(DamageType type) => type switch
    {
        DamageType.Slash => new Color(0.2f, 0.65f, 1f),
        _ => new Color(0.2f, 0.65f, 1f)
    };
}
