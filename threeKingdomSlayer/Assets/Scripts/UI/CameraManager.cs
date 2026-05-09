using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景相机管理器：背景图缩放 + OnRenderImage 模糊 + 场景过渡。
/// 挂载在 Main Camera 上，每个场景独立配置。
/// </summary>
public class CameraManager : MonoBehaviour
{
    [Header("背景")]
    [Tooltip("拖入要做缩放动画的 RectTransform（如背景 Image）")]
    public RectTransform background;

    [Header("动画参数")]
    [Tooltip("动画时长（秒）")]
    public float duration = 1.2f;
    [Tooltip("模糊强度峰值")]
    public float maxBlur = 4f;
    [Tooltip("背景目标缩放倍率（Departure=放大推入，Arrival=从大缩回）")]
    public float targetScale = 1.5f;
    [Tooltip("背景位移偏移（相对 anchoredPosition 的增量）")]
    public Vector2 positionOffset;

    [Header("场景加载（仅 Departure）")]
    [Tooltip("推入完成后要加载的场景名")]
    public string nextScene;

    [Header("模糊")]
    [Tooltip("用于 OnRenderImage 后处理模糊的 Shader")]
    [SerializeField] private Shader blurShader;

    [Header("独立测试")]
    [Tooltip("勾选后 Start 时自动播放 Arrival 动画（不受场景切换影响）")]
    public bool autoPlayOnStart;

    // 跨场景信号
    public static bool IsArriving { get; private set; }

    private Material blurMat;
    private bool isRunning;

    private void Awake()
    {
        var shader = blurShader != null ? blurShader : Shader.Find("Hidden/BlurEffect");
        blurMat = shader != null ? new Material(shader) : null;
        if (blurMat == null)
            Debug.LogError("[CameraManager] Shader 'Hidden/BlurEffect' 未找到且 blurShader 未赋值");
    }

    private void Start()
    {
        if (IsArriving || autoPlayOnStart)
        {
            IsArriving = false;
            StartCoroutine(ArrivalRoutine());
        }
    }

    private void OnDestroy()
    {
        if (blurMat != null) Destroy(blurMat);
    }

    /// <summary>
    /// MainMenu 调用：开始推入动画 + 加载下一场景。
    /// </summary>
    public void PlayDeparture()
    {
        if (isRunning || background == null) return;
        StartCoroutine(DepartureRoutine());
    }

    /// <summary>
    /// 也可手动触发接入动画（用于测试 Battle 场景）。
    /// </summary>
    public void PlayArrival()
    {
        if (isRunning || background == null) return;
        StartCoroutine(ArrivalRoutine());
    }

    // ──── 动画协程 ────

    private System.Collections.IEnumerator DepartureRoutine()
    {
        isRunning = true;
        Debug.Log("[CameraManager] Departure 开始");

        Vector2 origAnchored = background.anchoredPosition;
        Vector3 origScale = background.localScale;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float easeT = t * t;

            background.localScale = Vector3.Lerp(origScale, Vector3.one * targetScale, easeT);
            background.anchoredPosition = origAnchored + positionOffset * easeT;
            SetBlur(easeT * maxBlur);
            yield return null;
        }

        background.localScale = Vector3.one * targetScale;
        background.anchoredPosition = origAnchored + positionOffset;
        SetBlur(maxBlur);

        isRunning = false;
        Debug.Log("[CameraManager] Departure 完成，加载 " + nextScene);

        if (!string.IsNullOrEmpty(nextScene))
        {
            IsArriving = true;
            SceneManager.LoadSceneAsync(nextScene);
        }
    }

    private System.Collections.IEnumerator ArrivalRoutine()
    {
        isRunning = true;
        Debug.Log("[CameraManager] Arrival 开始");

        // 以当前 scale 为起点，targetScale 为终点
        Vector3 startScale = background.localScale;
        Vector3 endScale = Vector3.one * targetScale;
        Vector2 startAnchored = background.anchoredPosition;
        Vector2 endAnchored = startAnchored - positionOffset;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float easeT = 1f - (1f - t) * (1f - t);

            background.localScale = Vector3.Lerp(startScale, endScale, easeT);
            background.anchoredPosition = Vector2.Lerp(startAnchored, endAnchored, easeT);
            SetBlur((1f - easeT) * maxBlur);
            yield return null;
        }

        background.localScale = endScale;
        background.anchoredPosition = endAnchored;
        SetBlur(0f);

        isRunning = false;
        Debug.Log("[CameraManager] Arrival 完成");
    }

    // ──── 模糊 ────

    private float currentBlur;

    public void SetBlur(float t)
    {
        currentBlur = Mathf.Max(0f, t);
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        if (blurMat == null || currentBlur <= 0.001f)
        {
            Graphics.Blit(src, dst);
            return;
        }

        int w = src.width / 2;
        int h = src.height / 2;

        var rtA = RenderTexture.GetTemporary(w, h, 0, src.format);
        var rtB = RenderTexture.GetTemporary(w, h, 0, src.format);
        rtA.filterMode = FilterMode.Bilinear;
        rtB.filterMode = FilterMode.Bilinear;

        blurMat.SetFloat("_BlurSize", currentBlur);

        Graphics.Blit(src, rtA, blurMat, 0);
        Graphics.Blit(rtA, rtB, blurMat, 1);
        Graphics.Blit(rtB, dst, blurMat, 2);

        RenderTexture.ReleaseTemporary(rtA);
        RenderTexture.ReleaseTemporary(rtB);
    }
}
