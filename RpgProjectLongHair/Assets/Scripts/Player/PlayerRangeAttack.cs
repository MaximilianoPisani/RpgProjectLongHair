using UnityEngine;
using Fusion;

public class PlayerRangeAttack : NetworkBehaviour
{
    [SerializeField] private RangedAttackData _attackData;
    [SerializeField] private Transform[] _spawnPoints; 

    private bool _canAttack = true;

    public override void Spawned()
    {
        if (_attackData == null)
            Debug.LogWarning("RangedAttackData not assigned!");
        if (_spawnPoints == null || _spawnPoints.Length == 0)
            Debug.LogWarning("Spawn points not assigned!");
    }

    void Update()
    {
        if (!HasInputAuthority) return;

        if (Input.GetMouseButtonDown(1) && _canAttack)
        {
            Transform spawnPoint = GetClosestSpawnPoint();
            Vector3 shootDir = transform.forward; 
            RPC_Shoot(spawnPoint.position, shootDir);

            _canAttack = false;
            Invoke(nameof(ResetAttack), _attackData.Cooldown);
        }
    }

    private void ResetAttack() => _canAttack = true;

    private Transform GetClosestSpawnPoint()
    {
        if (_spawnPoints.Length == 1) return _spawnPoints[0];

        Transform bestPoint = _spawnPoints[0];
        float bestDot = -1f;

        foreach (var point in _spawnPoints)
        {
            Vector3 dirToPoint = (point.position - transform.position).normalized;
            float dot = Vector3.Dot(transform.forward, dirToPoint);
            if (dot > bestDot)
            {
                bestDot = dot;
                bestPoint = point;
            }
        }

        return bestPoint;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_Shoot(Vector3 spawnPos, Vector3 direction)
    {
        NetworkObject projObj = Runner.Spawn(
            _attackData.ProjectilePrefab,
            spawnPos,
            Quaternion.LookRotation(direction),
            Object.InputAuthority
        );

        var proj = projObj.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.InitProjectile(direction, _attackData, gameObject);
        }
    }
}