using UnityEngine;
using System.Collections.Generic;

public class EnemyKamikazeExplodeState : IEnemyState
{
    private readonly EnemyKamikazeController _enemy;
    private bool _exploded = false;

    public EnemyKamikazeExplodeState(EnemyKamikazeController enemy) => _enemy = enemy;

    public void EnterState()
    {
        if (!_enemy.Object.HasStateAuthority) return;
        Explode();
    }

    public void ExitState() { }
    public void UpdateState() { }

    private void Explode()
    {
        if (_exploded) return;
        _exploded = true;

        if (_enemy.Agent != null && _enemy.Agent.isOnNavMesh)
            _enemy.Agent.SetDestination(_enemy.transform.position);

        int damage = _enemy.KamikazeData.Damage;
        float radius = _enemy.KamikazeData.HitRadius;

        Collider[] hits = Physics.OverlapSphere(
            _enemy.transform.position,
            radius,
            _enemy.PlayerLayer
        );

        var alreadyHit = new HashSet<PlayerHealth>();

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;

            var ph = hit.GetComponent<PlayerHealth>()
                   ?? hit.GetComponentInParent<PlayerHealth>();

            if (ph == null) continue;
            if (ph.IsDead) continue;
            if (!alreadyHit.Add(ph)) continue;

            ph.TakeDamage(damage, _enemy.transform.position);
        }

        SpawnVFX();
        _enemy.ChangeState(new EnemyDeathState(_enemy));
    }

    private void SpawnVFX()
    {
        var prefab = _enemy.KamikazeData.ExplosionVFXPrefab;
        if (prefab == null) return;

        GameObject vfx = GameObject.Instantiate(
            prefab,
            _enemy.transform.position,
            Quaternion.identity
        );
        GameObject.Destroy(vfx, 3f);
    }
}