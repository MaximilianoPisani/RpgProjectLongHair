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

            if (closestPlayer != null)
            {
                _targetPlayer = closestPlayer;
                _agent.SetDestination(_targetPlayer.position);
            }

            _timer = 0f;
        }
    }
}