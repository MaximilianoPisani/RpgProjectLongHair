using UnityEngine;

public class EnemyAttackMeleeState : IEnemyState
{
    private readonly EnemyMeleeController _enemy;
    private float ExitRange => _enemy.MeleeAttackData.AttackRange + 0.6f;

    public EnemyAttackMeleeState(EnemyMeleeController enemy)
    {
        _enemy = enemy;
    }

    public void EnterState() { }
    public void ExitState() { }

    public void UpdateState()
    {
        if (!_enemy.Object.HasStateAuthority) return;

        if (_enemy.TargetPlayer == null)
        {
            _enemy.ChangeState(new EnemyMeleeIdleState(_enemy));
            return;
        }

        if (_enemy.TargetPlayer.TryGetComponent<PlayerHealth>(out var ph) && ph.IsDead)
        {
            _enemy.OnTargetDied();
            return;
        }

        float dist = Vector3.Distance(_enemy.transform.position, _enemy.TargetPlayer.position);

        if (dist > ExitRange)
        {
            _enemy.ChangeState(new EnemyMeleeChaseState(_enemy));
            return;
        }

        if (_enemy.Agent != null && _enemy.Agent.isOnNavMesh)
        {
            Vector3 dir = (_enemy.TargetPlayer.position - _enemy.transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                _enemy.transform.rotation = Quaternion.LookRotation(dir);

            _enemy.Agent.SetDestination(_enemy.TargetPlayer.position);
        }

        if (_enemy.Runner.SimulationTime >= _enemy.NextAttackTime)
        {
            Collider[] hits = Physics.OverlapSphere(
                _enemy.AttackOrigin.position,
                _enemy.MeleeAttackData.HitRadius,
                _enemy.PlayerLayer
            );

            _enemy.NextAttackTime = _enemy.Runner.SimulationTime + _enemy.MeleeAttackData.Cooldown;

            var alreadyHit = new System.Collections.Generic.HashSet<PlayerHealth>();

            foreach (var hit in hits)
            {
                if (!hit.CompareTag("Player")) continue;

                var playerHealth = hit.GetComponent<PlayerHealth>()
                                ?? hit.GetComponentInParent<PlayerHealth>();

                if (playerHealth == null) continue;
                if (playerHealth.IsDead) continue;
                if (!alreadyHit.Add(playerHealth)) continue; 

                playerHealth.TakeDamage(_enemy.MeleeAttackData.Damage, _enemy.transform.position);
                Debug.Log($"[Enemy] Hit player for {_enemy.MeleeAttackData.Damage} damage.");
            }
        }
    }
}