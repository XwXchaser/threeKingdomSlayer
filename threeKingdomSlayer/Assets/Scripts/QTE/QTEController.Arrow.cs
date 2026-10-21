using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
    /// QTEController 的**箭矢波（多段防御型 QTE）**。
    ///
    /// 按机制从 <c>QTEController.cs</c> 拆出（2026-10-22，partial 分文件）。
    /// partial class 与 QTEController.cs **同类型**，行为完全一致，只为降低单文件体量与阅读成本。
    /// </summary>

public partial class QTEController : MonoBehaviour
{
    #region 箭矢波（多段防御型 QTE）

    private void SpawnArrowWaveForSlot(int slotIndex, float overrideFlightTime = -1f)
    {
        if (_currentAttack == null || _currentAttack.arrowPrefab == null) return;
        if (_arrowWaves.ContainsKey(slotIndex)) return;
        if (slotIndex < 0 || slotIndex >= _activeQTEs.Count) return;

        var qte = _activeQTEs[slotIndex];
        if (qte.config == null) return;

        float row5Z = GetRow5ZPosition();
        float spawnZ = row5Z + _currentAttack.arrowSpawnOffsetZ;
        float offsetZ = StageFormation != null ? StageFormation.GetFormationOffsetZ() : 0f;
        float targetZ = offsetZ + _currentAttack.projectileTargetZ;
        float playerX = PlayerState.Instance != null ? PlayerState.Instance.transform.position.x : 0f;

        var wave = new List<EnemyProjectile>();
        int count = _currentAttack.arrowsPerWave;
        float baseDmg = qte.config.failureDamage > 0f ? qte.config.failureDamage : qte.config.poiseDamage;
        float dmgPerArrow = baseDmg / count;

        float jitter = arrowConfig != null ? arrowConfig.randomPositionJitter : 0.3f;
        float arcVar = arrowConfig != null ? arrowConfig.randomArcVariation : 0.15f;
        float staggerMax = arrowConfig != null ? arrowConfig.staggerMax : 0.12f;
        float maxDescentPitch = arrowConfig != null ? arrowConfig.maxDescentPitch : 89f;

        // 计算飞行时间：优先使用显式传入值，否则从 warningEndTime 反推
        float baseFlightTime;
        if (overrideFlightTime >= 0f)
        {
            baseFlightTime = overrideFlightTime;
        }
        else
        {
            float timeUntilWarningEnd = qte.warningEndTime - _qtePhaseTimer;
            baseFlightTime = Mathf.Max(timeUntilWarningEnd, 0.25f);
        }

        // 弧高钳制到可见范围（配置值可能过大，此处做视觉补偿）
        float arcHeightCap = 3.5f;
        float rawArcH = _currentAttack.arrowArcHeight;
        float arcH = Mathf.Min(rawArcH, arcHeightCap) * Random.Range(1f - arcVar, 1f + arcVar);

        for (int i = 0; i < count; i++)
        {
            float xOffset = count > 1 ? Mathf.Lerp(-_currentAttack.arrowSpreadX * 0.5f, _currentAttack.arrowSpreadX * 0.5f, (float)i / (count - 1)) : 0f;
            float spawnX = playerX + xOffset + Random.Range(-jitter, jitter);
            float spawnY = 1.5f + Random.Range(-jitter * 0.67f, jitter);
            float spawnZJitter = Random.Range(-jitter * 0.67f, jitter * 0.67f);
            Vector3 spawnPos = new Vector3(spawnX, spawnY, spawnZ + spawnZJitter);

            // 仅保留发射错峰；所有箭矢按同一绝对到达时刻计算飞行时间。
            float stagger = Random.Range(0f, staggerMax);
            float flightTime = Mathf.Max(baseFlightTime - stagger, 0.2f);

            var arrowObj = Instantiate(_currentAttack.arrowPrefab, spawnPos, Quaternion.identity);
            EnemyProjectileVisualPriority.Apply(arrowObj);
            var projectile = arrowObj.GetComponent<EnemyProjectile>();
            if (projectile != null)
            {
                projectile.isQTEProjectile = true;
                if (stagger > 0.001f)
                {
                    arrowObj.SetActive(false);
                    float d = dmgPerArrow;
                    float tz = targetZ;
                    float sx = spawnX;
                    float ah = arcH;
                    float ft = flightTime;
                    EnemyProjectile p = projectile;
                    Tween launchDelay = null;
                    launchDelay = DOVirtual.DelayedCall(stagger, () =>
                    {
                        _arrowLaunchDelays.Remove(launchDelay);
                        if (p != null)
                        {
                            p.gameObject.SetActive(true);
                            p.Launch(spawnPos, tz, sx, d, ah, ft, null,
                                _currentAttack.arrowTargetY, maxDescentPitch);
                        }
                    });
                    _arrowLaunchDelays.Add(launchDelay);
                }
                else
                {
                    projectile.Launch(spawnPos, targetZ, spawnX, dmgPerArrow, arcH, flightTime, null,
                        _currentAttack.arrowTargetY, maxDescentPitch);
                }
            }
            wave.Add(projectile);
        }

        _arrowWaves[slotIndex] = wave;
        _arrowWavesSpawned = true;
    }

    private void DeflectArrowWave(int slotIndex)
    {
        if (!_arrowWaves.TryGetValue(slotIndex, out var wave)) return;
        foreach (var p in wave)
        {
            if (p != null) p.Deflect();
        }
    }

    private void ClearAllArrowWaves()
    {
        for (int i = 0; i < _arrowLaunchDelays.Count; i++)
        {
            if (_arrowLaunchDelays[i] != null && _arrowLaunchDelays[i].IsActive())
                _arrowLaunchDelays[i].Kill();
        }
        _arrowLaunchDelays.Clear();

        foreach (var kv in _arrowWaves)
        {
            foreach (var p in kv.Value)
            {
                if (p != null) Destroy(p.gameObject);
            }
        }
        _arrowWaves.Clear();
    }

    private float GetRow5ZPosition()
    {
        float offsetZ = StageFormation != null ? StageFormation.GetFormationOffsetZ() : 0f;
        float rowSpacing = StageFormation != null ? StageFormation.GetRowSpacing() : 2.5f;
        int maxRow = StageFormation != null ? StageFormation.GetMaxVisibleRows() - 1 : 4;
        // 使用与 Enemy.GetRowZ 一致的公式: (maxRow - row) * (-spacing) + offset
        return (maxRow - 5) * (-rowSpacing) + offsetZ;
    }

    #endregion
}