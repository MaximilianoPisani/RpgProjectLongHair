using UnityEngine;

public class EnemyKamikazeIdleState : IEnemyState
{
    private readonly EnemyKamikazeController _enemy;

    public EnemyKamikazeIdleState(EnemyKamikazeController enemy) => _enemy = enemy;

    public void EnterState() { }
    public void ExitState() { }

    public void UpdateState()
    {
        if (!_enemy.Object.HasStateAuthority) return;

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        Transform closest = null;
        float minDist = float.MaxValue;

        foreach (var p in players)
        {
            if (p.TryGetComponent<PlayerHealth>(out var ph) && ph.IsDead) continue;

            float d = Vector3.Distance(_enemy.transform.position, p.transform.position);
            if (d < minDist && d <= _enemy.DetectionRadius)
            {
                minDist = d;
                closest = p.transform;
            }
        }

        if (closest != null)
        {
            _enemy.SetTarget(closest);
            _enemy.ChangeState(new EnemyKamikazeChaseState(_enemy));
        }
    }
}