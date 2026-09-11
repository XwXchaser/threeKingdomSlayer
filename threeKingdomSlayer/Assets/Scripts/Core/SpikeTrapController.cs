using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpikeTrapController : MonoBehaviour
{
    public static SpikeTrapController Instance { get; private set; }

    [Header("美术素材")]
    [SerializeField] private Sprite baseSprite;
    [SerializeField] private Sprite hitSprite;

    [Header("动画时序")]
    [SerializeField] private float hitDuration = 0.2f;

    [Header("位置与缩放")]
    [SerializeField] private float zOffset = 0f;
    [SerializeField] private float yOffset = 0f;
    [SerializeField] private float visualScale = 1f;

    [Header("遮挡描边")]
    [SerializeField] private Material outlineMaterial;
    [SerializeField] private Color outlineColor = new Color(0.85f, 0.85f, 0.85f, 1f);
    [SerializeField] private float outlineWidth = 2f;
    [SerializeField] private int outlineSortingOrder = 100;

    private int _spikeRow;
    private int _spikeCol;
    private float _damagePerPass;
    private GameObject _visualGo;
    private GameObject _baseChild;
    private GameObject _hitChild;
    private SpriteRenderer _hitSr;
    private GameObject _outlineGo;
    private SpriteRenderer _outlineSr;
    private bool _animating;
    private Coroutine _spawnCoroutine;
    private HashSet<Enemy> _triggeredThisFrame = new HashSet<Enemy>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void LateUpdate()
    {
        _triggeredThisFrame.Clear();
        UpdateOutlineOverlay();
    }

    public bool IsActive => _visualGo != null;

    public void Initialize(int row, int col, float damage)
    {
        _spikeRow = row;
        _spikeCol = col;
        _damagePerPass = damage;
        RequestVisual();
    }

    public void ReactivateVisual()
    {
        RequestVisual();
    }

    private void RequestVisual()
    {
        if (!CanBeActive || _damagePerPass <= 0f)
        {
            DeactivateVisual();
            return;
        }

        if (_visualGo != null || _spawnCoroutine != null)
            return;
        _spawnCoroutine = StartCoroutine(SpawnVisualWhenReady());
    }

    private IEnumerator SpawnVisualWhenReady()
    {
        while (CanBeActive && _visualGo == null)
        {
            var enemies = EnemyManager.Instance?.columnManager?.GetAllEnemies();
            if (enemies != null && enemies.Count > 0)
            {
                for (int i = 0; i < enemies.Count; i++)
                {
                    if (enemies[i] != null)
                    {
                        SpawnVisual();
                        _spawnCoroutine = null;
                        yield break;
                    }
                }
            }
            yield return null;
        }
        _spawnCoroutine = null;
    }

    public void DeactivateVisual()
    {
        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
        }
        StopAllCoroutines();
        _animating = false;
        _triggeredThisFrame.Clear();
        if (_visualGo != null)
            Destroy(_visualGo);
        _visualGo = null;
        _baseChild = null;
        _hitChild = null;
        _hitSr = null;
        _outlineGo = null;
        _outlineSr = null;
    }

    private bool CanBeActive => PlayerState.Instance != null
        && PlayerState.Instance.stageState == StageState.InProgress
        && (StageController.Instance == null || StageController.Instance.IsCombatActive);

    public void SetDamage(float newDamage)
    {
        _damagePerPass = newDamage;
        RequestVisual();
    }

    public void CheckAndTrigger(Enemy enemy)
    {
        if (!IsActive || !CanBeActive) return;
        if (enemy == null || enemy.state == EnemyState.Dead) return;
        if (enemy.columnIndex != _spikeCol || enemy.rowIndex != _spikeRow) return;
        if (!_triggeredThisFrame.Add(enemy)) return;

        if (_animating)
            enemy.TakeDamage(_damagePerPass, DamageType.Pierce, new Color(1f, 0.6f, 0f), feedbackSource: HitFeedbackSource.SpikeTrap);
        else
            StartCoroutine(TriggerAnimation(enemy));
    }

    public void ResetAll()
    {
        DeactivateVisual();
        _spikeRow = 0;
        _spikeCol = 0;
        _damagePerPass = 0f;
    }

    private void SpawnVisual()
    {
        if (_visualGo != null) return;

        Transform parent = null;
        var enemies = EnemyManager.Instance?.columnManager?.GetAllEnemies();
        if (enemies != null && enemies.Count > 0)
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null)
                {
                    parent = enemies[i].transform.parent;
                    break;
                }
            }
        }
        if (parent == null)
            return;

        _visualGo = new GameObject("SpikeTrap_Visual");
        _visualGo.transform.SetParent(parent, worldPositionStays: false);

        Vector3 localPos = GetLocalPosition(_spikeRow, _spikeCol);
        _visualGo.transform.localPosition = localPos;
        _visualGo.transform.localScale = Vector3.one * visualScale;

        // 与敌人同级 sortingOrder=0，Z 位置（zOffset）决定与前后排敌人的遮挡
        int baseOrder = 0;
        int hitOrder = 0;

        _baseChild = CreateChild("Base", baseSprite, baseOrder);
        _hitChild = CreateChild("Hit", hitSprite, hitOrder);
        _hitSr = _hitChild.GetComponent<SpriteRenderer>();
        _hitChild.SetActive(false);

        if (outlineMaterial != null)
        {
            _outlineGo = CreateChild("OutlineOverlay", baseSprite, outlineSortingOrder, outlineMaterial);
            _outlineSr = _outlineGo.GetComponent<SpriteRenderer>();
            _outlineSr.sharedMaterial.SetColor("_OutlineColor", outlineColor);
            _outlineSr.sharedMaterial.SetFloat("_OutlineWidth", outlineWidth);
            _outlineGo.SetActive(false);
        }
        else
        {
            _outlineGo = null;
            _outlineSr = null;
        }
    }

    private GameObject CreateChild(string name, Sprite sprite, int order, Material mat = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_visualGo.transform, worldPositionStays: false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        if (mat != null)
            sr.sharedMaterial = mat;
        return go;
    }

    private IEnumerator TriggerAnimation(Enemy enemy)
    {
        _animating = true;

        if (_hitChild != null)
            _hitChild.SetActive(true);

        yield return new WaitForSeconds(hitDuration);

        if (enemy != null && enemy.state != EnemyState.Dead)
            enemy.TakeDamage(_damagePerPass, DamageType.Pierce, new Color(1f, 0.6f, 0f), feedbackSource: HitFeedbackSource.SpikeTrap);

        yield return new WaitForSeconds(0.1f);

        if (_hitChild != null)
            _hitChild.SetActive(false);

        _animating = false;
    }

    private Vector3 GetLocalPosition(int row, int col)
    {
        float xPos = 0f;
        float zPos = 0f;

        if (StageController.Instance != null)
        {
            xPos = StageController.Instance.GetFormationOffset(col, row);

            float rowSpacing = StageController.Instance.GetRowSpacing();
            float offsetZ = StageController.Instance.GetFormationOffsetZ();
            int maxRow = StageController.Instance.GetMaxVisibleRows() - 1;
            zPos = (maxRow - row) * (-rowSpacing) + offsetZ;
        }

        return new Vector3(xPos, yOffset, zPos + zOffset);
    }

    private void UpdateOutlineOverlay()
    {
        if (_outlineGo == null) return;

        bool occluded = false;
        var cm = EnemyManager.Instance?.columnManager;
        if (cm != null && _spikeRow > 0)
        {
            var enemies = cm.GetEnemiesInColumn(_spikeCol);
            if (enemies != null)
            {
                for (int i = 0; i < enemies.Count; i++)
                {
                    var enemy = enemies[i];
                    if (enemy != null && enemy.state != EnemyState.Dead && enemy.rowIndex < _spikeRow)
                    {
                        occluded = true;
                        break;
                    }
                }
            }
        }

        _outlineGo.SetActive(occluded);
    }
}
