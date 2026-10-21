using UnityEngine;

[CreateAssetMenu(fileName = "BossDialogueProfile", menuName = "Dialogue/Boss Profile")]
public sealed class BossDialogueProfile : ScriptableObject
{
    [Header("Identity")]
    public int bossId;
    public string displayName;

    [Header("Portrait")]
    public Sprite portrait;
    public Sprite portraitFrame;
}
