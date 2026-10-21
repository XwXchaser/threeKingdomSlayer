using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;


/// <summary>
/// StageProgressBar 的**编辑期预览**：程序化纹理与编辑器下的预览构建/清理。
///
/// 按机制从 <c>StageProgressBar.cs</c> 拆出（2026-10-22，partial 分文件）。
/// partial class 与 StageProgressBar.cs **同类型**，行为完全一致，只为降低单文件体量与阅读成本。
/// </summary>

public partial class StageProgressBar : MonoBehaviour
{
    private static void EnsureTextures()
    {
        if (_circleTex == null)
        {
            int size = 64;
            _circleTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            _circleTex.wrapMode = TextureWrapMode.Clamp;
            _circleTex.filterMode = FilterMode.Bilinear;
            var pixels = new Color[size * size];
            float center = size * 0.5f;
            float radius = size * 0.5f - 1f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = 1f - Mathf.Clamp01((dist - radius + 1f) / 2f);
                    pixels[y * size + x] = new Color(1, 1, 1, alpha);
                }
            }
            _circleTex.SetPixels(pixels);
            _circleTex.Apply();
        }

        if (_whiteTex == null)
        {
            _whiteTex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            _whiteTex.wrapMode = TextureWrapMode.Clamp;
            _whiteTex.filterMode = FilterMode.Bilinear;
            var px = new Color[16];
            for (int i = 0; i < 16; i++) px[i] = Color.white;
            _whiteTex.SetPixels(px);
            _whiteTex.Apply();
        }
    }

    // 编辑模式预览脏标记（非 EDITOR 也声明，避免 OnEnable 引用失败）
    private bool _editModePreviewDirty;

#if UNITY_EDITOR
    private static readonly HideFlags PreviewFlags = HideFlags.DontSave | HideFlags.NotEditable;

    private void Update()
    {
        if (Application.isPlaying) return;
        if (_editModePreviewDirty && gameObject.activeInHierarchy)
        {
            _editModePreviewDirty = false;
            RebuildEditModePreview();
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying) return;
        if (!gameObject.activeInHierarchy) return;
        // 参数变化 → 延迟重建预览（避免 serialization 冲突）
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null && !Application.isPlaying && gameObject.activeInHierarchy)
                RebuildEditModePreview();
        };
    }

    private void ScheduleDestroyEditModePreview()
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null && !Application.isPlaying)
                DestroyEditModePreview();
        };
    }

    private void DestroyEditModePreview()
    {
        // 清理临时 RectMask2D
        var masks = GetComponents<RectMask2D>();
        foreach (var m in masks)
            if ((m.hideFlags & PreviewFlags) != 0) DestroyImmediate(m);

        // 清理 Content 及其子节点，以及 Frame（仅 DontSave 标记的临时对象）
        if (_contentRect != null && (_contentRect.hideFlags & HideFlags.DontSave) != 0)
        {
            DestroyImmediate(_contentRect.gameObject);
            _contentRect = null;
        }
        else if (_contentRect != null)
        {
            for (int i = _contentRect.childCount - 1; i >= 0; i--)
            {
                var child = _contentRect.GetChild(i);
                if ((child.hideFlags & PreviewFlags) != 0)
                    DestroyImmediate(child.gameObject);
            }
        }

        if (_frameImage != null && (_frameImage.hideFlags & HideFlags.DontSave) != 0)
        {
            DestroyImmediate(_frameImage.gameObject);
            _frameImage = null;
        }

        _lineImage = null;
        _playerDotImage = null;
        _playerDotRect = null;
        _nodeDots.Clear();
    }

    private void RebuildEditModePreview()
    {
        if (this == null || gameObject == null) return;

        DestroyEditModePreview();
        if (this == null || gameObject == null) return;
        EnsureTextures();

        var mask = GetComponent<RectMask2D>();
        if (mask == null)
        {
            mask = gameObject.AddComponent<RectMask2D>();
            mask.hideFlags = PreviewFlags;
        }

        BuildVisuals();

        // 仅标记 Content 的子节点为不可编辑，保留 Frame/Content 可选中调整位置
        if (_contentRect != null)
            MarkChildrenOnly(_contentRect, PreviewFlags);

        // 预览节点与连接路段
        if (_contentRect != null)
        {
            for (int i = 0; i < visibleDots - 1; i++)
            {
                var start = GetNodePosition(i);
                var end = GetNodePosition(i + 1);
                var segment = CreateSegmentImage($"UncompletedSegment_{i}", lineSprite, lineColor, start, end);
                segment.gameObject.hideFlags = PreviewFlags;
            }

            for (int i = 0; i < visibleDots; i++)
            {
                var dotGo = CreateNodeDot(i);
                dotGo.hideFlags = PreviewFlags;
            }
        }

        if (_playerDotRect != null) _playerDotRect.SetAsLastSibling();
    }

    private static void MarkSubtree(Transform root, HideFlags flags)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            child.gameObject.hideFlags = flags;
            MarkSubtree(child, flags);
        }
    }

    private static void MarkChildrenOnly(Transform parent, HideFlags flags)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            child.gameObject.hideFlags = flags;
            MarkSubtree(child, flags);
        }
    }
#endif
}