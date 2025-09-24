using UnityEngine;
using UnityEngine.AI;
using Fusion;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : NetworkBehaviour
{
    private NavMeshAgent _agent;
    private NetworkObject _targetPlayer;
    private float _timer;

    [SerializeField] private float _updateInterval = 0.5f;

    public void SetTarget(NetworkObject player)
    {
        _targetPlayer = player;
        if (_targetPlayer != null)
            _agent.SetDestination(_targetPlayer.transform.position);
    }

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        if (_targetPlayer == null)
            return;

        _timer += Runner.DeltaTime;
        if (_timer >= _updateInterval)
        {
            _agent.SetDestination(_targetPlayer.transform.position);
            _timer = 0f;
        }
    }
}