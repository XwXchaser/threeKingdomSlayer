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
    private System.Action onAllHit;
    private bool _onAllHitInvoked;
    private bool canInterruptCFrame;
    private List<TargetEntry> targets = new List<TargetEntry>();
    private int nextIndex;
    private bool leftToRight;
    private Material mat;
    private Color waveColor;
    private Color? damageNumberColor;
    private Sequence seq;
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
        Material materialOverride = null, System.Action onAllHit = null, float targetDuration = -1f,
        Sprite rotateSprite1 = null, Sprite rotateSprite2 = null)
    {
        if (targets == null || targets.Count == 0) return;

        float startX = leftToRight ? -halfWidth : halfWidth;
        float endX = leftToRight ? halfWidth : -halfWidth;
        float startAngle = leftToRight ? fanAngle : -fanAngle;
        float endAngle = leftToRight ? -fanAngle : fanAngle;

        Vector3 spawnPos = new Vector3(startX, centerPos.y, centerPos.z);
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
            color.a = alphaOverride ?? 0.85f;
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
                color = Color.Lerp(color, Color.white, 0.5f);
                color.a = alphaOverride ?? 0.85f;
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

        SweepEffect effect = obj.AddComponent<SweepEffect>();
        effect._creationTime = Time.unscaledTime;
        effect._instanceId = obj.GetInstanceID();
        AliveCount++;
        effect.mat = material;
        effect.waveColor = color;
        effect.damage = damage;
        effect.damageType = damageType;
        effect.onHit = onHit;
        effect.leftToRight = leftToRight;
        effect.damageNumberColor = damageNumberColor;
        effect.canInterruptCFrame = canInterruptCFrame;
        effect.onAllHit = onAllHit;

        // 按 X 排序：L→R 升序，R→L 降序
        List<Enemy> sorted = new List<Enemy>(targets);
        if (leftToRight)
            sorted.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
        else
            sorted.Sort((a, b) => b.transform.position.x.CompareTo(a.transform.position.x));

        foreach (var enemy in sorted)
            effect.targets.Add(new TargetEntry { enemy = enemy, xThreshold = enemy.transform.position.x });

        // L→R 时翻转 prefab X 使枪头朝向运动方向（必须在归零前做）
        if (leftToRight)
        {
            Vector3 s = obj.transform.localScale;
            s.x = -Mathf.Abs(s.x);
            obj.transform.localScale = s;
        }

        // 缩放淡入
        Vector3 targetScale = obj.transform.localScale;
        obj.transform.localScale = Vector3.zero;
        var scaleIn = obj.transform.DOScale(targetScale, 0.05f).SetEase(Ease.OutQuad);

        // 主序列：X 移动 + Z 旋转
        effect.seq = DOTween.Sequence();
        effect.seq.SetTarget(obj.transform);
        effect.seq.SetUpdate(true);

        var move = obj.transform.DOMoveX(endX, duration).SetEase(Ease.InOutQuad);
        move.OnUpdate(effect.CheckHitThresholds);
        effect.seq.Append(move);

        // 设置起始旋转姿态（挥刀起点角度）
        Vector3 initEuler = obj.transform.eulerAngles;
        obj.transform.eulerAngles = new Vector3(initEuler.x, initEuler.y, initEuler.z + startAngle);

        Vector3 targetEuler = new Vector3(initEuler.x, initEuler.y, initEuler.z + endAngle);
        var rotate = obj.transform.DORotate(targetEuler, duration, RotateMode.Fast)
            .SetEase(Ease.InOutQuad);
        effect.seq.Join(rotate);

        // Stab sprite 三帧动画：stab → rotate1 → rotate2（时长均分）
        // R→L 时 flipX 翻转素材，使枪头朝向运动方向
        if (rotateSprite1 != null && rotateSprite2 != null)
        {
            SpriteRenderer sr = obj.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                Sprite orig = sr.sprite;
                float t1 = duration * 0.10f;
                float t2 = duration * 0.20f;
                float t3 = duration * 0.70f;
                // Scale.x 已在 R→L 时翻转了整个 prefab，sprite 无需再用 flipX
                effect.seq.Insert(0, DOTween.Sequence()
                    .AppendCallback(() => sr.sprite = orig)
                    .AppendInterval(t1)
                    .AppendCallback(() => sr.sprite = rotateSprite1)
                    .AppendInterval(t2)
                    .AppendCallback(() => sr.sprite = rotateSprite2)
                    .AppendInterval(t3));
            }
        }

        // 淡出
        if (material != null)
        {
            effect.seq.AppendInterval(0.03f);
            effect.seq.Append(material.DOFade(0f, 0.25f).SetEase(Ease.InQuad));
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

        // 特效时长拉伸/压缩以匹配cooldown（限制最大缩放防极端值）
        if (targetDuration > 0f)
        {
            float naturalDuration = effect.seq.Duration();
            if (naturalDuration > 0f)
                effect.seq.timeScale = Mathf.Clamp(naturalDuration / targetDuration, 0.1f, 10f);
        }
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
            enemy.TakeDamage(damage, damageType, damageNumberColor, canInterruptCFrame);
            onHit?.Invoke(enemy);
        }
    }

    private void OnDestroy()
    {
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
