using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
    /// PixelHitEffectManager 的**像素绘制基元与戳击素材**：像素块 / 射线 / 圆盘等绘制原语与戳击帧生成。
    ///
    /// 按机制从 <c>PixelHitEffectManager.cs</c> 拆出（2026-10-22，partial 分文件）。
    /// partial class 与 PixelHitEffectManager.cs **同类型**，行为完全一致。
    /// </summary>

public sealed partial class PixelHitEffectManager : MonoBehaviour
{
    private void CreateStabSprites()
    {
        _stabFrames = new Sprite[StabVariantCount * StabFramesPerVariant];
        for (int variant = 0; variant < StabVariantCount; variant++)
        {
            _stabFrames[variant * StabFramesPerVariant] = CreateStabFrameSprite(variant, 0);
            _stabFrames[variant * StabFramesPerVariant + 1] = CreateStabFrameSprite(variant, 1);
            _stabFrames[variant * StabFramesPerVariant + 2] = CreateStabFrameSprite(variant, 2);
        }
    }

    private Sprite CreateStabFrameSprite(int variant, int frame)
    {
        const int size = 80;
        const float pixelsPerUnit = 30f;
        int center = size / 2;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = $"PixelHitEffect_Stab_{variant}_{frame}_Texture"
        };
        var pixels = new Color32[size * size];
        var random = new System.Random(92821 + variant * 7919);
        Color32 outline = new Color32(53, 16, 8, 255);
        Color32 darkRed = new Color32(145, 24, 8, 255);
        Color32 red = new Color32(213, 42, 8, 255);
        Color32 orange = new Color32(255, 101, 7, 255);
        Color32 yellow = new Color32(255, 211, 16, 255);
        Color32 white = new Color32(255, 253, 224, 255);

        float[] directions =
        {
            12f, 52f, 96f, 151f, 198f, 242f, 292f, 338f
        };
        float[] baseLengths =
        {
            0.82f, 0.48f, 0.3f, 0.68f, 0.78f, 0.34f, 0.5f, 0.28f
        };
        var lengths = new float[directions.Length];
        for (int i = 0; i < directions.Length; i++)
        {
            directions[i] += ((float)random.NextDouble() * 2f - 1f) * 4.5f;
            lengths[i] = baseLengths[i] * (0.94f + (float)random.NextDouble() * 0.12f);
        }

        if (frame == 0)
        {
            DrawSolidBurstCore(pixels, size, center, 3.4f, outline);
            DrawSolidBurstCore(pixels, size, center, 2.2f, white);
            DrawBurstLayer(pixels, size, center, directions, lengths, 2.4f, 12f, 3.2f, outline);
            DrawBurstLayer(pixels, size, center, directions, lengths, 1.8f, 8.2f, 2.4f, white);
        }
        else
        {
            float expansion = frame == 1 ? 0.8f : 1f;
            DrawSolidBurstCore(pixels, size, center, 7.4f * expansion, outline);
            DrawSolidBurstCore(pixels, size, center, 6.2f * expansion, darkRed);
            DrawSolidBurstCore(pixels, size, center, 5.1f * expansion, orange);
            DrawSolidBurstCore(pixels, size, center, 4f * expansion, yellow);
            DrawSolidBurstCore(pixels, size, center, 2.9f * expansion, white);
            DrawBurstLayer(pixels, size, center, directions, lengths,
                6.8f * expansion, 22f * expansion, 3.5f, outline);
            DrawBurstLayer(pixels, size, center, directions, lengths,
                5.9f * expansion, 19.5f * expansion, 2.8f, darkRed);
            DrawBurstLayer(pixels, size, center, directions, lengths,
                5.1f * expansion, 17f * expansion, 2.35f, red);
            DrawBurstLayer(pixels, size, center, directions, lengths,
                4.3f * expansion, 14f * expansion, 1.95f, orange);
            DrawBurstLayer(pixels, size, center, directions, lengths,
                3.6f * expansion, 11.5f * expansion, 1.55f, yellow);
            DrawBurstLayer(pixels, size, center, directions, lengths,
                2.9f * expansion, 8.2f * expansion, 1.15f, white);
        }

        if (frame == 2)
            DrawStabDebris(pixels, size, center, directions, outline, darkRed, random);

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        _generatedTextures.Add(texture);
        var sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), Vector2.one * 0.5f, pixelsPerUnit);
        sprite.name = $"PixelHitEffect_Stab_{variant}_{frame}";
        return sprite;
    }

    private static void DrawStabDebris(Color32[] pixels, int size, int center, float[] directions,
        Color32 outline, Color32 darkRed, System.Random random)
    {
        for (int i = 0; i < 5; i++)
        {
            int directionIndex = (i * 3 + 1) % directions.Length;
            float angle = (directions[directionIndex] + 10f) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            Vector2 position = Vector2.one * center + direction * (27f + (float)random.NextDouble() * 4f);
            int length = 3 + random.Next(0, 4);
            int width = 1 + random.Next(0, 2);
            Color32 color = i % 2 == 0 ? outline : darkRed;
            for (int step = 0; step < length; step++)
            {
                Vector2 p = position + direction * step;
                for (int side = -width; side <= width; side++)
                    SetPixel(pixels, size, Mathf.RoundToInt(p.x + perpendicular.x * side),
                        Mathf.RoundToInt(p.y + perpendicular.y * side), color);
            }
        }
    }

    private static void DrawSolidBurstCore(Color32[] pixels, int size, int center, float radius, Color32 color)
    {
        int limit = Mathf.CeilToInt(radius);
        for (int y = -limit; y <= limit; y++)
        {
            for (int x = -limit; x <= limit; x++)
            {
                float normalizedX = Mathf.Abs(x) / Mathf.Max(radius, 0.01f);
                float normalizedY = Mathf.Abs(y) / Mathf.Max(radius, 0.01f);
                float diamond = normalizedX + normalizedY;
                float square = Mathf.Max(normalizedX, normalizedY);
                if (diamond <= 1.28f && square <= 1f)
                    SetPixel(pixels, size, center + x, center + y, color);
            }
        }
    }

    private static void DrawDiseaseCloudLayer(Color32[] pixels, int size, int center,
        float[] lobeAngles, float[] lobeRadii, float radius, Color32 color)
    {
        int limit = Mathf.CeilToInt(radius + 1.5f);
        float radiusSq = (radius + 1.5f) * (radius + 1.5f);
        int lobeCount = lobeAngles.Length;
        for (int y = -limit; y <= limit; y++)
        {
            for (int x = -limit; x <= limit; x++)
            {
                float distSq = x * x + y * y;
                if (distSq > radiusSq)
                    continue;
                float dist = Mathf.Sqrt(distSq);
                float angle;
                if (dist < 0.5f)
                {
                    SetPixel(pixels, size, center + x, center + y, color);
                    continue;
                }
                angle = Mathf.Atan2(y, x) * Mathf.Rad2Deg;
                if (angle < 0f) angle += 360f;

                int next = 0;
                for (int i = 0; i < lobeCount; i++)
                {
                    if (lobeAngles[i] >= angle) { next = i; break; }
                }
                int prev = next == 0 ? lobeCount - 1 : next - 1;
                float anglePrev = lobeAngles[prev];
                float angleNext = lobeAngles[next];
                if (next == 0 && angle < lobeAngles[0])
                {
                    prev = lobeCount - 1;
                    anglePrev = lobeAngles[prev] - 360f;
                }
                if (prev == lobeCount - 1 && angle >= lobeAngles[lobeCount - 1])
                {
                    next = 0;
                    angleNext = lobeAngles[0] + 360f;
                }
                float t = (angle - anglePrev) / (angleNext - anglePrev);
                float boundary = Mathf.Lerp(lobeRadii[prev], lobeRadii[next], t) * radius;
                if (dist <= boundary)
                    SetPixel(pixels, size, center + x, center + y, color);
            }
        }
    }

    private static void DrawBurstLayer(Color32[] pixels, int size, int center, float[] directions,
        float[] lengths, float innerRadius, float outerRadius, float width, Color32 color)
    {
        for (int i = 0; i < directions.Length; i++)
        {
            float radians = directions[i] * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            float length = Mathf.Lerp(innerRadius, outerRadius, lengths[i]);
            int steps = Mathf.CeilToInt(length - innerRadius);
            for (int step = 0; step <= steps; step++)
            {
                float distance = innerRadius + step;
                float taper = 1f - Mathf.Clamp01((distance - innerRadius) / Mathf.Max(1f, length - innerRadius));
                float halfWidth = Mathf.Max(0.5f, width * taper);
                int sideLimit = Mathf.CeilToInt(halfWidth);
                for (int side = -sideLimit; side <= sideLimit; side++)
                {
                    if (Mathf.Abs(side) > halfWidth)
                        continue;
                    Vector2 position = Vector2.one * center + direction * distance + perpendicular * side;
                    SetPixel(pixels, size, Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y), color);
                }
            }
        }
    }

    private static void DrawRadialLayer(Color32[] pixels, int size, int center, float[] directions,
        float[] lengths, float maxLength, float baseHalfWidth, Color32 color)
    {
        for (int i = 0; i < directions.Length; i++)
        {
            float radians = directions[i] * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            float length = maxLength * lengths[i];
            float start = i % 2 == 0 ? 4f : 6f;
            int steps = Mathf.CeilToInt(length - start);
            for (int step = 0; step <= steps; step++)
            {
                float distance = start + step;
                float normalized = Mathf.Clamp01(distance / length);
                float halfWidth = Mathf.Max(0.55f, baseHalfWidth * Mathf.Pow(1f - normalized, 0.72f));
                int widthSteps = Mathf.CeilToInt(halfWidth);
                for (int side = -widthSteps; side <= widthSteps; side++)
                {
                    if (Mathf.Abs(side) > halfWidth)
                        continue;
                    Vector2 position = Vector2.one * center + direction * distance + perpendicular * side;
                    SetPixel(pixels, size, Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y), color);
                }
            }
        }
    }

    private static void DrawPixelDisc(Color32[] pixels, int size, int centerX, int centerY,
        float radius, Color32 color)
    {
        int limit = Mathf.CeilToInt(radius);
        float radiusSquared = radius * radius;
        for (int y = -limit; y <= limit; y++)
        {
            for (int x = -limit; x <= limit; x++)
            {
                if (x * x + y * y <= radiusSquared)
                    SetPixel(pixels, size, centerX + x, centerY + y, color);
            }
        }
    }

    private static void SetPixel(Color32[] pixels, int size, int x, int y, Color32 color)
    {
        if (x < 0 || x >= size || y < 0 || y >= size)
            return;
        pixels[y * size + x] = color;
    }
}