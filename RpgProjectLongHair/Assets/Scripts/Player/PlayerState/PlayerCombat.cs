using UnityEngine;
using Fusion;

public class PlayerCombat : NetworkBehaviour
{
    [Networked] public WeaponCategory CurrentWeapon { get; set; }

    [Header("Melee")]
    public MeleeAttackData meleeData;

    [Header("Common Melee")]
    public Transform meleeOrigin;
    public LayerMask enemyLayer;

    [Header("Range")]
    public RangedAttackData RangeData;

    [Header("Common Range")]
    public Transform[] shootPoints;

    public MeleeAttackData GetCurrentMeleeData()
    {
        switch (CurrentWeapon)
        {
            case WeaponCategory.Axe:
                return meleeData;

            case WeaponCategory.Hammer:
                return meleeData;
        }

        return null;
    }

    public RangedAttackData GetCurrentRangeData()
    {
        switch (CurrentWeapon)
        {
            case WeaponCategory.Rifle:
                return RangeData;

            case WeaponCategory.Gatling:
                return RangeData;
        }

        return null;
    }
}