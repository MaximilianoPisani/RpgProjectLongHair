using UnityEngine;
using Fusion;

[CreateAssetMenu(fileName = "RangedAttack_000_Data", menuName = "Data/RangedAttack")]
public class RangedAttackData : AttackData
{
    [SerializeField] private NetworkObject _projectilePrefab;

    [Min(0f)]
    [SerializeField] private float _projectileSpeed = 25f;

    [Min(0f)]
    [SerializeField] private float _lifetimeSeconds = 5f;

    [SerializeField] private LayerMask _targetLayer;

    public NetworkObject ProjectilePrefab => _projectilePrefab;
    public float ProjectileSpeed => _projectileSpeed;
    public float LifetimeSeconds => _lifetimeSeconds;
    public LayerMask TargetLayer => _targetLayer;

    protected override void OnValidate()
    {
        base.OnValidate();

        if (_projectileSpeed < 0f) _projectileSpeed = 0f;
        if (_lifetimeSeconds < 0f) _lifetimeSeconds = 0f;


        if (AttackRange <= 0f && _lifetimeSeconds <= 0f)
            _lifetimeSeconds = 5f;
    }
}