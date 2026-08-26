using System;
using System.Collections;
using UnityEngine;

public sealed class FakeMovementPresenter : MonoBehaviour
{
    [SerializeField] private bool showDebugPanel = true;
    [SerializeField] private string debugTitle = "假移动占位表现";

    private bool _playing;
    private bool _skipRequested;
    private float _remaining;
    private string _choiceLabel;

    public bool IsPlaying => _playing;

    public IEnumerator Play(FakeRouteChoiceConfig choice, Func<bool> canContinue)
    {
        _playing = true;
        _skipRequested = false;
        _remaining = Mathf.Max(0f, choice != null ? choice.placeholderDuration : 0f);
        _choiceLabel = choice != null ? choice.displayName : "未知路线";
        Debug.Log("[FakeRoute] placeholder presentation begin choice=" + _choiceLabel + " duration=" + _remaining.ToString("F2"));

        while (!_skipRequested && _remaining > 0f && canContinue())
        {
            _remaining -= Time.unscaledDeltaTime;
            yield return null;
        }

        bool skipped = _skipRequested;
        _playing = false;
        _skipRequested = false;
        _remaining = 0f;
        Debug.Log("[FakeRoute] placeholder presentation complete choice=" + _choiceLabel + " skipped=" + skipped);
    }

    public void Skip()
    {
        if (_playing) _skipRequested = true;
    }

    private void OnGUI()
    {
        if (!showDebugPanel || !_playing) return;
        GUILayout.BeginArea(new Rect(20f, 320f, 420f, 130f), GUI.skin.box);
        GUILayout.Label(debugTitle + "\n" + _choiceLabel + "\n剩余 " + Mathf.Max(0f, _remaining).ToString("F1") + " 秒");
        if (GUILayout.Button("跳过占位表现", GUILayout.Height(38f)))
            Skip();
        GUILayout.EndArea();
    }
}
