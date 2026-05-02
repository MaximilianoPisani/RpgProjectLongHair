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
    [SerializeField] private float UpdateRate = 0.2f;
    [SerializeField] private float MovementThreshold = 2f;

    [Header("Tamaño del área bakeada")]
    [SerializeField] private Vector3 NavMeshSize = new Vector3(25f, 10f, 25f);

    public bool IsReady { get; private set; } = false;

    // ?? Registro de jugadores ????????????????????????????????????
    // Los players se registran ellos mismos vía RegisterPlayer().
    // No dependemos de GetPlayerObject() que solo funciona en el servidor.
    private static readonly List<Transform> _registeredPlayers = new List<Transform>();

    /// <summary>
    /// Llamar desde el NetworkBehaviour del player en Spawned().
    /// Funciona en servidor y cliente.
    /// </summary>
    public static void RegisterPlayer(Transform playerTransform)
    {
        if (!_registeredPlayers.Contains(playerTransform))
        {
            _registeredPlayers.Add(playerTransform);
            Debug.Log($"[Baker] Jugador registrado: {playerTransform.name} en {playerTransform.position}");
        }
    }

    /// <summary>Llamar desde Despawned() del player.</summary>
    public static void UnregisterPlayer(Transform playerTransform)
    {
        _registeredPlayers.Remove(playerTransform);
    }

    // ?? Estado interno ???????????????????????????????????????????
    private NavMeshData _navMeshData;
    private NavMeshDataInstance _instance;
    private readonly List<NavMeshBuildSource> _sources = new List<NavMeshBuildSource>();
    private Vector3 _lastCenter;
    private AsyncOperation _pendingBake;

    // ????????????????????????????????????????????????????????????

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
            Debug.LogError("[Baker] ¡SURFACE NO ASIGNADA! Asigná el NavMeshSurface en el Inspector.");
            return;
        }

        Debug.Log("[Baker] Start(). Iniciando corrutina.");
        StartCoroutine(Init());
    }

    private IEnumerator Init()
    {
        // ?? 1. Esperar que haya al menos un jugador registrado ???
        Debug.Log("[Baker] Esperando jugadores registrados...");

        float timeout = 0f;
        while (_registeredPlayers.Count == 0 ||
               _registeredPlayers.TrueForAll(p => p == null))
        {
            timeout += Time.deltaTime;
            if (timeout > 60f)
            {
                Debug.LogError("[Baker] TIMEOUT 60s: Ningún jugador se registró.\n" +
                               "Asegurate de llamar AreaFloorBaker.RegisterPlayer(transform) " +
                               "en el Spawned() del NetworkBehaviour del player.");
                yield break;
            }
            yield return null;
        }

        Debug.Log($"[Baker] {_registeredPlayers.Count} jugador(es) registrado(s).");

        // ?? 2. Crear NavMeshData ?????????????????????????????????
        _navMeshData = new NavMeshData();
        _instance = NavMesh.AddNavMeshData(_navMeshData);

        // ?? 3. Primer bake centrado en el jugador ????????????????
        _lastCenter = GetPlayersCenter();
        Debug.Log($"[Baker] Primer bake en {_lastCenter}...");
        yield return StartCoroutine(BuildNavMeshAndWait(_lastCenter));

        IsReady = true;
        Debug.Log("[Baker] IsReady = true. NavMesh lista.");

        // ?? 4. Loop de actualización ?????????????????????????????
        WaitForSeconds wait = new WaitForSeconds(UpdateRate);
        while (true)
        {
            yield return wait;

            // Limpiar referencias nulas (players que se fueron)
            _registeredPlayers.RemoveAll(p => p == null);
            if (_registeredPlayers.Count == 0) continue;

            Vector3 center = GetPlayersCenter();
            if (Vector3.Distance(center, _lastCenter) > MovementThreshold)
            {
                Debug.Log($"[Baker] Rebakeando: {_lastCenter:F0} ? {center:F0}");
                _lastCenter = center;
                StartCoroutine(BuildNavMeshAndWait(center));
            }
        }
    }

    private IEnumerator BuildNavMeshAndWait(Vector3 center)
    {
        if (_pendingBake != null && !_pendingBake.isDone)
            yield return new WaitUntil(() => _pendingBake.isDone);

        Bounds bounds = new Bounds(center, NavMeshSize);
        _sources.Clear();

        NavMeshBuilder.CollectSources(
            bounds,
            Surface.layerMask,
            Surface.useGeometry,
            Surface.defaultArea,
            new List<NavMeshBuildMarkup>(),
            _sources
        );

        _sources.RemoveAll(s =>
            s.component != null &&
            (s.component.GetComponent<NavMeshAgent>() != null ||
             s.component.GetComponent<NetworkObject>() != null));

        Debug.Log($"[Baker] Sources: {_sources.Count} | Centro: {center:F0}");

        if (_sources.Count == 0)
            Debug.LogWarning("[Baker] 0 sources. Verificá que el Layer del suelo esté en el layerMask del NavMeshSurface.");

        _pendingBake = NavMeshBuilder.UpdateNavMeshDataAsync(
            _navMeshData,
            Surface.GetBuildSettings(),
            _sources,
            bounds
        );

        yield return _pendingBake;
        Debug.Log($"[Baker] Bake completado en {center:F0}.");
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

    private void OnDestroy()
    {
        if (_instance.valid)
            NavMesh.RemoveNavMeshData(_instance);
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = IsReady ? Color.green : Color.yellow;
        Gizmos.DrawWireCube(_lastCenter, NavMeshSize);
    }
}