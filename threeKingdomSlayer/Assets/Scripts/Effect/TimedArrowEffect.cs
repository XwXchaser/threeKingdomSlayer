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

    [Header("玩家位置")]
    public Transform playerTransform;

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
        _damage = damage;
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
            Destroy(gameObject);
            return;
        }

        int totalCols = cm.columnCount;
        Vector3 playerPos = playerTransform != null ? playerTransform.position : Vector3.zero;

        // 收集目标区域所有存活敌人（不去重，Boss 多 cell 吃多份）
        var targets = new List<(Enemy enemy, Vector3 pos)>();

        for (int row = 0; row < rowCount; row++)
        {
            for (int col = 0; col < totalCols; col++)
            {
                var enemy = cm.GetEnemyAt(col, row);
                if (enemy == null || enemy.state == EnemyState.Dead) continue;
                targets.Add((enemy, enemy.transform.position));
            }
        }

        if (targets.Count == 0)
        {
            Destroy(gameObject);
            return;
        }

        int totalArrows = targets.Count * arrowCount;
        float interval = totalArrows > 1 ? spawnWindow / (totalArrows - 1) : 0f;

        // 在 spawnWindow 内均匀发射所有箭矢
        int arrowIndex = 0;
        foreach (var (enemy, basePos) in targets)
        {
            for (int i = 0; i < arrowCount; i++)
            {
                // 目标点加随机偏移模拟"箭雨覆盖区域"
                Vector3 scatteredPos = new Vector3(
                    basePos.x + Random.Range(-_spreadX, _spreadX),
                    basePos.y,
                    basePos.z + Random.Range(-_spreadZ, _spreadZ)
                );

                float delay = interval * arrowIndex;
                arrowIndex++;
                StartCoroutine(SpawnArrowWithDelay(enemy, scatteredPos, playerPos, delay));
            }
        }

        Destroy(gameObject, spawnWindow + _flyDuration + _fadeOutDuration + 0.5f);
    }

    private System.Collections.IEnumerator SpawnArrowWithDelay(Enemy enemy, Vector3 targetPos, Vector3 playerPos, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        SpawnArrow(enemy, targetPos, playerPos);
    }

    private void SpawnArrow(Enemy enemy, Vector3 targetPos, Vector3 playerPos)
    {
        if (arrowTemplate == null) return;

        var arrowGO = Instantiate(arrowTemplate.gameObject, transform);
        arrowGO.SetActive(true);
        var sr = arrowGO.GetComponent<SpriteRenderer>();
        if (sr == null) { Destroy(arrowGO); return; }
        sr.color = Color.white;

        // 出发位置：玩家身后高处
        Vector3 startPos = new Vector3(
            playerPos.x + Random.Range(-_xJitter, _xJitter),
            _startY,
            playerPos.z + startBehindPlayer
        );
        arrowGO.transform.position = startPos;

        // X / Z: 线性
        arrowGO.transform.DOMoveX(targetPos.x, _flyDuration).SetEase(Ease.Linear);
        arrowGO.transform.DOMoveZ(targetPos.z, _flyDuration).SetEase(Ease.Linear);

        // Y: 直接下坠曲线
        arrowGO.transform.DOMoveY(targetPos.y, _flyDuration).SetEase(Ease.InQuad);

        // —— 旋转 ——
        // ArrowTemplate rotation = (90,0,0) → tip = world +Z
        // 1. 偏航：绕世界 Y 轴，对准目标水平方向
        // 2. 炮口抖动：绕 local Z 滚动
        // 3. 俯仰：飞行中绕箭头 local right 轴（取自偏航后的 X 轴）低头
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
        DOTween.To(() => startPitch,
            v => arrowGO.transform.rotation = Quaternion.AngleAxis(v, localRight) * yawBase * barrelRot,
            endPitch, _flyDuration).SetEase(Ease.InQuad);

        // 伤害：Z 到达后结算一次
        bool hasHit = false;
        DOTween.To(
            () => 0f,
            v =>
            {
                if (!hasHit && arrowGO != null && arrowGO.transform.position.z >= targetPos.z)
                {
                    hasHit = true;
                    if (enemy != null && enemy.state != EnemyState.Dead)
                        enemy.TakeDamage(_damage, DamageType.Pierce);
                }
            },
            1f,
            _flyDuration
        ).SetEase(Ease.Linear);

        // 淡出
        var fadeSeq = DOTween.Sequence();
        fadeSeq.AppendInterval(_flyDuration);
        if (sr != null)
            fadeSeq.Append(sr.DOFade(0f, _fadeOutDuration));
        fadeSeq.OnComplete(() =>
        {
            if (arrowGO != null) Destroy(arrowGO);
        });
    }
}
