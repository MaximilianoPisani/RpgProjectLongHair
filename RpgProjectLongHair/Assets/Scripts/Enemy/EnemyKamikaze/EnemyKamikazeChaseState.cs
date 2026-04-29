using UnityEngine;

public class EnemyKamikazeChaseState : IEnemyState
{
    private readonly EnemyKamikazeController _enemy;
    private float _recheckTargetInterval = 0.5f;
    private float _nextRecheckTime = 0f;

    public EnemyKamikazeChaseState(EnemyKamikazeController enemy) => _enemy = enemy;

    public void EnterState()
    {
        _nextRecheckTime = Time.time + _recheckTargetInterval;
    }

    public void ExitState() { }

    public void UpdateState()
    {
        if (!_enemy.Object.HasStateAuthority) return;

        // Verificar si el objetivo sigue siendo válido
        if (_enemy.TargetPlayer == null || !IsTargetValid())
        {
            _enemy.ChangeState(new EnemyKamikazeIdleState(_enemy));
            return;
        }

        // Calcular distancia al objetivo
        float distanceToTarget = Vector3.Distance(
            _enemy.transform.position,
            _enemy.TargetPlayer.position
        );

        // Verificar si está en rango de explosión
        if (distanceToTarget <= _enemy.KamikazeData.ExplodeDistance)
        {
            _enemy.ChangeState(new EnemyKamikazeExplodeState(_enemy));
            return;
        }

        // Revisar si el objetivo se alejó demasiado (opcional, para optimización)
        if (Time.time >= _nextRecheckTime)
        {
            _nextRecheckTime = Time.time + _recheckTargetInterval;

            // Si el objetivo está muy lejos del rango de detección, volver a idle
            if (distanceToTarget > _enemy.DetectionRadius * 1.5f)
            {
                _enemy.ChangeState(new EnemyKamikazeIdleState(_enemy));
                return;
            }
        }

        // Moverse hacia el objetivo
        if (_enemy.Agent != null && _enemy.Agent.isOnNavMesh)
        {
            _enemy.Agent.SetDestination(_enemy.TargetPlayer.position);
        }
    }

    /// <summary>
    /// Verifica si el objetivo actual sigue siendo válido (no está muerto).
    /// </summary>
    private bool IsTargetValid()
    {
        if (_enemy.TargetPlayer == null) return false;

        var playerHealth = _enemy.TargetPlayer.GetComponent<PlayerHealth>()
                        ?? _enemy.TargetPlayer.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null && playerHealth.IsDead)
            return false;

        return true;
    }
}