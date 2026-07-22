using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(DialogueEventData))]
public sealed class DialogueEventDataEditor : Editor
{
    private SerializedProperty _eventId;
    private SerializedProperty _eventType;
    private SerializedProperty _triggerType;
    private SerializedProperty _stageId;
    private SerializedProperty _waveNumber;
    private SerializedProperty _bossId;
    private SerializedProperty _bossPhaseIndex;
    private SerializedProperty _killCount;
    private SerializedProperty _bossProfile;
    private SerializedProperty _lines;
    private ReorderableList _lineList;

    private void OnEnable()
    {
        _eventId = serializedObject.FindProperty("eventId");
        _eventType = serializedObject.FindProperty("eventType");
        _triggerType = serializedObject.FindProperty("triggerType");
        _stageId = serializedObject.FindProperty("stageId");
        _waveNumber = serializedObject.FindProperty("waveNumber");
        _bossId = serializedObject.FindProperty("bossId");
        _bossPhaseIndex = serializedObject.FindProperty("bossPhaseIndex");
        _killCount = serializedObject.FindProperty("killCount");
        _bossProfile = serializedObject.FindProperty("bossProfile");
        _lines = serializedObject.FindProperty("lines");

        _lineList = new ReorderableList(serializedObject, _lines, true, true, true, true);
        _lineList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "台词（点击看板逐句推进）");
        _lineList.elementHeight = EditorGUIUtility.singleLineHeight * 3f + 12f;
        _lineList.drawElementCallback = (rect, index, active, focused) =>
        {
            var line = _lines.GetArrayElementAtIndex(index);
            rect.y += 2f;
            var speakerRect = new Rect(rect.x, rect.y, 110f, EditorGUIUtility.singleLineHeight);
            var textRect = new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight + 4f, rect.width, EditorGUIUtility.singleLineHeight * 2f);
            EditorGUI.PropertyField(speakerRect, line.FindPropertyRelative("speaker"), new GUIContent("说话者"));
            EditorGUI.PropertyField(textRect, line.FindPropertyRelative("text"), GUIContent.none);
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("事件标识", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_eventId, new GUIContent("事件 ID"));
        EditorGUILayout.PropertyField(_eventType, new GUIContent("类型"));
        if (string.IsNullOrWhiteSpace(_eventId.stringValue))
            EditorGUILayout.HelpBox("事件 ID 不能为空；手动教学通过 DialogueManager.Trigger(\"事件ID\") 调用。", MessageType.Warning);
        else
        {
            var database = FindDatabaseContaining((DialogueEventData)target);
            if (database != null && database.ContainsEventId(_eventId.stringValue, (DialogueEventData)target))
                EditorGUILayout.HelpBox("事件 ID 在数据库中重复，运行时只会匹配第一个事件。", MessageType.Error);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("触发条件", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_triggerType, new GUIContent("触发方式"));
        EditorGUILayout.PropertyField(_stageId, new GUIContent("关卡 ID（0=任意）"));

        var trigger = (DialogueTriggerType)_triggerType.enumValueIndex;
        switch (trigger)
        {
            case DialogueTriggerType.WaveStart:
                EditorGUILayout.PropertyField(_waveNumber, new GUIContent("波次（从1开始）"));
                break;
            case DialogueTriggerType.BossEngaged:
            case DialogueTriggerType.BossDefeated:
                EditorGUILayout.PropertyField(_bossId, new GUIContent("Boss ID（0=任意）"));
                break;
            case DialogueTriggerType.BossPhaseChanged:
                EditorGUILayout.PropertyField(_bossId, new GUIContent("Boss ID（0=任意）"));
                EditorGUILayout.PropertyField(_bossPhaseIndex, new GUIContent("阶段索引"));
                break;
            case DialogueTriggerType.KillCount:
                EditorGUILayout.PropertyField(_killCount, new GUIContent("累计击杀数"));
                break;
            case DialogueTriggerType.Manual:
                EditorGUILayout.HelpBox("由任意脚本调用 DialogueManager.Trigger(\"事件ID\") 入队。", MessageType.Info);
                break;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("角色展示", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_bossProfile, new GUIContent("Boss 头像资料"));
        if ((DialogueEventType)_eventType.enumValueIndex == DialogueEventType.Tutorial)
            EditorGUILayout.HelpBox("教学：不可跳过；只有关卡胜利后才写入全局完成记录。", MessageType.Info);
        else
            EditorGUILayout.HelpBox("对白：本局同一事件只播放一次；玩家可跳过整段。", MessageType.Info);

        EditorGUILayout.Space();
        _lineList.DoLayoutList();

        if (_lines.arraySize == 0)
            EditorGUILayout.HelpBox("至少配置一句台词，否则运行时会跳过该事件。", MessageType.Warning);

        serializedObject.ApplyModifiedProperties();
    }

    private static DialogueDatabase FindDatabaseContaining(DialogueEventData dialogueEvent)
    {
        string[] guids = AssetDatabase.FindAssets("t:DialogueDatabase");
        for (int i = 0; i < guids.Length; i++)
        {
            var database = AssetDatabase.LoadAssetAtPath<DialogueDatabase>(AssetDatabase.GUIDToAssetPath(guids[i]));
            if (database != null && database.events.Contains(dialogueEvent))
                return database;
        }
        return null;
    }
}

[CustomEditor(typeof(DialogueDatabase))]
public sealed class DialogueDatabaseEditor : Editor
{
    private SerializedProperty _events;
    private ReorderableList _eventList;

    private void OnEnable()
    {
        _events = serializedObject.FindProperty("events");
        _eventList = new ReorderableList(serializedObject, _events, true, true, true, true);
        _eventList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "已注册对话事件");
        _eventList.drawElementCallback = (rect, index, active, focused) =>
        {
            var property = _events.GetArrayElementAtIndex(index);
            EditorGUI.PropertyField(rect, property, GUIContent.none);
        };
        _eventList.onAddCallback = AddEvent;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.HelpBox("运行时只从此列表读取事件。新建事件会自动保存到本数据库同目录并加入列表。", MessageType.Info);
        _eventList.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
    }

    private void AddEvent(ReorderableList list)
    {
        var database = (DialogueDatabase)target;
        string databasePath = AssetDatabase.GetAssetPath(database);
        string directory = Path.GetDirectoryName(databasePath)?.Replace("\\", "/") ?? "Assets";
        var dialogueEvent = CreateInstance<DialogueEventData>();
        dialogueEvent.eventId = "new_dialogue_event";
        dialogueEvent.eventType = DialogueEventType.Conversation;
        dialogueEvent.triggerType = DialogueTriggerType.Manual;
        dialogueEvent.lines.Add(new DialogueEventData.Line { speaker = DialogueSpeaker.Player, text = "新台词" });
        string path = AssetDatabase.GenerateUniqueAssetPath(directory + "/DialogueEvent_New.asset");
        AssetDatabase.CreateAsset(dialogueEvent, path);

        Undo.RecordObject(database, "添加对话事件");
        database.events.Add(dialogueEvent);
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        Selection.activeObject = dialogueEvent;
    }
}

[CustomEditor(typeof(BossDialogueProfile))]
public sealed class BossDialogueProfileEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("bossId"), new GUIContent("Boss ID"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("displayName"), new GUIContent("显示名称"));
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("portrait"), new GUIContent("Boss 头像"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("portraitFrame"), new GUIContent("头像框"));
        serializedObject.ApplyModifiedProperties();
    }
}
