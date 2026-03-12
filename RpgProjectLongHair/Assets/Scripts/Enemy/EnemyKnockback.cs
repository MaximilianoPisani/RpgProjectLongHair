using UnityEngine;
using UnityEngine.AI;
using Fusion;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(NetworkObject))]
public class EnemyKnockback : NetworkBehaviour
{
    [Header("Knockback Settings")]
    [Tooltip("Multiplicador global de fuerza de knockback")]
    [SerializeField] private float _knockbackMultiplier = 1f;

    [Tooltip("Velocidad inicial del knockback en metros/segundo (se escala por fuerza del ataque)")]
    [SerializeField] private float _baseKnockbackSpeed = 6f;

    [Tooltip("Cuánto se desacelera el knockback por segundo (fricción)")]
    [SerializeField] private float _deceleration = 14f;

    [Tooltip("Duración máxima del knockback en segundos")]
    [SerializeField] private float _maxDuration = 0.35f;

    [Tooltip("Fuerza mínima para activar knockback")]
    [SerializeField] private float _minForceThreshold = 5f;

    [Header("Attack Force Reference")]
    [Tooltip("Daño base de referencia para normalizar la fuerza")]
    [SerializeField] private float _referenceDamage = 25f;

    [Networked] private Vector3 KnockbackVelocity { get; set; }
    [Networked] private float KnockbackTimer { get; set; }

    private NavMeshAgent _agent;
    private ChangeDetector _changeDetector;

    private Vector3 _localVelocity;
    private bool _isBeingKnockedBack;

    public override void Spawned()
    {
        _agent = GetComponent<NavMeshAgent>();
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        KnockbackVelocity = Vector3.zero;
        KnockbackTimer = 0f;
        _localVelocity = Vector3.zero;
        _isBeingKnockedBack = false;
    }

    public void ApplyKnockback(Vector3 attackDirection, int damage)
    {
        if (!Object.HasStateAuthority) return;

        Vector3 flatDir = new Vector3(attackDirection.x, 0f, attackDirection.z);
        if (flatDir.sqrMagnitude < 0.001f) return;
        flatDir.Normalize();

        float forceScale = Mathf.Clamp(damage / _referenceDamage, 0.3f, 3f);
        float force = _baseKnockbackSpeed * forceScale * _knockbackMultiplier;

        if (force < _minForceThreshold) return;

        Vector3 newVelocity = flatDir * force;
        if (KnockbackTimer > 0f)
        {
            newVelocity = (KnockbackVelocity + newVelocity) * 0.7f; 
        }

        KnockbackVelocity = newVelocity;
        KnockbackTimer = _maxDuration;

        RPC_PauseNavAgent();
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (KnockbackTimer <= 0f) return;

        float dt = Runner.DeltaTime;

        float newSpeed = KnockbackVelocity.magnitude - _deceleration * dt;

        if (newSpeed <= 0f)
        {
            KnockbackVelocity = Vector3.zero;
            KnockbackTimer = 0f;
            RPC_ResumeNavAgent();
            return;
        }

        KnockbackVelocity = KnockbackVelocity.normalized * newSpeed;
        KnockbackTimer = Mathf.Max(0f, KnockbackTimer - dt);

        Vector3 newPos = transform.position + KnockbackVelocity * dt;
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.Warp(newPos);
        }
        else
        {
            transform.position = newPos;
        }

        if (KnockbackTimer <= 0f)
        {
            KnockbackVelocity = Vector3.zero;
            RPC_ResumeNavAgent();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
    private void RPC_PauseNavAgent()
    {
        if (_agent == null) return;
        _agent.isStopped = true;
        _agent.ResetPath();
        _isBeingKnockedBack = true;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
    private void RPC_ResumeNavAgent()
    {
        if (_agent == null) return;
        _agent.isStopped = false;
        _isBeingKnockedBack = false;
    }

    public static void TryApplyMeleeKnockback(GameObject enemyRoot, Vector3 attackerPosition, int damage)
    {
        var kb = enemyRoot.GetComponentInChildren<EnemyKnockback>();
        if (kb == null || !kb.Object || !kb.Object.HasStateAuthority) return;

        Vector3 dir = (enemyRoot.transform.position - attackerPosition).normalized;
        kb.ApplyKnockback(dir, damage);
    }

    public static void TryApplyProjectileKnockback(GameObject enemyRoot, Vector3 projectileDirection, int damage)
    {
        var kb = enemyRoot.GetComponentInChildren<EnemyKnockback>();
        if (kb == null || !kb.Object || !kb.Object.HasStateAuthority) return;

        kb.ApplyKnockback(projectileDirection, damage);
    }
}
