using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 落地页（就绪态）右上角的**金币显示**：图标 + 玩家**总铜钱**。
///
/// 与局内那个 `CoinCounterUI`（显示"本局获得铜钱" + 飘字）分开：
/// 两者语义不同，落地页要的是总资产，所以这里独立一个轻量节点。
/// </summary>
public class HubCoinDisplay : MonoBehaviour
{
    [Tooltip("金币图标（落地页新素材）。")]
    public Sprite iconSprite;

    [Tooltip("显示总铜钱的文本。")]
    public TMP_Text amountText;

    private void OnEnable()
    {
        Refresh();
    }

    /// <summary>刷新为玩家当前总铜钱。</summary>
    public void Refresh()
    {
        if (amountText == null) return;
        amountText.text = SaveManager.GetCoins().ToString();
    }
}
