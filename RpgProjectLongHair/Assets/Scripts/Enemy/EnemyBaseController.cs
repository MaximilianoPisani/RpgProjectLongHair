using UnityEngine;
using UnityEngine.AI;
using Fusion;

[RequireComponent(typeof(NetworkObject), typeof(NavMeshAgent))]
public abstract class EnemyBaseController : NetworkBehaviour
{
    [Header("Detection")]
    [SerializeField] protected float detectionRadius = 15f;
    public LayerMask PlayerLayer;
    private Transform _targetPlayer;

    [Header("References")]
    [SerializeField] protected Transform attackOrigin;

    public NavMeshAgent Agent { get; private set; }
    public EnemyHealth Health { get; private set; }
    public float NextAttackTime { get; set; } = 0f;
    public Transform TargetPlayer => _targetPlayer;
    public float DetectionRadius => detectionRadius;
    public Transform AttackOrigin => attackOrigin;

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