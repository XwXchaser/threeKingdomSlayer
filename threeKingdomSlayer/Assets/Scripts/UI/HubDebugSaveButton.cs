using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 落地页的**临时调试按钮**：清除存档（回主菜单后想重玩教学关时用）。
///
/// 说明：
///   • 这是开发期工具，不是玩家功能。正式发版前应把这个对象从场景里删掉（或加个开关）。
///   • 清除存档 = <c>SaveManager.Delete()</c>（清 PlayerPrefs 的存档键 + 内存缓存）。
///   • 第一次点只改文案确认，3 秒内再点才真删 —— 防止误触把存档抹了。
/// </summary>
public class HubDebugSaveButton : MonoBehaviour
{
    [Tooltip("要清除的存档键（一般就是 SaveManager 用的那个）。留空 = 全部存档。")]
    public bool clearAllSaves = true;

    [Tooltip("确认提示的文案")]
    public string confirmText = "再点一次清除存档";

    [Tooltip("正常态文案")]
    public string normalText = "清除存档";

    [Tooltip("清除后是否重载场景（回落地页初始态）")]
    public bool reloadSceneAfterClear = true;

    [Tooltip("重载的场景名（留空 = 当前场景）")]
    public string reloadSceneName = "";

    [Tooltip("确认窗口（秒）")]
    public float confirmWindow = 3f;

    private Button _button;
    private TMP_Text _label;
    private Coroutine _resetRoutine;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _label = GetComponentInChildren<TMP_Text>(true);
        RefreshLabel();
    }

    private void OnEnable()
    {
        if (_button != null) _button.onClick.AddListener(OnClick);
        RefreshLabel();
    }

    private void OnDisable()
    {
        if (_button != null) _button.onClick.RemoveListener(OnClick);
        if (_resetRoutine != null) StopCoroutine(_resetRoutine);
        _resetRoutine = null;
    }

    private void OnClick()
    {
        // 第一次点：只进确认态
        if (!string.Equals(_label != null ? _label.text : normalText, confirmText, System.StringComparison.Ordinal))
        {
            if (_label != null) _label.text = confirmText;
            if (_resetRoutine != null) StopCoroutine(_resetRoutine);
            _resetRoutine = StartCoroutine(ResetLabelAfter(confirmWindow));
            return;
        }

        // 第二次点：真删
        SaveManager.Delete();
        Debug.Log("[Hub] 存档已清除（SaveManager.Delete）");

        if (reloadSceneAfterClear)
        {
            string sceneName = string.IsNullOrEmpty(reloadSceneName)
                ? UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
                : reloadSceneName;
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
    }

    private IEnumerator ResetLabelAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        _resetRoutine = null;
        RefreshLabel();
    }

    private void RefreshLabel()
    {
        if (_label != null) _label.text = normalText;
    }
}
