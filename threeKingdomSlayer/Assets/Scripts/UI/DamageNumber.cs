using UnityEngine;
using TMPro;

/// <summary>
/// 伤害跳字
/// 受击后在敌人右侧弹出红色带黑色描边的伤害数字，
/// 向上飘动并逐渐淡出，完成后回收到对象池。
/// 使用自驱动 Update() 动画，不依赖 DOTween，避免 Sequence 池耗尽。
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

    private Vector3 _startPos;
    private Vector3 _endPos;
    private Color _startColor;
    private float _elapsed;
    private bool _isAnimating;
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
    public void Show(Vector3 worldPos, float damage, Color? colorOverride = null)
    {
        // 重新激活对象
        gameObject.SetActive(true);
        transform.position = worldPos;

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

        // 启动自驱动动画
        _startPos = worldPos;
        _endPos = worldPos + new Vector3(0f, floatUpDistance, 0f);
        _startColor = displayColor;
        _elapsed = 0f;
        _isAnimating = true;

        // 安全超时兜底：2 倍动画时长后强制回收
        if (_safetyTimeoutRoutine != null)
            StopCoroutine(_safetyTimeoutRoutine);
        _safetyTimeoutRoutine = StartCoroutine(SafetyTimeoutRealtime(duration * 2f));
    }

    private void Update()
    {
        if (!_isAnimating) return;

        _elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(_elapsed / duration);

        // 上飘：OutQuad 等价 — t*(2-t)，先快后慢
        float moveT = t * (2f - t);
        transform.position = Vector3.Lerp(_startPos, _endPos, moveT);

        // 淡出：InQuad 等价 — t²，先慢后快
        float alpha = 1f - t * t;
        Color c = tmp.color;
        c.a = _startColor.a * alpha;
        tmp.color = c;

        if (t >= 1f)
        {
            _isAnimating = false;
            OnAnimationComplete();
        }
    }

    /// <summary>
    /// 重置状态（对象池回收时调用）
    /// </summary>
    public void ResetNumber()
    {
        _isAnimating = false;

        if (_safetyTimeoutRoutine != null)
        {
            StopCoroutine(_safetyTimeoutRoutine);
            _safetyTimeoutRoutine = null;
        }

        Color c = tmp.color;
        c.a = 1f;
        tmp.color = c;

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        _isAnimating = false;
        if (_safetyTimeoutRoutine != null)
        {
            StopCoroutine(_safetyTimeoutRoutine);
            _safetyTimeoutRoutine = null;
        }
    }

    private void OnAnimationComplete()
    {
        _isAnimating = false;
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
        _isAnimating = false;
        if (_safetyTimeoutRoutine != null)
        {
            _safetyTimeoutRoutine = null;
        }
        gameObject.SetActive(false);
        OnReturnToPool?.Invoke(this);
    }
}
