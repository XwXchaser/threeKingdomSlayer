using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景流程管理器：统一「替换 active scene」与「叠加纯 UI overlay 场景」两类切换。
///
/// 两种场景：
///   1) **active scene**（MainMenu / Battle）：互相替换，玩家始终只有一个主场景。
///   2) **overlay 场景**（StageSelect / TalentTree 等纯 UI 页面）：additive 叠加在 active scene 之上，
///      可以叠多层，返回时逐层卸载。overlay 场景的根节点挂 <see cref="UIOverlayScene"/> 标记。
///
/// 挂在 active scene 的常驻对象上（Battle / MainMenu 各挂一个），用 DontDestroyOnLoad 保持单例。
/// </summary>
public class SceneFlowManager : MonoBehaviour
{
    public static SceneFlowManager Instance { get; private set; }

    public const string SceneBattle = "Battle";
    public const string SceneStageSelect = "StageSelect";
    public const string SceneTalentTree = "TalentTree";

    [Header("过渡")]
    [Tooltip("切回主菜单 / 进入战斗时是否播放推入动画（由 CameraManager 实现）")]
    public bool useCameraTransition = true;

    private readonly List<string> _overlayStack = new List<string>();
    private CameraManager _cameraManager;

    /// <summary>当前叠着的 overlay 场景名（栈顶 = 最后打开的）</summary>
    public string CurrentOverlay => _overlayStack.Count > 0 ? _overlayStack[_overlayStack.Count - 1] : null;

    public bool HasOverlay => _overlayStack.Count > 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _cameraManager = FindObjectOfType<CameraManager>();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ────────────────────────── active scene 切换 ──────────────────────────

    /// <summary>进入战斗场景（替换当前 active scene）。关卡数据由调用方先写进 Pending* 静态字段。</summary>
    public void GoToBattle()
    {
        Debug.Log("[SceneFlow] → Battle");
        ReplaceActiveScene(SceneBattle);
    }

    private void ReplaceActiveScene(string sceneName)
    {
        CloseAllOverlays();
        if (useCameraTransition && _cameraManager != null && !string.IsNullOrEmpty(_cameraManager.nextScene))
        {
            // CameraManager 会在推入动画结束后 LoadSceneAsync(nextScene)
            _cameraManager.nextScene = sceneName;
            _cameraManager.PlayDeparture();
            return;
        }
        SceneManager.LoadSceneAsync(sceneName);
    }

    // ────────────────────────── overlay 场景 ──────────────────────────

    /// <summary>叠加打开一个纯 UI overlay 场景。已打开则不再重复加载。</summary>
    public void OpenOverlay(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        if (_overlayStack.Contains(sceneName))
        {
            Debug.Log("[SceneFlow] overlay 已在栈中，跳过: " + sceneName);
            return;
        }
        Debug.Log("[SceneFlow] 打开 overlay: " + sceneName);
        _overlayStack.Add(sceneName);
        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
    }

    /// <summary>关闭栈顶 overlay 场景。</summary>
    public void CloseOverlay()
    {
        if (_overlayStack.Count == 0) return;
        string sceneName = _overlayStack[_overlayStack.Count - 1];
        _overlayStack.RemoveAt(_overlayStack.Count - 1);
        Debug.Log("[SceneFlow] 关闭 overlay: " + sceneName);
        UnloadScene(sceneName);
    }

    /// <summary>关闭全部 overlay 场景。</summary>
    public void CloseAllOverlays()
    {
        for (int i = _overlayStack.Count - 1; i >= 0; i--)
            UnloadScene(_overlayStack[i]);
        _overlayStack.Clear();
    }

    private static void UnloadScene(string sceneName)
    {
        var scene = SceneManager.GetSceneByName(sceneName);
        if (scene.IsValid() && scene.isLoaded)
        {
            if (Application.isPlaying) SceneManager.UnloadSceneAsync(scene);
            else SceneManager.UnloadScene(scene);
        }
    }

    // ────────────────────────── overlay 内部用 ──────────────────────────

    /// <summary>overlay 场景里的 UI 调这个来关闭自己。</summary>
    public void RequestCloseOverlay(string sceneName = null)
    {
        if (string.IsNullOrEmpty(sceneName)) sceneName = CurrentOverlay;
        if (string.IsNullOrEmpty(sceneName)) return;
        _overlayStack.Remove(sceneName);
        UnloadScene(sceneName);
    }
}

/// <summary>
/// 标记一个场景是「纯 UI overlay 场景」：additive 加载，覆盖在 active scene 之上，不接管摄像机。
/// 挂在 overlay 场景的根 GameObject 上。
/// </summary>
public class UIOverlayScene : MonoBehaviour
{
    [Tooltip("overlay 场景名（与场景文件名一致）。用于 SceneFlowManager 关闭时定位。")]
    public string sceneName = "";

    [Tooltip("打开时是否暂停 active scene 的战斗（timeScale=0）。选关/天赋页通常不需要暂停。")]
    public bool pauseUnderlyingScene = false;

    private void OnEnable()
    {
        if (!pauseUnderlyingScene) return;
        Time.timeScale = 0f;
    }

    private void OnDisable()
    {
        if (!pauseUnderlyingScene) return;
        Time.timeScale = 1f;
    }
}
