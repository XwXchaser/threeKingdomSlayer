using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 主菜单UI
/// 显示游戏标题和新游戏按钮
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("UI元素")]
    public Text titleText;
    public Button startButton;
    public Button quitButton;

    [Header("场景名称")]
    public string battleSceneName = "Battle";

    private void Start()
    {
        if (titleText != null)
        {
            titleText.text = "一夫当关";
        }
        else
        {
            Debug.LogWarning("[MainMenuUI] titleText 未赋值，标题将不显示");
        }

        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartGame);
        }
        else
        {
            Debug.LogWarning("[MainMenuUI] startButton 未赋值，无法开始游戏");
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitGame);
        }
        // quitButton 可选，不强制
    }

    /// <summary>
    /// 开始游戏按钮
    /// </summary>
    public void OnStartGame()
    {
        Debug.Log("[MainMenu] 开始游戏");
        UnityEngine.SceneManagement.SceneManager.LoadScene(battleSceneName);
    }

    /// <summary>
    /// 退出游戏按钮
    /// </summary>
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
