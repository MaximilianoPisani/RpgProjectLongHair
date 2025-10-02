using UnityEngine;
using UnityEngine.AI;
using Fusion;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : NetworkBehaviour
{
    private NavMeshAgent _agent;
    private Transform _targetPlayer;
    private float _timer;

    [SerializeField] private float _updateInterval = 0.5f;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.enabled = false; 
    }

    public void InitializeAgent()
    {
        if (_agent != null)
        {
            _agent.enabled = true;
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                _agent.Warp(hit.position); 
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        _timer += Runner.DeltaTime;
        if (_timer >= _updateInterval)
        {
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

            Transform closestPlayer = null;
            float closestDistance = float.MaxValue;

            foreach (var player in players)
            {
                float dist = Vector3.Distance(transform.position, player.transform.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestPlayer = player.transform;
                }
            }

            _targetPlayer = closestPlayer;

            if (_agent != null && _agent.isOnNavMesh && _targetPlayer != null)
            {
                _agent.SetDestination(_targetPlayer.position);
            }

            _timer = 0f;
        }
    }
}