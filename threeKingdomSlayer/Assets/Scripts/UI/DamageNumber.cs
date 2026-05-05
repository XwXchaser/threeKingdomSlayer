using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// 伤害跳字
/// 受击后在敌人右侧弹出红色带黑色描边的伤害数字，
/// 向上飘动并逐渐淡出，完成后回收到对象池
/// </summary>
public class DamageNumber : MonoBehaviour
{
    [Header("组件")]
    [SerializeField] private TextMeshPro tmp;

    [Header("动效参数")]
    [SerializeField] private float floatUpDistance = 2f;     // 上飘距离
    [SerializeField] private float duration = 0.8f;           // 动画时长
    [SerializeField] private float fontSize = 3f;             // 字体大小
    [SerializeField] private float outlineWidth = 0.35f;      // 描边宽度
    [SerializeField] private Color textColor = Color.red;     // 文字颜色
    [SerializeField] private Color outlineColor = Color.black; // 描边颜色

    // 对象池引用（由 DamageNumberManager 设置）
    [System.NonSerialized] public System.Action<DamageNumber> OnReturnToPool;

    private Sequence animSeq;

    private void Awake()
    {
        if (tmp == null)
            tmp = GetComponent<TextMeshPro>();

        if (tmp == null)
        {
            tmp = gameObject.AddComponent<TextMeshPro>();
        }

        // 初始化 TextMeshPro 属性
        tmp.fontSize = fontSize;
        tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;

        // 启用描边
        tmp.fontMaterial.EnableKeyword("OUTLINE_ON");
        tmp.outlineWidth = outlineWidth;
        tmp.outlineColor = outlineColor;
    }

    /// <summary>
    /// 在指定世界坐标位置显示伤害数字
    /// </summary>
    /// <param name="worldPos">世界坐标位置</param>
    /// <param name="damage">伤害数值</param>
    public void Show(Vector3 worldPos, float damage)
    {
        // 重新激活对象
        gameObject.SetActive(true);
        transform.position = worldPos;

        // 重置透明度
        Color c = tmp.color;
        c.a = 1f;
        tmp.color = c;

        // 设置伤害数值（取整显示）
        tmp.text = Mathf.RoundToInt(damage).ToString();

        // 终止可能正在播放的动画
        if (animSeq != null && animSeq.IsActive())
        {
            animSeq.Kill();
            animSeq = null;
        }

        // 上飘终点
        Vector3 endPos = worldPos + new Vector3(0f, floatUpDistance, 0f);

        // DOTween 动画：向上漂浮 + 透明度淡出
        animSeq = DOTween.Sequence();
        animSeq.SetTarget(transform);
        animSeq.SetId("damageNumber");

        animSeq.Join(transform.DOMove(endPos, duration).SetEase(Ease.OutQuad));
        animSeq.Join(tmp.DOFade(0f, duration).SetEase(Ease.InQuad));

        animSeq.OnComplete(() =>
        {
            animSeq = null;
            gameObject.SetActive(false);
            OnReturnToPool?.Invoke(this);
        });
    }

    /// <summary>
    /// 重置状态（对象池回收时调用）
    /// </summary>
    public void ResetNumber()
    {
        if (animSeq != null && animSeq.IsActive())
        {
            animSeq.Kill();
            animSeq = null;
        }

        Color c = tmp.color;
        c.a = 1f;
        tmp.color = c;

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (animSeq != null && animSeq.IsActive())
        {
            animSeq.Kill();
            animSeq = null;
        }
    }
}
