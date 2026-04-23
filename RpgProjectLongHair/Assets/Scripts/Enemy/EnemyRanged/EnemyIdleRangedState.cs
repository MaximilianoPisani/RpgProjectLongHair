using UnityEngine;

public class EnemyIdleRangedState : IEnemyState
{
    private readonly EnemyRangedController _enemy;

    public EnemyIdleRangedState(EnemyRangedController enemy) => _enemy = enemy;

    public void EnterState() 
    {
        // Resetear estado de recarga
        _enemy.IsReloading = false;
    }
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
            _enemy.ChangeState(new EnemyChaseRangedState(_enemy));
        }
    }
}