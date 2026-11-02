using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 程序化 UI 贴图（美术图没到位时的临时"垫底"件）。
///
/// 为什么这么做：
///   · 参考图那套造型（毛毡/金属描边面板、页签、页码点）不需要复杂美术，圆角矩形 + 描边 + 微渐变就能撑住；
///   · 项目已有先例（PixelHitEffectManager 也是运行时生成 Texture2D）；
///   · 生成出来的 sprite 全部走 SerializedField 槽位，美术做好图直接拖进去覆盖，**代码不用改**。
///
/// 约定：
///   · 一律生成**白色/灰阶**贴图，颜色靠 Image.color 染色 → 一套图能复用到所有面板；
///   · 圆角矩形带九宫格边框（Sprite.Create 的 border），拉伸不糊角；
///   · 全部静态缓存，只在第一次用时生成一次。
/// </summary>
public static class ProceduralUiSprites
{
    private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

    /// <summary>
    /// 圆角矩形底板（白色，可染色）。fillAlpha &lt; 1 时是半透明底，borderAlpha=1 画出描边。
    /// 返回的 sprite 已带九宫格边框，Image.type 设 Sliced 使用。
    /// </summary>
    public static Sprite RoundedPlate(int radius = 22, int border = 3, float fillAlpha = 0.85f, float borderAlpha = 1f)
    {
        string key = $"plate_{radius}_{border}_{fillAlpha:0.00}_{borderAlpha:0.00}";
        if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

        int size = Mathf.Clamp(Mathf.Max(128, radius * 2 + 16), 32, 256);   // 纹理够大，九宫格中间区域才不会被拉伸模糊
        var tex = NewTexture(size, size, key);
        var px = new Color32[size * size];
        float r = radius;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = RoundedRectDistance(x + 0.5f, y + 0.5f, size, size, r);
                float a;
                if (d > 0.5f) a = 0f;                                  // 圆角外
                else if (d > 0.5f - border) a = borderAlpha;            // 描边
                else a = fillAlpha;                                     // 内部
                px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f));
            }
        }
        tex.SetPixels32(px);
        tex.Apply();

        int b = Mathf.Clamp(radius + 2, 1, size / 2 - 1);
        var sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                                   SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        sprite.name = key;
        Cache[key] = sprite;
        return sprite;
    }

    /// <summary>实心圆点（页码指示用），白色可染色。</summary>
    public static Sprite Dot(int diameter = 24, float alpha = 1f)
    {
        string key = $"dot_{diameter}_{alpha:0.00}";
        if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

        int size = Mathf.Clamp(diameter, 8, 128);
        var tex = NewTexture(size, size, key);
        var px = new Color32[size * size];
        float cx = (size - 1) * 0.5f;
        float outer = size * 0.5f - 1f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cx) * (y - cx));
                float a = d > outer ? 0f : 1f;
                px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f * Mathf.Clamp01(alpha)));
            }
        }
        tex.SetPixels32(px);
        tex.Apply();

        var sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = key;
        Cache[key] = sprite;
        return sprite;
    }

    /// <summary>圆形节点外框（白色环，可染色）——没有任何贴图时给节点当兜底。</summary>
    public static Sprite Ring(int diameter = 128, int thickness = 6, float alpha = 1f)
    {
        string key = $"ring_{diameter}_{thickness}_{alpha:0.00}";
        if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

        int size = Mathf.Clamp(diameter, 16, 256);
        var tex = NewTexture(size, size, key);
        var px = new Color32[size * size];
        float c = (size - 1) * 0.5f;
        float outer = size * 0.5f - 1f;
        float inner = outer - thickness;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                float a = d > outer ? 0f : d > inner ? 1f : 0.92f;
                px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f * Mathf.Clamp01(alpha)));
            }
        }
        tex.SetPixels32(px);
        tex.Apply();

        var sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = key;
        Cache[key] = sprite;
        return sprite;
    }

    /// <summary>
    /// 节点外框（照着刀锋那套实测参数做的“平面版”）：
    ///   · 单色描边，**无渐变、无浮雕、无高光**（Blades 的框 alpha 只有 0/255 两极）；
    ///   · 上下边比左右边厚一倍（实测左右 33–36px / 上下 56–59px）；
    ///   · 框内是平面深色，不放内沿亮线。
    /// 三态只靠描边颜色区分：锁定=冷灰 / 可解锁=暖铜 / 已解锁=亮铜。
    /// </summary>
    public static Sprite NodeFrame(int size, bool unlocked, bool available)
    {
        string key = $"nodeframe_flat_{size}_{unlocked}_{available}";
        if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

        int s = Mathf.Clamp(size, 32, 256);
        float radius = s * 0.08f;
        // 刀的实测：左右边 33–36px / 上下边 56–59px（对 256 的框）= 13% / 22%
        float side = Mathf.Max(3f, s * 0.130f);
        float topBottom = Mathf.Max(4f, s * 0.220f);

        Color frame = unlocked ? new Color(0.91f, 0.78f, 0.56f)
                    : available ? new Color(0.85f, 0.65f, 0.45f)
                    : new Color(0.42f, 0.40f, 0.38f);
        Color inner = new Color(0.11f, 0.10f, 0.09f);

        var tex = NewTexture(s, s, key);
        var px = new Color32[s * s];
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float d = -RoundedRectDistance(x + 0.5f, y + 0.5f, s, s, radius);
                if (d <= 0f) { px[y * s + x] = new Color(0f, 0f, 0f, 0f); continue; }

                float dx = Mathf.Min(x + 0.5f, s - x - 0.5f);
                float dy = Mathf.Min(y + 0.5f, s - y - 0.5f);
                bool isBorder = dy <= topBottom || dx <= side;
                px[y * s + x] = isBorder ? frame : inner;
            }
        }
        tex.SetPixels32(px);
        tex.Apply();

        var sprite = Sprite.Create(tex, new Rect(0f, 0f, s, s), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = key;
        Cache[key] = sprite;
        return sprite;
    }

    /// <summary>点到圆角矩形的有符号距离（&lt;0 在内部）。</summary>
    private static float RoundedRectDistance(float x, float y, float w, float h, float radius)
    {
        float halfW = w * 0.5f;
        float halfH = h * 0.5f;
        float px = Mathf.Abs(x - halfW) - (halfW - radius);
        float py = Mathf.Abs(y - halfH) - (halfH - radius);
        float outside = Mathf.Sqrt(Mathf.Max(px, 0f) * Mathf.Max(px, 0f) + Mathf.Max(py, 0f) * Mathf.Max(py, 0f));
        float inside = Mathf.Min(Mathf.Max(px, py), 0f);
        return outside + inside - radius;
    }

    private static Texture2D NewTexture(int w, int h, string name)
    {
        return new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            name = "ProcUi_" + name
        };
    }
}
