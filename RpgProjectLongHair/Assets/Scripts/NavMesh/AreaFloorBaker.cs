using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using NavMeshBuilder = UnityEngine.AI.NavMeshBuilder;
using Fusion;
using Unity.AI.Navigation;

public class AreaFloorBaker : MonoBehaviour
{
    [Header("NavMesh Surface de referencia (settings)")]
    [SerializeField] private NavMeshSurface Surface;

    [Header("Configuración")]
    [SerializeField] private float UpdateRate = 0.2f;
    [SerializeField] private float MovementThreshold = 2f;

    [Header("Tamaño del área bakeada")]
    [SerializeField] private Vector3 NavMeshSize = new Vector3(25f, 10f, 25f);

    private NetworkRunner _runner;

    private NavMeshData _navMeshData;
    private NavMeshDataInstance _instance;

    private readonly List<NavMeshBuildSource> _sources = new List<NavMeshBuildSource>();
    private readonly List<Transform> _players = new List<Transform>();

    private Vector3 _lastCenter;

    private void Start()
    {
        StartCoroutine(Init());
    }

    private IEnumerator Init()
    {
        yield return new WaitUntil(() => NetworkRunner.Instances != null && NetworkRunner.Instances.Count > 0);
        _runner = NetworkRunner.Instances[0];

        _navMeshData = new NavMeshData();
        _instance = NavMesh.AddNavMeshData(_navMeshData);

        yield return new WaitUntil(() =>
        {
            RefreshPlayers();
            return _players.Count > 0;
        });

        _lastCenter = GetPlayersCenter();
        BuildNavMesh(_lastCenter);

        WaitForSeconds wait = new WaitForSeconds(UpdateRate);

        while (true)
        {
            if (_runner.IsServer)
            {
                RefreshPlayers();

                Vector3 center = GetPlayersCenter();

                if (Vector3.Distance(center, _lastCenter) > MovementThreshold)
                {
                    _lastCenter = center;
                    BuildNavMesh(center);
                }
            }

            yield return wait;
        }
    }

    private void RefreshPlayers()
    {
        _players.Clear();

        foreach (PlayerRef playerRef in _runner.ActivePlayers)
        {
            NetworkObject obj = _runner.GetPlayerObject(playerRef);
            if (obj != null)
                _players.Add(obj.transform);
        }
    }

    private Vector3 GetPlayersCenter()
    {
        if (_players.Count == 0) return Vector3.zero;

        Vector3 sum = Vector3.zero;
        foreach (var p in _players)
            sum += p.position;

        return sum / _players.Count;
    }

    private void BuildNavMesh(Vector3 center)
    {
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
            (
                s.component.GetComponent<NavMeshAgent>() != null ||
                s.component.GetComponent<NetworkObject>() != null
            )
        );

        Debug.Log($"[NavMesh] Sources: {_sources.Count}");

        NavMeshBuilder.UpdateNavMeshDataAsync(
            _navMeshData,
            Surface.GetBuildSettings(),
            _sources,
            bounds
        );
    }

    private void OnDestroy()
    {
        NavMesh.RemoveNavMeshData(_instance);
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        if (_players == null || _players.Count == 0) return;

        Vector3 center = Vector3.zero;

        foreach (var p in _players)
        {
            if (p != null)
                center += p.position;
        }

        center /= _players.Count;

        Gizmos.DrawWireCube(center, NavMeshSize);
    }
}