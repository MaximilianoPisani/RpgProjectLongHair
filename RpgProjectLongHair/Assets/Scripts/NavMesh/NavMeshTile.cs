using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshTile : MonoBehaviour
{
    [Header("Área del tile")]
    [SerializeField] private Vector3 tileSize = new Vector3(50f, 10f, 50f);

    private NavMeshSurface _surface;
    private NavMeshDataInstance _instance;
    private bool _isActive;
    public bool IsActive => _isActive;

    private void Awake()
    {
        _surface = GetComponent<NavMeshSurface>();
    }

    public void EnableTile()
    {
        if (_isActive) return;

        if (_surface.navMeshData == null)
        {
            Debug.LogError($"[NavMeshTile] {name}: navMeshData es NULL — ¿bakeaste este tile en el editor?");
            return;
        }

        _instance = NavMesh.AddNavMeshData(_surface.navMeshData);
        _isActive = true;
    }

    public void DisableTile()
    {
        if (!_isActive) return;
        NavMesh.RemoveNavMeshData(_instance);
        _isActive = false;
    }

    public Bounds GetBounds() => new Bounds(transform.position, tileSize);

    private void OnDestroy()
    {
        if (_isActive) NavMesh.RemoveNavMeshData(_instance);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Bounds bounds = GetBounds();

        Gizmos.color = _isActive
            ? new Color(0f, 1f, 0f, 0.15f)
            : new Color(1f, 1f, 1f, 0.05f);
        Gizmos.DrawCube(bounds.center, bounds.size);

        Gizmos.color = _isActive
            ? new Color(0f, 1f, 0f, 0.8f)
            : new Color(0.5f, 0.5f, 0.5f, 0.3f);
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
#endif
}