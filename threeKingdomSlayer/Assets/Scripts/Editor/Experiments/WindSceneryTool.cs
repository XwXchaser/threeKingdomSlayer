#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Authoring tool for <c>CurvedScroll/Y Scenery Wind</c>.
/// Converts an authored SpriteRenderer into the same "real subdivided mesh + shader vertex
/// wind" setup already used by the route grass: the original renderer stays in the hierarchy
/// as the layout source but is disabled, and a child "Wind Mesh" renderer does the drawing.
/// Everything stays Inspector-authored assets (one mesh per mirror combination, one material
/// per sprite); nothing is built at runtime and the route sampling code is untouched.
/// </summary>
public static class WindSceneryTool
{
    const string AssetRoot = "Assets/Experiments/CurvedScroll/Authoring/WindScenery";
    const string WindShaderName = "CurvedScroll/Y Scenery Wind";
    const string ChildName = "Wind Mesh";

    /// <summary>0 = tree, 1 = bush, 2 = flag. Mirrors the shader's _WindMode values.</summary>
    public enum Mode { Tree = 0, Bush = 1, Flag = 2 }

    struct ModeDefaults
    {
        public Mode mode;
        public float start;
        public float end;
        public float power;
        public float strength;
        public float speed;
        public float wave;
        public float waveFrequency;
    }

    static ModeDefaults DefaultsFor(Mode mode)
    {
        switch (mode)
        {
            case Mode.Bush:
                return new ModeDefaults { mode = mode, start = 0f, end = 0.2f, power = 1.7f,
                    strength = 0.05f, speed = 1.6f, wave = 0.25f, waveFrequency = 4f };
            case Mode.Flag:
                return new ModeDefaults { mode = mode, start = 0.35f, end = 0.35f, power = 1.7f,
                    strength = 0.045f, speed = 2.8f, wave = 0.9f, waveFrequency = 6f };
            default:
                return new ModeDefaults { mode = Mode.Tree, start = 0.30f, end = 0.62f, power = 1.7f,
                    strength = 0.02f, speed = 0.9f, wave = 0.3f, waveFrequency = 4f };
        }
    }

    public static Mode GuessMode(Sprite sprite)
    {
        if (sprite == null) return Mode.Tree;
        var n = sprite.name.ToLowerInvariant();
        if (n.Contains("flag") || n.Contains("banner") || n.Contains("pennant")) return Mode.Flag;
        if (n.Contains("bush") || n.Contains("shrub")) return Mode.Bush;
        return Mode.Tree;
    }

    [MenuItem("Tools/Curved Scroll/Wind/Apply Wind To Selected Scenery", priority = 210)]
    public static void ApplyToSelection()
    {
        var targets = CollectSelected();
        var report = Apply(targets, null);
        Debug.Log("[WindScenery] " + report);
    }

    [MenuItem("Tools/Curved Scroll/Wind/Remove Wind From Selected Scenery", priority = 211)]
    public static void RemoveFromSelection()
    {
        var report = Remove(CollectSelected());
        Debug.Log("[WindScenery] " + report);
    }

    static List<SpriteRenderer> CollectSelected()
    {
        var list = new List<SpriteRenderer>();
        foreach (var go in Selection.gameObjects)
            list.AddRange(go.GetComponentsInChildren<SpriteRenderer>(true));
        return list.Distinct().ToList();
    }

    /// <summary>
    /// Deploys wind rendering for every given sprite renderer. Pass a mode to force it,
    /// otherwise the sprite name decides (flag/banner, bush/shrub, everything else = tree).
    /// Returns a human readable report; the scene is left dirty and unsaved.
    /// </summary>
    public static string Apply(IReadOnlyList<SpriteRenderer> targets, Mode? forcedMode)
    {
        EnsureAssetFolder();
        var shader = Shader.Find(WindShaderName);
        if (shader == null) return "FAILED: shader " + WindShaderName + " not found (import may not be finished).";

        var lines = new List<string>();
        int created = 0, updated = 0, skipped = 0;
        var childs = new List<Renderer>();
        var usedMaterials = new HashSet<Material>();

        foreach (var sr in targets)
        {
            if (sr == null) { skipped++; continue; }
            if (sr.sprite == null) { lines.Add("skip " + sr.name + " (no sprite)"); skipped++; continue; }

            var mode = forcedMode ?? GuessMode(sr.sprite);
            var sprite = sr.sprite;
            var bake = SpriteBakeData.Get(sprite);
            var defaults = DefaultsFor(mode);
            var mat = GetOrCreateMaterial(sprite, mode, bake, defaults, shader, lines);
            usedMaterials.Add(mat);

            bool flipX = sr.flipX, flipY = sr.flipY;
            var grid = GetGrid(flipX, flipY);
            var verts = sprite.vertices;
            float xmin = verts.Min(v => v.x), xmax = verts.Max(v => v.x);
            float ymin = verts.Min(v => v.y), ymax = verts.Max(v => v.y);

            var child = sr.transform.Find(ChildName);
            bool isNew = child == null;
            if (isNew)
            {
                var go = new GameObject(ChildName);
                Undo.RegisterCreatedObjectUndo(go, "Add wind mesh");
                go.transform.SetParent(sr.transform, false);
                child = go.transform;
            }
            // A SpriteRenderer flip mirrors the quad around the transform origin in the shader
            // (UnityFlipSprite), so a flipped sprite with an off-centre pivot moves to the
            // opposite side. Mirror the child offset the same way and mirror the mesh UVs.
            float signX = flipX ? -1f : 1f;
            float signY = flipY ? -1f : 1f;
            child.localPosition = new Vector3((xmin + xmax) * 0.5f * signX, (ymin + ymax) * 0.5f * signY, 0f);
            child.localRotation = Quaternion.identity;
            child.localScale = new Vector3(Mathf.Abs(xmax - xmin), Mathf.Abs(ymax - ymin), 1f);

            var mf = child.GetComponent<MeshFilter>();
            if (mf == null) mf = Undo.AddComponent<MeshFilter>(child.gameObject);
            mf.sharedMesh = grid;

            var mr = child.GetComponent<MeshRenderer>();
            if (mr == null) mr = Undo.AddComponent<MeshRenderer>(child.gameObject);
            mr.sharedMaterial = mat;
            mr.sortingLayerID = sr.sortingLayerID;
            mr.sortingOrder = sr.sortingOrder;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

            if (sr.enabled)
            {
                Undo.RecordObject(sr, "Disable layout sprite");
                sr.enabled = false;
            }

            RegisterScenery(sr, mr);
            childs.Add(mr);
            if (isNew) created++; else updated++;
        }

        AssetDatabase.SaveAssets();
        if (targets.Count > 0) EditorSceneManager.MarkSceneDirty(targets[0].gameObject.scene);

        lines.Add("renderers created=" + created + " updated=" + updated + " skipped=" + skipped
                  + " materials=" + usedMaterials.Count
                  + " sceneDirty=true (not saved)");
        return string.Join("\n", lines);
    }

    /// <summary>Reverts wind rendering: removes the generated child, re-enables the sprite and unregisters it.</summary>
    public static string Remove(IReadOnlyList<SpriteRenderer> targets)
    {
        int removed = 0, restored = 0;
        foreach (var sr in targets)
        {
            if (sr == null) continue;
            var child = sr.transform.Find(ChildName);
            if (child != null)
            {
                var mr = child.GetComponent<MeshRenderer>();
                UnregisterScenery(sr, mr);
                Undo.DestroyObjectImmediate(child.gameObject);
                removed++;
            }
            if (!sr.enabled)
            {
                Undo.RecordObject(sr, "Enable layout sprite");
                sr.enabled = true;
                restored++;
            }
        }
        if (targets.Count > 0) EditorSceneManager.MarkSceneDirty(targets[0].gameObject.scene);
        return "wind children removed=" + removed + " sprites re-enabled=" + restored;
    }

    static Material GetOrCreateMaterial(Sprite sprite, Mode mode, SpriteBakeData bake,
        ModeDefaults defaults, Shader shader, List<string> lines)
    {
        string assetName = mode + "_" + Sanitize(sprite.name) + "_Wind";
        string path = AssetRoot + "/" + assetName + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        bool isNew = mat == null;
        if (isNew)
        {
            mat = new Material(shader) { name = assetName };
            AssetDatabase.CreateAsset(mat, path);
            lines.Add("material created " + path);
        }
        else if (mat.shader != shader)
        {
            mat.shader = shader;
        }

        mat.SetTexture("_MainTex", sprite.texture);
        mat.SetVector("_UVRect", bake.uvRect);
        mat.SetVector("_ContentRect", bake.contentRect);

        // Mode, band and motion defaults are only written on creation so user tuning survives a refresh.
        if (isNew)
        {
            float start = defaults.start;
            if (mode == Mode.Flag && bake.flagBandStart > 0f) start = bake.flagBandStart;
            mat.SetFloat("_WindMode", (float)mode);
            mat.SetFloat("_WindStart", start);
            mat.SetFloat("_WindEnd", defaults.end);
            mat.SetFloat("_WindPower", defaults.power);
            mat.SetFloat("_WindStrength", defaults.strength);
            mat.SetFloat("_WindSpeed", defaults.speed);
            mat.SetFloat("_WindWave", defaults.wave);
            mat.SetFloat("_WindWaveFrequency", defaults.waveFrequency);
            mat.SetFloat("_WindPhase", 0f);
            mat.SetFloat("_WindTimeOverride", -1f);
        }
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void RegisterScenery(SpriteRenderer sr, Renderer wind)
    {
        if (wind == null) return;
        var sample = FindSample(sr.gameObject.scene);
        if (sample == null) return;
        if (sample.scenery == null) sample.scenery = new Renderer[0];
        if (sample.scenery.Contains(wind)) return;
        Undo.RecordObject(sample, "Register wind renderer");
        var list = new List<Renderer>(sample.scenery) { wind };
        sample.scenery = list.ToArray();
        EditorUtility.SetDirty(sample);
    }

    static void UnregisterScenery(SpriteRenderer sr, Renderer wind)
    {
        if (wind == null) return;
        var sample = FindSample(sr.gameObject.scene);
        if (sample == null || sample.scenery == null) return;
        if (!sample.scenery.Contains(wind)) return;
        Undo.RecordObject(sample, "Unregister wind renderer");
        sample.scenery = sample.scenery.Where(r => r != wind).ToArray();
        EditorUtility.SetDirty(sample);
    }

    static YScrollSample FindSample(Scene scene)
    {
        if (!scene.IsValid()) return null;
        foreach (var root in scene.GetRootGameObjects())
        {
            var found = root.GetComponentInChildren<YScrollSample>(true);
            if (found != null) return found;
        }
        return null;
    }

    static void EnsureAssetFolder()
    {
        if (AssetDatabase.IsValidFolder(AssetRoot)) return;
        var parent = Path.GetDirectoryName(AssetRoot)?.Replace('\\', '/');
        var leaf = Path.GetFileName(AssetRoot);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolderRecursive(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    static void EnsureFolderRecursive(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        var leaf = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent)) EnsureFolderRecursive(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    static string Sanitize(string raw)
    {
        var chars = (raw ?? "unnamed").Select(ch => char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_').ToArray();
        return new string(chars);
    }

    // ---------------------------------------------------------------- mesh

    const int GridColumns = 13;
    const int GridRows = 21;

    /// <summary>Unit grid in sprite space (x and y -0.5..0.5 so the child can be placed on the quad
    /// centre, uv 0..1). A flip only has to mirror the UVs and the child offset because Unity
    /// mirrors sprite quads around the transform origin. Keeping the renderer scale positive
    /// means the shader never needs a flip parameter.</summary>
    static Mesh GetGrid(bool mirrorX, bool mirrorY)
    {
        string name = "WindSceneryGrid" + (mirrorX ? "_MirrorX" : "") + (mirrorY ? "_MirrorY" : "");
        string path = AssetRoot + "/" + name + ".asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null)
        {
            mesh = new Mesh { name = name };
            AssetDatabase.CreateAsset(mesh, path);
        }

        var verts = new Vector3[GridColumns * GridRows];
        var uvs = new Vector2[verts.Length];
        for (int row = 0; row < GridRows; row++)
        {
            float v = row / (float)(GridRows - 1);
            for (int col = 0; col < GridColumns; col++)
            {
                float u = col / (float)(GridColumns - 1);
                int i = row * GridColumns + col;
                verts[i] = new Vector3(u - 0.5f, v - 0.5f, 0f);
                uvs[i] = new Vector2(mirrorX ? 1f - u : u, mirrorY ? 1f - v : v);
            }
        }
        if (mesh.vertexCount == verts.Length && mesh.uv != null && mesh.uv.Length == uvs.Length
            && (mesh.uv[1] - uvs[1]).sqrMagnitude < 1e-8f
            && (mesh.vertices[1] - verts[1]).sqrMagnitude < 1e-8f) return mesh;
        var tris = new List<int>();
        for (int row = 0; row < GridRows - 1; row++)
        for (int col = 0; col < GridColumns - 1; col++)
        {
            int bl = row * GridColumns + col;
            int br = bl + 1;
            int tl = bl + GridColumns;
            int tr = tl + 1;
            tris.Add(bl); tris.Add(tl); tris.Add(tr);
            tris.Add(bl); tris.Add(tr); tris.Add(br);
        }
        mesh.Clear();
        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris.ToArray();
        mesh.RecalculateBounds();
        EditorUtility.SetDirty(mesh);
        AssetDatabase.SaveAssets();
        return mesh;
    }

    // ---------------------------------------------------------------- sprite sampling

    /// <summary>Per sprite constants baked from the sprite itself: UV rect, visible content rect
    /// and (for flags) where the pole ends. Cached for the duration of one tool run.</summary>
    sealed class SpriteBakeData
    {
        static readonly Dictionary<int, SpriteBakeData> Cache = new Dictionary<int, SpriteBakeData>();

        public Vector4 uvRect = new Vector4(0, 0, 1, 1);
        public Vector4 contentRect = new Vector4(0, 0, 1, 1);
        public float flagBandStart = 0f;

        public static SpriteBakeData Get(Sprite sprite)
        {
            SpriteBakeData data;
            if (Cache.TryGetValue(sprite.GetInstanceID(), out data)) return data;
            data = Build(sprite);
            Cache[sprite.GetInstanceID()] = data;
            return data;
        }

        static SpriteBakeData Build(Sprite sprite)
        {
            var data = new SpriteBakeData { uvRect = UvRect(sprite) };
            var alpha = AlphaMask.Load(sprite);
            if (alpha == null) return data;   // keep full content rect as a safe fallback

            var rect = sprite.textureRect;
            int x0 = Mathf.Clamp(Mathf.RoundToInt(rect.x), 0, alpha.Width - 1);
            int y0 = Mathf.Clamp(Mathf.RoundToInt(rect.y), 0, alpha.Height - 1);
            int x1 = Mathf.Clamp(Mathf.RoundToInt(rect.x + rect.width), 1, alpha.Width);
            int y1 = Mathf.Clamp(Mathf.RoundToInt(rect.y + rect.height), 1, alpha.Height);

            int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
            for (int y = y0; y < y1; y++)
            for (int x = x0; x < x1; x++)
            {
                if (!alpha.IsOpaque(x, y)) continue;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
            if (maxX < 0 || maxY < 0) return data;

            data.contentRect = ToSpriteSpace(data.uvRect, alpha.Width, alpha.Height,
                minX, minY, maxX + 1, maxY + 1);

            // Flag pole detection: a pole is the only structure that runs the full content
            // height. The cloth band starts just right of the last full-height column.
            int contentHeight = maxY - minY + 1;
            int poleEnd = -1;
            for (int x = minX; x <= maxX; x++)
            {
                int covered = 0;
                for (int y = minY; y <= maxY; y++) if (alpha.IsOpaque(x, y)) covered++;
                if (covered >= contentHeight * 0.78f) poleEnd = x;
            }
            if (poleEnd >= 0 && poleEnd < maxX - 2)
            {
                var poleTop = ToSpriteSpace(data.uvRect, alpha.Width, alpha.Height,
                    poleEnd, minY, poleEnd + 1, maxY + 1);
                // The shader compares the band against the content-normalized X, not sprite X.
                float poleRight = poleTop.x + poleTop.z;
                float contentSpace = (poleRight - data.contentRect.x) / Mathf.Max(1e-4f, data.contentRect.z);
                data.flagBandStart = Mathf.Clamp(contentSpace + 0.02f, 0.02f, 0.9f);
            }
            return data;
        }

        static Vector4 UvRect(Sprite sprite)
        {
            var uv = sprite.uv;
            float u0 = uv.Min(p => p.x), u1 = uv.Max(p => p.x);
            float v0 = uv.Min(p => p.y), v1 = uv.Max(p => p.y);
            return new Vector4(u0, v0, u1 - u0, v1 - v0);
        }

        /// <summary>Pixel rect (half open) to sprite space 0..1 relative to the sprite's own UV rect.</summary>
        static Vector4 ToSpriteSpace(Vector4 uvRect, int texWidth, int texHeight,
            int px0, int py0, int px1, int py1)
        {
            float nx0 = px0 / (float)texWidth, ny0 = py0 / (float)texHeight;
            float nx1 = px1 / (float)texWidth, ny1 = py1 / (float)texHeight;
            float sx = Mathf.Max(1e-4f, uvRect.z), sy = Mathf.Max(1e-4f, uvRect.w);
            return new Vector4((nx0 - uvRect.x) / sx, (ny0 - uvRect.y) / sy,
                               (nx1 - nx0) / sx, (ny1 - ny0) / sy);
        }
    }

    /// <summary>Reads sprite alpha from the source PNG. Imported textures are not readable and
    /// the importer settings are not touched: the file is decoded into a temporary texture.</summary>
    sealed class AlphaMask
    {
        public int Width, Height;
        byte[] pixels;

        public static AlphaMask Load(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null) return null;
            var path = AssetDatabase.GetAssetPath(sprite.texture);
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            Texture2D tmp = null;
            try
            {
                tmp = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!tmp.LoadImage(File.ReadAllBytes(path))) return null;
                var colors = tmp.GetPixels32();
                var mask = new AlphaMask { Width = tmp.width, Height = tmp.height, pixels = new byte[colors.Length] };
                for (int i = 0; i < colors.Length; i++) mask.pixels[i] = colors[i].a;
                return mask;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[WindScenery] could not read pixels for " + path + ": " + e.Message);
                return null;
            }
            finally
            {
                if (tmp != null) Object.DestroyImmediate(tmp);
            }
        }

        public bool IsOpaque(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return false;
            return pixels[y * Width + x] > 16;
        }
    }
}
#endif
