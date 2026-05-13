using System.Collections.Generic;
using UnityEngine;

public class NavMeshTileManager : MonoBehaviour
{
    public static NavMeshTileManager Instance { get; private set; }

    [Header("Performance")]
    [SerializeField] private int tilesPerFrame = 10;

    [Header("Anti-titileo")]
    [SerializeField] private float minActiveTime = 3f;

    private List<NavMeshTile> _tiles = new();
    private List<NavMeshPlayerTracker> _players = new();  // ahora guarda el tracker, no el Transform
    private Dictionary<NavMeshTile, float> _activationTimestamps = new();
    private int _currentTileIndex = 0;

    public float ActivationDistance => 0f;   // ya no aplica global — cada tracker tiene la suya
    public float DeactivationDistance => 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        _tiles.AddRange(FindObjectsOfType<NavMeshTile>());
        Debug.Log($"[NavMeshTileManager] Tiles encontrados: {_tiles.Count}");
        foreach (var tile in _tiles)
            tile?.DisableTile();
    }

    public void RegisterPlayer(NavMeshPlayerTracker tracker)
    {
        if (tracker == null || _players.Contains(tracker)) return;
        _players.Add(tracker);
        Debug.Log($"[NavMeshTileManager] Tracker registrado: {tracker.name} (total: {_players.Count})");
    }

    public void UnregisterPlayer(NavMeshPlayerTracker tracker)
    {
        if (_players.Remove(tracker))
            Debug.Log($"[NavMeshTileManager] Tracker removido: {tracker.name} (total: {_players.Count})");
    }

    // Compatibilidad con Init() por si algo externo aún lo llama
    public void Init(Transform[] playerRefs) { }

    private void Update()
    {
        _players.RemoveAll(p => p == null);

        if (_tiles.Count == 0) return;

        if (_players.Count == 0)
        {
            ProcessDisableOnly();
            return;
        }

        int tilesToProcess = Mathf.Min(tilesPerFrame, _tiles.Count);
        for (int i = 0; i < tilesToProcess; i++)
        {
            int idx = (_currentTileIndex + i) % _tiles.Count;
            var tile = _tiles[idx];
            if (tile == null) continue;

            // Chequear contra cada tracker con sus propias distancias
            bool anyPlayerClose = false;
            bool allPlayersFar = true;

            Bounds bounds = tile.GetBounds();

            foreach (var tracker in _players)
            {
                if (tracker == null) continue;

                Vector3 closestPoint = bounds.ClosestPoint(tracker.transform.position);
                float distSqr = (tracker.transform.position - closestPoint).sqrMagnitude;

                if (distSqr < tracker.ActivationDistance * tracker.ActivationDistance)
                    anyPlayerClose = true;

                if (distSqr < tracker.DeactivationDistance * tracker.DeactivationDistance)
                    allPlayersFar = false;
            }

            if (!tile.IsActive && anyPlayerClose)
            {
                tile.EnableTile();
                _activationTimestamps[tile] = Time.time;
            }
            else if (tile.IsActive && allPlayersFar)
            {
                if (Time.time - _activationTimestamps.GetValueOrDefault(tile, 0f) >= minActiveTime)
                {
                    tile.DisableTile();
                    _activationTimestamps.Remove(tile);
                }
            }
        }

        _currentTileIndex = (_currentTileIndex + tilesToProcess) % _tiles.Count;
    }

    private void ProcessDisableOnly()
    {
        int tilesToProcess = Mathf.Min(tilesPerFrame, _tiles.Count);
        for (int i = 0; i < tilesToProcess; i++)
        {
            int idx = (_currentTileIndex + i) % _tiles.Count;
            var tile = _tiles[idx];
            if (tile != null && tile.IsActive) tile.DisableTile();
        }
        _currentTileIndex = (_currentTileIndex + tilesToProcess) % _tiles.Count;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}