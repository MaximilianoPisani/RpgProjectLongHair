using UnityEngine;
using Fusion;

public class PlayerCombat : NetworkBehaviour
{
    [Header("Melee")]
    public MeleeAttackData meleeData;
    public Transform meleeOrigin;
    public LayerMask enemyLayer;

    [Header("Range")]
    public RangedAttackData rangeData;
    public Transform[] shootPoints;
}