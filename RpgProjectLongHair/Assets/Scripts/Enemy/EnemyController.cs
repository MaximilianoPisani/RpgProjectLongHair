using UnityEngine;
using UnityEngine.AI;
using Fusion;

[RequireComponent(typeof(NetworkObject), typeof(NavMeshAgent))]
public class EnemyController : NetworkBehaviour
{
    public NavMeshAgent Agent { get; private set; }
    public EnemyHealth Health { get; private set; }

    public float DetectionRadius = 10f;
    public LayerMask PlayerLayer;

    public MeleeAttackData MeleeAttackData;
    public Transform AttackOrigin;

    private Transform _targetPlayer;
    private EnemyStateMachine _stateMachine;

    public float NextAttackTime { get; set; } = 0f;

    public Transform TargetPlayer => _targetPlayer;

    public override void Spawned()
    {
        Agent = GetComponent<NavMeshAgent>();
        Health = GetComponent<EnemyHealth>();
        Agent.enabled = true;

        _stateMachine = new EnemyStateMachine();
        _stateMachine.ChangeState(new EnemyIdleState(this));
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        _stateMachine.Update();
    }

    public void SetTarget(Transform target)
    {
        _targetPlayer = target;
    }

    public void ChangeState(IEnemyState newState)
    {
        _stateMachine.ChangeState(newState);
    }
}