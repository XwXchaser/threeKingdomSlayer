#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class YSampleSceneTools
{
    const string Sample = "Assets/Experiments/CurvedScroll/YJunctionSample.unity";
    const string Battle = "Assets/Scenes/Battle.scene";

    [MenuItem("Tools/Curved Scroll/Open Y Junction Sample")]
    public static void OpenSample()
    {
        if (!EnsureSaved()) return;
        EditorSceneManager.OpenScene(Sample, OpenSceneMode.Single);
        var sample = Object.FindObjectOfType<YScrollSample>();
        if (sample != null)
        {
            Selection.activeGameObject = sample.gameObject;
            SceneView.lastActiveSceneView?.FrameSelected();
        }
    }

    [MenuItem("Tools/Curved Scroll/Open Battle Host")]
    public static void OpenBattle()
    {
        if (!EnsureSaved()) return;
        EditorSceneManager.OpenScene(Battle, OpenSceneMode.Single);
        var routeScene = SceneManager.GetSceneByPath(Sample);
        if (!routeScene.IsValid() || !routeScene.isLoaded)
            routeScene = EditorSceneManager.OpenScene(Sample, OpenSceneMode.Additive);
        var sample = FindInScene<YScrollSample>(routeScene);
        var battleCamera = Camera.main;
        if (sample != null)
        {
            sample.viewCamera = battleCamera;
            sample.enabled = true;
            EditorUtility.SetDirty(sample);
        }
        SceneManager.SetActiveScene(SceneManager.GetSceneByPath(Battle));
        var stage = Object.FindObjectOfType<StageController>();
        if (stage != null) Selection.activeGameObject = stage.gameObject;
        SceneView.RepaintAll();
    }

    [MenuItem("Tools/Curved Scroll/Validate Sample/Battle Scene Workflow")]
    public static void Validate()
    {
        var active = SceneManager.GetActiveScene().path;
        Debug.Log("[YSample] EditMode active scene=" + active + ". Open Y Junction Sample for route preview; Open Battle Host for combat authoring.");
        if (active == Sample && Object.FindObjectOfType<YScrollSample>() == null) Debug.LogError("[YSample] YScrollSample missing in sample scene");
        if (active == Battle && Object.FindObjectOfType<StageController>() == null) Debug.LogError("[YSample] StageController missing in Battle scene");
    }

    static T FindInScene<T>(Scene scene) where T : Object
    {
        if (!scene.IsValid() || !scene.isLoaded) return null;
        foreach (var root in scene.GetRootGameObjects())
        {
            var found = root.GetComponentInChildren<T>(true);
            if (found != null) return found;
        }
        return null;
    }

    static bool EnsureSaved()
    {
        if (!SceneManager.GetActiveScene().isDirty) return true;
        return EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
    }
}
#endif
