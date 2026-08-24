using System.Collections;
using UnityEngine;

public sealed class RouteWorldMotionPrototype : MonoBehaviour
{
    [SerializeField] private Transform routeWorldRoot;
    [SerializeField] private Transform junctionPivot;
    [SerializeField] private float travelSpeed = 4f;
    [SerializeField] private float turnDuration = 1f;

    private CameraFeedbackController _cameraFeedback;
    private bool _cameraFeedbackWasEnabled;

    public bool IsPlaying { get; private set; }

    public void PlayForward(float distance, System.Action onComplete = null)
    {
        if (IsPlaying || routeWorldRoot == null)
            return;
        StartCoroutine(PlayForwardRoutine(distance, onComplete));
    }

    public void PlayLeftTurn(System.Action onComplete = null)
    {
        if (IsPlaying || routeWorldRoot == null || junctionPivot == null)
            return;
        StartCoroutine(PlayLeftTurnRoutine(onComplete));
    }

    public void PlayForwardThenLeftTurn()
    {
        if (IsPlaying || routeWorldRoot == null || junctionPivot == null)
            return;
        StartCoroutine(PlayForwardThenLeftRoutine());
    }

    public void PlayToPose(Vector3 targetPosition, float targetYaw, System.Action onComplete = null)
    {
        if (IsPlaying || routeWorldRoot == null)
            return;
        StartCoroutine(MoveToPoseRoutine(targetPosition, Quaternion.Euler(0f, targetYaw, 0f), onComplete));
    }

    public void StopMotion()
    {
        StopAllCoroutines();
        IsPlaying = false;
        if (_cameraFeedback != null)
            _cameraFeedback.enabled = _cameraFeedbackWasEnabled;
    }

    private IEnumerator PlayForwardThenLeftRoutine()
    {
        yield return PlayForwardRoutine(8f, null);
        yield return PlayLeftTurnRoutine(null);
    }


    private IEnumerator PlayForwardRoutine(float distance, System.Action onComplete)
    {
        IsPlaying = true;
        var feedback = FindObjectOfType<CameraFeedbackController>();
        bool wasEnabled = feedback != null && feedback.enabled;
        if (feedback != null) feedback.enabled = false;
        yield return MoveRoot(routeWorldRoot.position, routeWorldRoot.position + Vector3.back * distance, travelSpeed);
        IsPlaying = false;
        if (feedback != null) feedback.enabled = wasEnabled;
        onComplete?.Invoke();
    }
    private IEnumerator PlayLeftTurnRoutine(System.Action onComplete)
    {
        IsPlaying = true;
        var feedback = FindObjectOfType<CameraFeedbackController>();
        bool wasEnabled = feedback != null && feedback.enabled;
        if (feedback != null) feedback.enabled = false;
        Vector3 pivot = junctionPivot.position;
        Quaternion startRotation = routeWorldRoot.rotation;
        Quaternion targetRotation = startRotation * Quaternion.Euler(0f, -90f, 0f);
        Vector3 targetPosition = pivot + targetRotation * Quaternion.Inverse(startRotation) * (routeWorldRoot.position - pivot);
        yield return MoveToPoseRoutine(targetPosition, targetRotation, null);
        IsPlaying = false;
        if (feedback != null) feedback.enabled = wasEnabled;
        onComplete?.Invoke();
    }



    private IEnumerator MoveToPoseRoutine(Vector3 targetPosition, Quaternion targetRotation, System.Action onComplete)
    {
        IsPlaying = true;
        var feedback = FindObjectOfType<CameraFeedbackController>();
        bool wasEnabled = feedback != null && feedback.enabled;
        if (feedback != null) feedback.enabled = false;
        Vector3 startPosition = routeWorldRoot.position;
        Quaternion startRotation = routeWorldRoot.rotation;
        float elapsed = 0f;
        while (elapsed < turnDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, turnDuration));
            routeWorldRoot.SetPositionAndRotation(Vector3.Lerp(startPosition, targetPosition, t), Quaternion.Slerp(startRotation, targetRotation, t));
            yield return null;
        }
        routeWorldRoot.SetPositionAndRotation(targetPosition, targetRotation);
        IsPlaying = false;
        if (feedback != null) feedback.enabled = wasEnabled;
        onComplete?.Invoke();
    }


    private IEnumerator MoveRoot(Vector3 from, Vector3 to, float speed)
    {
        float distance = Vector3.Distance(from, to);
        float duration = distance / Mathf.Max(0.01f, speed);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            routeWorldRoot.position = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        routeWorldRoot.position = to;
    }
}
