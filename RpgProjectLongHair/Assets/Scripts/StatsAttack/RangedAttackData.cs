using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RangedAttack_000_Data", menuName = "Data/RangedAttack")]
public class RangedAttackData : AttackData
{
    [SerializeField] private float _projectileSpeed;
    [SerializeField] private GameObject _projectilePrefab;

    public float ProjectileSpeed => _projectileSpeed;
    public GameObject ProjectilePrefab => _projectilePrefab;

    public LayerMask TargetLayer;
    protected override void OnValidate()
    {
        base.OnValidate();
        if (_projectileSpeed < 0) _projectileSpeed = 0;
    }
}