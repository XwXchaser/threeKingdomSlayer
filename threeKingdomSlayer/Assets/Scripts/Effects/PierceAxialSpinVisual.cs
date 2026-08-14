using UnityEngine;

public sealed class PierceAxialSpinVisual : MonoBehaviour
{
    private Transform _spinPivot;
    private SpriteRenderer _planeA;
    private SpriteRenderer _planeB;
    private Color _baseColor;
    private Camera _camera;
    private float _profileDuration;
    private float _profileElapsed;
    private float _startSpeed;
    private float _endSpeed;
    private float _fade = 1f;
    private float _endFadeElapsed;
    private float _endFadeDuration;
    private bool _endFading;
    private bool _flightBlurEnabled;
    private Vector3 _flightDirection;
    private WeaponMotionBlurController _blurA;
    private WeaponMotionBlurController _blurB;
    private PierceVortexVisual _vortexVisual;

    public static PierceAxialSpinVisual Create(Transform root, SpriteRenderer sourceRenderer)
    {
        if (root == null || sourceRenderer == null || sourceRenderer.sprite == null)
            return null;

        sourceRenderer.enabled = false;
        var controller = root.gameObject.AddComponent<PierceAxialSpinVisual>();
        controller.Initialize(sourceRenderer);
        return controller;
    }

    private void Initialize(SpriteRenderer sourceRenderer)
    {
        _camera = Camera.main;
        _baseColor = sourceRenderer.color;

        _spinPivot = new GameObject("Pierce_SpinPivot").transform;
        _spinPivot.SetParent(transform, false);

        _planeA = CreatePlane("Pierce_PlaneA", sourceRenderer, Quaternion.identity);
        _planeB = CreatePlane("Pierce_PlaneB", sourceRenderer, Quaternion.Euler(0f, 90f, 0f));
        UpdatePlaneAlpha();
    }

    private SpriteRenderer CreatePlane(string objectName, SpriteRenderer source, Quaternion rotation)
    {
        var plane = new GameObject(objectName).transform;
        plane.SetParent(_spinPivot, false);
        plane.localRotation = rotation;

        var renderer = plane.gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = source.sprite;
        renderer.sharedMaterial = source.sharedMaterial;
        renderer.color = source.color;
        renderer.flipX = source.flipX;
        renderer.flipY = source.flipY;
        renderer.sortingLayerID = source.sortingLayerID;
        renderer.sortingOrder = source.sortingOrder;
        return renderer;
    }

    public void SetSpinProfile(float duration, float startDegreesPerSecond, float endDegreesPerSecond)
    {
        _profileDuration = Mathf.Max(duration, 0.001f);
        _profileElapsed = 0f;
        _startSpeed = startDegreesPerSecond;
        _endSpeed = endDegreesPerSecond;
    }

    public void EnableFlightBlur(Vector3 worldDirection)
    {
        _flightDirection = worldDirection.sqrMagnitude > 0.0001f
            ? worldDirection.normalized
            : Vector3.forward;
        _blurA ??= new WeaponMotionBlurController(_planeA, 0.75f, 0.012f, 24f);
        _blurB ??= new WeaponMotionBlurController(_planeB, 0.75f, 0.012f, 24f);
        _blurA.ResetSampleWorld(_planeA.transform.position, _planeA.transform.rotation);
        _blurB.ResetSampleWorld(_planeB.transform.position, _planeB.transform.rotation);
        _blurA.SetStrength(14f);
        _blurB.SetStrength(14f);
        _vortexVisual ??= PierceVortexVisual.Create(transform);
        _flightBlurEnabled = true;
    }

    public void SetFade(float fade)
    {
        _fade = Mathf.Clamp01(fade);
        UpdatePlaneAlpha();
        _vortexVisual?.SetFade(_fade);
    }

    public void BeginEndFade(float duration)
    {
        if (_endFading)
            return;

        _endFading = true;
        _endFadeElapsed = 0f;
        _endFadeDuration = Mathf.Max(duration, 0.001f);
        _vortexVisual?.StopAfterimages();
    }

    private void Update()
    {
        if (_spinPivot == null)
            return;

        _profileElapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(_profileElapsed / _profileDuration);
        float speed = Mathf.Lerp(_startSpeed, _endSpeed, progress * progress);
        _spinPivot.Rotate(Vector3.up, speed * Time.deltaTime, Space.Self);
        if (_flightBlurEnabled)
        {
            float blurMultiplier = Mathf.Lerp(0.85f, 1.15f, progress);
            float fallbackSpeed = Mathf.Lerp(16f, 24f, progress);
            _blurA?.UpdateMotionWorld(_planeA.transform.position, _planeA.transform.rotation,
                _flightDirection, blurMultiplier, fallbackSpeed, Time.deltaTime);
            _blurB?.UpdateMotionWorld(_planeB.transform.position, _planeB.transform.rotation,
                _flightDirection, blurMultiplier, fallbackSpeed, Time.deltaTime);
        }

        if (_endFading)
        {
            _endFadeElapsed += Time.deltaTime;
            SetFade(1f - Mathf.Clamp01(_endFadeElapsed / _endFadeDuration));
            if (_endFadeElapsed >= _endFadeDuration)
            {
                _blurA?.SetStrength(0f);
                _blurB?.SetStrength(0f);
            }
        }

        UpdatePlaneAlpha();
    }

    private void UpdatePlaneAlpha()
    {
        if (_planeA == null || _planeB == null)
            return;

        if (_camera == null)
            _camera = Camera.main;

        float weightA = 1f;
        float weightB = 0f;
        if (_camera != null)
        {
            Vector3 viewDirection = _camera.transform.forward;
            float facingA = Mathf.Abs(Vector3.Dot(_planeA.transform.forward, viewDirection));
            float facingB = Mathf.Abs(Vector3.Dot(_planeB.transform.forward, viewDirection));
            weightA = facingA >= facingB ? 1f : 0f;
            weightB = 1f - weightA;
        }

        Color colorA = _baseColor;
        colorA.a *= _fade * weightA;
        _planeA.color = colorA;

        Color colorB = _baseColor;
        colorB.a *= _fade * weightB;
        _planeB.color = colorB;
    }

    private void OnDestroy()
    {
        _blurA?.Dispose();
        _blurA = null;
        _blurB?.Dispose();
        _blurB = null;
    }
}
