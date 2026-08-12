using UnityEngine;

public sealed class WeaponMotionBlurController
{
    private static readonly int MainTextureId = Shader.PropertyToID("_MainTex");
    private static readonly int MotionDirectionId = Shader.PropertyToID("_MotionDirectionUV");
    private static readonly int MotionStrengthId = Shader.PropertyToID("_MotionStrengthPixels");
    private static Material _sharedMaterial;
    private static bool _missingMaterialLogged;

    private readonly SpriteRenderer _renderer;
    private readonly Material _materialInstance;
    private readonly Material _originalMaterial;
    private readonly float _linearStrengthScale;
    private readonly float _angularStrengthScale;
    private readonly float _maxStrengthPixels;

    private Vector3 _lastPosition;
    private float _lastAngle;
    private Quaternion _lastRotation;
    private bool _hasSample;
    private bool _disposed;

    public bool IsValid => !_disposed && _renderer != null && _materialInstance != null;

    public WeaponMotionBlurController(SpriteRenderer renderer, float linearStrengthScale,
        float angularStrengthScale, float maxStrengthPixels)
    {
        _renderer = renderer;
        _linearStrengthScale = linearStrengthScale;
        _angularStrengthScale = angularStrengthScale;
        _maxStrengthPixels = maxStrengthPixels;

        if (_renderer == null)
            return;

        if (_sharedMaterial == null)
            _sharedMaterial = Resources.Load<Material>("Materials/WeaponDirectionalPixelBlur");
        if (_sharedMaterial == null)
        {
            if (!_missingMaterialLogged)
            {
                Debug.LogWarning("[WeaponMotionBlur] Resources/Materials/WeaponDirectionalPixelBlur 未找到");
                _missingMaterialLogged = true;
            }
            return;
        }

        _originalMaterial = _renderer.sharedMaterial;
        _materialInstance = new Material(_sharedMaterial);
        _materialInstance.SetTexture(MainTextureId, _renderer.sprite != null ? _renderer.sprite.texture : null);
        _materialInstance.SetVector(MotionDirectionId, new Vector4(1f, 0f, 0f, 0f));
        _materialInstance.SetFloat(MotionStrengthId, 0f);
        _renderer.material = _materialInstance;
        ResetSample(_renderer.transform.position, _renderer.transform.eulerAngles.z);
    }

    public void ResetSample(Vector3 worldPosition, float worldAngle)
    {
        _lastPosition = worldPosition;
        _lastAngle = worldAngle;
        _hasSample = true;
        SetStrength(0f);
    }

    public void UpdateMotion(Vector3 worldPosition, float worldAngle,
        Vector3 fallbackWorldDirection, float strengthMultiplier = 1f, float fallbackSpeed = 0f,
        float deltaTimeOverride = -1f)
    {
        if (!IsValid)
            return;

        if (!_hasSample)
        {
            ResetSample(worldPosition, worldAngle);
            return;
        }

        float deltaTime = deltaTimeOverride > 0f ? deltaTimeOverride : Time.deltaTime;
        Vector3 delta = worldPosition - _lastPosition;
        float angleDelta = Mathf.DeltaAngle(_lastAngle, worldAngle);
        _lastPosition = worldPosition;
        _lastAngle = worldAngle;

        if (deltaTime <= 0.00001f)
            return;

        Vector3 motionDirection = delta.sqrMagnitude > 0.000001f
            ? delta.normalized
            : fallbackWorldDirection.normalized;
        if (motionDirection.sqrMagnitude < 0.0001f)
        {
            SetStrength(0f);
            return;
        }

        Vector3 localX = _renderer.transform.localToWorldMatrix.MultiplyVector(Vector3.right).normalized;
        Vector3 localY = _renderer.transform.localToWorldMatrix.MultiplyVector(Vector3.up).normalized;
        Vector2 directionUv = new Vector2(
            Vector3.Dot(motionDirection, localX),
            Vector3.Dot(motionDirection, localY));
        if (directionUv.sqrMagnitude < 0.0001f)
            directionUv = Vector2.right;
        else
            directionUv.Normalize();

        float linearSpeed = delta.magnitude / deltaTime;
        float angularSpeed = Mathf.Abs(angleDelta) / deltaTime;
        float strength = (Mathf.Max(linearSpeed, fallbackSpeed) * _linearStrengthScale
            + angularSpeed * _angularStrengthScale) * Mathf.Max(0f, strengthMultiplier);
        strength = Mathf.Clamp(strength, 0f, _maxStrengthPixels);

        _materialInstance.SetVector(MotionDirectionId, new Vector4(directionUv.x, directionUv.y, 0f, 0f));
        _materialInstance.SetFloat(MotionStrengthId, strength);
    }

    public void UpdateMotionWorld(Vector3 worldPosition, Quaternion worldRotation,
        Vector3 fallbackWorldDirection, float strengthMultiplier = 1f, float fallbackSpeed = 0f,
        float deltaTimeOverride = -1f)
    {
        if (!IsValid)
            return;

        if (!_hasSample)
        {
            ResetSampleWorld(worldPosition, worldRotation);
            return;
        }

        float deltaTime = deltaTimeOverride > 0f ? deltaTimeOverride : Time.deltaTime;
        Vector3 delta = worldPosition - _lastPosition;
        float angleDelta = Quaternion.Angle(_lastRotation, worldRotation);
        _lastPosition = worldPosition;
        _lastRotation = worldRotation;

        if (deltaTime <= 0.00001f)
            return;

        Vector3 motionDirection = delta.sqrMagnitude > 0.000001f
            ? delta.normalized
            : fallbackWorldDirection.normalized;
        if (motionDirection.sqrMagnitude < 0.0001f)
        {
            SetStrength(0f);
            return;
        }

        Vector3 localX = _renderer.transform.localToWorldMatrix.MultiplyVector(Vector3.right).normalized;
        Vector3 localY = _renderer.transform.localToWorldMatrix.MultiplyVector(Vector3.up).normalized;
        Vector2 directionUv = new Vector2(
            Vector3.Dot(motionDirection, localX),
            Vector3.Dot(motionDirection, localY));
        if (directionUv.sqrMagnitude < 0.0001f)
            directionUv = Vector2.right;
        else
            directionUv.Normalize();

        float linearSpeed = delta.magnitude / deltaTime;
        float angularSpeed = angleDelta / deltaTime;
        float strength = (Mathf.Max(linearSpeed, fallbackSpeed) * _linearStrengthScale
            + angularSpeed * _angularStrengthScale) * Mathf.Max(0f, strengthMultiplier);
        strength = Mathf.Clamp(strength, 0f, _maxStrengthPixels);

        _materialInstance.SetVector(MotionDirectionId, new Vector4(directionUv.x, directionUv.y, 0f, 0f));
        _materialInstance.SetFloat(MotionStrengthId, strength);
    }

    public void ResetSampleWorld(Vector3 worldPosition, Quaternion worldRotation)
    {
        _lastPosition = worldPosition;
        _lastRotation = worldRotation;
        _hasSample = true;
        SetStrength(0f);
    }

    public void SetStrength(float strengthPixels)
    {
        if (!IsValid)
            return;

        if (_renderer.sprite != null)
            _materialInstance.SetTexture(MainTextureId, _renderer.sprite.texture);
        _materialInstance.SetFloat(MotionStrengthId, Mathf.Clamp(strengthPixels, 0f, _maxStrengthPixels));
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        if (_renderer == null)
            return;

        _renderer.material = _originalMaterial;
        if (_materialInstance != null)
            Object.Destroy(_materialInstance);
    }
}
