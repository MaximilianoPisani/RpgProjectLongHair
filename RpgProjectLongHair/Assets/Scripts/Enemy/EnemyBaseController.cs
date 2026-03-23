using UnityEngine;
using UnityEngine.AI;
using Fusion;

[RequireComponent(typeof(NetworkObject), typeof(NavMeshAgent))]
public abstract class EnemyBaseController : NetworkBehaviour
{
    [Header("Detection")]
    public float DetectionRadius = 10f;
    public LayerMask PlayerLayer;

    [Header("References")]
    public Transform AttackOrigin;

    public NavMeshAgent Agent { get; private set; }
    public EnemyHealth Health { get; private set; }
    public float NextAttackTime { get; set; } = 0f;

    private Transform _targetPlayer;
    public Transform TargetPlayer => _targetPlayer;

    protected EnemyStateMachine StateMachine { get; private set; }

    public override void Spawned()
    {
        Agent = GetComponent<NavMeshAgent>();
        Health = GetComponent<EnemyHealth>();

        Agent.enabled = false;
        Agent.enabled = true;

        if (!Object.HasStateAuthority)
            Agent.enabled = false;

        StateMachine = new EnemyStateMachine();
        InitStateMachine();
    }

    protected abstract void InitStateMachine();

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        StateMachine.Update();
    }

    public void SetTarget(Transform target) => _targetPlayer = target;

    public void ChangeState(IEnemyState newState) => StateMachine.ChangeState(newState);

    public void OnTargetDied()
    {
        _targetPlayer = null;
        ChangeState(GetIdleState());
    }

    protected abstract IEnemyState GetIdleState();
}