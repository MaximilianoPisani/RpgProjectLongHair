using System.Collections;
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

        StateMachine = new EnemyStateMachine();
        InitStateMachine();

        if (!Object.HasStateAuthority)
        {
            // Clientes no necesitan el agente de navegación
            Agent.enabled = false;
            return;
        }

        // El servidor inicializa el agente de forma segura
        // (el baker ya terminó porque EnemySpawner esperó IsReady)
        StartCoroutine(InitAgentSafe());
    }

    /// <summary>
    /// Habilita el agente solo cuando hay NavMesh válida en la posición actual.
    /// Reintenta si no la encuentra de inmediato (puede pasar en el primer frame).
    /// </summary>
    private IEnumerator InitAgentSafe()
    {
        Agent.enabled = false;

        // Intentar hasta encontrar NavMesh en esta posición
        int maxAttempts = 10;
        for (int i = 0; i < maxAttempts; i++)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                // Mover al punto exacto de la NavMesh y habilitar el agente
                transform.position = hit.position;
                Agent.enabled = true;

                if (Agent.isOnNavMesh)
                {
                    Debug.Log($"[Enemy] {name} agente inicializado en {hit.position}");
                    yield break;
                }

                // Si isOnNavMesh sigue false, deshabilitar y reintentar
                Agent.enabled = false;
            }

            Debug.LogWarning($"[Enemy] {name} sin NavMesh en intento {i + 1}/{maxAttempts}. Reintentando...");
            yield return new WaitForSeconds(0.2f);
        }

        Debug.LogError($"[Enemy] {name} no pudo inicializar el agente. Sin NavMesh en {transform.position}");
    }


    protected abstract void InitStateMachine();

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        // No correr la máquina de estados si el agente no está listo
        if (Agent == null || !Agent.enabled || !Agent.isOnNavMesh)
            return;

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