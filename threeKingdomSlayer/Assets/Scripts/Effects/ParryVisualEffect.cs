using DG.Tweening;
using UnityEngine;

public sealed class ParryVisualEffect : MonoBehaviour
{
    private Sequence _sequence;
    private Material _weaponMaterial;
    private GameObject _weapon;
    private Transform _weaponPivot;
    private bool _destroying;

    public static void Create(GameObject weaponPrefab, Vector3 playerPosition, AttackSkillConfig config)
    {
        if (!Application.isPlaying || weaponPrefab == null || config == null)
            return;

        GameObject root = new GameObject("ParryVisualEffect");
        root.transform.position = playerPosition;
        root.AddComponent<ParryVisualEffect>().Initialize(weaponPrefab, config);
    }

    private void Initialize(GameObject weaponPrefab, AttackSkillConfig config)
    {
        Camera mainCamera = Camera.main;
        Vector3 guardPosition = new Vector3(
            config.parrySpawnXOffset,
            config.parrySpawnYOffset,
            config.parrySpawnZOffset);

        float angleVariance = Mathf.Clamp(config.parryAngleVariance, 0f, 30f);
        float guardAngle = 0f + Random.Range(-angleVariance * 0.1f, angleVariance * 0.1f);
        Quaternion guardRotation = Quaternion.Euler(54f, 270f, guardAngle);
        Quaternion entryRotation = Quaternion.Euler(54f, 270f, guardAngle - 48f);

        Vector3 cameraBack = mainCamera != null ? -mainCamera.transform.forward : Vector3.back;
        Vector3 cameraDown = mainCamera != null ? -mainCamera.transform.up : Vector3.down;
        Vector3 entryDirection = (cameraBack + cameraDown * 0.45f).normalized;
        Vector3 entryPosition = guardPosition + entryDirection * 1.8f;
        Vector3 recoilPosition = guardPosition + cameraBack * 0.28f;

        _weapon = Instantiate(weaponPrefab, transform);
        _weapon.name = "ParryGuardWeapon";
        foreach (var collider in _weapon.GetComponentsInChildren<Collider>(true))
            Destroy(collider);
        foreach (var collider2D in _weapon.GetComponentsInChildren<Collider2D>(true))
            Destroy(collider2D);

        Vector3 targetScale = _weapon.transform.localScale;
        Vector3 guardScale = targetScale;
        _weaponPivot = new GameObject("ParryGuardWeaponPivot").transform;
        _weaponPivot.SetParent(transform, false);
        _weaponPivot.localPosition = entryPosition;
        _weaponPivot.localRotation = entryRotation;
        _weapon.transform.SetParent(_weaponPivot, true);
        _weapon.transform.localPosition = Vector3.zero;
        _weapon.transform.localRotation = Quaternion.identity;
        _weapon.transform.localScale = targetScale * 0.82f;

        Quaternion guardWeaponRotation = guardRotation * Quaternion.Inverse(entryRotation);

        Renderer weaponRenderer = _weapon.GetComponentInChildren<Renderer>();
        if (weaponRenderer != null)
            _weaponMaterial = weaponRenderer.material;

        float totalDuration = Mathf.Max(0.66f, config.parrySweepDuration * 2.4f);
        float enterDuration = totalDuration * 0.18f;
        float guardTurnDuration = totalDuration * 0.21f;
        float guardDuration = totalDuration * 0.22f;
        float recoilDuration = totalDuration * 0.12f;
        float recoverDuration = totalDuration * 0.09f;
        float exitDuration = totalDuration * 0.18f;

        _sequence = DOTween.Sequence().SetTarget(transform).SetUpdate(UpdateType.Normal, false);
        _sequence.Append(_weaponPivot.DOLocalMove(guardPosition, enterDuration).SetEase(Ease.OutCubic));
        _sequence.Join(_weaponPivot.DOLocalRotate(guardRotation.eulerAngles, enterDuration, RotateMode.Fast)
            .SetEase(Ease.OutCubic));
        _sequence.Join(_weaponPivot.DOScale(Vector3.one, enterDuration).SetEase(Ease.OutQuad));
        _sequence.Join(_weapon.transform.DOScale(guardScale, enterDuration).SetEase(Ease.OutQuad));
        _sequence.Append(_weaponPivot.DOLocalRotate(
            new Vector3(0f, 0f, 220f), guardTurnDuration, RotateMode.FastBeyond360).SetEase(Ease.InOutCubic));
        _sequence.Join(_weapon.transform.DOLocalRotate(guardWeaponRotation.eulerAngles, guardTurnDuration, RotateMode.Fast)
            .SetEase(Ease.InOutCubic));
        _sequence.AppendInterval(guardDuration);
        _sequence.Append(_weaponPivot.DOLocalMove(recoilPosition, recoilDuration).SetEase(Ease.OutQuad));
        _sequence.Join(_weaponPivot.DOPunchRotation(new Vector3(0f, 0f, -4f), recoilDuration, 1, 0.2f));
        _sequence.Append(_weaponPivot.DOLocalMove(guardPosition, recoverDuration).SetEase(Ease.OutCubic));
        _sequence.Append(_weaponPivot.DOLocalMove(entryPosition, exitDuration).SetEase(Ease.InCubic));
        _sequence.Join(_weaponPivot.DOLocalRotate(entryRotation.eulerAngles, exitDuration, RotateMode.Fast)
            .SetEase(Ease.InCubic));
        _sequence.Join(_weapon.transform.DOScale(targetScale * 0.82f, exitDuration).SetEase(Ease.InQuad));
        if (_weaponMaterial != null)
            _sequence.Join(_weaponMaterial.DOFade(0f, exitDuration).SetEase(Ease.InQuad));

        _sequence.OnKill(() =>
        {
            if (!_destroying)
                Destroy(gameObject);
        });
        _sequence.OnComplete(() =>
        {
            _destroying = true;
            Destroy(gameObject);
        });
    }

    private void OnDestroy()
    {
        _destroying = true;
        _sequence?.Kill(false);
        if (_weaponMaterial != null)
            Destroy(_weaponMaterial);
    }
}
