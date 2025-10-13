using UnityEngine;

public class EnemyIdleState : IEnemyState
{
    private readonly EnemyController _enemy;

    public EnemyIdleState(EnemyController enemy)
    {
        _enemy = enemy;
    }

    public void EnterState() { }
    public void ExitState() { }

    public void UpdateState()
    {
        Collider[] players = Physics.OverlapSphere(
            _enemy.transform.position,
            _enemy.DetectionRadius,
            _enemy.PlayerLayer
        );

        if (players.Length > 0)
        {
            _enemy.SetTarget(players[0].transform);
            _enemy.ChangeState(new EnemyChaseState(_enemy));
        }
    }
}