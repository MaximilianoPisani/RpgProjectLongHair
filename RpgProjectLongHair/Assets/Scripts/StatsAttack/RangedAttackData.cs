using UnityEngine;
using Fusion;

[CreateAssetMenu(fileName = "RangedAttack_000_Data", menuName = "Data/RangedAttack")]
public class RangedAttackData : AttackData
{
    [SerializeField] private float _projectileSpeed;
    [SerializeField] private NetworkObject _projectilePrefab;
    public float ProjectileSpeed => _projectileSpeed;
    public NetworkObject ProjectilePrefab => _projectilePrefab;

    public LayerMask TargetLayer;
    protected override void OnValidate()
    {
        base.OnValidate();
        if (_projectileSpeed < 0) _projectileSpeed = 0;
    }
}