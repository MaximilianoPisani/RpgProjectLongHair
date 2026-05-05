using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using NavMeshBuilder = UnityEngine.AI.NavMeshBuilder;
using Fusion;
using Unity.AI.Navigation;

public class AreaFloorBaker : MonoBehaviour
{
    public static AreaFloorBaker Instance { get; private set; }

    [Header("NavMesh Surface de referencia (settings)")]
    [SerializeField] private NavMeshSurface Surface;

    [Header("Configuración")]
    [SerializeField] private float UpdateRate = 1f;  // Aumentado de 0.2 menos bakes
    [SerializeField] private float MovementThreshold = 6f;  // Aumentado de 2 menos bakes

    [Header("Tamaño del área bakeada")]
    [SerializeField] private Vector3 NavMeshSize = new Vector3(40f, 10f, 40f);

    [Header("Optimización")]
    [Tooltip("Si un bake está en progreso, ignorar nuevos bakes")]
    [SerializeField] private bool SkipIfBaking = true;
    [Tooltip("Límite de sources para el bake (0 = sin límite)")]
    [SerializeField] private int MaxSources = 200;
    [Tooltip("Cachear la lista de sources entre frames")]
    [SerializeField] private bool CacheSources = true;

    public bool IsReady { get; private set; } = false;
    public bool IsBaking => _pendingBake != null && !_pendingBake.isDone;

    // Registro de jugadores
    private static readonly List<Transform> _registeredPlayers = new List<Transform>();

    public static void RegisterPlayer(Transform playerTransform)
    {
        if (!_registeredPlayers.Contains(playerTransform))
        {
            _registeredPlayers.Add(playerTransform);
            Debug.Log($"[Baker] Jugador registrado: {playerTransform.name}");
        }
    }

    public static void UnregisterPlayer(Transform playerTransform)
    {
        _registeredPlayers.Remove(playerTransform);
    }

    // Estado interno
    private NavMeshData _navMeshData;
    private NavMeshDataInstance _instance;
    private readonly List<NavMeshBuildSource> _sources = new List<NavMeshBuildSource>();
    private readonly List<NavMeshBuildSource> _cachedSources = new List<NavMeshBuildSource>();
    private Vector3 _lastCenter;
    private AsyncOperation _pendingBake;
    private float _lastBakeTime;
    private int _bakeCount = 0;

    // Performance stats
    private float _totalBakeTime = 0f;
    private int _totalBakes = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _registeredPlayers.Clear();
    }

    private void Start()
    {
        if (Surface == null)
        {
            Debug.LogError("[Baker] ¡SURFACE NO ASIGNADA!");
            return;
        }

        StartCoroutine(Init());
    }

    private IEnumerator Init()
    {
        Debug.Log("[Baker] Esperando jugadores...");

        float timeout = 0f;
        while (_registeredPlayers.Count == 0 || _registeredPlayers.TrueForAll(p => p == null))
        {
            timeout += Time.deltaTime;
            if (timeout > 60f)
            {
                Debug.LogError("[Baker] TIMEOUT: Ningún jugador registrado.");
                yield break;
            }
            yield return null;
        }

        Debug.Log($"[Baker] {_registeredPlayers.Count} jugador(es) registrado(s).");

        // Crear NavMeshData
        _navMeshData = new NavMeshData();
        _instance = NavMesh.AddNavMeshData(_navMeshData);

        // Primer bake
        _lastCenter = GetPlayersCenter();
        yield return StartCoroutine(BuildNavMeshAndWait(_lastCenter));

        IsReady = true;
        Debug.Log("[Baker] IsReady = true.");

        // Loop de actualización
        WaitForSeconds wait = new WaitForSeconds(UpdateRate);
        while (true)
        {
            yield return wait;

            // Limpiar referencias nulas
            _registeredPlayers.RemoveAll(p => p == null);
            if (_registeredPlayers.Count == 0) continue;

            //  Optimización: Skip si ya está bakeando
            if (SkipIfBaking && IsBaking)
            {
                continue;
            }

            Vector3 center = GetPlayersCenter();
            float distance = Vector3.Distance(center, _lastCenter);

            if (distance > MovementThreshold)
            {
                _lastCenter = center;
                StartCoroutine(BuildNavMeshAndWait(center));
            }
        }
    }

    private IEnumerator BuildNavMeshAndWait(Vector3 center)
    {
        // Optimización: Esperar bake anterior
        if (_pendingBake != null && !_pendingBake.isDone)
            yield return _pendingBake;

        float startTime = Time.realtimeSinceStartup;

        Bounds bounds = new Bounds(center, NavMeshSize);

        // Optimización: Usar cache si está disponible y la posición no cambió mucho
        bool reuseCache = CacheSources && _cachedSources.Count > 0;

        if (reuseCache)
        {
            _sources.Clear();
            _sources.AddRange(_cachedSources);
        }
        else
        {
            _sources.Clear();

            NavMeshBuilder.CollectSources(
                bounds,
                Surface.layerMask,
                Surface.useGeometry,
                Surface.defaultArea,
                new List<NavMeshBuildMarkup>(),
                _sources
            );

            // Remover objetos dinámicos
            _sources.RemoveAll(s =>
                s.component != null &&
                (s.component.GetComponent<NavMeshAgent>() != null ||
                 s.component.GetComponent<NetworkObject>() != null));

            // Optimización: Limitar sources si hay demasiados
            if (MaxSources > 0 && _sources.Count > MaxSources)
            {
                Debug.LogWarning($"[Baker] Demasiados sources ({_sources.Count}), limitando a {MaxSources}");
                _sources.RemoveRange(MaxSources, _sources.Count - MaxSources);
            }

            // Cachear para próximo frame
            if (CacheSources)
            {
                _cachedSources.Clear();
                _cachedSources.AddRange(_sources);
            }
        }

        if (_sources.Count == 0)
        {
            Debug.LogWarning("[Baker] 0 sources. Verificá layerMask.");
            yield break;
        }

        // Bakear
        _pendingBake = NavMeshBuilder.UpdateNavMeshDataAsync(
            _navMeshData,
            Surface.GetBuildSettings(),
            _sources,
            bounds
        );

        yield return _pendingBake;

        // Stats
        float bakeTime = Time.realtimeSinceStartup - startTime;
        _totalBakeTime += bakeTime;
        _totalBakes++;
        _bakeCount++;

        Debug.Log($"[Baker] Bake #{_bakeCount} | Sources: {_sources.Count} | Tiempo: {bakeTime:F3}s | Promedio: {(_totalBakeTime / _totalBakes):F3}s");
    }

    private Vector3 GetPlayersCenter()
    {
        Vector3 sum = Vector3.zero;
        int count = 0;
        foreach (var p in _registeredPlayers)
        {
            if (p != null) { sum += p.position; count++; }
        }
        return count > 0 ? sum / count : _lastCenter;
    }

    /// <summary>
    /// Verifica si una posición está dentro del área actual de NavMesh
    /// </summary>
    public bool IsPositionInBounds(Vector3 position)
    {
        Vector3 min = _lastCenter - NavMeshSize * 0.5f;
        Vector3 max = _lastCenter + NavMeshSize * 0.5f;

        return position.x >= min.x && position.x <= max.x &&
               position.z >= min.z && position.z <= max.z;
    }

    private void OnDestroy()
    {
        if (_instance.valid)
            NavMesh.RemoveNavMeshData(_instance);

        // Log final de stats
        if (_totalBakes > 0)
        {
            Debug.Log($"[Baker] Stats finales: {_totalBakes} bakes, " +
                     $"tiempo promedio: {(_totalBakeTime / _totalBakes):F3}s, " +
                     $"tiempo total: {_totalBakeTime:F2}s");
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Color según estado
        if (IsBaking)
            Gizmos.color = Color.yellow;
        else if (IsReady)
            Gizmos.color = Color.green;
        else
            Gizmos.color = Color.red;

        Gizmos.DrawWireCube(_lastCenter, NavMeshSize);
    }
}