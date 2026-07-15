using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 定时AOE火焰特效 — 连续生成多个火焰团沿Z轴穿透敌人
///
/// 每间隔 particleInterval 秒生成一个火焰团粒子，每个粒子：
///   - 随机选择精灵（fireSprites中随机）
///   - 近端小亮 → 远端大暗（缩放渐进）
///   - 透明度淡入 → 峰值 → 淡出
///   - 随机旋转 ±rotJitter
///   - X轴随机抖动 ±xJitter
/// 多个粒子重叠形成连续喷火束效果。
///
/// 用法: Instantiate(prefab), then call Play(columns, damage)
/// </summary>
public class ShootFireEffect : MonoBehaviour
{
    [Header("精灵")]
    [Tooltip("火焰团精灵组，运行时随机选取")]
    public Sprite[] fireSprites;
    [Tooltip("整体缩放倍率（调大=火焰更大）")]
    public float globalScale = 1f;

    [Header("生成节奏")]
    [Tooltip("整个喷火束的粒子生成持续时间")]
    public float burstDuration = 0.35f;
    [Tooltip("粒子生成间隔")]
    public float particleInterval = 0.04f;
    [Tooltip("单个粒子生命周期")]
    public float particleLifetime = 0.28f;

    [Header("缩放（近小远大）")]
    public float scaleNearMin = 0.2f;
    public float scaleNearMax = 0.35f;
    public float scaleFarMin = 0.55f;
    public float scaleFarMax = 0.8f;

    [Header("透明度")]
    public float alphaFadeInTime = 0.08f;
    public float alphaPeak = 1f;

    [Header("随机变化")]
    [Tooltip("旋转随机范围（度）")]
    public float rotJitter = 20f;
    [Tooltip("X轴横向抖动")]
    public float xJitter = 0.3f;

    [Header("飞行")]
    [Tooltip("喷火起始Z（玩家身前）")]
    public float fireStartZ = -2f;
    [Tooltip("喷火终点Z（最远覆盖范围）")]
    public float fireEndZ = 15f;
    public float zTravelSpeed = 20f;

    // ── 运行时 ──
    private HashSet<Enemy> _hitEnemies = new HashSet<Enemy>();
    private List<Enemy> _sortedEnemies;
    private float _zStart, _zEnd;
    private float _centerX, _halfWidth;
    private int _damage;
    public void Play(List<int> columns, int damage)
    {
        _damage = damage;

        var cm = AttackSystem.Instance?.columnManager;
        if (cm == null)
        {
            Destroy(gameObject);
            return;
        }

        int maxRows = StageController.Instance != null
            ? StageController.Instance.GetMaxVisibleRows()
            : 5;

        // 收集受影响列所有存活敌人（去重）
        var seen = new HashSet<Enemy>();
        _sortedEnemies = new List<Enemy>();
        foreach (int col in columns)
        {
            var list = cm.GetEnemiesInRange(col, maxRows);
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e == null || e.state == EnemyState.Dead) continue;
                if (seen.Add(e))
                    _sortedEnemies.Add(e);
            }
        }

        if (_sortedEnemies.Count == 0)
        {
            Destroy(gameObject);
            return;
        }

        _zStart = fireStartZ;
        _zEnd = fireEndZ;

        // 按Z升序排序
        _sortedEnemies.Sort((a, b) => a.transform.position.z.CompareTo(b.transform.position.z));

        // X范围
        _centerX = GetCenterX(columns);
        _halfWidth = GetHalfWidth(columns);

        transform.position = Vector3.zero;

        StartCoroutine(SpawnRoutine());

        // 自毁：等所有粒子生命周期结束
        Destroy(gameObject, burstDuration + particleLifetime + 0.5f);
    }

    private IEnumerator SpawnRoutine()
    {
        float elapsed = 0f;
        while (elapsed < burstDuration)
        {
            float t = elapsed / burstDuration;
            SpawnParticle(t);
            elapsed += particleInterval;
            yield return new WaitForSeconds(particleInterval);
        }
    }

    private void SpawnParticle(float progressT)
    {
        if (fireSprites == null || fireSprites.Length == 0) return;

        var p = new GameObject("FireParticle");
        p.transform.SetParent(transform);

        var sr = p.AddComponent<SpriteRenderer>();
        sr.sprite = fireSprites[Random.Range(0, fireSprites.Length)];
        sr.sortingOrder = 50;
        sr.color = new Color(1f, 1f, 1f, 0f);

        // X位置：列范围 + 随机抖动
        float x = _centerX + Random.Range(-_halfWidth, _halfWidth) + Random.Range(-xJitter, xJitter);
        p.transform.position = new Vector3(x, 0f, _zStart);

        // 旋转
        p.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-rotJitter, rotJitter));

        // 缩放：近小远大，按 progressT 插值
        float nearScale = Random.Range(scaleNearMin, scaleNearMax);
        float farScale = Random.Range(scaleFarMin, scaleFarMax);
        float startScale = Mathf.Lerp(nearScale, farScale, progressT) * globalScale;
        float endScale = startScale * 1.5f;
        p.transform.localScale = Vector3.one * startScale;

        // 飞行
        float zDist = _zEnd - _zStart;
        float travelTime = zDist / zTravelSpeed;
        float actualTravelTime = Mathf.Min(travelTime, particleLifetime);

        // 透明度序列：淡入 → 保持 → 淡出
        float fadeInEnd = Mathf.Min(alphaFadeInTime, particleLifetime * 0.3f);
        float fadeOutStart = particleLifetime * 0.4f;

        var fadeSeq = DOTween.Sequence().SetUpdate(UpdateType.Normal, false);
        fadeSeq.Append(sr.DOFade(alphaPeak, fadeInEnd).SetEase(Ease.OutQuad));
        fadeSeq.AppendInterval(fadeOutStart - fadeInEnd);
        fadeSeq.Append(sr.DOFade(0f, particleLifetime - fadeOutStart).SetEase(Ease.InQuad));
        fadeSeq.SetTarget(p);

        // 缩放：持续放大
        p.transform.DOScale(endScale, particleLifetime).SetEase(Ease.OutQuad).SetTarget(p).SetUpdate(UpdateType.Normal, false);

        // 颜色：白黄 → 橙红
        var colorSeq = DOTween.Sequence().SetUpdate(UpdateType.Normal, false);
        colorSeq.Append(sr.DOColor(new Color(1f, 0.85f, 0.2f, 1f), particleLifetime * 0.5f));
        colorSeq.Append(sr.DOColor(new Color(0.8f, 0.3f, 0.05f, 1f), particleLifetime * 0.5f));
        colorSeq.SetTarget(p);

        // Z轴移动 + 命中检测 + 动态排序
        var srRef = sr;
        p.transform
            .DOMoveZ(_zEnd, actualTravelTime)
            .SetEase(Ease.Linear)
            .OnUpdate(() =>
            {
                CheckHit(p);
                // Z越大（越远）order越小，穿过敌人后不再覆盖
                srRef.sortingOrder = 50 - (int)(p.transform.position.z * 10f);
            })
            .SetTarget(p)
            .SetUpdate(UpdateType.Normal, false);

        // 清理
        Destroy(p, particleLifetime + 0.1f);
    }

    private void CheckHit(GameObject particle)
    {
        for (int i = 0; i < _sortedEnemies.Count; i++)
        {
            var enemy = _sortedEnemies[i];
            if (enemy == null || enemy.state == EnemyState.Dead) continue;
            if (_hitEnemies.Contains(enemy)) continue;

            if (particle.transform.position.z >= enemy.transform.position.z)
            {
                _hitEnemies.Add(enemy);
                enemy.TakeDamage(_damage, DamageType.Pierce);
            }
        }
    }

    private static float GetCenterX(List<int> columns)
    {
        float sum = 0f;
        foreach (int col in columns)
            sum += GetColumnX(col);
        return sum / columns.Count;
    }

    private static float GetHalfWidth(List<int> columns)
    {
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        foreach (int col in columns)
        {
            float x = GetColumnX(col);
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
        }
        return (maxX - minX) / 2f + 0.5f;
    }

    private static float GetColumnX(int col)
    {
        if (StageController.Instance != null)
            return StageController.Instance.GetFormationOffset(col, 0);
        return (col - 2) * 2f;
    }
}
