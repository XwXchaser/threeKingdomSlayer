using System.IO;
using UnityEngine;

/// <summary>
/// Pierce 飞行旋涡视觉：围绕枪轴的手工火焰爪片碎片层。
/// 每帧独立定义爪片位置/曲率/宽度，前后景分层渲染，中心留出武器通道。
/// 不再使用规则环形扇区推导。
/// </summary>
public sealed class PierceVortexVisual : MonoBehaviour
{
    private const int FrameCount = 4;
    private const int TextureSize = 128;
    private const int Center = TextureSize / 2;
    private const float CenterHoleRadius = 30f;
    private const float AfterimageInterval = 0.025f;

    private static Sprite[] _backFrames;
    private static Sprite[] _frontFrames;

    private SpriteRenderer _backRenderer;
    private SpriteRenderer _frontRenderer;
    private Transform _anchor;
    private float _elapsed;
    private float _afterimageTimer;
    private float _slowRotation;
    private float _fade = 1f;

    // 深色（背景）层配色
    private static readonly Color32 BackOutline = new Color32(26, 8, 6, 255);
    private static readonly Color32 BackBody = new Color32(96, 20, 8, 255);
    private static readonly Color32 BackMid = new Color32(150, 34, 10, 255);
    private static readonly Color32 BackHi = new Color32(188, 58, 12, 255);

    // 亮色（前景）层配色：黑 → 红 → 橙 → 金黄 → 白
    private static readonly Color32 FrontOutline = new Color32(38, 10, 6, 255);
    private static readonly Color32 FrontBody = new Color32(176, 35, 9, 255);
    private static readonly Color32 FrontMid = new Color32(224, 58, 10, 255);
    private static readonly Color32 FrontGold = new Color32(255, 205, 25, 255);
    private static readonly Color32 FrontWhite = new Color32(255, 255, 250, 255);

    private readonly struct Claw
    {
        public readonly float startAngle;
        public readonly float endAngle;
        public readonly float startRadius;
        public readonly float endRadius;
        public readonly float curveAngle;
        public readonly float baseWidth;
        public readonly float tipWidth;
        public readonly bool front;
        public readonly int gold;

        public Claw(float sa, float ea, float sr, float er, float ca,
            float bw, float tw, bool front, int gold)
        {
            startAngle = sa;
            endAngle = ea;
            startRadius = sr;
            endRadius = er;
            curveAngle = ca;
            baseWidth = bw;
            tipWidth = tw;
            this.front = front;
            this.gold = gold;
        }
    }

    // 四帧：聚拢 → 旋转偏移 → 破碎外扩 → 消散残片。
    private static readonly Claw[][] Claws =
    {
        new[]
        {
            new Claw(  0,  78, 36, 44,  22, 9f, 3f, true, 1),
            new Claw(120, 198, 34, 46,  20, 8f, 2f, true, 0),
            new Claw(240, 318, 37, 42,  16, 7f, 3f, true, 1),
            new Claw( 55, 112, 30, 38, -18, 6f, 2f, true, 0),
            new Claw(175, 232, 32, 40, -16, 6f, 2f, true, 0),
            new Claw(300,   8, 33, 43, -20, 6f, 2f, true, 1),
            new Claw( 30,  88, 46, 54,  14, 7f, 2f, false, 0),
            new Claw(150, 208, 48, 56,  12, 7f, 2f, false, 0),
            new Claw(270, 328, 45, 53,  10, 6f, 2f, false, 0),
            new Claw( 92, 138, 44, 50,  -9, 5f, 1f, false, 0),
        },
        new[]
        {
            new Claw( 30, 108, 37, 46,  24, 8f, 2f, true, 1),
            new Claw(150, 228, 35, 48,  22, 8f, 2f, true, 0),
            new Claw(270, 348, 38, 44,  18, 7f, 2f, true, 1),
            new Claw( 80, 138, 31, 40, -16, 6f, 2f, true, 0),
            new Claw(200, 258, 33, 42, -14, 6f, 2f, true, 0),
            new Claw(320,  28, 34, 45, -18, 6f, 2f, true, 1),
            new Claw( 50, 106, 48, 56,  15, 6f, 2f, false, 0),
            new Claw(170, 226, 50, 58,  13, 6f, 2f, false, 0),
            new Claw(290, 346, 47, 55,  11, 5f, 2f, false, 0),
            new Claw(112, 156, 46, 52,  -8, 5f, 1f, false, 0),
        },
        new[]
        {
            new Claw(  0,  60, 38, 50,  20, 6f, 2f, true, 1),
            new Claw(100, 150, 40, 52,  16, 5f, 1f, true, 0),
            new Claw(200, 258, 36, 48,  14, 5f, 2f, true, 1),
            new Claw(310, 360, 42, 54, -14, 5f, 1f, true, 0),
            new Claw( 60, 104, 46, 58,  10, 5f, 2f, false, 0),
            new Claw(170, 212, 48, 60,   8, 4f, 1f, false, 0),
            new Claw(280, 322, 46, 57,   6, 4f, 1f, false, 0),
            new Claw( 25,  62, 52, 62,   9, 3f, 1f, false, 0),
            new Claw(135, 175, 51, 61,   7, 3f, 1f, false, 0),
            new Claw(245, 288, 52, 62,   5, 3f, 1f, false, 0),
        },
        new[]
        {
            new Claw( 15,  55, 40, 50,  12, 4f, 1f, true, 0),
            new Claw(130, 168, 44, 54,  10, 3f, 1f, true, 1),
            new Claw(250, 288, 42, 52,   8, 3f, 1f, true, 0),
            new Claw( 70, 104, 50, 60,   6, 3f, 1f, false, 0),
            new Claw(185, 216, 52, 62,   5, 3f, 1f, false, 0),
            new Claw(300, 330, 51, 60,   4, 2f, 1f, false, 0),
        },
    };

    public static PierceVortexVisual Create(Transform projectileRoot)
    {
        if (projectileRoot == null)
            return null;

        var visual = projectileRoot.gameObject.AddComponent<PierceVortexVisual>();
        visual.Initialize();
        return visual;
    }

    public void SetFade(float fade)
    {
        _fade = Mathf.Clamp01(fade);
        if (_backRenderer != null)
        {
            Color back = _backRenderer.color;
            back.a = _fade;
            _backRenderer.color = back;
        }
        if (_frontRenderer != null)
        {
            Color front = _frontRenderer.color;
            front.a = _fade;
            _frontRenderer.color = front;
        }
    }

    private void Initialize()
    {
        EnsureFrames();

        _anchor = new GameObject("Pierce_VortexAnchor").transform;
        _anchor.SetParent(transform, false);
        _anchor.localPosition = Vector3.up * -0.10f;

        var back = new GameObject("Pierce_VortexBack").transform;
        back.SetParent(_anchor, false);
        back.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        _backRenderer = back.gameObject.AddComponent<SpriteRenderer>();
        _backRenderer.sprite = _backFrames[0];
        _backRenderer.sortingLayerName = "Default";
        _backRenderer.sortingOrder = -1;
        _backRenderer.color = Color.white;

        var front = new GameObject("Pierce_VortexFront").transform;
        front.SetParent(_anchor, false);
        front.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        _frontRenderer = front.gameObject.AddComponent<SpriteRenderer>();
        _frontRenderer.sprite = _frontFrames[0];
        _frontRenderer.sortingLayerName = "Default";
        _frontRenderer.sortingOrder = 3;
        _frontRenderer.color = Color.white;
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        _afterimageTimer -= Time.deltaTime;
        _slowRotation += 110f * Time.deltaTime;

        int frame = Mathf.FloorToInt(_elapsed / 0.075f) % FrameCount;
        _backRenderer.sprite = _backFrames[frame];
        _frontRenderer.sprite = _frontFrames[frame];
        if (_fade < 1f)
        {
            Color back = _backRenderer.color;
            back.a = _fade;
            _backRenderer.color = back;
            Color front = _frontRenderer.color;
            front.a = _fade;
            _frontRenderer.color = front;
        }

        float speedProgress = Mathf.Clamp01(_elapsed / 0.28f);
        float scale = Mathf.Lerp(7.0f, 8.0f, speedProgress);
        _anchor.localScale = Vector3.one * scale;
        _anchor.localRotation = Quaternion.Euler(0f, _slowRotation, 0f);

        if (_afterimageTimer <= 0f)
        {
            _afterimageTimer = AfterimageInterval;
            PierceVortexAfterimage.Create(_anchor.position, _anchor.rotation,
                _anchor.lossyScale, frame, _slowRotation);
        }
    }

    public static Sprite GetFrame(int frame) => _frontFrames[Mathf.Clamp(frame, 0, FrameCount - 1)];
    public static Sprite GetBackFrame(int frame) => _backFrames[Mathf.Clamp(frame, 0, FrameCount - 1)];
    public static Sprite GetFrontFrame(int frame) => _frontFrames[Mathf.Clamp(frame, 0, FrameCount - 1)];

    private static void EnsureFrames()
    {
        if (_frontFrames != null)
            return;

        _backFrames = new Sprite[FrameCount];
        _frontFrames = new Sprite[FrameCount];
        for (int i = 0; i < FrameCount; i++)
        {
            _backFrames[i] = CreateFrame(i, false);
            _frontFrames[i] = CreateFrame(i, true);
        }
    }

    private static Sprite CreateFrame(int frame, bool front)
    {
        var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = $"PierceVortex_{frame}_{(front ? "F" : "B")}"
        };
        var pixels = new Color32[TextureSize * TextureSize];

        DrawQuarterSpiralLayout(pixels, frame, front);
        if (front)
            DrawDirectionalFragments(pixels, frame);

        ClearCenterHole(pixels);

        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        var sprite = Sprite.Create(texture, new Rect(0f, 0f, TextureSize, TextureSize),
            Vector2.one * 0.5f, 32f);
        sprite.name = $"PierceVortex_{frame}_{(front ? "F" : "B")}";
        return sprite;
    }

    private static void DrawQuarterSpiralLayout(Color32[] pixels, int frame, bool front)
    {
        // 以概念图右侧 1-4 帧为节奏：少量大型勾玉面片旋转、断裂、消散。
        int[] angles = { -12, 78, 168, 258 };
        int[] lengths = { 92, 84, 76, 64 };
        int[] radii = { 45, 43, 46, 48 };
        int count = frame == 3 ? 3 : 4;

        for (int i = 0; i < count; i++)
        {
            int angle = angles[i] + frame * 17 + (i % 2 == 0 ? frame * 3 : -frame * 2);
            float radius = radii[i] + (frame == 2 ? i * 2f : 0f);
            float length = lengths[i] - frame * (i == 0 ? 4f : 2f);
            bool pieceFront = i % 2 == 0;
            if (pieceFront != front)
                continue;

            DrawQuarterSpiralPiece(pixels, angle, radius, length, frame, i, front);
        }

        if (frame == 2 || frame == 3)
            DrawLargeDebris(pixels, frame, front);
    }

    private static void DrawQuarterSpiralPiece(Color32[] pixels, float angleDeg,
        float radius, float length, int frame, int index, bool front)
    {
        float baseAngle = angleDeg * Mathf.Deg2Rad;
        float arcHalf = Mathf.Lerp(38f, 50f, Mathf.Clamp01(length / 100f));
        float thickness = frame == 0 ? 13f : frame == 1 ? 12f : 10f;
        const int samples = 18;

        Vector2 previous = Vector2.zero;
        Vector2 previousTangent = Vector2.right;
        float previousOuterWidth = 0f;
        float previousInnerWidth = 0f;
        bool hasPrevious = false;

        for (int i = 0; i <= samples; i++)
        {
            float t = i / (float)samples;
            float arcT = Mathf.Pow(t, 0.82f);
            float arc = Mathf.Lerp(-arcHalf, arcHalf, arcT) * Mathf.Deg2Rad;
            float localRadius = radius + Mathf.Sin(t * Mathf.PI) * (index % 2 == 0 ? 5f : -4f);
            float angle = baseAngle + arc;
            Vector2 radial = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 point = new Vector2(Center, Center) + radial * localRadius;
            float bodyProfile = t < 0.22f
                ? Mathf.Lerp(0.92f, 1f, t / 0.22f)
                : Mathf.Lerp(1f, 0.04f, Mathf.InverseLerp(0.22f, 1f, t));
            float rootProfile = Mathf.Clamp01(bodyProfile);
            float outerWidth = Mathf.Lerp(3f, thickness, rootProfile);
            float innerWidth = outerWidth * Mathf.Lerp(0.52f, 0.72f, rootProfile);
            Vector2 tangent = i == 0 ? radial : (point - previous).normalized;
            if (tangent.sqrMagnitude < 0.001f)
                tangent = previousTangent;

            if (hasPrevious)
            {
                DrawAsymmetricRibbonSegment(pixels, previous, point, previousTangent, tangent,
                    previousOuterWidth, outerWidth, previousInnerWidth, innerWidth, front);

                // 仅在中后段绘制短内侧高光，保持概念图的分段亮边。
                if (front && t > 0.18f && t < 0.78f)
                {
                    float highlightWidth = Mathf.Max(innerWidth * 0.48f, 1f);
                    DrawRibbonSegment(pixels, previous, point, previousTangent, tangent,
                        previousInnerWidth * 0.34f, highlightWidth, front, true);
                }
            }

            previous = point;
            previousTangent = tangent;
            previousOuterWidth = outerWidth;
            previousInnerWidth = innerWidth;
            hasPrevious = true;
        }
    }

    private static void DrawAsymmetricRibbonSegment(Color32[] pixels, Vector2 p0, Vector2 p1,
        Vector2 tangent0, Vector2 tangent1, float outerWidth0, float outerWidth1,
        float innerWidth0, float innerWidth1, bool front)
    {
        Vector2 normal0 = new Vector2(-tangent0.y, tangent0.x);
        Vector2 normal1 = new Vector2(-tangent1.y, tangent1.x);
        float outlineExpand = 1.8f;

        FillQuad(pixels,
            p0 + normal0 * (outerWidth0 + outlineExpand),
            p1 + normal1 * (outerWidth1 + outlineExpand),
            p1 - normal1 * (innerWidth1 + outlineExpand),
            p0 - normal0 * (innerWidth0 + outlineExpand),
            front ? FrontOutline : BackOutline);

        FillQuad(pixels,
            p0 + normal0 * outerWidth0,
            p1 + normal1 * outerWidth1,
            p1 - normal1 * innerWidth1,
            p0 - normal0 * innerWidth0,
            front ? FrontBody : BackBody);

        if (front)
        {
            float goldOuter0 = Mathf.Max(outerWidth0 * 0.42f, 1f);
            float goldOuter1 = Mathf.Max(outerWidth1 * 0.42f, 1f);
            FillQuad(pixels,
                p0 + normal0 * goldOuter0,
                p1 + normal1 * goldOuter1,
                p1 - normal1 * (innerWidth1 * 0.42f),
                p0 - normal0 * (innerWidth0 * 0.42f),
                FrontGold);
        }
    }
    private static void DrawRibbonSegment(Color32[] pixels, Vector2 p0, Vector2 p1,
        Vector2 tangent0, Vector2 tangent1, float width0, float width1,
        bool front, bool highlight)
    {
        Vector2 normal0 = new Vector2(-tangent0.y, tangent0.x);
        Vector2 normal1 = new Vector2(-tangent1.y, tangent1.x);
        Vector2 outer0 = p0 + normal0 * (width0 + 1.8f);
        Vector2 outer1 = p1 + normal1 * (width1 + 1.8f);
        Vector2 outer2 = p1 - normal1 * (width1 + 1.8f);
        Vector2 outer3 = p0 - normal0 * (width0 + 1.8f);
        FillQuad(pixels, outer0, outer1, outer2, outer3, front ? FrontOutline : BackOutline);

        Vector2 inner0 = p0 + normal0 * width0;
        Vector2 inner1 = p1 + normal1 * width1;
        Vector2 inner2 = p1 - normal1 * width1;
        Vector2 inner3 = p0 - normal0 * width0;
        Color32 body = highlight ? FrontWhite : front ? FrontBody : BackBody;
        FillQuad(pixels, inner0, inner1, inner2, inner3, body);

        if (!highlight && front)
        {
            float goldWidth0 = Mathf.Max(width0 * 0.42f, 1f);
            float goldWidth1 = Mathf.Max(width1 * 0.42f, 1f);
            FillQuad(pixels,
                p0 + normal0 * goldWidth0, p1 + normal1 * goldWidth1,
                p1 - normal1 * goldWidth1, p0 - normal0 * goldWidth0,
                FrontGold);
        }
    }



    private static void FillQuad(Color32[] pixels, Vector2 a, Vector2 b, Vector2 c,
        Vector2 d, Color32 color)
    {
        FillTriangle(pixels, a, b, c, color);
        FillTriangle(pixels, a, c, d, color);
    }

    private static void FillTriangle(Color32[] pixels, Vector2 a, Vector2 b, Vector2 c,
        Color32 color)
    {
        int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))));
        int maxX = Mathf.Min(TextureSize - 1, Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))));
        int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))));
        int maxY = Mathf.Min(TextureSize - 1, Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))));

        float area = Edge(a, b, c);
        if (Mathf.Abs(area) < 0.001f)
            return;

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float w0 = Edge(b, c, p);
                float w1 = Edge(c, a, p);
                float w2 = Edge(a, b, p);
                if ((w0 >= 0f && w1 >= 0f && w2 >= 0f)
                    || (w0 <= 0f && w1 <= 0f && w2 <= 0f))
                    SetPixel(pixels, x, y, color);
            }
        }
    }

    private static float Edge(Vector2 a, Vector2 b, Vector2 p)
    {
        return (p.x - a.x) * (b.y - a.y) - (p.y - a.y) * (b.x - a.x);
    }

    private static void DrawLargeDebris(Color32[] pixels, int frame, bool front)
    {
        int seed = 9011 + frame * 97 + (front ? 1 : 0);
        var random = new System.Random(seed);
        int count = frame == 2 ? 5 : 8;
        for (int i = 0; i < count; i++)
        {
            float angle = (float)random.NextDouble() * 360f;
            float radius = 54f + (float)random.NextDouble() * 12f;
            float rad = angle * Mathf.Deg2Rad;
            int x = Mathf.RoundToInt(Center + Mathf.Cos(rad) * radius);
            int y = Mathf.RoundToInt(Center + Mathf.Sin(rad) * radius);
            float size = 2f + (float)random.NextDouble() * 3f;
            Color32 color = front && i % 3 == 0 ? FrontGold : front ? FrontBody : BackBody;
            FillDisc(pixels, x, y, size, color);
        }
    }

    private static void DrawClaw(Color32[] pixels, Claw c)
    {
        float start = c.startAngle;
        float end = c.endAngle;
        if (end < start)
            end += 360f;
        float mid = (start + end) * 0.5f + c.curveAngle;

        int seed = unchecked((int)(c.startAngle * 13f + c.endAngle * 7f + c.startRadius * 31f));
        var random = new System.Random(seed);

        // 离散碎片：沿路径画 4-6 个独立碎片，之间有明显间隙
        int fragmentCount = 4 + random.Next(3);
        for (int i = 0; i < fragmentCount; i++)
        {
            float t = Mathf.Clamp01((i + 0.5f) / fragmentCount + (float)(random.NextDouble() - 0.5) * 0.25f);
            float angle = Bezier(start, mid, end, t) + (float)(random.NextDouble() - 0.5) * 9f;
            float radius = Mathf.Lerp(c.startRadius, c.endRadius, t) + (float)(random.NextDouble() - 0.5) * 5f;
            float width = Mathf.Lerp(c.baseWidth, c.tipWidth, t) * 2.5f;
            StampFlame(pixels, angle, radius, width, random, c.front, c.gold);
        }
    }

    private static void StampFlame(Color32[] pixels, float angleDeg, float radius,
        float width, System.Random random, bool front, int gold)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        float cx = Center + Mathf.Cos(rad) * radius;
        float cy = Center + Mathf.Sin(rad) * radius;

        // 旋涡切线方向（垂直于径向），碎片沿此方向细长延伸
        float tx = -Mathf.Sin(rad);
        float ty = Mathf.Cos(rad);

        Color32 outline = front ? FrontOutline : BackOutline;
        Color32 body = front ? FrontBody : BackBody;

        float w = Mathf.Max(width, 1f);
        // 细长尖刺：长度远大于厚度，两端尖锐，黄白只出现在厚实的中间段
        float length = w * (3.2f + (float)random.NextDouble() * 1.8f);
        float thickness = w * (0.45f + (float)random.NextDouble() * 0.3f);

        int samples = 16;
        for (int s = 0; s <= samples; s++)
        {
            float tt = (s / (float)samples - 0.5f) * length;
            float profile = 1f - Mathf.Abs(tt) / (length * 0.5f);
            float r = thickness * 0.5f * profile;
            if (r < 0.4f)
                continue;

            int sx = Mathf.RoundToInt(cx + tx * tt);
            int sy = Mathf.RoundToInt(cy + ty * tt);
            FillDisc(pixels, sx, sy, r + 0.8f, outline);
            FillDisc(pixels, sx, sy, r, body);
            if (front)
            {
                if (profile > 0.35f)
                    FillDisc(pixels, sx, sy, r * 0.65f, FrontGold);
                if (profile > 0.7f)
                    FillDisc(pixels, sx, sy, r * 0.35f, FrontWhite);
            }
            else if (profile > 0.4f)
            {
                FillDisc(pixels, sx, sy, r * 0.6f, BackMid);
            }
        }
    }

    private static void DrawDirectionalFragments(Color32[] pixels, int frame)
    {
        var random = new System.Random(5100 + frame * 71);
        int count = frame < 2 ? 3 : frame == 2 ? 5 : 4;
        for (int i = 0; i < count; i++)
        {
            float angle = 35f + i * 97f + frame * 23f;
            float radius = 52f + (float)random.NextDouble() * 9f;
            float rad = angle * Mathf.Deg2Rad;
            Vector2 tangent = new Vector2(-Mathf.Sin(rad), Mathf.Cos(rad));
            Vector2 center = new Vector2(Center, Center)
                + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
            float length = 6f + (float)random.NextDouble() * 8f;
            float width = 2f + (float)random.NextDouble() * 2f;
            Vector2 p0 = center - tangent * length * 0.5f;
            Vector2 p1 = center + tangent * length * 0.5f;
            DrawRibbonSegment(pixels, p0, p1, tangent, tangent, width * 0.35f,
                width, true, false);
        }
    }

    private static void ClearCenterHole(Color32[] pixels)
    {
        float radiusSquared = CenterHoleRadius * CenterHoleRadius;
        for (int y = -TextureSize / 2; y <= TextureSize / 2; y++)
        {
            for (int x = -TextureSize / 2; x <= TextureSize / 2; x++)
            {
                if (x * x + y * y <= radiusSquared)
                    pixels[(Center + y) * TextureSize + Center + x] = default;
            }
        }
    }

    private static void FillDisc(Color32[] pixels, int centerX, int centerY, float radius, Color32 color)
    {
        int limit = Mathf.CeilToInt(radius);
        float radiusSquared = radius * radius;
        for (int y = -limit; y <= limit; y++)
        {
            for (int x = -limit; x <= limit; x++)
            {
                if (x * x + y * y > radiusSquared)
                    continue;
                SetPixel(pixels, centerX + x, centerY + y, color);
            }
        }
    }

    private static void FillRect(Color32[] pixels, int x, int y, int width, int height, Color32 color)
    {
        for (int row = 0; row < height; row++)
        {
            for (int column = 0; column < width; column++)
                SetPixel(pixels, x + column, y + row, color);
        }
    }

    private static void SetPixel(Color32[] pixels, int x, int y, Color32 color)
    {
        if (x < 0 || y < 0 || x >= TextureSize || y >= TextureSize)
            return;
        pixels[y * TextureSize + x] = color;
    }

    private static float Bezier(float a, float b, float c, float t)
    {
        float inverse = 1f - t;
        return inverse * inverse * a + 2f * inverse * t * b + t * t * c;
    }

    /// <summary>把前后景四帧导出为 PNG，供静态预览对照概念图。</summary>
    public static void GeneratePreviewPng(string directory)
    {
        EnsureFrames();
        Directory.CreateDirectory(directory);
        for (int i = 0; i < FrameCount; i++)
        {
            SaveSprite(_backFrames[i], Path.Combine(directory, $"pierce_vortex_back_{i}.png"));
            SaveSprite(_frontFrames[i], Path.Combine(directory, $"pierce_vortex_front_{i}.png"));
        }
    }

    private static void SaveSprite(Sprite sprite, string path)
    {
        if (sprite == null)
            return;
        File.WriteAllBytes(path, sprite.texture.EncodeToPNG());
    }
}
