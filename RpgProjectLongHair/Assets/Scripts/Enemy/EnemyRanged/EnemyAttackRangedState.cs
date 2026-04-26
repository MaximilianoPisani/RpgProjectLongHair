using Fusion;
using UnityEngine;

public class EnemyAttackRangedState : IEnemyState
{
    private readonly EnemyRangedController _enemy;
    private float _shotStartTime;
    private bool _shotTriggered;
    private bool _projectileSpawned;
    private readonly bool _comingFromReload;

    // Para modo automático
    private float _continuousFireStartTime;
    private int _shotsFired;
    public EnemyAttackRangedState(EnemyRangedController enemy, bool comingFromReload = false)
    {
        _enemy = enemy;
        _comingFromReload = comingFromReload;
    }
    public void EnterState()
    {

        _shotTriggered = false;
        _projectileSpawned = false;
        _shotStartTime = 0f;
        _continuousFireStartTime = _enemy.Runner.SimulationTime;
        _shotsFired = 0;

        if (!_comingFromReload)
            _enemy.VFXController?.SpawnAttackIndicator();

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

        float dist = Vector3.Distance(_enemy.transform.position, _enemy.TargetPlayer.position);

        // Si está muy lejos, perseguir
        if (dist > _enemy.PreferredMaxRange + 1f)
        {
            _enemy.ChangeState(new EnemyChaseRangedState(_enemy));
            return;
        }

        // Gestión de posicionamiento
        if (_enemy.Agent != null && _enemy.Agent.isOnNavMesh)
        {
            if (dist < _enemy.PreferredMinRange)
            {
                // Retroceder si está muy cerca
                Vector3 retreatDir = (_enemy.transform.position - _enemy.TargetPlayer.position).normalized;
                Vector3 retreatTarget = _enemy.transform.position + retreatDir * 2f;
                _enemy.Agent.SetDestination(retreatTarget);
            }
            else
            {
                // Mantener posición
                _enemy.Agent.SetDestination(_enemy.transform.position);
            }
        }

        // Rotar hacia el objetivo
        Vector3 dir = (_enemy.TargetPlayer.position - _enemy.transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            _enemy.transform.rotation = Quaternion.LookRotation(dir);

        // Sistema de disparo según el modo de fuego
        if (_enemy.RangedAttackData.Mode == FireMode.SingleShot)
        {
            HandleSingleShotWithTiming();
        }
        else if (_enemy.RangedAttackData.Mode == FireMode.Automatic)
        {
            HandleAutomaticFireWithTiming();
        }
    }

    /// <summary>
    /// Maneja el disparo para modo Single Shot (Mosquete)
    /// Dispara una vez y luego entra en estado de recarga
    /// </summary>
    private void HandleSingleShotWithTiming()
    {
        if (!_shotTriggered)
        {
            // Verificar si puede disparar
            if (_enemy.Runner.SimulationTime >= _enemy.NextRangedAttackTime)
            {
                // Trigger animación y VFX
                _enemy.ExecuteShot();
                _shotTriggered = true;
                _shotStartTime = _enemy.Runner.SimulationTime;
                _projectileSpawned = false;
            }
        }
        else
        {
            float elapsedTime = _enemy.Runner.SimulationTime - _shotStartTime;

            // Spawnear proyectil en el frame correcto
            if (!_projectileSpawned && elapsedTime >= _enemy.RangedAttackData.ShootFrameTime)
            {
                _enemy.FireProjectile();
                _projectileSpawned = true;
            }

            // Esperar a que termine la animación de disparo
            if (elapsedTime >= _enemy.RangedAttackData.ShootDuration)
            {
                // Disparo completo, cambiar a estado de recarga
                _enemy.ChangeState(new EnemyReloadRangedState(_enemy));
            }
        }
    }

    /// <summary>
    /// Maneja el disparo para modo Automático (Gatling)
    /// Dispara ráfagas y recarga cuando sea necesario
    /// </summary>
    private void HandleAutomaticFireWithTiming()
    {
        // Verificar si necesita recargar después de disparar continuamente
        if (_enemy.RangedAttackData.MaxContinuousFireTime > 0f)
        {
            float continuousFireElapsed = _enemy.Runner.SimulationTime - _continuousFireStartTime;

            if (continuousFireElapsed >= _enemy.RangedAttackData.MaxContinuousFireTime)
            {
                // Tiempo de ráfaga completo, entrar en recarga
                _enemy.ChangeState(new EnemyReloadRangedState(_enemy));
                return;
            }
        }

        if (!_shotTriggered)
        {
            // Verificar si puede disparar
            if (_enemy.Runner.SimulationTime >= _enemy.NextRangedAttackTime)
            {
                // Trigger animación y VFX
                _enemy.ExecuteShot();
                _shotTriggered = true;
                _shotStartTime = _enemy.Runner.SimulationTime;
                _projectileSpawned = false;
                _shotsFired++;

                // Siguiente disparo según el fire rate
                _enemy.NextRangedAttackTime = _enemy.Runner.SimulationTime + _enemy.RangedAttackData.FireRate;
            }
        }
        else
        {
            float elapsedTime = _enemy.Runner.SimulationTime - _shotStartTime;

            // Spawnear proyectil en el frame correcto
            if (!_projectileSpawned && elapsedTime >= _enemy.RangedAttackData.ShootFrameTime)
            {
                _enemy.FireProjectile();
                _projectileSpawned = true;
            }

            // Esperar a que termine la animación para el siguiente disparo
            if (elapsedTime >= _enemy.RangedAttackData.ShootDuration)
            {
                _shotTriggered = false; // Permitir siguiente disparo en la ráfaga
            }
        }
    }
}