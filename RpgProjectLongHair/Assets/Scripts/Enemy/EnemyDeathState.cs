using UnityEngine;

public class EnemyDeathState : IEnemyState
{
    private readonly EnemyBaseController _enemy;  

    public EnemyDeathState(EnemyBaseController enemy)
    {
        _enemy = enemy;
    }

    public void EnterState()
    {
        Debug.Log($"{_enemy.name} dead.");
        if (_enemy.Runner != null && _enemy.Object.HasStateAuthority)
            _enemy.Runner.Despawn(_enemy.Object);
    }

    public void ExitState() { }
    public void UpdateState() { }
}