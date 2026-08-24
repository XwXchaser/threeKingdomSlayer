using System.Collections;
using UnityEngine;

public sealed class RouteWorldMotion : MonoBehaviour
{
    [SerializeField] private Transform routeWorldRoot;
    [SerializeField] private bool disableCameraFeedbackDuringTravel = true;

    public bool IsPlaying { get; private set; }


    public void PlayChannel(RouteWorldChannelPoint channel, Vector3 sourceNodeLocalPosition, Quaternion sourceNodeLocalRotation, Vector3 targetNodeLocalPosition, Quaternion targetNodeLocalRotation, Vector3 turnPivotLocalPosition, System.Action onComplete = null)
    {
        if (IsPlaying || channel == null || routeWorldRoot == null || !channel.TryValidate(out _))
        {
            Debug.LogWarning("[RouteTravelDiag] PlayChannel rejected playing=" + IsPlaying + " channel=" + (channel != null ? channel.channelId : "NULL") + " root=" + (routeWorldRoot != null ? routeWorldRoot.name : "NULL"));
            return;
        }
        Debug.Log("[RouteTravelDiag] PlayChannel start=" + channel.channelId + " root=" + routeWorldRoot.name + " pos=" + routeWorldRoot.position + " rot=" + routeWorldRoot.rotation.eulerAngles + " pathDelta=" + (channel.pathPoints[channel.pathPoints.Length - 1].position - channel.pathPoints[0].position));
        StartCoroutine(PlayRoutine(channel, sourceNodeLocalPosition, sourceNodeLocalRotation, targetNodeLocalPosition, targetNodeLocalRotation, turnPivotLocalPosition, onComplete));
    }

    public void Stop()
    {
        StopAllCoroutines();
        IsPlaying = false;
    }

    private IEnumerator PlayRoutine(RouteWorldChannelPoint channel, Vector3 sourceNodeLocalPosition, Quaternion sourceNodeLocalRotation, Vector3 targetNodeLocalPosition, Quaternion targetNodeLocalRotation, Vector3 turnPivotLocalPosition, System.Action onComplete)
    {
        IsPlaying = true;
        var feedback = disableCameraFeedbackDuringTravel ? FindObjectOfType<CameraFeedbackController>() : null;
        bool feedbackWasEnabled = feedback != null && feedback.enabled;
        if (feedback != null) feedback.enabled = false;

        Vector3 rootStart = routeWorldRoot.position;
        Quaternion rootRotation = routeWorldRoot.rotation;
        Vector3 incoming = sourceNodeLocalRotation * Vector3.forward;
        Vector3 nodeDelta = targetNodeLocalPosition - sourceNodeLocalPosition;
        Vector3 outgoing = targetNodeLocalRotation * Vector3.forward;
        if (outgoing.sqrMagnitude < 0.0001f)
            outgoing = nodeDelta.sqrMagnitude > 0.0001f ? nodeDelta.normalized : incoming;
        float yawDelta = Vector3.SignedAngle(incoming, outgoing, Vector3.up);
        Quaternion targetRotation = rootRotation * Quaternion.Euler(0f, -yawDelta, 0f);
        Vector3 sourceAnchorWorld = rootStart + rootRotation * sourceNodeLocalPosition;
        Vector3 pivotLocal = turnPivotLocalPosition;
        float elapsed = 0f;
        if (channel.direction == RouteDirection.Left || channel.direction == RouteDirection.Right)
        {
            float approachDistance = Vector3.Distance(sourceNodeLocalPosition, pivotLocal);
            Vector3 approachTarget = rootStart - rootRotation * (pivotLocal - sourceNodeLocalPosition);
            float approachDuration = Mathf.Max(0.1f, approachDistance / 4f);
            while (elapsed < approachDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                routeWorldRoot.position = Vector3.Lerp(rootStart, approachTarget, Mathf.Clamp01(elapsed / approachDuration));
                if (Time.frameCount % 15 == 0)
                    Debug.Log("[RouteTravelPose] phase=approach t=" + (elapsed / approachDuration).ToString("F2") + " pos=" + routeWorldRoot.position + " rot=" + routeWorldRoot.rotation.eulerAngles);
                yield return null;
            }
            routeWorldRoot.position = approachTarget;
            Vector3 pivotWorld = approachTarget + rootRotation * pivotLocal;
            Debug.Log("[RouteTravelPose] phase=approach-complete pos=" + routeWorldRoot.position + " pivot=" + pivotWorld + " pivotLocal=" + pivotLocal + " incoming=" + incoming + " outgoing=" + outgoing);

            elapsed = 0f;
            Quaternion approachRotation = routeWorldRoot.rotation;
            Vector3 approachPosition = routeWorldRoot.position;
            while (elapsed < 1f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed);
                var rotation = Quaternion.Slerp(approachRotation, targetRotation, t);
                routeWorldRoot.rotation = rotation;
                routeWorldRoot.position = pivotWorld - rotation * pivotLocal;
                if (Time.frameCount % 15 == 0)
                    Debug.Log("[RouteTravelPose] phase=turn t=" + t.ToString("F2") + " pos=" + routeWorldRoot.position + " rot=" + routeWorldRoot.rotation.eulerAngles);
                yield return null;
            }
            routeWorldRoot.SetPositionAndRotation(pivotWorld - targetRotation * pivotLocal, targetRotation);
            Debug.Log("[RouteTravelPose] phase=turn-complete pos=" + routeWorldRoot.position + " rot=" + routeWorldRoot.rotation.eulerAngles);

            Vector3 exitTarget = sourceAnchorWorld - targetRotation * targetNodeLocalPosition;
            float exitDistance = Vector3.Distance(routeWorldRoot.position, exitTarget);
            float exitDuration = Mathf.Max(0.1f, exitDistance / 4f);
            elapsed = 0f;
            Vector3 exitStart = routeWorldRoot.position;
            while (elapsed < exitDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                routeWorldRoot.position = Vector3.Lerp(exitStart, exitTarget, Mathf.Clamp01(elapsed / exitDuration));
                if (Time.frameCount % 15 == 0)
                    Debug.Log("[RouteTravelPose] phase=exit t=" + (elapsed / exitDuration).ToString("F2") + " pos=" + routeWorldRoot.position + " rot=" + routeWorldRoot.rotation.eulerAngles);
                yield return null;
            }
            routeWorldRoot.position = exitTarget;
            Debug.Log("[RouteTravelPose] phase=exit-complete pos=" + routeWorldRoot.position + " rot=" + routeWorldRoot.rotation.eulerAngles + " target=" + exitTarget);
        }
        else
        {
            Vector3 targetPosition = sourceAnchorWorld - targetRotation * targetNodeLocalPosition;
            float distance = Vector3.Distance(rootStart, targetPosition);
            float duration = Mathf.Max(0.1f, distance / 4f);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                routeWorldRoot.position = Vector3.Lerp(rootStart, targetPosition, Mathf.Clamp01(elapsed / duration));
                if (Time.frameCount % 15 == 0)
                    Debug.Log("[RouteTravelPose] phase=forward t=" + (elapsed / duration).ToString("F2") + " pos=" + routeWorldRoot.position + " rot=" + routeWorldRoot.rotation.eulerAngles);
                yield return null;
            }
            routeWorldRoot.SetPositionAndRotation(targetPosition, targetRotation);
            Debug.Log("[RouteTravelPose] phase=forward-complete pos=" + routeWorldRoot.position + " rot=" + routeWorldRoot.rotation.eulerAngles);
        }


        IsPlaying = false;
        Debug.Log("[RouteTravelDiag] complete channel=" + channel.channelId + " pos=" + routeWorldRoot.position + " rot=" + routeWorldRoot.rotation.eulerAngles);
        if (feedback != null) feedback.enabled = feedbackWasEnabled;
        onComplete?.Invoke();
    }
}
