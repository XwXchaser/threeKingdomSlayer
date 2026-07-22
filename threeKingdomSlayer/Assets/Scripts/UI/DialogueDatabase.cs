using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueDatabase", menuName = "Dialogue/Database")]
public sealed class DialogueDatabase : ScriptableObject
{
    public List<DialogueEventData> events = new List<DialogueEventData>();

    public bool ContainsEventId(string eventId, DialogueEventData except = null)
    {
        if (string.IsNullOrEmpty(eventId)) return false;

        for (int i = 0; i < events.Count; i++)
        {
            var dialogueEvent = events[i];
            if (dialogueEvent != null && dialogueEvent != except && dialogueEvent.eventId == eventId)
                return true;
        }

        return false;
    }

    public DialogueEventData FindById(string eventId)
    {
        if (string.IsNullOrEmpty(eventId)) return null;

        for (int i = 0; i < events.Count; i++)
        {
            var dialogueEvent = events[i];
            if (dialogueEvent != null && dialogueEvent.eventId == eventId)
                return dialogueEvent;
        }

        return null;
    }
}
