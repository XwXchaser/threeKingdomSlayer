using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class AttackWave : MonoBehaviour
{
    private enum WaveMode { Fixed, Travel }

    private struct TargetEntry
    {
        public Enemy enemy;
        public float hitDelay;
        public float zThreshold;
    }

    private WaveMode mode;
    private float damage;
    private DamageType damageType;
    private System.Action<Enemy> onHit;
    private List<TargetEntry> targets = new List<TargetEntry>();
    private float elapsed;
    private int nextIndex;
    private float lifetime;
    private float fadeStartTime;
    private Material mat;
    private Vector3 targetScale;
    private Color waveColor;

    private Sequence travelSeq;

    private const float StabStagger = 0.03f;
    private const float SlashStagger = 0.05f;
    private const float PierceStagger = 0.02f;
    private const float SweepStagger = 0.06f;
    private const float LaunchStagger = 0.04f;

    private const float ProjectileSpeed = 8f;
    private const float StabThrustTime = 0.45f;
    private const float StabRetractTime = 0.75f;
    private const float EndZOffset = 3f;

    public static AttackWave Create(Vector3 position, DamageType damageType, float damage,
        List<Enemy> targets, System.Action<Enemy> onHit = null, GameObject prefab = null)
    {
        GameObject obj;
        Material material = null;
        Color color = GetColor(damageType);
        color.a = 0.85f;

        if (prefab != null)
        {
            // 用 prefab 自己的 Z 作为旅行起点，X/Y 保持 GetWavePosition 计算值（列对齐+高度）
            Vector3 spawnPos = position;
            spawnPos.z = prefab.transform.position.z;
            obj = Object.Instantiate(prefab, spawnPos, prefab.transform.rotation);
            obj.name = $"Wave_{damageType}";
            Renderer r = obj.GetComponentInChildren<Renderer>();
            if (r != null) material = r.material;
        }
        else
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            obj.name = $"Wave_{damageType}";
            Renderer renderer = obj.GetComponent<Renderer>();
            material = new Material(Shader.Find("Sprites/Default"));
            material.color = color;
            renderer.material = material;
            obj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            obj.transform.position = position;
            obj.transform.localScale = Vector3.zero;
        }

        AttackWave wave = obj.AddComponent<AttackWave>();
        wave.mat = material;
        wave.damage = damage;
        wave.damageType = damageType;
        wave.onHit = onHit;
        wave.waveColor = color;

        if (prefab != null)
            wave.targetScale = obj.transform.localScale;
        else
            wave.targetScale = damageType switch
            {
                DamageType.Stab => new Vector3(0.5f, 3f, 1f),
                DamageType.Slash => new Vector3(9f, 2f, 1f),
                DamageType.Pierce => new Vector3(0.6f, 1.2f, 1f),
                DamageType.Sweep => new Vector3(12f, 2.5f, 1f),
                DamageType.Launch => new Vector3(9f, 1.5f, 1f),
                _ => new Vector3(5f, 1.5f, 1f)
            };

        List<Enemy> alive = new List<Enemy>();
        foreach (var e in targets)
            if (e != null && e.state != EnemyState.Dead)
                alive.Add(e);

        if (alive.Count == 0)
        {
            wave.mode = WaveMode.Fixed;
            wave.lifetime = 0.2f;
            wave.fadeStartTime = 0f;
            wave.targetScale = Vector3.zero;
            return wave;
        }

        bool isTravel = damageType == DamageType.Sweep || damageType == DamageType.Pierce || damageType == DamageType.Stab;
        wave.mode = isTravel ? WaveMode.Travel : WaveMode.Fixed;

        if (isTravel)
            wave.SetupTravel(alive, obj.transform.position.z);
        else
            wave.SetupFixed(alive);

        return wave;
    }

    private void SetupTravel(List<Enemy> alive, float startZ)
    {
        // 确定旅行方向：取最远敌人的 Z（离 startZ 最远的那个）
        float furthestZ = alive[0].transform.position.z;
        float closestZ = alive[0].transform.position.z;
        foreach (var e in alive)
        {
            if (e.transform.position.z > furthestZ) furthestZ = e.transform.position.z;
            if (e.transform.position.z < closestZ) closestZ = e.transform.position.z;
        }

        // 判断旅行方向：从 startZ 朝向敌人最远端的那个方向
        bool travelPositiveZ = furthestZ > startZ;
        bool isStab = damageType == DamageType.Stab;

        // 戳击：刺到最近敌人前 2.5 单位即返回，不穿到敌人身后
        float endTravelZ;
        if (isStab)
            endTravelZ = travelPositiveZ ? closestZ - 2.5f : closestZ + 2.5f;
        else
            endTravelZ = travelPositiveZ ? furthestZ + EndZOffset : furthestZ - EndZOffset;

        float thrustTime;
        if (isStab)
        {
            thrustTime = StabThrustTime;
        }
        else
        {
            float thrustDistance = Mathf.Abs(startZ - endTravelZ);
            thrustTime = thrustDistance / ProjectileSpeed;
        }

        // 按旅行途中遇到的顺序排序 zThreshold（+Z 旅行则升序，-Z 则降序）
        if (travelPositiveZ)
            alive.Sort((a, b) => a.transform.position.z.CompareTo(b.transform.position.z));
        else
            alive.Sort((a, b) => b.transform.position.z.CompareTo(a.transform.position.z));

        foreach (var enemy in alive)
            targets.Add(new TargetEntry { enemy = enemy, zThreshold = enemy.transform.position.z });

        bool shouldRetract = isStab;

        // 缩放淡入
        transform.localScale = Vector3.zero;
        transform.DOScale(targetScale, 0.06f).SetEase(Ease.OutQuad).SetTarget(transform);

        // 戳击：刺出前立即命中所有目标（刺击范围只有1排，视觉上刺到最近敌人前方即返回）
        if (isStab)
        {
            foreach (var t in targets)
                HitTarget(t.enemy);
        }

        // DOTween 序列：刺出 → (收回) → 销毁
        travelSeq = DOTween.Sequence();
        travelSeq.SetTarget(transform);

        var thrust = transform.DOMoveZ(endTravelZ, thrustTime).SetEase(Ease.OutQuad);
        if (!isStab)
            thrust.OnUpdate(CheckHitThresholds);
        travelSeq.Append(thrust);

        if (shouldRetract)
        {
            float retractTime = StabRetractTime;
            var retract = transform.DOMoveZ(startZ, retractTime).SetEase(Ease.InQuad);
            travelSeq.Append(retract);

            if (mat != null)
                travelSeq.Join(mat.DOFade(0f, retractTime).SetEase(Ease.InQuad));
        }
        else
        {
            // 贯穿后短停顿再淡出
            if (mat != null)
            {
                travelSeq.AppendInterval(0.05f);
                travelSeq.Append(mat.DOFade(0f, 0.35f).SetEase(Ease.InQuad));
            }
        }

        travelSeq.OnComplete(() =>
        {
            travelSeq = null;
            Destroy(gameObject);
        });
    }

    private void CheckHitThresholds()
    {
        // 判断旅行方向以选择正确的比较方式
        if (targets.Count < 2 || targets[0].zThreshold < targets[targets.Count - 1].zThreshold)
        {
            // +Z 方向旅行：zThreshold 升序，超过阈值即命中
            while (nextIndex < targets.Count)
            {
                if (transform.position.z < targets[nextIndex].zThreshold) break;
                HitTarget(targets[nextIndex].enemy);
                nextIndex++;
            }
        }
        else
        {
            // -Z 方向旅行：zThreshold 降序，低于阈值即命中
            while (nextIndex < targets.Count)
            {
                if (transform.position.z > targets[nextIndex].zThreshold) break;
                HitTarget(targets[nextIndex].enemy);
                nextIndex++;
            }
        }
    }

    private void SetupFixed(List<Enemy> alive)
    {
        float staggerPerRow = damageType switch
        {
            DamageType.Stab => StabStagger,
            DamageType.Slash => SlashStagger,
            DamageType.Launch => LaunchStagger,
            _ => 0.05f
        };

        alive.Sort((a, b) => a.rowIndex.CompareTo(b.rowIndex));

        float maxDelay = 0f;
        int minRow = alive[0].rowIndex;

        foreach (var enemy in alive)
        {
            float delay = (enemy.rowIndex - minRow) * staggerPerRow;
            targets.Add(new TargetEntry { enemy = enemy, hitDelay = delay });
            if (delay > maxDelay) maxDelay = delay;
        }

        lifetime = maxDelay + 0.3f;
        fadeStartTime = maxDelay + 0.05f;
    }

    private void Update()
    {
        if (mode == WaveMode.Fixed)
        {
            elapsed += Time.deltaTime;
            UpdateFixed();
        }
    }

    private void UpdateFixed()
    {
        float scaleDuration = 0.06f;
        if (elapsed < scaleDuration)
        {
            float t = elapsed / scaleDuration;
            t = t * t * (3f - 2f * t);
            transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);
        }
        else
        {
            transform.localScale = targetScale;
        }

        while (nextIndex < targets.Count && elapsed >= targets[nextIndex].hitDelay)
        {
            HitTarget(targets[nextIndex].enemy);
            nextIndex++;
        }

        if (elapsed >= fadeStartTime && mat != null)
        {
            float fadeDuration = lifetime - fadeStartTime;
            float alpha = fadeDuration > 0f ? 1f - (elapsed - fadeStartTime) / fadeDuration : 0f;
            alpha = Mathf.Clamp01(alpha);
            Color c = waveColor;
            c.a = alpha;
            mat.color = c;
        }

        if (elapsed >= lifetime)
            Destroy(gameObject);
    }

    private void HitTarget(Enemy enemy)
    {
        if (enemy != null && enemy.state != EnemyState.Dead)
        {
            enemy.TakeDamage(damage, damageType);
            onHit?.Invoke(enemy);
        }
    }

    private void OnDestroy()
    {
        if (travelSeq != null && travelSeq.IsActive())
            travelSeq.Kill();
        travelSeq = null;
    }

    private static Color GetColor(DamageType type) => type switch
    {
        DamageType.Stab => new Color(1f, 0.85f, 0.15f),
        DamageType.Slash => new Color(0.2f, 0.65f, 1f),
        DamageType.Pierce => new Color(0.2f, 1f, 0.35f),
        DamageType.Sweep => new Color(1f, 0.25f, 0.1f),
        DamageType.Launch => new Color(0.7f, 0.2f, 1f),
        _ => Color.white
    };
}
