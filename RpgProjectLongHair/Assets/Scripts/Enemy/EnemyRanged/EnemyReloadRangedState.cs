using Fusion;
using UnityEngine;

/// <summary>
/// Estado de recarga para enemigos de rango.
/// Mantiene la posición y orientación hacia el jugador mientras recarga.
/// </summary>
public class EnemyReloadRangedState : IEnemyState
{
    private readonly EnemyRangedController _enemy;
    private float _reloadEndTime;

    public EnemyReloadRangedState(EnemyRangedController enemy)
    {
        _enemy = enemy;
    }

    public void EnterState()
    {
        // Calcular cuándo termina la recarga
        _reloadEndTime = _enemy.Runner.SimulationTime + _enemy.RangedAttackData.ReloadDuration;

        // Detener el movimiento del agente
        if (_enemy.Agent != null && _enemy.Agent.isOnNavMesh)
        {
            _enemy.Agent.SetDestination(_enemy.transform.position);
        }
        // Trigger animación de recarga
        _enemy.ExecuteReload();
    }

    public void ExitState()
    {
        _enemy.CompleteReload();
    }

    public void UpdateState()
    {
        if (!_enemy.Object.HasStateAuthority) return;

        // Verificar si el objetivo sigue siendo válido
        if (_enemy.TargetPlayer == null)
        {
            _enemy.ChangeState(new EnemyIdleRangedState(_enemy));
            return;
        }

        if (_enemy.TargetPlayer.TryGetComponent<PlayerHealth>(out var ph) && ph.IsDead)
        {
            _enemy.ChangeState(new EnemyIdleRangedState(_enemy));
            return;
        }

        // Mantener la rotación hacia el jugador durante la recarga
        Vector3 dir = (_enemy.TargetPlayer.position - _enemy.transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
        {
            _enemy.transform.rotation = Quaternion.Slerp(
                _enemy.transform.rotation,
                Quaternion.LookRotation(dir),
                Time.deltaTime * 5f
            );
        }

        // Verificar si la recarga ha terminado
        if (_enemy.Runner.SimulationTime >= _reloadEndTime)
        {
            // Recarga completa, volver al estado de ataque
            _enemy.ChangeState(new EnemyAttackRangedState(_enemy));
        }
    }
}