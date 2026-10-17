using UnityEngine;

/// <summary>Explicit mesh weapon rendering references, independent of sprite frames.</summary>
[DisallowMultipleComponent]
public sealed class MeshWeaponVisual : MonoBehaviour
{
    [SerializeField] private Renderer[] renderers;
    [SerializeField] private Material fadeMaterial;
    [SerializeField] private Transform modelRoot;
    [SerializeField] private Transform tipAnchor;
    [SerializeField] private Transform tailAnchor;
    [SerializeField] private WeaponTasselDynamics dynamics;
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private Material[][] _opaqueMaterials;
    private Material[][] _fadeMaterials;
    private MaterialPropertyBlock _properties;
    private bool _fading;
    private bool _initialized;
    private float _alpha = 1f;
    private Color _tint = Color.white;

    public Renderer[] Renderers => renderers;
    public Transform ModelRoot => modelRoot;
    public Vector3 TipPosition => tipAnchor != null ? tipAnchor.position : transform.position;
    public Vector3 TailPosition => tailAnchor != null ? tailAnchor.position
        : TipPosition - transform.up * Length;
    public float Length => dynamics != null ? dynamics.WeaponLength : 0f;
    public float Alpha => _alpha;

    private void Initialize()
    {
        if (_initialized) return;
        _properties = new MaterialPropertyBlock();
        int count = renderers != null ? renderers.Length : 0;
        _opaqueMaterials = new Material[count][];
        _fadeMaterials = new Material[count][];
        for (int i = 0; i < count; i++)
        {
            if (renderers[i] == null) continue;
            _opaqueMaterials[i] = renderers[i].sharedMaterials;
            _fadeMaterials[i] = new Material[_opaqueMaterials[i].Length];
            for (int j = 0; j < _fadeMaterials[i].Length; j++)
                _fadeMaterials[i][j] = fadeMaterial != null ? fadeMaterial : _opaqueMaterials[i][j];
        }
        _initialized = true;
    }

    public void SetAlpha(float alpha)
    {
        Initialize();
        _alpha = Mathf.Clamp01(alpha);
        bool fading = _alpha < 0.999f;
        for (int i = 0; i < _opaqueMaterials.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null) continue;
            if (_fading != fading) r.sharedMaterials = fading ? _fadeMaterials[i] : _opaqueMaterials[i];
            r.GetPropertyBlock(_properties);
            Color color = _tint;
            color.a = _alpha;
            _properties.SetColor(ColorId, color);
            r.SetPropertyBlock(_properties);
        }
        _fading = fading;
    }

    public void SetTint(Color color)
    {
        _tint = color;
        SetAlpha(_alpha);
    }

    public void SetSorting(int order)
    {
        if (renderers == null) return;
        foreach (Renderer r in renderers)
            if (r != null) { r.sortingLayerName = "Default"; r.sortingOrder = order; }
    }
}
