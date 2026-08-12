using System;
using DG.Tweening;
using UnityEngine;

public sealed class AttackReleaseTimeline : MonoBehaviour
{
    private Sequence _sequence;
    private Action _onRelease;
    private bool _released;
    private bool _completed;

    public static void Create(AttackType attackType, float actionDuration, Action onRelease)
    {
        if (onRelease == null)
            return;

        float duration = Mathf.Max(actionDuration, 0.01f);
        var root = new GameObject($"{attackType}_ReleaseTimeline");
        root.AddComponent<AttackReleaseTimeline>().Initialize(attackType, duration, onRelease);
    }

    private void Initialize(AttackType attackType, float duration, Action onRelease)
    {
        _onRelease = onRelease;
        float releaseRatio = attackType == AttackType.Pierce ? 0.42f : 0.48f;

        _sequence = DOTween.Sequence().SetTarget(transform).SetUpdate(UpdateType.Normal, false);
        _sequence.AppendInterval(duration * releaseRatio);
        _sequence.AppendCallback(Release);
        _sequence.AppendInterval(duration * (1f - releaseRatio));
        _sequence.OnKill(() =>
        {
            if (!_completed)
                Destroy(gameObject);
        });
        _sequence.OnComplete(() =>
        {
            _completed = true;
            Destroy(gameObject);
        });
    }

    private void Release()
    {
        if (_released)
            return;
        _released = true;
        Action callback = _onRelease;
        _onRelease = null;
        callback?.Invoke();
    }

    private void OnDestroy()
    {
        _sequence?.Kill(false);
        _sequence = null;
        _onRelease = null;
    }
}
