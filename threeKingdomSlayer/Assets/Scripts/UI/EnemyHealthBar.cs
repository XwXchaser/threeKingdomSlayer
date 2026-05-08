using UnityEngine;

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
    private float hideTimer;
    private bool created;
    private Vector3 fillOriginPos;
    private float fillFullWidth;
    private float _cachedYOffset;
    private Transform _enemyTransform;

    private static Material _barMaterial;

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
        bgRenderer.material.color = new Color(0.05f, 0.05f, 0.05f, 0.75f);

        // 填充 — 左对齐，通过 position + scale 控制宽度
        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(barRoot.transform, false);
        fillGo.transform.localPosition = new Vector3(-barWidth * 0.5f, 0, -0.01f);
        fillGo.transform.localScale = new Vector3(barWidth, barHeight, 1f);
        var fillMf = fillGo.AddComponent<MeshFilter>();
        fillMf.sharedMesh = quad;
        fillRenderer = fillGo.AddComponent<MeshRenderer>();
        fillRenderer.sharedMaterial = _barMaterial;
        fillRenderer.material.color = highColor;

        fillOriginPos = fillGo.transform.localPosition;
        fillFullWidth = barWidth;

        barRoot.SetActive(false);
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

        // 填充宽度 = barWidth * percent，左对齐：pos.x = fillOriginPos.x + fillWidth * 0.5f
        var fillWidth = fillFullWidth * percent;
        var fillT = fillRenderer.transform;
        var pos = fillOriginPos;
        pos.x = fillOriginPos.x + fillWidth * 0.5f;
        fillT.localPosition = pos;
        fillT.localScale = new Vector3(fillWidth, barHeight, 1f);

        var t = lowThreshold > 0.001f ? Mathf.Clamp01(percent / lowThreshold) : 1f;
        fillRenderer.material.color = Color.Lerp(lowColor, highColor, t);
    }

    private Vector3 GetHeadWorldPosition()
    {
        return _enemyTransform.position + new Vector3(0, _cachedYOffset * _enemyTransform.lossyScale.y, 0);
    }

    private void Update()
    {
        if (!created) return;
        if (hideTimer <= 0f) return;

        hideTimer -= Time.deltaTime;
        if (hideTimer <= 0f)
        {
            barRoot.SetActive(false);
            return;
        }

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
