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
    [Tooltip("一波箭矢的总发射窗口（秒），所有箭矢在此窗口内均匀分布")]
    public float spawnWindow = 1.5f;

    [Header("目标散射")]
    [Tooltip("目标点 X 随机偏移范围（模拟箭雨覆盖感）")]
    public float spreadX = 2f;
    [Tooltip("目标点 Z 随机偏移范围")]
    public float spreadZ = 0.8f;

    [Header("飞行")]
    [Tooltip("出发 Z 偏移（玩家身后，负=后方）")]
    public float startBehindPlayer = -5f;
    [Tooltip("出发高度")]
    public float startY = 2.5f;
    [Tooltip("抛物线最高点超过 startY 的高度")]
    public float arcHeight = 2.5f;
    [Tooltip("飞行时间")]
    public float flyDuration = 0.8f;

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
    [Tooltip("箭矢落点判定半径")]
    public float impactRadius = 0.75f;

    [Header("玩家位置")]
    public Transform playerTransform;

    private const int ArrowBurstMultiplier = 4;
    private const float MaxBurstWindow = 0.28f;

    private int _damage;
    private float _flyDuration;
    private float _fadeOutDuration;
    private float _startY;
    private float _arcHeight;
    private float _xJitter;
    private float _rotJitter;
    private float _spreadX;
    private float _spreadZ;

    public void Play(int rowCount, int arrowCount, int damage)
    {
        _damage = Mathf.Max(1, damage / ArrowBurstMultiplier);
        _flyDuration = flyDuration;
        _fadeOutDuration = fadeOutDuration;
        _startY = startY;
        _arcHeight = arcHeight;
        _xJitter = xJitter;
        _rotJitter = rotJitter;
        _spreadX = spreadX;
        _spreadZ = spreadZ;

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
        int totalArrows = Mathf.Max(1, arrowCount) * ArrowBurstMultiplier;
        Vector3 playerPos = GetArrowStartOrigin();
        float burstWindow = Mathf.Min(spawnWindow, MaxBurstWindow);
        float interval = totalArrows > 1 ? burstWindow / (totalArrows - 1) : 0f;

        Vector3 battlefieldOffset = GetBattlefieldWorldOffset(cm);

        for (int i = 0; i < totalArrows; i++)
        {
            Vector3 targetPos = GetTargetAreaPosition(cm, battlefieldOffset, clampedRows);
            float delay = interval * i;
            StartCoroutine(SpawnArrowWithDelay(targetPos, playerPos, delay));
        }

        Destroy(gameObject, spawnWindow + _flyDuration + _fadeOutDuration + 0.5f);
    }

    private System.Collections.IEnumerator SpawnArrowWithDelay(Vector3 targetPos, Vector3 playerPos, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        SpawnArrow(targetPos, playerPos);
    }

    private void SpawnArrow(Vector3 targetPos, Vector3 playerPos)
    {
        if (arrowTemplate == null) return;

        var arrowGO = Instantiate(arrowTemplate.gameObject, transform);
        arrowGO.SetActive(true);
        var sr = arrowGO.GetComponent<SpriteRenderer>();
        if (sr == null) { Destroy(arrowGO); return; }
        sr.color = Color.white;

        Vector3 startPos = new Vector3(
            playerPos.x,
            _startY,
            playerPos.z + startBehindPlayer
        );
        arrowGO.transform.position = startPos;

        arrowGO.transform.DOMoveX(targetPos.x, _flyDuration).SetEase(Ease.Linear);
        arrowGO.transform.DOMoveZ(targetPos.z, _flyDuration).SetEase(Ease.Linear);
        arrowGO.transform.DOMoveY(targetPos.y, _flyDuration).SetEase(Ease.InQuad);

        Vector3 delta = targetPos - startPos;
        float horizDist = new Vector2(delta.x, delta.z).magnitude;
        float trajectoryAngle = Mathf.Atan2(targetPos.y - startPos.y, horizDist) * Mathf.Rad2Deg;
        float yawAngle = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;

        Quaternion yawBase = Quaternion.Euler(0f, yawAngle, 0f) * Quaternion.Euler(arrowBaseRotation);
        float barrelZ = Random.Range(-_rotJitter, _rotJitter);
        Quaternion barrelRot = Quaternion.Euler(0f, 0f, barrelZ);
        arrowGO.transform.rotation = yawBase * barrelRot;

        Vector3 localRight = yawBase * Vector3.right;
        float startPitch = trajectoryAngle * pitchStartMult;
        float endPitch = trajectoryAngle * pitchEndMult;
        Tween pitchTween = DOTween.To(() => startPitch,
            v => arrowGO.transform.rotation = Quaternion.AngleAxis(v, localRight) * yawBase * barrelRot,
            endPitch, _flyDuration).SetEase(Ease.InQuad);

        Sequence seq = DOTween.Sequence();
        seq.AppendInterval(_flyDuration);
        seq.AppendCallback(() =>
        {
            if (arrowGO != null)
                ApplyImpactDamage(targetPos);
        });
        if (sr != null)
            seq.Append(sr.DOFade(0f, _fadeOutDuration));
        seq.OnComplete(() =>
        {
            if (arrowGO != null) Destroy(arrowGO);
        });
        seq.OnKill(() =>
        {
            if (arrowGO != null) Destroy(arrowGO);
        });
    }

    private void ApplyImpactDamage(Vector3 impactPos)
    {
        var cm = AttackSystem.Instance?.columnManager;
        if (cm == null) return;

        float radiusSqr = impactRadius * impactRadius;
        var enemies = cm.GetAllEnemies();
        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (enemy == null || enemy.state == EnemyState.Dead) continue;
            if (enemy.isBoss && enemy.bossState != BossState.InCombat) continue;

            Vector3 enemyPos = enemy.transform.position;
            float dx = enemyPos.x - impactPos.x;
            float dz = enemyPos.z - impactPos.z;
            if (dx * dx + dz * dz <= radiusSqr)
                enemy.TakeDamage(_damage, DamageType.Pierce);
        }
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

    private Vector3 GetTargetAreaPosition(ColumnManager cm, Vector3 battlefieldOffset, int rowCount)
    {
        int totalCols = cm != null ? cm.columnCount : 5;
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        for (int col = 0; col < totalCols; col++)
        {
            float x = battlefieldOffset.x + GetColumnLocalX(col);
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
        }

        int lastRow = Mathf.Max(0, rowCount - 1);
        float frontZ = battlefieldOffset.z + GetRowLocalZ(0);
        float backZ = battlefieldOffset.z + GetRowLocalZ(lastRow);
        float minZ = Mathf.Min(frontZ, backZ);
        float maxZ = Mathf.Max(frontZ, backZ);

        return new Vector3(
            Random.Range(minX, maxX),
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
