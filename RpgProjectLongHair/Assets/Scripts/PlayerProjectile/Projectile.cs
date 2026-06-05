using Fusion;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    [Networked] private Vector3 Dir { get; set; }
    [Networked] private float Speed { get; set; }
    [Networked] private float MaxRange { get; set; }
    [Networked] private float HitRadius { get; set; }
    [Networked] private int Damage { get; set; }
    [Networked] private Vector3 StartPos { get; set; }
    [Networked] private TickTimer Life { get; set; }
    [Networked] private PlayerRef Attacker { get; set; }

    private int _targetLayerMask;
    private bool _consumed;
    private int _navMeshWalkableMask;

    private const string ENVIRONMENT_TAG = "Environment";

    public void InitServer(Vector3 direction, RangedAttackData data, PlayerRef attacker, Vector3 spawnPos)
    {
        Dir = direction.sqrMagnitude > 0f ? direction.normalized : Vector3.forward;
        Speed = Mathf.Max(0f, data.ProjectileSpeed);
        MaxRange = Mathf.Max(0f, data.AttackRange);
        HitRadius = data.HitRadius > 0f ? data.HitRadius : 0.3f;
        Damage = Mathf.Max(0, data.Damage);
        StartPos = spawnPos;
        Attacker = attacker;
        Life = TickTimer.CreateFromSeconds(
            Runner,
            data.LifetimeSeconds > 0f ? data.LifetimeSeconds : 5f);

        _targetLayerMask = data.TargetLayer.value;

        _navMeshWalkableMask = LayerMask.GetMask("NavMeshWalkable");

        transform.SetPositionAndRotation(
            spawnPos,
            Dir != Vector3.zero
                ? Quaternion.LookRotation(Dir)
                : transform.rotation
        );

        _consumed = false;

        IgnoreNonTargetColliders();
    }

    private void IgnoreNonTargetColliders()
    {
        var ownCollider = GetComponent<Collider>();
        if (ownCollider == null) return;

        var allColliders = Physics.OverlapSphere(transform.position, 500f);
        foreach (var col in allColliders)
        {
            if (((_targetLayerMask >> col.gameObject.layer) & 1) == 0)
                Physics.IgnoreCollision(ownCollider, col, true);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Runner.IsRunning || !Object.HasStateAuthority)
            return;

        if (Speed <= 0f || Dir == Vector3.zero)
        {
            DespawnSafe();
            return;
        }

        Vector3 previousPosition = transform.position;

        transform.position += Dir * Speed * Runner.DeltaTime;

        Vector3 movement = transform.position - previousPosition;
        float distance = movement.magnitude;

        if (distance > 0f)
        {
            if (Physics.SphereCast(
                    previousPosition,
                    HitRadius,
                    movement.normalized,
                    out RaycastHit hit,
                    distance,
                    _navMeshWalkableMask,
                    QueryTriggerInteraction.Ignore))
            {
                DespawnSafe();
                return;
            }
        }

        if (!_consumed && Damage > 0)
        {
            var hits = Physics.OverlapSphere(
                transform.position,
                HitRadius,
                _targetLayerMask,
                QueryTriggerInteraction.Collide
            );

            if (hits != null && hits.Length > 0)
            {
                foreach (var col in hits)
                {
                    var damageable = col.GetComponentInParent<DamageableObject>();

                    if (damageable != null && damageable.Object.HasStateAuthority)
                    {
                        int finalDamage = GetFinalDamage();

                        damageable.ApplyDamageServer(
                            finalDamage,
                            Attacker
                        );

                        _consumed = true;
                        DespawnSafe();
                        return;
                    }

                    var hb = col.GetComponentInParent<Hitbox>();
                    var eh = col.GetComponentInParent<EnemyHealth>();

                    if (hb != null)
                    {
                        int finalDamage = GetFinalDamage();

                        if (EnemyHealth.TryApplyFromHitbox(
                            hb,
                            finalDamage,
                            Attacker))
                        {
                            PlayerRageHandler.NotifyDamageDealt(
                                Attacker,
                                finalDamage
                            );

                            EnemyKnockback.TryApplyProjectileKnockback(
                                hb.Root.gameObject,
                                Dir,
                                finalDamage
                            );

                            _consumed = true;
                            DespawnSafe();
                            return;
                        }
                    }

                    if (eh != null &&
                        eh.Object &&
                        eh.Object.HasStateAuthority)
                    {
                        int finalDamage = GetFinalDamage();

                        eh.ApplyDamageServer(
                            finalDamage,
                            Attacker
                        );

                        PlayerRageHandler.NotifyDamageDealt(
                            Attacker,
                            finalDamage
                        );

                        EnemyKnockback.TryApplyProjectileKnockback(
                            eh.gameObject,
                            Dir,
                            finalDamage
                        );

                        _consumed = true;
                        DespawnSafe();
                        return;
                    }
                }
            }
        }

        if (Life.Expired(Runner) ||
            Vector3.Distance(StartPos, transform.position) >= MaxRange)
        {
            DespawnSafe();
        }
    }

    private int GetFinalDamage()
    {
        float multiplier = 1f;

        if (Runner.TryGetPlayerObject(Attacker, out NetworkObject playerObj))
        {
            var rage = playerObj.GetComponent<PlayerRageHandler>();
            if (rage != null) multiplier = rage.GetDamageMultiplier();
        }

        return Mathf.RoundToInt(Damage * multiplier);
    }

    private void DespawnSafe()
    {
        if (Object && Object.HasStateAuthority)
            Runner.Despawn(Object);
    }
}