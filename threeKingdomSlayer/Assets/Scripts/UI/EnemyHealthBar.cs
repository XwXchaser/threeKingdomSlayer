using UnityEngine;
using TMPro;

/// <summary>
/// 敌人血条UI — 受击时短暂显示百分比血条（无文字）
/// 使用程序化 Quad Mesh + 自发光材质，确保在 BIRP 场景中始终可见
///
/// 2026-10-22（asmdef 分层第六批）：实现 <see cref="IEnemyHealthBarView"/> 端口
/// —— 内核（Enemy / SharedHealthGroup）只经工厂拿视图，不再直接引用本组件。
/// </summary>
public class EnemyHealthBar : MonoBehaviour, IEnemyHealthBarView
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

    // ── 被加攻标记（督战单位 1013 的号令）──
    /// <summary>标记的根（与血条根平级，**不挂在血条里** → 血条隐藏时标记仍可见）。</summary>
    private GameObject _atkBuffMarkerRoot;
    private SpriteRenderer _atkBuffIcon;
    private float _atkBuffIconScale = 1f;

    [Header("被加攻标记（敌人左上角）")]
    [Tooltip("标记图标（留空 = 用**玩家连击的加攻标记**，即 combo_red_dagger_pixel）")]
    public Sprite atkBuffIconSprite;
    [Tooltip("标记图标尺寸（相对血条宽度）")]
    public float atkBuffIconSizeRatio = 0.4f;
    [Tooltip("标记相对美术**左边界**的横向间距（0 = 跨在左边界上）")]
    public float atkBuffIconGap = 0.12f;
    [Tooltip("标记相对美术**上边界**的纵向间距")]
    public float atkBuffIconLift = 0.06f;
    private float hideTimer;
    private bool created;
    private Vector3 fillOriginPos;
    private float fillFullWidth;
    private float _cachedYOffset;
    private bool _useSilhouetteTop;
    private float _cachedSilhouetteTopLocalY;
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
            // 用真实剪影顶点定位头顶，避开 Tight 打包的透明边距。
            // 旧实现取画布半高（bounds），109 这类高大单位会被算高一截。
            // 存下剪影顶点 Y，由世界位置公式换算出世界空间偏移。
            if (enemySr != null && enemySr.sprite != null
                && EnemyGroundAligner.TryGetSilhouetteBounds(enemySr.sprite, out var silMinY, out var silMaxY))
            {
                _cachedSilhouetteTopLocalY = silMaxY;
                _useSilhouetteTop = true;
                actualYOffset = silMaxY + 0.3f;
            }
            else if (enemySr != null && enemySr.sprite != null)
            {
                _useSilhouetteTop = false;
                actualYOffset = enemySr.sprite.bounds.size.y * 0.5f + 0.3f;
            }
            else
            {
                _useSilhouetteTop = false;
                actualYOffset = 2.5f;
            }
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
        CreateAtkBuffMarker();

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

    /// <summary>
    /// 建「被加攻标记」（敌人**左上角**）。
    ///
    /// 需求（2026-11-07 用户定）：「敌人加攻暂时在单位左上方用**连击的加攻标记**表示」
    ///   • 图标 = **玩家连击的加攻标记**（<c>ComboBuffHUD.DamageBuffIcon</c> = combo_red_dagger_pixel）
    ///     → 玩家一眼认得「这是加攻」；
    ///   • 位置 = 单位美术剪影的左边界 / 上边界（左上方）；
    ///   • 层数**不做文字**：世界空间 TMP 在本项目里画不出可读的尺寸（实测网格 10×0.02），
    ///     层数仍可查（<c>EnemyStatusAtkMod.GetUpStacks</c>）；要可见的层数应该做在**指挥头顶飘字**上。
    ///
    /// ⚠️ 标记根与血条根**平级**（不挂在血条里）→ 血条隐藏（敌人没掉血）时标记仍然可见。
    /// </summary>
    private void CreateAtkBuffMarker()
    {
        if (_atkBuffMarkerRoot != null) return;

        _atkBuffMarkerRoot = new GameObject("AtkBuffMarker");
        _atkBuffMarkerRoot.transform.position = GetHeadWorldPosition();

        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(_atkBuffMarkerRoot.transform, false);
        _atkBuffIcon = iconGo.AddComponent<SpriteRenderer>();
        _atkBuffIcon.sortingOrder = StatusSortingOrder + 4;
        iconGo.transform.localScale = Vector3.one;

        _atkBuffMarkerRoot.SetActive(false);
    }

    /// <summary>标记图标：Inspector 指定优先；否则用玩家连击的加攻标记。</summary>
    private Sprite ResolveAtkBuffIcon()
    {
        if (atkBuffIconSprite != null) return atkBuffIconSprite;
        var hud = HeroHUD.Instance;
        if (hud == null) return null;
        var combo = hud.ComboBuffHud;
        return combo != null ? combo.DamageBuffIcon : null;
    }

    /// <summary>刷新被加攻标记（每帧跟随敌人）。</summary>
    private void UpdateAtkBuffMarker()
    {
        if (_atkBuffMarkerRoot == null) return;
        var enemy = GetComponent<Enemy>();
        var status = EnemyStatusRegistry.GetStatus(enemy, EnemyStatusRegistry.AtkUp);
        bool show = status.active && status.stacks > 0;

        var icon = ResolveAtkBuffIcon();
        if (show && icon == null) show = false;   // 图标还没取到（玩家 HUD 未就绪）→ 先不显示
        if (_atkBuffMarkerRoot.activeSelf != show) _atkBuffMarkerRoot.SetActive(show);
        if (!show) return;

        if (_atkBuffIcon != null)
        {
            if (_atkBuffIcon.sprite != icon) _atkBuffIcon.sprite = icon;
            float target = barWidth * atkBuffIconSizeRatio;
            float spriteSize = icon.bounds.size.x;
            float scale = spriteSize > 0.0001f ? target / spriteSize : 1f;
            if (!Mathf.Approximately(_atkBuffIconScale, scale))
            {
                _atkBuffIconScale = scale;
                _atkBuffIcon.transform.localScale = Vector3.one * scale;
            }
        }

        // 位置：**单位的正上方居中**（2026-11-07 用户定：「保持高度然后居中」）
        //   → X 用美术的**水平中心**（不是左边缘），Y 保持美术顶边往上一点。
        float iconWorld = barWidth * atkBuffIconSizeRatio;
        Vector3 pos;
        var sr = enemy != null ? enemy.GetComponent<SpriteRenderer>() : null;
        if (sr != null && sr.sprite != null && TryGetArtAnchor(sr, out Vector3 artCenter, out Vector3 artTop))
        {
            pos = new Vector3(artCenter.x, artTop.y + atkBuffIconLift, artCenter.z);
        }
        else
        {
            pos = GetHeadWorldPosition();
        }
        _atkBuffMarkerRoot.transform.position = pos;
    }

    /// <summary>
    /// 精灵**实际画出来那块**的水平中心与顶边（世界坐标）。
    ///
    /// ⚠️ 不能用 <c>sprite.bounds</c>：打包图集里的多帧精灵，bounds 是**整块图位**
    /// （1012 实测：图位 1108×1108px = 69.25 世界单位，实际画面只有 530×609px 且贴在图位左下角）。
    /// → 用 bounds 当锚点，标记会飘到美术外面几十个世界单位（2026-11-07 用户反馈「位置很离谱」）。
    /// </summary>
    private static bool TryGetArtAnchor(SpriteRenderer sr, out Vector3 center, out Vector3 top)
    {
        center = top = Vector3.zero;
        var sprite = sr.sprite;
        if (sprite == null) return false;

        Rect tr = sprite.textureRect;                       // 真正被画出来的区域（在图集里的位置）
        float ppu = Mathf.Max(0.0001f, sprite.pixelsPerUnit);
        Vector2 pivot = sprite.pivot;                       // 枢轴（在图位像素空间）
        float artCenterLocal = (tr.x + tr.width * 0.5f - pivot.x) / ppu;
        float artTopLocal = (tr.y + tr.height - pivot.y) / ppu;

        Transform owner = sr.transform;
        Vector3 p = owner.position;
        Vector3 s = owner.lossyScale;
        center = new Vector3(p.x + artCenterLocal * s.x, p.y, p.z);
        top = new Vector3(p.x, p.y + artTopLocal * s.y, p.z);
        return true;
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
        // 状态查询走 EnemyStatusRegistry（统一查询层）；显示的槽位仍是疾病条 / 灼烧条。
        var enemy = GetComponent<Enemy>();
        var disease = EnemyStatusRegistry.GetStatus(enemy, EnemyStatusRegistry.Disease);
        var burn = EnemyStatusRegistry.GetStatus(enemy, EnemyStatusRegistry.Burn);

        float statusHeight = barHeight * StatusBarHeightMultiplier;
        float diseaseY = -(barHeight * 0.5f + StatusBarGap + statusHeight * 0.5f);
        bool hasDisease = disease.active;
        bool hasBurn = burn.active;

        if (_diseaseBar != null)
        {
            _diseaseBar.root.SetActive(hasDisease);
            if (hasDisease)
                SetStatusBarFill(_diseaseBar, disease.progress, diseaseY, statusHeight);
        }

        if (_burnBar != null)
        {
            _burnBar.root.SetActive(hasBurn);
            if (hasBurn)
            {
                float burnY = hasDisease
                    ? diseaseY - statusHeight - StatusBarGap
                    : diseaseY;
                SetStatusBarFill(_burnBar, burn.progress, burnY, statusHeight);
            }
        }

        if (_diseaseLayersText != null)
        {
            _diseaseLayersText.gameObject.SetActive(hasDisease);
            if (hasDisease)
            {
                _diseaseLayersText.text = disease.stacks.ToString();
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
        if (!_useSilhouetteTop)
            return _enemyTransform.position + new Vector3(0, _cachedYOffset * _enemyTransform.lossyScale.y, 0);

        // 剪影顶点在 sprite 网格空间；敌人根节点的 world Y 已含 visualYOffset 抬升，
        // 所以直接用顶点换算即可，不再叠加偏移。
        float headY = _enemyTransform.position.y + _cachedSilhouetteTopLocalY * _enemyTransform.lossyScale.y;
        return new Vector3(_enemyTransform.position.x, headY + 0.3f, _enemyTransform.position.z);
    }

    private void Update()
    {
        // ⚠️ 不再因为「没被 Show 过」而早退：
        //   被加攻的敌人可能从没掉过血（血条没显示过）→ 但它的加攻标记必须出现。
        //   EnsureCreated 幂等，且血条根建好后是隐藏的（不会凭空冒出血条）。
        if (!created) EnsureCreated();

        bool hasDot = UpdateDotVisuals();
        UpdateAtkBuffMarker();
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
        if (_atkBuffMarkerRoot != null)
            Destroy(_atkBuffMarkerRoot);
    }
}
