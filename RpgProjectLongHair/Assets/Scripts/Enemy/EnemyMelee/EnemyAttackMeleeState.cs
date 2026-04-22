using UnityEngine;

public class EnemyAttackMeleeState : IEnemyState
{
    private readonly EnemyMeleeController _enemy;
    private float _attackStartTime;
    private int _executedAttackIndex = -1;
    private bool _damageApplied = false;
    private float ExitRange => _enemy.AttackRange + 0.6f;

    public EnemyAttackMeleeState(EnemyMeleeController enemy)
    {
        _enemy = enemy;
    }

    public void EnterState()
    {
        _executedAttackIndex = -1;
        _damageApplied = false;
        _attackStartTime = 0f;

        // Detener movimiento
        if (_enemy.Agent != null && _enemy.Agent.isOnNavMesh)
        {
            _enemy.Agent.SetDestination(_enemy.transform.position);
        }
    }
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

        // Sistema de ataque con timing del ScriptableObject
        if (_executedAttackIndex == -1)
        {
            // Verificar si puede atacar
            if (_enemy.Runner.SimulationTime >= _enemy.NextMeleeAttackTime)
            {
                // Ejecutar ataque
                _executedAttackIndex = _enemy.ExecuteCurrentAttack();
                _attackStartTime = _enemy.Runner.SimulationTime;
                _damageApplied = false;
            }
        }
        else
        {
            // Aplicar daño en el frame correcto
            float elapsedTime = _enemy.Runner.SimulationTime - _attackStartTime;
            float damageFrame = _enemy.MeleeAttackData.GetDamageFrameTime(_executedAttackIndex);

            if (!_damageApplied && elapsedTime >= damageFrame)
            {
                _enemy.ApplyDamageToTarget();
                _damageApplied = true;
            }

            // Esperar a que termine la animación
            float attackDuration = _enemy.MeleeAttackData.GetAttackDuration(_executedAttackIndex);
            if (elapsedTime >= attackDuration)
            {
                // Ataque completado, resetear para el siguiente
                _executedAttackIndex = -1;
            }
        }
    }
}