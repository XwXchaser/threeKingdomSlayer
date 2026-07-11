using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public class UIReadyVerticalPulse : BaseMeshEffect
{
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField, Range(0f, 1f)] private float bottomMinAlpha = 0.8f;
    [SerializeField, Range(0f, 1f)] private float topMinAlpha = 0.25f;

    private bool _playing;

    public void SetPlaying(bool playing)
    {
        _playing = playing;
        graphic?.SetVerticesDirty();
    }

    private void Update()
    {
        if (_playing)
            graphic.SetVerticesDirty();
    }

    public override void ModifyMesh(VertexHelper vertexHelper)
    {
        if (!IsActive() || !_playing)
            return;

        float pulse = Mathf.Sin(Time.unscaledTime * pulseSpeed * Mathf.PI * 2f) * 0.5f + 0.5f;
        Rect rect = graphic.rectTransform.rect;
        float height = Mathf.Max(rect.height, 0.001f);
        UIVertex vertex = default;

        for (int i = 0; i < vertexHelper.currentVertCount; i++)
        {
            vertexHelper.PopulateUIVertex(ref vertex, i);
            float vertical = Mathf.Clamp01((vertex.position.y - rect.yMin) / height);
            float minimumAlpha = Mathf.Lerp(bottomMinAlpha, topMinAlpha, vertical);
            float alphaMultiplier = Mathf.Lerp(minimumAlpha, 1f, pulse);
            Color32 color = vertex.color;
            color.a = (byte)Mathf.RoundToInt(color.a * alphaMultiplier);
            vertex.color = color;
            vertexHelper.SetUIVertex(vertex, i);
        }
    }
}
