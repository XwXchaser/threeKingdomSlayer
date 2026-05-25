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
    private List<TargetEntry> targets = new List<TargetEntry>();
    private int nextIndex;
    private bool leftToRight;
    private Material mat;
    private Color waveColor;
    private Color? damageNumberColor;
    private Sequence seq;

    public static void Create(Vector3 centerPos, DamageType damageType, float damage,
        List<Enemy> targets, bool leftToRight, float halfWidth, float fanAngle, float duration,
        System.Action<Enemy> onHit = null, GameObject prefab = null, float? alphaOverride = null,
        Color? damageNumberColor = null)
    {
        if (targets == null || targets.Count == 0) return;

        float startX = leftToRight ? -halfWidth : halfWidth;
        float endX = leftToRight ? halfWidth : -halfWidth;
        float startAngle = leftToRight ? fanAngle : -fanAngle;
        float endAngle = leftToRight ? -fanAngle : fanAngle;

        Vector3 spawnPos = new Vector3(startX, centerPos.y, centerPos.z);
        GameObject obj;
        Material material = null;
        Color color = GetSlashColor(damageType);
        color.a = alphaOverride ?? 0.85f;

        if (prefab != null)
        {
            // prefab 路径：用浅色调避免覆盖 sprite 纹理细节
            color = Color.Lerp(color, Color.white, 0.5f);
            color.a = alphaOverride ?? 0.85f;

            obj = Object.Instantiate(prefab, spawnPos, prefab.transform.rotation);
            obj.name = $"Slash_{damageType}";
            Renderer r = obj.GetComponentInChildren<Renderer>();
            if (r != null) { material = r.material; material.color = color; }
        }
        else
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            obj.name = $"Slash_{damageType}";
            obj.transform.position = spawnPos;
            obj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Renderer renderer = obj.GetComponent<Renderer>();
            material = new Material(Shader.Find("Sprites/Default"));
            material.color = color;
            renderer.material = material;
            obj.transform.localScale = new Vector3(12f, 2.5f, 1f);
        }

        SweepEffect effect = obj.AddComponent<SweepEffect>();
        effect.mat = material;
        effect.waveColor = color;
        effect.damage = damage;
        effect.damageType = damageType;
        effect.onHit = onHit;
        effect.leftToRight = leftToRight;
        effect.damageNumberColor = damageNumberColor;

        // 按 X 排序：L→R 升序，R→L 降序
        List<Enemy> sorted = new List<Enemy>(targets);
        if (leftToRight)
            sorted.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
        else
            sorted.Sort((a, b) => b.transform.position.x.CompareTo(a.transform.position.x));

        foreach (var enemy in sorted)
            effect.targets.Add(new TargetEntry { enemy = enemy, xThreshold = enemy.transform.position.x });

        // 缩放淡入
        Vector3 targetScale = obj.transform.localScale;
        obj.transform.localScale = Vector3.zero;
        obj.transform.DOScale(targetScale, 0.05f).SetEase(Ease.OutQuad);

        // 主序列：X 移动 + Z 旋转
        effect.seq = DOTween.Sequence();
        effect.seq.SetTarget(obj.transform);

        var move = obj.transform.DOMoveX(endX, duration).SetEase(Ease.InOutQuad);
        move.OnUpdate(effect.CheckHitThresholds);
        effect.seq.Append(move);

        // 设置起始旋转姿态（挥刀起点角度）
        Vector3 initEuler = obj.transform.eulerAngles;
        obj.transform.eulerAngles = new Vector3(initEuler.x, initEuler.y, initEuler.z + startAngle);

        // R→L 时翻转 prefab X 使头部（刀尖）始终朝向运动方向
        if (!leftToRight)
        {
            Vector3 s = obj.transform.localScale;
            s.x = -Mathf.Abs(s.x);
            obj.transform.localScale = s;
            targetScale = s;
        }

        Vector3 targetEuler = new Vector3(initEuler.x, initEuler.y, initEuler.z + endAngle);
        var rotate = obj.transform.DORotate(targetEuler, duration, RotateMode.Fast)
            .SetEase(Ease.InOutQuad);
        effect.seq.Join(rotate);

        // 淡出
        if (material != null)
        {
            effect.seq.AppendInterval(0.03f);
            effect.seq.Append(material.DOFade(0f, 0.25f).SetEase(Ease.InQuad));
        }

        effect.seq.OnComplete(() =>
        {
            effect.seq = null;
            Destroy(effect.gameObject);
        });
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
    }

    private void HitTarget(Enemy enemy)
    {
        if (enemy != null && enemy.state != EnemyState.Dead)
        {
            enemy.TakeDamage(damage, damageType, damageNumberColor);
            onHit?.Invoke(enemy);
        }
    }

    private void OnDestroy()
    {
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
