using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 教学压暗：只压暗「场景」（路线场景里的背景、地形、植被、墙体），
/// 玩家、长枪与敌人保持原亮度 —— 让玩家看清「自己的武器 → 面前的敌人」这一组对应关系。
///
/// 为什么不用全屏 UI 遮罩：战场是透视相机 + 世界物件（长枪是 MeshRenderer），
/// 任何 Overlay 画布都必然盖在角色上面，层级无解。改成给场景渲染器乘一个亮度系数，
/// 既不挡角色，也不需要动相机/排序层。
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(1000)]   // 必须在 Y 场景脚本（LateUpdate 写属性块）之后压
public sealed class TutorialSceneDim : MonoBehaviour
{
    [Tooltip("压暗后场景保留的亮度（1 = 不压暗）")]
    [Range(0.1f, 1f)] public float dimBrightness = 0.42f;
    [Tooltip("进入/退出压暗的时长（秒，不受 timeScale 影响）")]
    public float fadeSeconds = 0.12f;

    private readonly List<Renderer> _renderers = new List<Renderer>();
    private readonly List<Color> _rendererColors = new List<Color>();
    private readonly List<string> _rendererProps = new List<string>();
    private readonly List<MaterialPropertyBlock> _blocks = new List<MaterialPropertyBlock>();

    // 精灵：改**共享材质**的 _Color
    // （2026-10-26 实测：SpriteRenderer 的 PropertyBlock 改不动 _Color（渲染器自己接管），
    //   而 Y 场景的自定义 shader `CurvedScroll/Y Sample Scenery E0 Route` **忽略顶点色**
    //   → sprite.color 也没用：手动压暗 497 个精灵，画面完全不变。）
    private readonly List<Material> _dimMaterials = new List<Material>();
    private readonly List<Color> _dimMaterialColors = new List<Color>();

    // 材质没有颜色属性的精灵才回落到 sprite.color
    private readonly List<SpriteRenderer> _fallbackSprites = new List<SpriteRenderer>();
    private readonly List<Color> _fallbackColors = new List<Color>();

    // 场景调色：天空盒实例 + 各渲染器原色缓存（强制重建标记 A）
    private bool _bound;
    private float _current;
    private float _target;
    private Coroutine _fadeRoutine;
    private Material _skyboxOriginal;
    private Material _skyboxInstance;
    private Color _skyTintOriginal = Color.white;
    private Color _groundColorOriginal = Color.white;

    public bool IsDimming => _target > 0.001f;

    /// <summary>绑定一个场景的渲染器（只调用一次；重新绑定会先恢复原色）。</summary>
    public void Bind(Scene scenery)
    {
        RestoreNow();
        Clear();
        if (!scenery.IsValid() || !scenery.isLoaded) return;

        var roots = scenery.GetRootGameObjects();
        for (int r = 0; r < roots.Length; r++)
        {
            var root = roots[r];
            if (root == null) continue;

            // 网格：只能走材质属性
            var meshes = root.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < meshes.Length; i++)
            {
                var mesh = meshes[i];
                var material = mesh != null ? mesh.sharedMaterial : null;
                AddByMaterial(mesh, material);
            }

            // 精灵：改共享材质的 _Color（PropertyBlock / sprite.color 对这个自定义 shader 都无效）
            var sprites = root.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < sprites.Length; i++)
            {
                var sprite = sprites[i];
                if (sprite == null) continue;
                var mat = sprite.sharedMaterial;
                if (mat != null && mat.HasProperty("_Color"))
                {
                    if (!_dimMaterials.Contains(mat))
                    {
                        _dimMaterials.Add(mat);
                        _dimMaterialColors.Add(mat.GetColor("_Color"));
                    }
                    continue;
                }
                _fallbackSprites.Add(sprite);
                _fallbackColors.Add(sprite.color);
            }
        }

        BindSkybox();

        _bound = true;
        _current = 0f;
        Apply(0f);
    }

    private bool AddByMaterial(Renderer renderer, Material material)
    {
        if (renderer == null || material == null) return false;
        string prop = ResolveDimProperty(material);
        if (prop == null) return false;
        _renderers.Add(renderer);
        _rendererColors.Add(material.GetColor(prop));
        _rendererProps.Add(prop);
        _blocks.Add(new MaterialPropertyBlock());
        return true;
    }

    /// <summary>
    /// 渲染器能用哪个属性调亮度。
    /// 路面/地面用的是自定义 shader，没有 _Color，只认 _TutorialDim；其余场景材质多数走 _Color。
    /// </summary>
    private static string ResolveDimProperty(Material material)
    {
        if (material.HasProperty("_TutorialDim")) return "_TutorialDim";
        if (material.HasProperty("_Color")) return "_Color";
        if (material.HasProperty("_BaseColor")) return "_BaseColor";
        return null;
    }

    /// <summary>天空是程序化 Skybox（不是场景渲染器），只能靠换一份材质实例来调暗。</summary>
    private void BindSkybox()
    {
        if (_skyboxInstance != null) return;
        var sky = RenderSettings.skybox;
        if (sky == null) return;
        if (!sky.HasProperty("_SkyTint") && !sky.HasProperty("_GroundColor")) return;

        _skyboxOriginal = sky;
        _skyboxInstance = Instantiate(sky);
        _skyboxInstance.name = sky.name + " (TutorialDim)";
        _skyTintOriginal = sky.HasProperty("_SkyTint") ? sky.GetColor("_SkyTint") : Color.white;
        _groundColorOriginal = sky.HasProperty("_GroundColor") ? sky.GetColor("_GroundColor") : Color.white;
        RenderSettings.skybox = _skyboxInstance;
    }

    private void RestoreSkybox()
    {
        if (_skyboxInstance == null) return;
        if (RenderSettings.skybox == _skyboxInstance) RenderSettings.skybox = _skyboxOriginal;
        Destroy(_skyboxInstance);
        _skyboxInstance = null;
    }

    /// <summary>开始/结束压暗（带短淡入淡出）。场景未绑定时自动忽略。</summary>
    public void FadeTo(bool dim)
    {
        if (!_bound) return;
        _target = dim ? 1f : 0f;
        // 停用/销毁中的对象不能开协程（OnDisable 里收场会报错），直接落终值
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy) { _current = _target; Apply(_current); return; }
        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        if (fadeSeconds <= 0.001f) { _current = _target; Apply(_current); return; }
        _fadeRoutine = StartCoroutine(FadeRoutine());
    }

    /// <summary>直接恢复原色（离开教学/换场景时兜底）。</summary>
    public void RestoreNow()
    {
        if (_fadeRoutine != null) { StopCoroutine(_fadeRoutine); _fadeRoutine = null; }
        _target = 0f;
        _current = 0f;
        Apply(0f);
        RestoreSkybox();
        // 共享材质还原（改了必须还原，否则场景一直是暗的）
        for (int i = 0; i < _dimMaterials.Count; i++)
        {
            var m = _dimMaterials[i];
            if (m != null && m.HasProperty("_Color")) m.SetColor("_Color", _dimMaterialColors[i]);
        }
    }

    private IEnumerator FadeRoutine()
    {
        while (Mathf.Abs(_current - _target) > 0.001f)
        {
            _current = Mathf.MoveTowards(_current, _target, Time.unscaledDeltaTime / fadeSeconds);
            Apply(_current);
            yield return null;
        }
        _current = _target;
        Apply(_current);
        _fadeRoutine = null;
    }

    /// <summary>
    /// **每帧重压**：Y 场景的脚本（`ValleyArtPresentation.LateUpdate` / `ScrollWorldItem`）每帧会重写
    /// 材质的 _Color 与 PropertyBlock → 只在淡入那 0.12s 压一次会被立刻改回去（实测：压暗完全无效）。
    /// 执行序 1000 保证排在它们之后。
    /// </summary>
    private void LateUpdate()
    {
        if (!_bound || _target <= 0.001f) return;
        Apply(_current);
    }

    private void Apply(float t)
    {
        if (!_bound) return;
        float k = Mathf.Lerp(1f, dimBrightness, Mathf.Clamp01(t));

        for (int i = 0; i < _renderers.Count; i++)
        {
            var renderer = _renderers[i];
            if (renderer == null) continue;
            Color original = _rendererColors[i];
            var block = _blocks[i];
            renderer.GetPropertyBlock(block);
            block.SetColor(_rendererProps[i], new Color(original.r * k, original.g * k, original.b * k, original.a));
            renderer.SetPropertyBlock(block);
        }

        for (int i = 0; i < _fallbackSprites.Count; i++)
        {
            var sprite = _fallbackSprites[i];
            if (sprite == null) continue;
            Color original = _fallbackColors[i];
            sprite.color = new Color(original.r * k, original.g * k, original.b * k, original.a);
        }

        // 精灵：改共享材质的 _Color
        for (int i = 0; i < _dimMaterials.Count; i++)
        {
            var m = _dimMaterials[i];
            if (m == null || !m.HasProperty("_Color")) continue;
            Color o = _dimMaterialColors[i];
            m.SetColor("_Color", new Color(o.r * k, o.g * k, o.b * k, o.a));
        }

        if (_skyboxInstance != null)
        {
            _skyboxInstance.SetColor("_SkyTint", new Color(_skyTintOriginal.r * k, _skyTintOriginal.g * k, _skyTintOriginal.b * k, _skyTintOriginal.a));
            _skyboxInstance.SetColor("_GroundColor", new Color(_groundColorOriginal.r * k, _groundColorOriginal.g * k, _groundColorOriginal.b * k, _groundColorOriginal.a));
        }
    }

    private void Clear()
    {
        _renderers.Clear();
        _rendererColors.Clear();
        _rendererProps.Clear();
        _blocks.Clear();
        _dimMaterials.Clear();
        _dimMaterialColors.Clear();
        _fallbackSprites.Clear();
        _fallbackColors.Clear();
        _bound = false;
    }

    private void OnDisable()
    {
        RestoreNow();
    }
}
