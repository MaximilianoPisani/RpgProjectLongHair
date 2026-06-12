using Fusion;
using UnityEngine;

public class EnemyProjectile : NetworkBehaviour
{
    [Networked] private Vector3 Dir { get; set; }
    [Networked] private float Speed { get; set; }
    [Networked] private float MaxRange { get; set; }
    [Networked] private float HitRadius { get; set; }
    [Networked] private int Damage { get; set; }
    [Networked] private Vector3 StartPos { get; set; }
    [Networked] private TickTimer Life { get; set; }
    [Networked] private NetworkId OwnerEnemyId { get; set; }

    private int _playerLayerMask;
    private bool _consumed;
    private int _navMeshWalkableMask;

    [SerializeField] private LayerMask _breakOnLayer;
    [SerializeField] private LayerMask _enemyLayer; // <- asignás el layer Enemy en Inspector

    public void InitServer(Vector3 direction, RangedAttackData data, Vector3 spawnPos, NetworkObject ownerEnemy = null)
    {
        Dir = direction.sqrMagnitude > 0f ? direction.normalized : Vector3.forward;
        Speed = Mathf.Max(0f, data.ProjectileSpeed);
        MaxRange = Mathf.Max(0f, data.AttackRange);
        HitRadius = data.HitRadius > 0f ? data.HitRadius : 0.3f;
        Damage = Mathf.Max(0, data.Damage);
        StartPos = spawnPos;
        Life = TickTimer.CreateFromSeconds(
            Runner,
            data.LifetimeSeconds > 0f ? data.LifetimeSeconds : 5f);

        _playerLayerMask = (int)data.TargetLayer;
        _navMeshWalkableMask = _breakOnLayer.value;
        OwnerEnemyId = ownerEnemy != null ? ownerEnemy.Id : default;

        transform.SetPositionAndRotation(
            spawnPos,
            Dir != Vector3.zero ? Quaternion.LookRotation(Dir) : transform.rotation
        );

        _consumed = false;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Runner.IsRunning || !Object.HasStateAuthority) return;

        if (Speed <= 0f || Dir == Vector3.zero)
        {
            DespawnSafe();
            return;
        }

        Vector3 previousPosition = transform.position;
        transform.position += Dir * Speed * Runner.DeltaTime;
        Vector3 movement = transform.position - previousPosition;
        float distance = movement.magnitude;

        // Colisión con entorno
        if (distance > 0f)
        {
            if (Physics.SphereCast(
                    previousPosition, HitRadius, movement.normalized,
                    out RaycastHit hit, distance,
                    _navMeshWalkableMask, QueryTriggerInteraction.Ignore))
            {
                DespawnSafe();
                return;
            }
        }

        if (_consumed || Damage <= 0) return;

        // 1 — Chequear player
        var playerHits = Physics.OverlapSphere(
            transform.position, HitRadius, _playerLayerMask, QueryTriggerInteraction.Collide);

        if (playerHits != null)
        {
            foreach (var col in playerHits)
            {
                var ph = col.GetComponentInParent<PlayerHealth>();
                if (ph == null) continue;

                ph.TakeDamage(Damage, transform.position);
                _consumed = true;
                DespawnSafe();
                return;
            }
        }

        // 2 — Chequear enemigos (layer separado)
        var enemyHits = Physics.OverlapSphere(
            transform.position, HitRadius, _enemyLayer, QueryTriggerInteraction.Collide);

        if (enemyHits != null)
        {
            foreach (var col in enemyHits)
            {
                var eh = col.GetComponentInParent<EnemyHealth>();
                if (eh == null) continue;
                if (!eh.Object.IsValid) continue;

                // Ignorar al enemigo que disparó
                if (OwnerEnemyId != default && eh.Object.Id == OwnerEnemyId) continue;

                // Ignorar enemigos ya muertos
                if (eh.IsDead) continue;

                eh.ApplyDamageServer(Damage, PlayerRef.None);
                _consumed = true;
                DespawnSafe();
                return;
            }
        }

        // Vida útil / rango máximo
        if (Life.Expired(Runner) ||
            Vector3.Distance(StartPos, transform.position) >= MaxRange)
        {
            DespawnSafe();
        }
    }

    private void DespawnSafe()
    {
        if (Object && Object.HasStateAuthority)
            Runner.Despawn(Object);
    }
}