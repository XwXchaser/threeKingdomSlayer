using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 纯 UI overlay 场景的统一关闭器：挂在「返回」按钮上，调用 SceneFlowManager 卸载自己这个 overlay 场景。
///
/// 用法：把本组件挂到按钮上，button 指向那个按钮即可。
/// </summary>
public class UIOverlayCloseButton : MonoBehaviour
{
    [Tooltip("要关闭的 overlay 场景名（与场景文件名一致）。留空则用 SceneFlowManager 的栈顶。")]
    public string sceneName = "";

    [Header("引用")]
    public Button button;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
    }

    private void Start()
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        var flow = SceneFlowManager.Instance;
        if (flow == null)
        {
            Debug.LogWarning("[UIOverlayCloseButton] 找不到 SceneFlowManager，无法关闭 overlay");
            return;
        }
        flow.RequestCloseOverlay(string.IsNullOrEmpty(sceneName) ? null : sceneName);
    }
}
