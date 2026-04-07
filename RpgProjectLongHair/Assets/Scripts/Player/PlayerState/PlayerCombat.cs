using UnityEngine;
using Fusion;

public class PlayerCombat : NetworkBehaviour
{
    [Networked] public WeaponCategory CurrentWeapon { get; set; }

    [Header("Melee - Axe")]
    public MeleeAttackData axeMeleeData;

    [Header("Melee - Hammer")]
    public MeleeAttackData hammerMeleeData;

    [Header("Common Melee")]
    public Transform meleeOrigin;
    public LayerMask enemyLayer;

    [Header("Range - Rifle")]
    public RangedAttackData rifleRangeData;

    [Header("Range - Gatling")]
    public RangedAttackData gatlingRangeData;

    [Header("Common Range")]
    public Transform[] shootPoints;

    public MeleeAttackData GetCurrentMeleeData()
    {
        switch (CurrentWeapon)
        {
            case WeaponCategory.Axe:
                return axeMeleeData;

            case WeaponCategory.Hammer:
                return hammerMeleeData;
        }

        return null;
    }

    public RangedAttackData GetCurrentRangeData()
    {
        switch (CurrentWeapon)
        {
            case WeaponCategory.Rifle:
                return rifleRangeData;

            case WeaponCategory.Gatling:
                return gatlingRangeData;
        }

        return null;
    }
}