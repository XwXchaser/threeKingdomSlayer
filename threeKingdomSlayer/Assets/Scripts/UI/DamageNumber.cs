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
    [SerializeField] private float outlineWidth = 0.2f;       // 描边宽度（0=无描边）
    [SerializeField] private float boldWeight = 0.75f;        // 粗体权重
    [SerializeField] private Color textColor = Color.white;   // 文字颜色（默认白色）
    [SerializeField] private Color outlineColor = Color.black; // 描边颜色

    // 对象池引用（由 DamageNumberManager 设置）
    [System.NonSerialized] public System.Action<DamageNumber> OnReturnToPool;

    private Sequence animSeq;
    private Coroutine _safetyTimeoutRoutine;

    private void Awake()
    {
        if (tmp == null)
            tmp = GetComponent<TextMeshPro>();

        if (tmp == null)
        {
            tmp = gameObject.AddComponent<TextMeshPro>();
        }

        // 初始化 TextMeshPro 基本属性（颜色/描边在 Show() 中按需设置）
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.text = "";
    }

    /// <summary>
    /// 在指定世界坐标位置显示伤害数字
    /// </summary>
    /// <param name="worldPos">世界坐标位置</param>
    /// <param name="damage">伤害数值</param>
    public void Show(Vector3 worldPos, float damage, Color? colorOverride = null, HitFeedbackStrength feedbackStrength = HitFeedbackStrength.Standard)
    {
        // 重新激活对象
        gameObject.SetActive(true);
        transform.position = worldPos;

        float scaleMultiplier = feedbackStrength switch
        {
            HitFeedbackStrength.Light => 0.9f,
            HitFeedbackStrength.Heavy => 1.35f,
            _ => 1f
        };
        transform.localScale = Vector3.one * scaleMultiplier;

        // 设置伤害数值（取整显示）
        tmp.text = Mathf.RoundToInt(damage).ToString();

        Color displayColor = colorOverride ?? textColor;
        displayColor.a = 1f;

        tmp.fontStyle = FontStyles.Bold;
        tmp.outlineWidth = 0f;
        tmp.color = displayColor;

        // tmp.color 可能触发材质重置，在之后强制覆盖材质属性
        Material mat = tmp.fontMaterial;
        mat.SetColor("_FaceColor", Color.white);
        mat.SetFloat("_WeightBold", boldWeight);
        mat.SetColor("_OutlineColor", outlineColor);
        if (outlineWidth > 0f)
        {
            mat.EnableKeyword("OUTLINE_ON");
            mat.SetFloat("_OutlineWidth", outlineWidth);
        }
        else
        {
            mat.DisableKeyword("OUTLINE_ON");
        }

        // 终止可能正在播放的动画
        if (animSeq != null && animSeq.IsActive())
        {
            animSeq.Kill();
            animSeq = null;
        }

        // 上飘终点
        Vector3 endPos = worldPos + new Vector3(0f, floatUpDistance, 0f);

        // DOTween 动画：向上漂浮 + 透明度淡出（SetUpdate 无视 timeScale 暂停）
        int instanceId = GetInstanceID();
        animSeq = DOTween.Sequence();
        animSeq.SetTarget(transform);
        animSeq.SetUpdate(true);
        animSeq.SetId($"damageNumber_{instanceId}");

        animSeq.Join(transform.DOMove(endPos, duration).SetEase(Ease.OutQuad));
        if (feedbackStrength == HitFeedbackStrength.Heavy)
        {
            transform.localScale = Vector3.one * (scaleMultiplier * 0.75f);
            animSeq.Insert(0f, transform.DOScale(scaleMultiplier * 1.1f, 0.06f).SetEase(Ease.OutBack));
            animSeq.Insert(0.06f, transform.DOScale(scaleMultiplier, 0.1f).SetEase(Ease.OutQuad));
        }
        animSeq.Join(DOTween.To(() => tmp.color.a, x => { var c = tmp.color; c.a = x; tmp.color = c; }, 0f, duration).SetEase(Ease.InQuad));

        animSeq.OnComplete(OnAnimationComplete);

        // 安全超时兜底：2 倍动画时长后强制回收
        if (_safetyTimeoutRoutine != null)
            StopCoroutine(_safetyTimeoutRoutine);
        _safetyTimeoutRoutine = StartCoroutine(SafetyTimeoutRealtime(duration * 2f));
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

        if (_safetyTimeoutRoutine != null)
        {
            StopCoroutine(_safetyTimeoutRoutine);
            _safetyTimeoutRoutine = null;
        }

        Color c = tmp.color;
        c.a = 1f;
        tmp.color = c;
        transform.localScale = Vector3.one;

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (animSeq != null && animSeq.IsActive())
        {
            animSeq.Kill();
            animSeq = null;
        }
        if (_safetyTimeoutRoutine != null)
        {
            StopCoroutine(_safetyTimeoutRoutine);
            _safetyTimeoutRoutine = null;
        }
    }

    private void OnAnimationComplete()
    {
        animSeq = null;
        if (_safetyTimeoutRoutine != null)
        {
            StopCoroutine(_safetyTimeoutRoutine);
            _safetyTimeoutRoutine = null;
        }
        gameObject.SetActive(false);
        OnReturnToPool?.Invoke(this);
    }

    private System.Collections.IEnumerator SafetyTimeoutRealtime(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        Debug.LogWarning($"[DamageNumber] 安全超时强制回收 instanceId={GetInstanceID()}");
        if (animSeq != null && animSeq.IsActive())
        {
            animSeq.Kill();
            animSeq = null;
        }
        _safetyTimeoutRoutine = null;
        gameObject.SetActive(false);
        OnReturnToPool?.Invoke(this);
    }
}
