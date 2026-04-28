using UnityEngine;

/// <summary>
/// Estado que se activa cuando el kamikaze recibe daño letal del jugador
/// (disparo o melee). En lugar de morir silenciosamente, primero explota.
/// </summary>
public class EnemyKamikazeDyingState : IEnemyState
{
    private readonly EnemyKamikazeController _enemy;

    public EnemyKamikazeDyingState(EnemyKamikazeController enemy) => _enemy = enemy;

    public void EnterState()
    {
        if (!_enemy.Object.HasStateAuthority) return;

        // Detener movimiento
        if (_enemy.Agent != null && _enemy.Agent.isOnNavMesh)
            _enemy.Agent.SetDestination(_enemy.transform.position);

        // Reutiliza la lógica completa del estado de explosión
        _enemy.ChangeState(new EnemyKamikazeExplodeState(_enemy));
    }

    public void ExitState() { }
    public void UpdateState() { }
}