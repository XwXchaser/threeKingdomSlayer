using UnityEngine;

public class EnemyProjectileVisualPriority : MonoBehaviour
{
    public const string SortingLayerName = "EnemyProjectiles";

    private void Awake()
    {
        Apply(gameObject);
    }

    private void OnEnable()
    {
        Apply(gameObject);
    }

    public static void Apply(GameObject root)
    {
        if (root == null) return;

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].sortingLayerName = SortingLayerName;
    }
}
