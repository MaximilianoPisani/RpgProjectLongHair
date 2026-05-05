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

    [Header("NavMesh Lazy Activation")]
    [SerializeField] private float _navMeshCheckInterval = 0.5f;
    [SerializeField] private float _navMeshSearchRadius = 2f;
    [Tooltip("Visualizar estado de activación")]
    [SerializeField] private bool _showActivationGizmos = true;

    private float _detectionTimer;
    private float _navMeshCheckTimer;
    private const float DetectionInterval = 0.5f;

    public NavMeshAgent Agent { get; private set; }
    public EnemyHealth Health { get; private set; }
    public float NextAttackTime { get; set; } = 0f;
    public Transform TargetPlayer => _targetPlayer;
    public float DetectionRadius => detectionRadius;
    public Transform AttackOrigin => attackOrigin;

    // Estados de activación
    public bool IsActive { get; private set; } = false;  // ¿El enemigo está activo?
    public bool HasNavMesh { get; private set; } = false; // ¿Hay NavMesh disponible?

    protected EnemyStateMachine StateMachine { get; private set; }

    public override void Spawned()
    {
        Agent = GetComponent<NavMeshAgent>();
        Health = GetComponent<EnemyHealth>();

        StateMachine = new EnemyStateMachine();
        InitStateMachine();

        if (!Object.HasStateAuthority)
        {
            // Clientes: deshabilitar agente
            if (Agent != null)
                Agent.enabled = false;
            return;
        }

        // Servidor: iniciar DORMIDO
        if (Agent != null)
            Agent.enabled = false;

        IsActive = false;
        HasNavMesh = false;

        Debug.Log($"[Enemy] {name} spawneado DORMIDO en {transform.position}. Esperando NavMesh...");

        // Iniciar coroutine de monitoreo
        StartCoroutine(MonitorNavMesh());
    }

    protected abstract void InitStateMachine();

    /// <summary>
    /// Monitorea continuamente si hay NavMesh disponible en esta posición
    /// Activa/desactiva el enemigo según corresponda
    /// </summary>
    private IEnumerator MonitorNavMesh()
    {
        WaitForSeconds wait = new WaitForSeconds(_navMeshCheckInterval);

        while (true)
        {
            yield return wait;

            if (Agent == null) continue;

            // Verificar si hay NavMesh en la posición actual
            bool hasNavMeshNow = NavMesh.SamplePosition(
                transform.position,
                out NavMeshHit hit,
                _navMeshSearchRadius,
                NavMesh.AllAreas
            );

            //  Transición: SIN NavMesh  CON NavMesh (ACTIVAR)
            if (!HasNavMesh && hasNavMeshNow)
            {
                HasNavMesh = true;
                ActivateEnemy(hit.position);
            }
            // Transición: CON NavMesh  SIN NavMesh (DESACTIVAR)
            else if (HasNavMesh && !hasNavMeshNow)
            {
                HasNavMesh = false;
                DeactivateEnemy();
            }
        }
    }

    /// <summary>
    /// Activa el enemigo cuando la NavMesh llega a él
    /// </summary>
    private void ActivateEnemy(Vector3 navMeshPosition)
    {
        if (IsActive) return;

        Debug.Log($"[Enemy] {name} ACTIVÁNDOSE - NavMesh detectada en {navMeshPosition}");

        // Ajustar posición exacta al NavMesh
        transform.position = navMeshPosition;

        // Habilitar el agente
        Agent.enabled = true;

        // Verificar que esté en NavMesh
        if (Agent.isOnNavMesh)
        {
            IsActive = true;
            Debug.Log($"[Enemy] {name} ACTIVO  - Agente habilitado en {navMeshPosition}");
        }
        else
        {
            // Si falla, deshabilitar y reintentar en el siguiente check
            Agent.enabled = false;
            Debug.LogWarning($"[Enemy] {name} falló activación (isOnNavMesh = false), reintentando...");
        }
    }

    /// <summary>
    /// Desactiva el enemigo cuando pierde la NavMesh
    /// </summary>
    private void DeactivateEnemy()
    {
        if (!IsActive) return;

        Debug.Log($"[Enemy] {name} DESACTIVÁNDOSE - NavMesh perdida en {transform.position}");

        // Deshabilitar el agente
        if (Agent != null)
            Agent.enabled = false;

        IsActive = false;

        // Resetear target
        _targetPlayer = null;
        ChangeState(GetIdleState());

        Debug.Log($"[Enemy] {name} DORMIDO  - Esperando que vuelva la NavMesh");
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        // Solo correr lógica si el enemigo está ACTIVO
        if (!IsActive || Agent == null || !Agent.enabled || !Agent.isOnNavMesh)
            return;

        // Detección de jugadores
        _detectionTimer += Runner.DeltaTime;
        if (_detectionTimer >= DetectionInterval)
        {
            _detectionTimer = 0f;
            TryFindTarget();
        }

        // Actualizar máquina de estados
        StateMachine.Update();
    }

    private void TryFindTarget()
    {
        // Si el target actual sigue vivo y cerca, mantenerlo
        if (_targetPlayer != null &&
            _targetPlayer.gameObject.activeInHierarchy &&
            Vector3.Distance(transform.position, _targetPlayer.position) <= detectionRadius)
        {
            return;
        }

        // Buscar nuevo target
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, PlayerLayer);

        if (hits.Length == 0)
        {
            SetTarget(null);
            return;
        }

        // Encontrar el más cercano
        float closest = float.MaxValue;
        Transform best = null;

        foreach (var hit in hits)
        {
            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < closest)
            {
                closest = dist;
                best = hit.transform;
            }
        }

        SetTarget(best);
    }

    public void SetTarget(Transform target) => _targetPlayer = target;

    public void ChangeState(IEnemyState newState) => StateMachine.ChangeState(newState);

    public void OnTargetDied()
    {
        _targetPlayer = null;
        ChangeState(GetIdleState());
    }

    protected abstract IEnemyState GetIdleState();

    private void OnDrawGizmosSelected()
    {
        if (!_showActivationGizmos) return;

        // Radio de detección de jugadores
        Gizmos.color = _targetPlayer != null ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Estado de activación (esfera sobre el enemigo)
        if (Application.isPlaying)
        {
            Vector3 statusPos = transform.position + Vector3.up * 3f;

            if (IsActive)
            {
                // ACTIVO = esfera verde
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(statusPos, 0.5f);
                Gizmos.DrawIcon(statusPos, "sv_icon_dot0_pix16_gizmo", true);
            }
            else
            {
                // DORMIDO = esfera gris
                Gizmos.color = Color.gray;
                Gizmos.DrawWireSphere(statusPos, 0.5f);
                Gizmos.DrawIcon(statusPos, "sv_icon_dot8_pix16_gizmo", true);
            }

            // NavMesh disponible (círculo en el suelo)
            if (HasNavMesh)
            {
                Gizmos.color = new Color(0, 1, 0, 0.3f);
                Gizmos.DrawSphere(transform.position, _navMeshSearchRadius);
            }
            else
            {
                Gizmos.color = new Color(1, 0, 0, 0.3f);
                Gizmos.DrawWireSphere(transform.position, _navMeshSearchRadius);
            }
        }

        // Línea hacia el target
        if (_targetPlayer != null && Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position + Vector3.up, _targetPlayer.position + Vector3.up);
        }
    }

    private void OnDrawGizmos()
    {
        if (!_showActivationGizmos || !Application.isPlaying) return;

        // Texto de estado (solo en Scene view)
#if UNITY_EDITOR
        Vector3 textPos = transform.position + Vector3.up * 4f;
        string status = IsActive ? "ACTIVE " : "DORMANT";
        UnityEditor.Handles.Label(textPos, status, new GUIStyle
        {
            normal = { textColor = IsActive ? Color.green : Color.gray },
            fontSize = 12,
            fontStyle = FontStyle.Bold
        });
#endif
    }
}