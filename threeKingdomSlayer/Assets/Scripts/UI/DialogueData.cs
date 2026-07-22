using UnityEngine;

[CreateAssetMenu(fileName = "DialogueData", menuName = "Dialogue/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [System.Serializable]
    public struct Line
    {
        [TextArea(2, 5)]
        public string text;
        [Tooltip("此行台词显示时长（秒）")]
        public float duration;
    }

    [Header("台词列表")]
    public Line[] lines;

    [Header("自动关闭")]
    [Tooltip("所有台词播完后自动翻回正面（留空则需要手动关闭）")]
    public bool autoClose = true;
}
