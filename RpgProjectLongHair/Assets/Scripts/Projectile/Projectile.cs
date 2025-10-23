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
        if (!Object.HasStateAuthority) return;      

        if (Speed <= 0f || Dir == Vector3.zero)
        {
            DespawnSafe();
            return;
        }

        transform.position += Dir * Speed * Runner.DeltaTime;

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
                foreach (var hit in hits)
                {
                    var hb = hit.GetComponentInParent<Hitbox>();
                    var eh = hit.GetComponentInParent<EnemyHealth>();

                    if (hb != null && EnemyHealth.TryApplyFromHitbox(hb, Damage, Attacker))
                    {
                        _consumed = true;
                        DespawnSafe();
                        return;
                    }

                    if (eh != null && eh.Object && eh.Object.HasStateAuthority)
                    {
                        eh.ApplyDamageServer(Damage, Attacker);
                        _consumed = true;
                        DespawnSafe();
                        return;
                    }
                }
            }
        }

        if (Life.Expired(Runner) || Vector3.Distance(StartPos, transform.position) >= MaxRange)
            DespawnSafe();
    }

    private void DespawnSafe()
    {
        if (Object.HasStateAuthority)
            Runner.Despawn(Object);
    }
}
