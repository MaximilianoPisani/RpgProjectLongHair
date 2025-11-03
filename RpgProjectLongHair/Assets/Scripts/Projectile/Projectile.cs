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
        if (!Runner.IsRunning || !Object.HasStateAuthority)
            return;

        if (Speed <= 0f || Dir == Vector3.zero)
        {
            DespawnSafe();
            return;
        }

        Vector3 oldPos = transform.position;
        Vector3 newPos = oldPos + Dir * Speed * Runner.DeltaTime;

        float distance = Vector3.Distance(oldPos, newPos);
        if (Physics.Raycast(oldPos, Dir, out RaycastHit environmentHit, distance + HitRadius))
        {
            if (environmentHit.collider.CompareTag(ENVIRONMENT_TAG))
            {
                _consumed = true;
                DespawnSafe();
                return;
            }
        }

        transform.position = newPos;

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
                    var hb = col.GetComponentInParent<Hitbox>();
                    var eh = col.GetComponentInParent<EnemyHealth>();

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
        if (Object && Object.HasStateAuthority)
            Runner.Despawn(Object);
    }
}