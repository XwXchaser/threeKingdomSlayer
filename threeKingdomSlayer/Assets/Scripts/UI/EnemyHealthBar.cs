using UnityEngine;
using TMPro;

/// <summary>
/// 敌人血条UI — 受击时短暂显示百分比血条（无文字）
/// 使用程序化 Quad Mesh + 自发光材质，确保在 BIRP 场景中始终可见
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    [Header("尺寸")]
    [Tooltip("血条宽度（世界单位）")]
    public float barWidth = 2f;
    [Tooltip("血条高度（世界单位）")]
    public float barHeight = 0.2f;
    [Tooltip("血条在敌人头顶的偏移（世界单位），0=自动计算")]
    public float yOffset = 0f;

    [Header("显示时长")]
    [Tooltip("受击后血条持续显示秒数")]
    public float displayDuration = 2f;

    [Header("颜色")]
    public Color highColor = new Color(0.2f, 0.8f, 0.2f);
    public Color lowColor = new Color(0.9f, 0.2f, 0.2f);
    [Range(0f, 1f)]
    public float lowThreshold = 0.3f;

    private GameObject barRoot;
    private MeshRenderer bgRenderer;
    private MeshRenderer fillRenderer;
    private Material fillMaterialInstance;
    private StatusBarVisual _diseaseBar;
    private StatusBarVisual _burnBar;
    private TextMeshPro _diseaseLayersText;
    private float hideTimer;
    private bool created;
    private Vector3 fillOriginPos;
    private float fillFullWidth;
    private float _cachedYOffset;
    private Transform _enemyTransform;

    private sealed class StatusBarVisual
    {
        public GameObject root;
        public Transform fillTransform;
        public Material fillMaterial;
        public Vector3 fillOrigin;
        public float fullWidth;
    }

    private static Material _barMaterial;
    private const float StatusBarHeightMultiplier = 0.55f;
    private const float StatusBarGap = 0f;
    private const int StatusSortingOrder = 200;

    private void Awake()
    {
        if (_barMaterial == null)
        {
            // 自发光材质：不受场景光照影响，始终显示纯色
            var shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            _barMaterial = new Material(shader);
        }
    }

    private void EnsureCreated()
    {
        if (created) return;
        created = true;

        float actualYOffset = yOffset;
        if (actualYOffset <= 0.001f)
        {
            var enemySr = GetComponent<SpriteRenderer>();
            if (enemySr != null && enemySr.sprite != null)
                actualYOffset = enemySr.sprite.bounds.size.y * 0.5f + 0.3f;
            else
                actualYOffset = 2.5f;
        }

        var quad = CreateQuadMesh();

        _cachedYOffset = actualYOffset;
        _enemyTransform = transform;

        barRoot = new GameObject("HealthBar");
        // 不挂载为敌人子物体，避免继承攻击翻转
        barRoot.transform.position = GetHeadWorldPosition();

        // 背景
        var bgGo = new GameObject("BG");
        bgGo.transform.SetParent(barRoot.transform, false);
        bgGo.transform.localScale = new Vector3(barWidth, barHeight, 1f);
        var bgMf = bgGo.AddComponent<MeshFilter>();
        bgMf.sharedMesh = quad;
        bgRenderer = bgGo.AddComponent<MeshRenderer>();
        bgRenderer.sharedMaterial = _barMaterial;
        bgRenderer.sortingOrder = StatusSortingOrder;
        bgRenderer.material.color = new Color(0.05f, 0.05f, 0.05f, 0.75f);

        // 填充 — 左对齐，通过 position + scale 控制宽度
        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(barRoot.transform, false);
        fillGo.transform.localPosition = new Vector3(-barWidth * 0.5f, 0, -0.01f);
        fillGo.transform.localScale = new Vector3(barWidth, barHeight, 1f);
        var fillMf = fillGo.AddComponent<MeshFilter>();
        fillMf.sharedMesh = quad;
        fillRenderer = fillGo.AddComponent<MeshRenderer>();
        // 显式创建材质实例并直接赋值给 Renderer，不依赖 Renderer.material 的内部缓存
        fillMaterialInstance = new Material(_barMaterial);
        fillMaterialInstance.mainTexture = Texture2D.whiteTexture;
        fillMaterialInstance.color = highColor;
        fillRenderer.material = fillMaterialInstance;
        fillRenderer.sortingOrder = StatusSortingOrder + 1;

        fillOriginPos = fillGo.transform.localPosition;
        fillFullWidth = barWidth;

        _diseaseBar = CreateStatusBar(quad, "DiseaseBar", new Color(0.72f, 0.28f, 0.9f));
        _burnBar = CreateStatusBar(quad, "BurnBar", new Color(0.9f, 0.15f, 0.15f));
        CreateDiseaseLayersText();

        barRoot.SetActive(false);
    }

    private StatusBarVisual CreateStatusBar(Mesh quad, string name, Color color)
    {
        float height = barHeight * StatusBarHeightMultiplier;
        var root = new GameObject(name);
        root.transform.SetParent(barRoot.transform, false);

        var background = new GameObject("Background");
        background.transform.SetParent(root.transform, false);
        background.transform.localScale = new Vector3(barWidth, height, 1f);
        var bgFilter = background.AddComponent<MeshFilter>();
        bgFilter.sharedMesh = quad;
        var bgRenderer = background.AddComponent<MeshRenderer>();
        bgRenderer.sharedMaterial = _barMaterial;
        bgRenderer.material.color = new Color(0.04f, 0.04f, 0.04f, 0.8f);
        bgRenderer.sortingOrder = StatusSortingOrder + 2;

        var fill = new GameObject("Fill");
        fill.transform.SetParent(root.transform, false);
        var fillFilter = fill.AddComponent<MeshFilter>();
        fillFilter.sharedMesh = quad;
        var fillRenderer = fill.AddComponent<MeshRenderer>();
        var material = new Material(_barMaterial);
        material.color = color;
        fillRenderer.material = material;
        fillRenderer.sortingOrder = StatusSortingOrder + 3;
        fill.transform.localPosition = new Vector3(-barWidth * 0.5f, 0f, -0.01f);
        fill.transform.localScale = new Vector3(barWidth, height, 1f);
        root.SetActive(false);

        return new StatusBarVisual
        {
            root = root,
            fillTransform = fill.transform,
            fillMaterial = material,
            fillOrigin = fill.transform.localPosition,
            fullWidth = barWidth
        };
    }

    private void CreateDiseaseLayersText()
    {
        var go = new GameObject("DiseaseLayers", typeof(TextMeshPro));
        go.transform.SetParent(barRoot.transform, false);
        _diseaseLayersText = go.GetComponent<TextMeshPro>();
        _diseaseLayersText.fontSize = 3.5f;
        _diseaseLayersText.fontStyle = FontStyles.Bold;
        _diseaseLayersText.color = new Color(0.82f, 0.55f, 1f);
        _diseaseLayersText.alignment = TextAlignmentOptions.Right;
        _diseaseLayersText.sortingOrder = StatusSortingOrder + 4;
        go.SetActive(false);
    }

    private static Mesh CreateQuadMesh()
    {
        var mesh = new Mesh();
        mesh.vertices = new[] {
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3( 0.5f, -0.5f, 0),
            new Vector3(-0.5f,  0.5f, 0),
            new Vector3( 0.5f,  0.5f, 0),
        };
        mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    public void Show(float percent)
    {
        EnsureCreated();

        percent = Mathf.Clamp01(percent);
        hideTimer = displayDuration;

        barRoot.SetActive(true);
        UpdateDotVisuals();

        // 填充宽度 = barWidth * percent，左对齐：pos.x = fillOriginPos.x + fillWidth * 0.5f
        var fillWidth = fillFullWidth * percent;
        var fillT = fillRenderer.transform;
        var pos = fillOriginPos;
        pos.x = fillOriginPos.x + fillWidth * 0.5f;
        fillT.localPosition = pos;
        fillT.localScale = new Vector3(fillWidth, barHeight, 1f);

        var t = lowThreshold > 0.001f ? Mathf.Clamp01(percent / lowThreshold) : 1f;
        // 确保 Renderer 使用我们的材质实例（disable/enable 后可能被 Unity 内部重置）
        if (fillMaterialInstance != null && fillRenderer.material != fillMaterialInstance)
        {
            fillRenderer.material = fillMaterialInstance;
        }
        fillMaterialInstance.color = Color.Lerp(lowColor, highColor, t);
    }

    public void ShowDotStatus()
    {
        EnsureCreated();
        barRoot.SetActive(true);
        UpdateDotVisuals();
    }

    private bool UpdateDotVisuals()
    {
        var status = UpgradeEffectManager.Instance != null
            ? UpgradeEffectManager.Instance.GetDotStatus(GetComponent<Enemy>())
            : default;

        float statusHeight = barHeight * StatusBarHeightMultiplier;
        float diseaseY = -(barHeight * 0.5f + StatusBarGap + statusHeight * 0.5f);
        bool hasDisease = status.isDiseased;
        bool hasBurn = status.isBurning;

        if (_diseaseBar != null)
        {
            _diseaseBar.root.SetActive(hasDisease);
            if (hasDisease)
                SetStatusBarFill(_diseaseBar, status.diseaseProgress, diseaseY, statusHeight);
        }

        if (_burnBar != null)
        {
            _burnBar.root.SetActive(hasBurn);
            if (hasBurn)
            {
                float burnY = hasDisease
                    ? diseaseY - statusHeight - StatusBarGap
                    : diseaseY;
                SetStatusBarFill(_burnBar, status.burnProgress, burnY, statusHeight);
            }
        }

        if (_diseaseLayersText != null)
        {
            _diseaseLayersText.gameObject.SetActive(hasDisease);
            if (hasDisease)
            {
                _diseaseLayersText.text = status.diseaseLayers.ToString();
                _diseaseLayersText.transform.localPosition = new Vector3(-barWidth * 0.5f - 0.08f, diseaseY, -0.02f);
            }
        }

        return hasDisease || hasBurn;
    }

    private static void SetStatusBarFill(StatusBarVisual bar, float progress, float y, float height)
    {
        float width = bar.fullWidth * Mathf.Clamp01(progress);
        var position = bar.fillOrigin;
        position.x = bar.fillOrigin.x + width * 0.5f;
        bar.fillTransform.localPosition = position;
        bar.fillTransform.localScale = new Vector3(width, height, 1f);
        bar.root.transform.localPosition = new Vector3(0f, y, 0f);
    }

    /// <summary>
    /// 立即隐藏血条（用于敌人死亡时立即关闭血条）
    /// </summary>
    public void Hide()
    {
        hideTimer = 0f;
        if (barRoot != null)
            barRoot.SetActive(false);
    }

    private Vector3 GetHeadWorldPosition()
    {
        return _enemyTransform.position + new Vector3(0, _cachedYOffset * _enemyTransform.lossyScale.y, 0);
    }

    private void Update()
    {
        if (!created) return;

        bool hasDot = UpdateDotVisuals();
        if (hideTimer <= 0f && !hasDot)
        {
            barRoot.SetActive(false);
            return;
        }

        if (hideTimer > 0f)
            hideTimer = Mathf.Max(0f, hideTimer - Time.deltaTime);

        barRoot.SetActive(true);
        // 每帧跟随敌人位置（独立于敌人旋转/缩放）
        barRoot.transform.position = GetHeadWorldPosition();
    }

    private void OnDisable()
    {
        if (barRoot != null) barRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (barRoot != null)
            Destroy(barRoot);
    }
}
