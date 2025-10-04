using Fusion;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    // Parámetros de runtime (replicados para debug/rejoin)
    [Networked] private Vector3 Dir { get; set; }
    [Networked] private float Speed { get; set; }
    [Networked] private float MaxRange { get; set; }
    [Networked] private float HitRadius { get; set; }
    [Networked] private int Damage { get; set; }
    [Networked] private Vector3 StartPos { get; set; }
    [Networked] private TickTimer Life { get; set; }
    [Networked] private PlayerRef Attacker { get; set; }

    // No es necesario networkear estos
    private int _targetLayerMask;
    private bool _consumed;

    /// Llamar SOLO desde el server (en Spawn callback)
    public void InitServer(Vector3 direction, RangedAttackData data, PlayerRef attacker, Vector3 spawnPos)
    {
        Dir = direction.sqrMagnitude > 0f ? direction.normalized : Vector3.forward;
        Speed = Mathf.Max(0f, data.ProjectileSpeed);
        MaxRange = Mathf.Max(0f, data.AttackRange);
        HitRadius = data.HitRadius > 0f ? data.HitRadius : 0.3f;
        Damage = Mathf.Max(0, data.Damage);
        StartPos = spawnPos;
        Attacker = attacker;
        Life = TickTimer.CreateFromSeconds(Runner, data.LifetimeSeconds > 0f ? data.LifetimeSeconds : 5f);

        _targetLayerMask = (int)data.TargetLayer;

        transform.SetPositionAndRotation(
            spawnPos,
            Dir != Vector3.zero ? Quaternion.LookRotation(Dir) : transform.rotation
        );

        _consumed = false;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Runner.IsRunning) return;
        if (!Object.HasStateAuthority) return;        // Server moves/applies

        if (Speed <= 0f || Dir == Vector3.zero)
        {
            DespawnSafe();
            return;
        }

        // Avanzar
        transform.position += Dir * Speed * Runner.DeltaTime;

        // Impacto server-side (PhysX). No hace falta LagComp: el server tiene la verdad de la posición.
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
                // Buscamos un Hitbox (Fusion) para mapear a EnemyHealth
                for (int i = 0; i < hits.Length; i++)
                {
                    var hb = hits[i].GetComponentInParent<Hitbox>();
                    if (hb == null) continue;

                    if (EnemyHealth.TryApplyFromHitbox(hb, Damage, Attacker))
                    {
                        _consumed = true;
                        DespawnSafe();
                        return;
                    }
                }

                // Si no hay Hitbox, probamos directo EnemyHealth (por si no usás hitboxes en un target)
                for (int i = 0; i < hits.Length; i++)
                {
                    var eh = hits[i].GetComponentInParent<EnemyHealth>();
                    if (eh == null || !eh.Object || !eh.Object.HasStateAuthority) continue;

                    eh.ApplyDamageServer(Damage, Attacker);
                    _consumed = true;
                    DespawnSafe();
                    return;
                }
            }
        }

        // Vida / alcance
        if (Life.Expired(Runner) || Vector3.Distance(StartPos, transform.position) >= MaxRange)
            DespawnSafe();
    }

    private void DespawnSafe()
    {
        if (Object.HasStateAuthority)
            Runner.Despawn(Object);
    }
}
