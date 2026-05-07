using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 主菜单UI — 点击开始后触发 CameraManager 推入动画
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("UI元素")]
    public Text titleText;
    public Button startButton;
    public Button quitButton;

    [Header("过渡")]
    public CameraManager cameraManager;

    private void Start()
    {
        if (titleText != null)
            titleText.text = "一夫当关";
        else
            Debug.LogWarning("[MainMenuUI] titleText 未赋值");

        if (startButton != null)
            startButton.onClick.AddListener(OnStartGame);
        else
            Debug.LogWarning("[MainMenuUI] startButton 未赋值");

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitGame);
    }

    public void OnStartGame()
    {
        Debug.Log("[MainMenu] 开始游戏");
        if (cameraManager != null)
            cameraManager.PlayDeparture();
        else
        {
            Debug.LogWarning("[MainMenu] CameraManager 未赋值，直接加载");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Battle");
        }
    }

    public void OnQuitGame()
    {
        Debug.Log("[MainMenu] 退出游戏");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
