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
    [SerializeField] private float UpdateRate = 1f;
    [SerializeField] private float MovementThreshold = 6f;

    [Header("Tamaño del área — pasos fijos")]
    [Tooltip("Área con un jugador o dos jugadores muy juntos")]
    [SerializeField] private Vector3 SizeClose = new Vector3(40f, 10f, 40f);
    [Tooltip("Área cuando los jugadores están a distancia media")]
    [SerializeField] private Vector3 SizeMid = new Vector3(60f, 10f, 60f);
    [Tooltip("Área cuando los jugadores están muy separados")]
    [SerializeField] private Vector3 SizeFar = new Vector3(80f, 10f, 80f);
    [Tooltip("Distancia entre jugadores que activa el paso Medio")]
    [SerializeField] private float ThresholdMid = 25f;
    [Tooltip("Distancia entre jugadores que activa el paso Lejano")]
    [SerializeField] private float ThresholdFar = 45f;

    [Header("Optimización")]
    [SerializeField] private float CacheInvalidationDistance = 12f;

    // ?? API pública ??????????????????????????????????????????????
    public bool IsReady { get; private set; } = false;
    public bool IsBaking => _pendingBake != null && !_pendingBake.isDone;

    // ?? Registro de jugadores ????????????????????????????????????
    private static readonly List<Transform> _registeredPlayers = new List<Transform>();

    public static void RegisterPlayer(Transform t)
    {
        if (t != null && !_registeredPlayers.Contains(t))
        {
            _registeredPlayers.Add(t);
            Debug.Log($"[Baker] Jugador registrado: {t.name}");
        }
    }

    public static void UnregisterPlayer(Transform t) => _registeredPlayers.Remove(t);

    // ?? Double buffer ????????????????????????????????????????????
    private NavMeshData _frontBuffer;
    private NavMeshData _backBuffer;
    private NavMeshDataInstance _frontInstance;
    private AsyncOperation _pendingBake;

    // ?? Caché de sources ?????????????????????????????????????????
    private readonly List<NavMeshBuildSource> _cachedSources = new List<NavMeshBuildSource>();
    private Vector3 _lastSourcesCenter = Vector3.one * float.MaxValue;
    private Vector3 _lastSourcesSize = Vector3.zero;

    // ?? Estado ???????????????????????????????????????????????????
    private Vector3 _lastBakeCenter;
    private Vector3 _lastBakeSize;
    private int _bakeCount = 0;

    // ????????????????????????????????????????????????????????????

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _registeredPlayers.Clear();
    }

    private void Start()
    {
        if (Surface == null) { Debug.LogError("[Baker] ¡SURFACE NO ASIGNADA!"); return; }
        StartCoroutine(Init());
    }

    private IEnumerator Init()
    {
        // ?? 1. Runner del juego ??????????????????????????????????
        Debug.Log("[Baker] Esperando NetworkRunner...");
        NetworkRunner runner = null;
        float timeout = 0f;

        while (runner == null)
        {
            foreach (var r in NetworkRunner.Instances)
                if (r != null && r.IsRunning && r.GameMode != GameMode.Single) { runner = r; break; }

            timeout += Time.deltaTime;
            if (timeout > 30f) { Debug.LogError("[Baker] TIMEOUT: NetworkRunner no encontrado."); yield break; }
            if (runner == null) yield return null;
        }

        // ?? 2. Solo servidor ?????????????????????????????????????
        if (!runner.IsServer)
        {
            Debug.Log("[Baker] Cliente — baker desactivado.");
            IsReady = true;
            yield break;
        }

        Debug.Log("[Baker] Servidor. Esperando jugadores...");

        // ?? 3. Esperar jugadores ?????????????????????????????????
        timeout = 0f;
        while (_registeredPlayers.Count == 0 || _registeredPlayers.TrueForAll(p => p == null))
        {
            timeout += Time.deltaTime;
            if (timeout > 60f) { Debug.LogError("[Baker] TIMEOUT: Sin jugadores registrados."); yield break; }
            yield return null;
        }

        // ?? 4. Primer bake ???????????????????????????????????????
        _frontBuffer = new NavMeshData();
        _frontInstance = NavMesh.AddNavMeshData(_frontBuffer);

        ComputeBounds(out _lastBakeCenter, out _lastBakeSize);
        yield return StartCoroutine(BakeIntoBuffer(_frontBuffer, _lastBakeCenter, _lastBakeSize));

        IsReady = true;
        Debug.Log("[Baker] IsReady = true.");

        // ?? 5. Loop ??????????????????????????????????????????????
        WaitForSeconds wait = new WaitForSeconds(UpdateRate);
        while (true)
        {
            yield return wait;

            _registeredPlayers.RemoveAll(p => p == null);
            if (_registeredPlayers.Count == 0 || IsBaking) continue;

            ComputeBounds(out Vector3 newCenter, out Vector3 newSize);

            // El tamaño es cuantizado: solo cambia cuando se cruza un umbral.
            // Comparar con == es suficiente porque los pasos son valores fijos.
            bool centerMoved = Vector3.Distance(newCenter, _lastBakeCenter) > MovementThreshold;
            bool sizeChanged = newSize != _lastBakeSize;

            if (centerMoved || sizeChanged)
            {
                _lastBakeCenter = newCenter;
                _lastBakeSize = newSize;
                StartCoroutine(DoubleBufferedBake(newCenter, newSize));
            }
        }
    }

    // ?? Cuantización de tamaño ???????????????????????????????????

    /// <summary>
    /// Calcula el centro (promedio de jugadores) y el tamaño cuantizado.
    /// El tamaño solo tiene 3 valores posibles (SizeClose/Mid/Far),
    /// por lo que no hay rebakes continuos por movimientos menores.
    /// </summary>
    private void ComputeBounds(out Vector3 center, out Vector3 size)
    {
        var valid = _registeredPlayers.FindAll(p => p != null);

        if (valid.Count == 0) { center = _lastBakeCenter; size = SizeClose; return; }
        if (valid.Count == 1) { center = valid[0].position; size = SizeClose; return; }

        // Centro = promedio de todos los jugadores
        Vector3 sum = Vector3.zero;
        foreach (var p in valid) sum += p.position;
        center = sum / valid.Count;

        // Distancia máxima entre cualquier par de jugadores
        float maxDist = 0f;
        for (int i = 0; i < valid.Count; i++)
            for (int j = i + 1; j < valid.Count; j++)
                maxDist = Mathf.Max(maxDist, Vector3.Distance(valid[i].position, valid[j].position));

        // Paso cuantizado según la distancia
        if (maxDist >= ThresholdFar) size = SizeFar;
        else if (maxDist >= ThresholdMid) size = SizeMid;
        else size = SizeClose;
    }

    // ?? Double-buffered bake ?????????????????????????????????????

    private IEnumerator DoubleBufferedBake(Vector3 center, Vector3 size)
    {
        if (_pendingBake != null && !_pendingBake.isDone)
            yield return new WaitUntil(() => _pendingBake.isDone);

        _backBuffer = new NavMeshData();
        yield return StartCoroutine(BakeIntoBuffer(_backBuffer, center, size));

        // Swap atómico: registrar nuevo antes de eliminar el viejo
        NavMeshDataInstance newInstance = NavMesh.AddNavMeshData(_backBuffer);
        if (_frontInstance.valid) NavMesh.RemoveNavMeshData(_frontInstance);

        _frontBuffer = _backBuffer;
        _frontInstance = newInstance;
        _backBuffer = null;
    }

    private IEnumerator BakeIntoBuffer(NavMeshData target, Vector3 center, Vector3 size)
    {
        float startTime = Time.realtimeSinceStartup;
        Bounds bounds = new Bounds(center, size);

        // Invalidar caché si el centro se alejó O si el tamaño cambió.
        // Ambas condiciones son necesarias: un área más grande necesita
        // sources nuevas aunque el centro sea el mismo.
        bool centerFarEnough = Vector3.Distance(center, _lastSourcesCenter) > CacheInvalidationDistance;
        bool sizeChanged = size != _lastSourcesSize;

        if (centerFarEnough || sizeChanged)
        {
            _cachedSources.Clear();

            NavMeshBuilder.CollectSources(
                bounds,
                Surface.layerMask,
                Surface.useGeometry,
                Surface.defaultArea,
                new List<NavMeshBuildMarkup>(),
                _cachedSources
            );

            _cachedSources.RemoveAll(s =>
                s.component != null &&
                (s.component.GetComponent<NavMeshAgent>() != null ||
                 s.component.GetComponent<NetworkObject>() != null));

            _lastSourcesCenter = center;
            _lastSourcesSize = size;

            Debug.Log($"[Baker] Sources recolectadas: {_cachedSources.Count} | Tamaño: {size:F0}");
        }

        if (_cachedSources.Count == 0)
        {
            Debug.LogWarning("[Baker] 0 sources. Verificá el layerMask del NavMeshSurface.");
            yield break;
        }

        _pendingBake = NavMeshBuilder.UpdateNavMeshDataAsync(
            target,
            Surface.GetBuildSettings(),
            _cachedSources,
            bounds
        );

        yield return _pendingBake;

        _bakeCount++;
        Debug.Log($"[Baker] Bake #{_bakeCount} en {(Time.realtimeSinceStartup - startTime):F3}s | " +
                  $"Centro: {center:F0} | Tamaño: {size.x:F0}×{size.z:F0}");
    }

    // ?? Helpers ??????????????????????????????????????????????????

    public bool IsPositionInBounds(Vector3 position)
    {
        Vector3 min = _lastBakeCenter - _lastBakeSize * 0.5f;
        Vector3 max = _lastBakeCenter + _lastBakeSize * 0.5f;
        return position.x >= min.x && position.x <= max.x &&
               position.z >= min.z && position.z <= max.z;
    }

    private void OnDestroy()
    {
        if (_frontInstance.valid)
            NavMesh.RemoveNavMeshData(_frontInstance);
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = IsBaking ? Color.yellow : (IsReady ? Color.green : Color.red);
        Gizmos.DrawWireCube(_lastBakeCenter, _lastBakeSize);
    }
}