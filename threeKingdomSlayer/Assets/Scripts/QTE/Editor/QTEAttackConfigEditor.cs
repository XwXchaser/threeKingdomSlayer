using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(QTEAttackConfig))]
public class QTEAttackConfigEditor : Editor
{
    private static Dictionary<QTEAttackConfig, float> _lastDuration = new();

    public override void OnInspectorGUI()
    {
        var config = (QTEAttackConfig)target;
        serializedObject.Update();
        float oldTotal = GetAnimationTotalDuration(config);

        // ── QTE 队列 ──
        EditorGUILayout.LabelField("QTE 队列", EditorStyles.boldLabel);
        SerializedProperty slotsProp = serializedObject.FindProperty("qteSlots");
        EditorGUILayout.PropertyField(slotsProp, true);

        // ── BOSS 演出 ──
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("BOSS 演出", EditorStyles.boldLabel);

        SerializedProperty clipProp = serializedObject.FindProperty("qteAnimationClip");
        EditorGUILayout.PropertyField(clipProp);

        // animationDuration with auto-compute hint
        float autoDuration = ComputeAutoDuration(config);
        SerializedProperty durProp = serializedObject.FindProperty("animationDuration");

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(durProp, new GUIContent("Animation Duration"));
        if (durProp.floatValue <= 0f)
        {
            EditorGUILayout.LabelField($"= {autoDuration:F2}s (auto)", GUILayout.Width(120));
        }
        else
        {
            EditorGUILayout.LabelField("(override)", GUILayout.Width(70));
        }
        EditorGUILayout.EndHorizontal();

        SerializedProperty leadProp = serializedObject.FindProperty("animationLeadTime");
        EditorGUILayout.PropertyField(leadProp);

        // ── 飞行物 ──
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("飞行物（可选）", EditorStyles.boldLabel);
        SerializedProperty projProp = serializedObject.FindProperty("projectilePrefab");
        SerializedProperty flightProp = serializedObject.FindProperty("projectileFlightTime");
        SerializedProperty targetZProp = serializedObject.FindProperty("projectileTargetZ");
        EditorGUILayout.PropertyField(projProp);
        EditorGUILayout.PropertyField(flightProp);
        EditorGUILayout.PropertyField(targetZProp);

        // ── 冷却 ──
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("冷却", EditorStyles.boldLabel);
        SerializedProperty cdProp = serializedObject.FindProperty("cooldownAfterQTE");
        EditorGUILayout.PropertyField(cdProp);

        serializedObject.ApplyModifiedProperties();

        // ── Change detection & auto-apply ──
        float newTotal = GetAnimationTotalDuration(config);
        if (!Mathf.Approximately(oldTotal, newTotal) && config.qteAnimationClip != null)
        {
            ApplyClipDuration(config, oldTotal, newTotal);
        }
        _lastDuration[config] = newTotal;

        // ── Clip sync bar ──
        if (config.qteAnimationClip != null)
        {
            EditorGUILayout.Space();
            float currentClipLength = config.qteAnimationClip.length;
            float targetLength = GetAnimationTotalDuration(config);
            EditorGUILayout.LabelField("Clip Sync", $"{currentClipLength:F3}s → {targetLength:F3}s");

            if (!Mathf.Approximately(currentClipLength, targetLength))
            {
                if (GUILayout.Button("Force Apply to Clip"))
                    ForceApplyClipDuration(config);
            }
        }
    }

    private void OnEnable()
    {
        var config = (QTEAttackConfig)target;
        _lastDuration[config] = GetAnimationTotalDuration(config);
    }

    private static float ComputeAutoDuration(QTEAttackConfig config)
    {
        return config.animationLeadTime + config.TotalDuration;
    }

    public static float GetAnimationTotalDuration(QTEAttackConfig config)
    {
        if (config.animationDuration > 0f)
            return config.animationDuration;
        return ComputeAutoDuration(config);
    }

    private static void ApplyClipDuration(QTEAttackConfig config, float oldDuration, float newDuration)
    {
        if (newDuration <= 0f || oldDuration <= 0f) return;

        var clip = config.qteAnimationClip;
        float scale = newDuration / oldDuration;

        var floatBindings = AnimationUtility.GetCurveBindings(clip);
        foreach (var binding in floatBindings)
        {
            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            if (curve == null) continue;
            var scaledKeys = new Keyframe[curve.keys.Length];
            for (int i = 0; i < curve.keys.Length; i++)
            {
                var k = curve.keys[i];
                k.time *= scale;
                k.inTangent /= scale;
                k.outTangent /= scale;
                scaledKeys[i] = k;
            }
            AnimationUtility.SetEditorCurve(clip, binding, new AnimationCurve(scaledKeys));
        }

        var objBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
        foreach (var binding in objBindings)
        {
            var keyframes = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            if (keyframes == null) continue;
            for (int i = 0; i < keyframes.Length; i++)
                keyframes[i].time *= scale;
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
        }

        var events = AnimationUtility.GetAnimationEvents(clip);
        if (events != null && events.Length > 0)
        {
            foreach (var evt in events)
                evt.time *= scale;
            AnimationUtility.SetAnimationEvents(clip, events);
        }

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.stopTime = newDuration;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssets();
    }

    public static void ForceApplyClipDuration(QTEAttackConfig config)
    {
        if (config.qteAnimationClip == null) return;
        float oldDuration = config.qteAnimationClip.length;
        float newDuration = GetAnimationTotalDuration(config);
        if (Mathf.Approximately(oldDuration, newDuration)) return;

        ApplyClipDuration(config, oldDuration, newDuration);
        _lastDuration[config] = newDuration;
        Debug.Log($"[QTEAttackConfigEditor] {config.name}: clip {oldDuration:F2}s → {newDuration:F2}s");
    }
}
