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

        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartGame);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitGame);
        }
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
