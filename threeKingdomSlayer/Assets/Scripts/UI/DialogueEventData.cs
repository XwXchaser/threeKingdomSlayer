using System;
using System.Collections.Generic;
using UnityEngine;

public enum DialogueEventType
{
    Tutorial,
    Conversation
}

public enum DialogueTriggerType
{
    Manual,
    StageStart,
    WaveStart,
    BossEngaged,
    BossPhaseChanged,
    BossDefeated,
    KillCount
}

public enum DialogueSpeaker
{
    Player,
    Boss
}

[CreateAssetMenu(fileName = "DialogueEvent", menuName = "Dialogue/Event")]
public sealed class DialogueEventData : ScriptableObject
{
    [Serializable]
    public struct Line
    {
        public DialogueSpeaker speaker;
        [TextArea(2, 5)] public string text;
    }

    [Header("Identity")]
    [Tooltip("唯一事件ID；手动触发时传给 DialogueManager.Trigger")]
    public string eventId;
    public DialogueEventType eventType;

    [Header("Trigger")]
    public DialogueTriggerType triggerType;
    [Tooltip("0 表示任意关卡")]
    public int stageId;
    [Tooltip("波次从 1 开始；仅 WaveStart 使用")]
    public int waveNumber;
    [Tooltip("敌人ID；仅 Boss 相关触发使用")]
    public int bossId;
    [Tooltip("Boss 阶段索引；仅 BossPhaseChanged 使用")]
    public int bossPhaseIndex;
    [Tooltip("达到该累计击杀数时触发；仅 KillCount 使用")]
    public int killCount;

    [Header("Presentation")]
    public BossDialogueProfile bossProfile;
    public List<Line> lines = new List<Line>();

    public bool HasBossLines
    {
        get
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].speaker == DialogueSpeaker.Boss)
                    return true;
            }
            return false;
        }
    }
}
