using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
    /// PixelHitEffectManager 的**像素素材生成**：火花 / 斩击帧 / 疾病戳击帧的程序化贴图。
    ///
    /// 按机制从 <c>PixelHitEffectManager.cs</c> 拆出（2026-10-22，partial 分文件）。
    /// partial class 与 PixelHitEffectManager.cs **同类型**，行为完全一致。
    /// </summary>

public sealed partial class PixelHitEffectManager : MonoBehaviour
{
    private void CreatePixelSprite()
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = "PixelHitEffectTexture"
        };
        texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        texture.Apply(false, true);
        _pixelSprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), Vector2.one * 0.5f, 2f);
        _pixelSprite.name = "PixelHitEffectSprite";
    }

    private void CreateSparkSprites()
    {
        _sparkLongSprite = CreatePatternSprite("Long", new[]
        {
            "00100",
            "00110",
            "01100",
            "01000",
            "00000"
        });
        _sparkForkSprite = CreatePatternSprite("Fork", new[]
        {
            "00100",
            "01110",
            "00100",
            "01010",
            "00000"
        });
        _sparkChipSprite = CreatePatternSprite("Chip", new[]
        {
            "0110",
            "1100",
            "0100",
            "0000"
        });
    }

    private Sprite CreatePatternSprite(string name, string[] rows)
    {
        int height = rows.Length;
        int width = rows[0].Length;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = $"PixelHitEffect_{name}_Texture"
        };
        var pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            string row = rows[height - 1 - y];
            for (int x = 0; x < width; x++)
                pixels[y * width + x] = row[x] == '1' ? Color.white : Color.clear;
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        var sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0.5f), 2f);
        sprite.name = $"PixelHitEffect_{name}";
        return sprite;
    }
    private void CreateSlashSprites()
    {
        _slashFrames = new Sprite[SlashVariantCount * SlashFramesPerVariant];
        for (int variant = 0; variant < SlashVariantCount; variant++)
        {
            _slashFrames[variant * SlashFramesPerVariant] = CreateSlashFrameSprite(variant, 0);
            _slashFrames[variant * SlashFramesPerVariant + 1] = CreateSlashFrameSprite(variant, 1);
            _slashFrames[variant * SlashFramesPerVariant + 2] = CreateSlashFrameSprite(variant, 2);
        }
    }

    private Sprite CreateSlashFrameSprite(int variant, int frame)
    {
        const int size = 80;
        const float pixelsPerUnit = 30f;
        int center = size / 2;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = $"PixelHitEffect_Slash_{variant}_{frame}_Texture"
        };
        var pixels = new Color32[size * size];
        var random = new System.Random(49157 + variant * 6311);
        Color32 outline = new Color32(53, 16, 8, 255);
        Color32 darkRed = new Color32(145, 24, 8, 255);
        Color32 red = new Color32(213, 42, 8, 255);
        Color32 orange = new Color32(255, 101, 7, 255);
        Color32 yellow = new Color32(255, 211, 16, 255);
        Color32 white = new Color32(255, 253, 224, 255);
        float mainAngle = 34f + (float)(random.NextDouble() * 2f - 1f) * 3f;
        float[] mainDirections = { mainAngle, mainAngle + 180f };
        float[] mainLengths = { 1.02f + (float)random.NextDouble() * 0.08f, 0.92f + (float)random.NextDouble() * 0.12f };
        float[] shortDirections =
        {
            mainAngle + 66f + (float)(random.NextDouble() * 2f - 1f) * 9f,
            mainAngle + 104f + (float)(random.NextDouble() * 2f - 1f) * 10f,
            mainAngle + 238f + (float)(random.NextDouble() * 2f - 1f) * 9f,
            mainAngle + 286f + (float)(random.NextDouble() * 2f - 1f) * 10f,
            mainAngle + 150f + (float)(random.NextDouble() * 2f - 1f) * 8f
        };
        float[] shortLengths = { 0.45f, 0.32f, 0.38f, 0.25f, 0.3f };
        float expansion = frame == 0 ? 0.35f : frame == 1 ? 0.82f : 1f;
        float coreRadius = frame == 0 ? 3.1f : 5.8f * expansion;
        DrawSolidBurstCore(pixels, size, center, coreRadius + 1.6f, outline);
        DrawSolidBurstCore(pixels, size, center, coreRadius, frame == 0 ? yellow : orange);
        DrawSolidBurstCore(pixels, size, center, coreRadius * 0.72f, white);

        float[] allDirections = new float[7];
        float[] allLengths = new float[7];
        allDirections[0] = mainDirections[0];
        allDirections[1] = mainDirections[1];
        allLengths[0] = mainLengths[0];
        allLengths[1] = mainLengths[1];
        for (int i = 0; i < shortDirections.Length; i++)
        {
            allDirections[i + 2] = shortDirections[i];
            allLengths[i + 2] = shortLengths[i] * (0.94f + (float)random.NextDouble() * 0.12f);
        }

        float outer = 27f * expansion;
        DrawBurstLayer(pixels, size, center, allDirections, allLengths, 5f * expansion, outer, 3.7f, outline);
        DrawBurstLayer(pixels, size, center, allDirections, allLengths, 4.4f * expansion, outer * 0.91f, 3.1f, darkRed);
        DrawBurstLayer(pixels, size, center, allDirections, allLengths, 3.8f * expansion, outer * 0.82f, 2.7f, red);
        DrawBurstLayer(pixels, size, center, allDirections, allLengths, 3.1f * expansion, outer * 0.7f, 2.2f, orange);
        DrawBurstLayer(pixels, size, center, allDirections, allLengths, 2.5f * expansion, outer * 0.55f, 1.7f, yellow);
        DrawBurstLayer(pixels, size, center, mainDirections, new[] { mainLengths[0], mainLengths[1] },
            2.1f * expansion, outer * 0.48f, 1.25f, white);

        if (frame == 2)
            DrawSlashDebris(pixels, size, center, mainAngle, outline, darkRed, random);

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        _generatedTextures.Add(texture);
        var sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), Vector2.one * 0.5f, pixelsPerUnit);
        sprite.name = $"PixelHitEffect_Slash_{variant}_{frame}";
        return sprite;
    }

    private static void DrawSlashDebris(Color32[] pixels, int size, int center, float mainAngle,
        Color32 outline, Color32 darkRed, System.Random random)
    {
        for (int i = 0; i < 5; i++)
        {
            float directionAngle = mainAngle + (i % 2 == 0 ? 0f : 180f) + (float)(random.NextDouble() * 2f - 1f) * 22f;
            float radians = directionAngle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            Vector2 position = Vector2.one * center + direction * (29f + (float)random.NextDouble() * 5f);
            int length = 3 + random.Next(0, 4);
            int width = 1 + random.Next(0, 2);
            Color32 color = i % 2 == 0 ? outline : darkRed;
            for (int step = 0; step < length; step++)
            {
                Vector2 p = position + direction * step;
                for (int side = -width; side <= width; side++)
                    SetPixel(pixels, size, Mathf.RoundToInt(p.x + perpendicular.x * side), Mathf.RoundToInt(p.y + perpendicular.y * side), color);
            }
        }
    }

    private void CreateDiseaseStabSprites()
    {
        _diseaseStabFrames = new Sprite[DiseaseStabVariantCount * DiseaseStabFramesPerVariant];
        for (int variant = 0; variant < DiseaseStabVariantCount; variant++)
        {
            for (int frame = 0; frame < DiseaseStabFramesPerVariant; frame++)
                _diseaseStabFrames[variant * DiseaseStabFramesPerVariant + frame] = CreateDiseaseStabFrameSprite(variant, frame);
        }
    }

    private Sprite CreateDiseaseStabFrameSprite(int variant, int frame)
    {
        const int size = 80;
        const float pixelsPerUnit = 30f;
        int center = size / 2;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = $"PixelHitEffect_DiseaseStab_{variant}_{frame}_Texture"
        };
        var pixels = new Color32[size * size];
        var random = new System.Random(31871 + variant * 5779);
        Color32 outline = new Color32(28, 8, 49, 255);
        Color32 darkPurple = new Color32(67, 16, 112, 255);
        Color32 purple = new Color32(126, 37, 190, 255);
        Color32 brightPurple = new Color32(190, 75, 241, 255);
        Color32 palePurple = new Color32(231, 190, 255, 255);
        Color32 white = new Color32(255, 253, 240, 255);

        Vector2[] lobeOffsets;
        float[] lobeSizes;
        if (frame == 0)
        {
            lobeOffsets = new[]
            {
                new Vector2(-6f, 2f), new Vector2(-1f, 7f), new Vector2(7f, 4f),
                new Vector2(4f, -5f), new Vector2(-5f, -6f), new Vector2(1f, -1f)
            };
            lobeSizes = new[] { 8f, 6.5f, 9f, 5.5f, 7f, 4.5f };
        }
        else if (frame == 1)
        {
            lobeOffsets = new[]
            {
                new Vector2(-13f, 6f), new Vector2(-6f, 14f), new Vector2(6f, 12f),
                new Vector2(15f, 4f), new Vector2(11f, -7f), new Vector2(2f, -15f),
                new Vector2(-9f, -12f), new Vector2(-17f, -2f), new Vector2(1f, 2f),
                new Vector2(-3f, 7f), new Vector2(8f, -2f)
            };
            lobeSizes = new[] { 8.5f, 7f, 10.5f, 7f, 9f, 6.5f, 8f, 6.5f, 6f, 5f, 4.5f };
        }
        else
        {
            lobeOffsets = new[]
            {
                new Vector2(-12f, 4f), new Vector2(-5f, 12f), new Vector2(8f, 9f),
                new Vector2(14f, -1f), new Vector2(5f, -11f), new Vector2(-7f, -13f),
                new Vector2(-16f, -4f), new Vector2(-1f, 1f)
            };
            lobeSizes = new[] { 6.5f, 7f, 7.5f, 6f, 7f, 5.5f, 6f, 4f };
        }

        float scale = frame == 0 ? 0.76f : frame == 1 ? 1f : 0.84f;
        for (int i = 0; i < lobeOffsets.Length; i++)
        {
            float sizeFactor = lobeSizes[i] * scale * (0.86f + (float)random.NextDouble() * 0.22f);
            Vector2 offset = lobeOffsets[i] * scale;
            bool largeLobe = lobeSizes[i] >= 7f;
            if (largeLobe)
                DrawDiseaseBlob(pixels, size, center, offset, sizeFactor + 0.9f, sizeFactor * 0.82f + 0.9f, 0.28f, purple, 17 + variant * 31 + i);
            DrawDiseaseBlob(pixels, size, center, offset + new Vector2(1f, 1f), sizeFactor * 0.68f, sizeFactor * 0.54f, 0.3f, brightPurple, 117 + variant * 31 + i);
        }

        int smallBlobCount = frame == 1 ? 18 : 11;
        for (int i = 0; i < smallBlobCount; i++)
        {
            float angle = (i * 151f + variant * 41f) * Mathf.Deg2Rad;
            float distance = (frame == 1 ? 11f : 9f) + (float)random.NextDouble() * 13f;
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance * scale;
            float blobSize = (1.8f + (float)random.NextDouble() * 3.4f) * scale;
            DrawDiseaseBlob(pixels, size, center, offset, blobSize, blobSize * 0.68f, 0.34f,
                random.Next(2) == 0 ? purple : brightPurple, 217 + variant * 17 + i);
        }

        float coreScale = frame == 0 ? 0.66f : frame == 1 ? 1.1f : 0.86f;
        DrawDiseasePixelCloud(pixels, size, center, new Vector2(0f, 0f), 16f * coreScale, 13f * coreScale, brightPurple, 401 + variant);
        DrawDiseasePixelCloud(pixels, size, center, new Vector2(-1f, 1f), 14.8f * coreScale, 12f * coreScale, palePurple, 421 + variant);
        DrawDiseasePixelCloud(pixels, size, center, new Vector2(-1f, 1f), 12.8f * coreScale, 10.4f * coreScale, white, 441 + variant);
        DrawDiseaseGrain(pixels, size, center, lobeOffsets, lobeSizes, scale, frame, variant, darkPurple, purple, brightPurple, palePurple);

        if (frame == 2)
        {
            DrawPixelDisc(pixels, size, center - 19, center + 14, 2.5f, darkPurple);
            DrawPixelDisc(pixels, size, center + 19, center + 8, 2f, purple);
            DrawPixelDisc(pixels, size, center + 13, center - 18, 2.5f, darkPurple);
            DrawPixelDisc(pixels, size, center - 5, center + 22, 1.5f, brightPurple);
            DrawPixelDisc(pixels, size, center + 22, center - 5, 1.5f, purple);
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        _generatedTextures.Add(texture);
        var sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), Vector2.one * 0.5f, pixelsPerUnit);
        sprite.name = $"PixelHitEffect_DiseaseStab_{variant}_{frame}";
        return sprite;
    }

    private static void DrawDiseasePixelCloud(Color32[] pixels, int size, int center, Vector2 offset,
        float radiusX, float radiusY, Color32 color, int seed)
    {
        int minX = Mathf.FloorToInt(center + offset.x - radiusX);
        int maxX = Mathf.CeilToInt(center + offset.x + radiusX);
        int minY = Mathf.FloorToInt(center + offset.y - radiusY);
        int maxY = Mathf.CeilToInt(center + offset.y + radiusY);
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float nx = (x - center - offset.x) / Mathf.Max(radiusX, 0.01f);
                float ny = (y - center - offset.y) / Mathf.Max(radiusY, 0.01f);
                float steppedX = Mathf.Round(nx * 4f) / 4f;
                float steppedY = Mathf.Round(ny * 4f) / 4f;
                float grain = Mathf.Sin((x + seed) * 1.37f) * Mathf.Cos((y - seed) * 1.11f) * 0.1f;
                if (steppedX * steppedX + steppedY * steppedY <= 1f + grain)
                    SetPixel(pixels, size, x, y, color);
            }
        }
    }
    private static void DrawDiseaseBlob(Color32[] pixels, int size, int center, Vector2 offset,
        float radiusX, float radiusY, float irregularity, Color32 color, int seed)
    {
        int minX = Mathf.FloorToInt(center + offset.x - radiusX - 2f);
        int maxX = Mathf.CeilToInt(center + offset.x + radiusX + 2f);
        int minY = Mathf.FloorToInt(center + offset.y - radiusY - 2f);
        int maxY = Mathf.CeilToInt(center + offset.y + radiusY + 2f);
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float nx = (x - center - offset.x) / Mathf.Max(radiusX, 0.01f);
                float ny = (y - center - offset.y) / Mathf.Max(radiusY, 0.01f);
                float wave = Mathf.Sin((x + seed) * 0.72f) * Mathf.Cos((y - seed) * 0.61f) * irregularity;
                if (nx * nx + ny * ny <= 1f + wave)
                    SetPixel(pixels, size, x, y, color);
            }
        }
    }

    private static void DrawDiseaseGrain(Color32[] pixels, int size, int center, Vector2[] lobeOffsets,
        float[] lobeSizes, float scale, int frame, int variant, Color32 darkPurple,
        Color32 purple, Color32 brightPurple, Color32 palePurple)
    {
        var random = new System.Random(8123 + variant * 997 + frame * 131);
        int count = frame == 1 ? 44 : 30;
        for (int i = 0; i < count; i++)
        {
            int lobe = random.Next(lobeOffsets.Length);
            Vector2 basePosition = lobeOffsets[lobe] * scale;
            Vector2 jitter = new Vector2(
                ((float)random.NextDouble() * 2f - 1f) * lobeSizes[lobe] * scale * 0.62f,
                ((float)random.NextDouble() * 2f - 1f) * lobeSizes[lobe] * scale * 0.62f);
            int x = Mathf.RoundToInt(center + basePosition.x + jitter.x);
            int y = Mathf.RoundToInt(center + basePosition.y + jitter.y);
            float distance = Vector2.Distance(new Vector2(center, center), new Vector2(x, y));
            if (distance < 7f * scale || distance > 25f * scale)
                continue;
            int grainSize = random.Next(1, frame == 1 ? 4 : 3);
            Color32 color = random.Next(5) switch
            {
                0 => darkPurple,
                1 => purple,
                2 => purple,
                3 => brightPurple,
                _ => palePurple
            };
            DrawPixelBlock(pixels, size, x, y, grainSize, color);
        }
    }

    private static void DrawPixelBlock(Color32[] pixels, int size, int centerX, int centerY,
        int blockSize, Color32 color)
    {
        for (int y = 0; y < blockSize; y++)
        {
            for (int x = 0; x < blockSize; x++)
            {
                if ((x + y) % 3 != 2 || blockSize == 1)
                    SetPixel(pixels, size, centerX + x, centerY + y, color);
            }
        }
    }
    private static void DrawSolidBurstCoreAt(Color32[] pixels, int size, Vector2 center, float radius, Color32 color)
    {
        int limit = Mathf.CeilToInt(radius);
        for (int y = -limit; y <= limit; y++)
        {
            for (int x = -limit; x <= limit; x++)
            {
                if (x * x + y * y <= radius * radius)
                    SetPixel(pixels, size, Mathf.RoundToInt(center.x + x), Mathf.RoundToInt(center.y + y), color);
            }
        }
    }

    private void BuildDiseaseStabEffect(EffectInstance instance, bool heavy)
    {
        int variant = _diseaseStabVariantIndex++ % DiseaseStabVariantCount;
        int frameBase = variant * DiseaseStabFramesPerVariant;
        float duration = heavy ? 0.26f : 0.21f;
        float peakScale = (heavy ? 2.65f : 2.35f) * StabImpactScale
            * (0.98f + (variant % 3) * 0.015f);
        float contactEnd = duration * 0.18f;
        float burstEnd = duration * 0.58f;
        float holdEnd = duration * 0.7f;

        instance.center.enabled = true;
        instance.center.sprite = _diseaseStabFrames[frameBase];
        instance.center.flipX = false;
        instance.center.color = Color.white;
        instance.center.transform.localPosition = Vector3.zero;
        instance.center.transform.localRotation = Quaternion.identity;
        instance.center.transform.localScale = Vector3.one * (peakScale * 0.38f);
        instance.sequence.Insert(0f, instance.center.transform.DOScale(Vector3.one * (peakScale * 0.62f), contactEnd).SetEase(Ease.OutCubic));
        instance.sequence.InsertCallback(contactEnd, () =>
        {
            instance.center.sprite = _diseaseStabFrames[frameBase + 1];
            instance.center.transform.localScale = Vector3.one * (peakScale * 0.7f);
        });
        instance.sequence.Insert(contactEnd, instance.center.transform.DOScale(Vector3.one * peakScale, burstEnd - contactEnd).SetEase(Ease.OutBack, 1.08f));
        instance.sequence.InsertCallback(burstEnd, () =>
        {
            instance.center.sprite = _diseaseStabFrames[frameBase + 2];
            instance.center.transform.localScale = Vector3.one * peakScale;
        });
        instance.sequence.Insert(holdEnd, instance.center.transform.DOScale(Vector3.one * (peakScale * 0.82f), duration - holdEnd).SetEase(Ease.InQuad));
        instance.sequence.InsertCallback(duration, () => instance.center.enabled = false);
        for (int i = 0; i < instance.rays.Length; i++)
            instance.rays[i].enabled = false;
    }

}