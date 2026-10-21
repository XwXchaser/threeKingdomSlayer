using UnityEngine;

[CreateAssetMenu(fileName = "NewHeroHUDSkin", menuName = "一夫当关/英雄HUD皮肤")]
public class HeroHUDSkin : ScriptableObject
{
    [Header("生命值")]
    public Sprite healthBottomSprite;
    public Sprite healthFillSprite;
    public Sprite healthFrameSprite;
    public Sprite shieldFillSprite;

    [Header("大招头像")]
    public Sprite ultimateBaseSprite;
    public Sprite ultimateFillSprite;
    public Sprite portraitSprite;
    public Sprite readyFireStartSprite;
    public Sprite[] readyFireLoopSprites;
    public float readyFireFps = 10f;

    [Header("技能图标")]
    public Sprite stabIcon;
    public Sprite stabChargeSprite;
    public Sprite slashIcon;
    public Sprite slashChargeSprite;
    public Sprite pierceIcon;
    public Sprite pierceChargeSprite;
    public Sprite sweepIcon;
    public Sprite sweepChargeSprite;
    public Sprite launchIcon;
    public Sprite launchChargeSprite;
    public Sprite parryIcon;
    public Sprite parryChargeSprite;

    [Header("扩展UI")]
    public GameObject[] extraUIPrefabs;
}
