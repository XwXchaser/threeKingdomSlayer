using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 定时箭矢特效 — 模拟"箭雨"：从玩家身后高处抛射箭矢覆盖目标区域
///
/// 每波箭矢在 spawnWindow 内均匀发射，目标点加随机偏移模拟覆盖感。
/// Boss 占据多个 cell 会吃到多份箭矢伤害。
///
/// 用法: Instantiate(prefab), then Play(rowCount, arrowCount, damage)
/// </summary>
public class TimedArrowEffect : MonoBehaviour
{
    [Header("箭矢精灵")]
    [Tooltip("箭矢 SpriteRenderer 模板（复制此精灵生成箭矢）")]
    public SpriteRenderer arrowTemplate;
    [Tooltip("箭矢初始朝向（欧拉角）。默认 (-90,0,0) = 箭尖指向 +Z")]
    public Vector3 arrowBaseRotation = new Vector3(-90f, 0f, 0f);

    [Header("发射节奏")]
    [Tooltip("相邻两波箭雨的间隔（秒）")]
    [Min(0f)] public float volleyInterval = 0.35f;
    [Tooltip("每波显示的箭矢总数；其中固定 4 支参与伤害判定，其余仅为视觉箭矢")]
    [Min(4)] public int visualArrowsPerVolley = 8;
    [Tooltip("同波箭矢的最大发射时间随机偏移（秒）")]
    public float volleyJitter = 0.035f;

    [Header("目标散射")]
    [Tooltip("目标点 X 随机偏移范围（模拟箭雨覆盖感）")]
    public float spreadX = 2f;
    [Tooltip("目标点 Z 随机偏移范围")]
    public float spreadZ = 0.8f;

    [Header("飞行")]
    [Tooltip("出发 Z 偏移（玩家身后，负=后方）")]
    public float startBehindPlayer = -5f;
    [Tooltip("后方发射阵列的纵深")]
    public float startDepth = 2f;
    [Tooltip("起点横向超出战场宽度的比例")]
    public float startWidthPadding = 0.2f;
    [Tooltip("出发高度")]
    public float startY = 2.5f;
    [Tooltip("出发高度随机偏移")]
    public float startYJitter = 0.6f;
    [Tooltip("抛物线最高点超过 startY 的高度")]
    public float arcHeight = 2.5f;
    [Tooltip("飞行时间")]
    public float flyDuration = 0.8f;
    [Tooltip("飞行时间随机浮动比例")]
    [Range(0f, 0.3f)] public float flyDurationJitter = 0.1f;

    [Tooltip("箭雨下落阶段允许的最大俯角（度）；仅限制视觉旋转，不改变轨迹。")]
    [Range(0f, 89f)] public float maxDescentPitch = 35f;

    [Header("淡出")]
    [Tooltip("到达后淡出时间")]
    public float fadeOutDuration = 0.12f;

    [Header("随机偏移")]
    [Tooltip("起点 X 轴随机偏移")]
    public float xJitter = 1.5f;
    [Tooltip("Z 轴旋转随机角度")]
    public float rotJitter = 20f;
    [Tooltip("飞行俯仰初段倍率（乘 trajectoryAngle）")]
    public float pitchStartMult = 0.3f;
    [Tooltip("飞行俯仰末段倍率（乘 trajectoryAngle）")]
    public float pitchEndMult = 1.0f;

    [Header("命中")]
    [Tooltip("箭矢中心到箭尖的世界距离，用于计算可见箭尖位置")]
    [Min(0f)] public float arrowTipDistance = 2.2f;
    [Tooltip("箭尖高于敌人身体接触点多少时提前结算；越大越早")]
    [Min(0f)] public float impactContactHeight = 0.3f;
    [Tooltip("敌人 Transform 原点到身体命中点的高度")]
    [Min(0f)] public float enemyBodyContactOffset = 1.0f;
    [Tooltip("稳定阵型格判定的额外水平容差，避免攻击动画位移导致落空")]
    [Min(0f)] public float impactMovementTolerance = 0.6f;
    [Tooltip("命中后沿飞行方向继续穿入的距离")]
    [Min(0f)] public float impactPenetrationDistance = 0.45f;
    [Tooltip("命中后减速穿入并淡出的时间")]
    [Min(0.01f)] public float impactPenetrationDuration = 0.12f;
    [Tooltip("箭矢落点判定半径")]
    public float impactRadius = 0.75f;

    [Header("玩家位置")]
    public Transform playerTransform;

    private int _damageArrowCount;
    private float _damage;
    private float _volleyInterval;
    private float _flyDuration;
    private float _fadeOutDuration;
    private float _startY;
    private float _arcHeight;
    private float _xJitter;
    private float _rotJitter;
    private float _spreadX;
    private float _spreadZ;
    private float _arrowTipDistance;
    private float _impactContactHeight;
    private float _enemyBodyContactOffset;
    private float _impactMovementTolerance;
    private float _maxDescentPitch;

    public void Play(int rowCount, int arrowCount, float damage, int damageArrowCount = 4,
        int visualArrowCount = -1, float volleyIntervalOverride = -1f)
    {
        _damageArrowCount = Mathf.Max(1, damageArrowCount);
        _damage = Mathf.Max(1f, damage / _damageArrowCount);
        _volleyInterval = volleyIntervalOverride >= 0f ? volleyIntervalOverride : volleyInterval;
        _flyDuration = flyDuration;
        _fadeOutDuration = fadeOutDuration;
        _startY = startY;
        _arcHeight = arcHeight;
        _xJitter = xJitter;
        _rotJitter = rotJitter;
        _spreadX = spreadX;
        _spreadZ = spreadZ;
        _arrowTipDistance = Mathf.Max(0f, arrowTipDistance);
        _impactContactHeight = Mathf.Max(0f, impactContactHeight);
        _enemyBodyContactOffset = Mathf.Max(0f, enemyBodyContactOffset);
        _impactMovementTolerance = Mathf.Max(0f, impactMovementTolerance);
        _maxDescentPitch = maxDescentPitch;

        var cm = AttackSystem.Instance?.columnManager;
        if (cm == null)
        {
            Debug.LogWarning("[TimedArrowEffect] Play aborted: ColumnManager is null");
            Destroy(gameObject);
            return;
        }

        if (arrowTemplate != null && arrowTemplate.gameObject.activeSelf)
            arrowTemplate.gameObject.SetActive(false);

        int clampedRows = Mathf.Max(1, rowCount);

        // Boss 固定在第二排，若 rowCount 覆盖不到则扩展目标区域到 Boss 排
        for (int col = 0; col < cm.columnCount; col++)
        {
            var boss = cm.GetCombatBossCoveringColumn(col);
            if (boss != null && boss.rowIndex + 1 > clampedRows)
                clampedRows = boss.rowIndex + 1;
        }
        int totalVolleys = Mathf.Max(1, arrowCount);
        Vector3 playerPos = GetArrowStartOrigin();
        int requestedVisualArrows = visualArrowCount > 0 ? visualArrowCount : visualArrowsPerVolley;
        int visualCount = Mathf.Max(_damageArrowCount, requestedVisualArrows);
        float interval = Mathf.Max(0f, _volleyInterval);
        float totalVolleySpan = (totalVolleys - 1) * interval;

        Vector3 battlefieldOffset = GetBattlefieldWorldOffset(cm);
        GetBattlefieldXBounds(cm, battlefieldOffset, out float minX, out float maxX);
        float widthPadding = (maxX - minX) * Mathf.Max(0f, startWidthPadding);

        for (int volleyIndex = 0; volleyIndex < totalVolleys; volleyIndex++)
        {
            float volleyDelay = volleyIndex * interval;
            for (int arrowIndex = 0; arrowIndex < visualCount; arrowIndex++)
            {
                Vector3 targetPos = GetTargetAreaPosition(cm, battlefieldOffset, clampedRows);
                float delay = volleyDelay + Random.Range(0f, Mathf.Max(0f, volleyJitter));
                Vector3 startPos = GetStartPosition(playerPos, targetPos.x, minX - widthPadding, maxX + widthPadding, volleyIndex, totalVolleys);
                StartCoroutine(SpawnArrowWithDelay(targetPos, startPos, delay, arrowIndex < _damageArrowCount));
            }
        }

        float maxFlightDuration = _flyDuration * (1f + flyDurationJitter);
        float maxImpactDuration = Mathf.Max(_fadeOutDuration, impactPenetrationDuration);
        Destroy(gameObject, totalVolleySpan + Mathf.Max(0f, volleyJitter) + maxFlightDuration + maxImpactDuration + 0.5f);
    }

    private System.Collections.IEnumerator SpawnArrowWithDelay(Vector3 targetPos, Vector3 startPos, float delay, bool dealsDamage)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        SpawnArrow(targetPos, startPos, dealsDamage);
    }

    private void SpawnArrow(Vector3 targetPos, Vector3 startPos, bool dealsDamage)
    {
        if (arrowTemplate == null) return;

        var arrowGO = Instantiate(arrowTemplate.gameObject, transform);
        arrowGO.SetActive(true);
        var sr = arrowGO.GetComponent<SpriteRenderer>();
        if (sr == null) { Destroy(arrowGO); return; }
        sr.color = Color.white;

        float arrowFlyDuration = _flyDuration * Random.Range(1f - flyDurationJitter, 1f + flyDurationJitter);
        arrowFlyDuration = Mathf.Max(0.05f, arrowFlyDuration);
        arrowGO.transform.position = startPos;

        Vector3 delta = targetPos - startPos;
        Quaternion axisCorrection = Quaternion.Euler(arrowBaseRotation);
        Quaternion rollOffset = Quaternion.Euler(0f, 0f, Random.Range(-_rotJitter, _rotJitter));
        bool damageApplied = false;
        bool flightStopped = false;
        Tween flightTween = null;
        Vector3 battlefieldOffset = GetBattlefieldWorldOffset(AttackSystem.Instance?.columnManager);
        var contactEnemies = new List<Enemy>();
        SpriteRenderer[] arrowRenderers = arrowGO.GetComponentsInChildren<SpriteRenderer>(true);
        System.Action<Vector3> startImpactVisual = flightDirection =>
        {
            if (flightStopped || arrowGO == null) return;
            flightStopped = true;

            if (flightTween != null && flightTween.IsActive())
                flightTween.Pause();

            float duration = Mathf.Max(0.01f, impactPenetrationDuration);
            Vector3 penetrationTarget = arrowGO.transform.position
                + flightDirection * Mathf.Max(0f, impactPenetrationDistance);
            Sequence impactSeq = DOTween.Sequence().SetTarget(arrowGO.transform).SetUpdate(UpdateType.Normal, false);
            impactSeq.Append(arrowGO.transform.DOMove(penetrationTarget, duration).SetEase(Ease.OutQuad));
            for (int i = 0; i < arrowRenderers.Length; i++)
            {
                if (arrowRenderers[i] != null)
                    impactSeq.Join(arrowRenderers[i].DOFade(0f, duration));
            }
            impactSeq.OnComplete(() =>
            {
                if (flightTween != null && flightTween.IsActive())
                    flightTween.Kill(false);
                if (arrowGO != null) Destroy(arrowGO);
            });
        };

        flightTween = DOTween.To(() => 0f, progress =>
        {
            if (arrowGO == null) return;

            Vector3 position = Vector3.Lerp(startPos, targetPos, progress);
            position.y += 4f * _arcHeight * progress * (1f - progress);
            arrowGO.transform.position = position;

            Vector3 visualTangent = new Vector3(
                delta.x,
                delta.y + 4f * _arcHeight * (1f - 2f * progress),
                delta.z);
            if (visualTangent.sqrMagnitude <= 0.0001f) return;

            Vector3 flightDirection = visualTangent.normalized;
            float horizontalDistance = new Vector2(flightDirection.x, flightDirection.z).magnitude;
            float pitch = Mathf.Atan2(-flightDirection.y, horizontalDistance) * Mathf.Rad2Deg;
            pitch = Mathf.Clamp(pitch, -_maxDescentPitch, _maxDescentPitch);
            Vector3 horizontalDirection = new Vector3(flightDirection.x, 0f, flightDirection.z).normalized;
            arrowGO.transform.rotation = Quaternion.LookRotation(horizontalDirection, Vector3.up)
                * Quaternion.Euler(pitch, 0f, 0f) * axisCorrection * rollOffset;

            if (dealsDamage && !damageApplied && flightDirection.y < 0f)
            {
                Vector3 tipPosition = position + flightDirection * _arrowTipDistance;
                CollectContactEnemies(targetPos, tipPosition.y, battlefieldOffset, contactEnemies);
                if (contactEnemies.Count > 0)
                {
                    damageApplied = true;
                    ApplyDamage(contactEnemies);
                    startImpactVisual(flightDirection);
                }
            }
        }, 1f, arrowFlyDuration).SetEase(Ease.Linear).SetTarget(arrowGO.transform).SetUpdate(UpdateType.Normal, false);
        flightTween.OnComplete(() =>
        {
            if (dealsDamage && !damageApplied)
            {
                damageApplied = true;
                ApplyImpactDamage(targetPos);
            }

            if (arrowGO == null || flightStopped) return;
            Sequence missFade = DOTween.Sequence().SetTarget(arrowGO.transform).SetUpdate(UpdateType.Normal, false);
            for (int i = 0; i < arrowRenderers.Length; i++)
            {
                if (arrowRenderers[i] != null)
                    missFade.Join(arrowRenderers[i].DOFade(0f, _fadeOutDuration));
            }
            missFade.OnComplete(() =>
            {
                if (arrowGO != null) Destroy(arrowGO);
            });
        });
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
        var arrows = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < arrows.Length; i++)
            DOTween.Kill(arrows[i]);
    }

    private void CollectContactEnemies(Vector3 impactPos, float arrowTipY, Vector3 battlefieldOffset, List<Enemy> results)
    {
        results.Clear();
        var cm = AttackSystem.Instance?.columnManager;
        if (cm == null) return;

        float radius = impactRadius + _impactMovementTolerance;
        float radiusSqr = radius * radius;
        var enemies = cm.GetAllEnemies();
        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (!CanDamage(enemy)) continue;

            Vector3 slotPosition = GetStableSlotPosition(enemy, battlefieldOffset);
            float dx = slotPosition.x - impactPos.x;
            float dz = slotPosition.z - impactPos.z;
            if (dx * dx + dz * dz > radiusSqr) continue;

            float contactY = enemy.transform.position.y + _enemyBodyContactOffset + _impactContactHeight;
            if (arrowTipY <= contactY)
                results.Add(enemy);
        }
    }

    private bool ApplyImpactDamage(Vector3 impactPos)
    {
        var cm = AttackSystem.Instance?.columnManager;
        if (cm == null) return false;

        bool hitAny = false;
        Vector3 battlefieldOffset = GetBattlefieldWorldOffset(cm);
        float radius = impactRadius + _impactMovementTolerance;
        float radiusSqr = radius * radius;
        var enemies = cm.GetAllEnemies();
        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (!CanDamage(enemy)) continue;

            Vector3 slotPosition = GetStableSlotPosition(enemy, battlefieldOffset);
            float dx = slotPosition.x - impactPos.x;
            float dz = slotPosition.z - impactPos.z;
            if (dx * dx + dz * dz <= radiusSqr)
            {
                hitAny = true;
                enemy.TakeDamage(_damage, DamageType.Pierce,
                    feedbackSource: HitFeedbackSource.Passive, feedbackStrength: HitFeedbackStrength.Light);
            }
        }

        return hitAny;
    }

    private void ApplyDamage(List<Enemy> enemies)
    {
        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (CanDamage(enemy))
                enemy.TakeDamage(_damage, DamageType.Pierce,
                    feedbackSource: HitFeedbackSource.Passive, feedbackStrength: HitFeedbackStrength.Light);
        }
    }

    private static bool CanDamage(Enemy enemy)
    {
        if (enemy == null || enemy.state == EnemyState.Dead) return false;
        return !enemy.isBoss || enemy.bossState == BossState.InCombat;
    }

    private static Vector3 GetStableSlotPosition(Enemy enemy, Vector3 battlefieldOffset)
    {
        float localX = StageController.Instance != null
            ? StageController.Instance.GetFormationOffset(enemy.columnIndex, enemy.rowIndex)
            : (enemy.columnIndex - 2) * 2f;
        return new Vector3(
            battlefieldOffset.x + localX,
            enemy.transform.position.y,
            battlefieldOffset.z + GetRowLocalZ(enemy.rowIndex));
    }

    private static Vector3 GetArrowStartOrigin()
    {
        if (AttackSystem.Instance != null)
        {
            if (AttackSystem.Instance.playerState != null)
                return AttackSystem.Instance.playerState.transform.position;
            return AttackSystem.Instance.transform.position;
        }

        if (PlayerState.Instance != null)
            return PlayerState.Instance.transform.position;

        return Vector3.zero;
    }

    private static Vector3 GetBattlefieldWorldOffset(ColumnManager cm)
    {
        if (cm != null)
        {
            var enemies = cm.GetAllEnemies();
            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy == null) continue;
                if (enemy.transform.parent != null)
                    return enemy.transform.parent.position;
            }
        }

        return Vector3.zero;
    }

    private Vector3 GetStartPosition(Vector3 playerPos, float targetX, float minX, float maxX, int volleyIndex, int volleyTotal)
    {
        float rowT = volleyTotal > 1 ? volleyIndex / (float)(volleyTotal - 1) : 0.5f;
        float z = playerPos.z + startBehindPlayer - rowT * Mathf.Max(0f, startDepth);
        float x = Mathf.Clamp(targetX + Random.Range(-_xJitter, _xJitter), minX, maxX);
        return new Vector3(
            x,
            _startY + Random.Range(-startYJitter, startYJitter),
            z
        );
    }

    private static void GetBattlefieldXBounds(ColumnManager cm, Vector3 battlefieldOffset, out float minX, out float maxX)
    {
        int totalCols = cm != null ? cm.columnCount : 5;
        minX = float.MaxValue;
        maxX = float.MinValue;
        for (int col = 0; col < totalCols; col++)
        {
            float x = battlefieldOffset.x + GetColumnLocalX(col);
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
        }
    }

    private Vector3 GetTargetAreaPosition(ColumnManager cm, Vector3 battlefieldOffset, int rowCount)
    {
        GetBattlefieldXBounds(cm, battlefieldOffset, out float minX, out float maxX);

        int lastRow = Mathf.Max(0, rowCount - 1);
        float frontZ = battlefieldOffset.z + GetRowLocalZ(0);
        float backZ = battlefieldOffset.z + GetRowLocalZ(lastRow);
        float minZ = Mathf.Min(frontZ, backZ);
        float maxZ = Mathf.Max(frontZ, backZ);

        return new Vector3(
            Random.Range(minX - _spreadX, maxX + _spreadX),
            0f,
            Random.Range(minZ - _spreadZ, maxZ + _spreadZ)
        );
    }

    private static float GetColumnLocalX(int col)
    {
        if (StageController.Instance != null)
            return StageController.Instance.GetFormationOffset(col, 0);
        return (col - 2) * 2f;
    }

    private static float GetRowLocalZ(int row)
    {
        if (StageController.Instance != null)
        {
            float rowSpacing = StageController.Instance.GetRowSpacing();
            float offsetZ = StageController.Instance.GetFormationOffsetZ();
            int maxRow = StageController.Instance.GetMaxVisibleRows() - 1;
            return (maxRow - row) * (-rowSpacing) + offsetZ;
        }

        return row * -2.5f;
    }
}
