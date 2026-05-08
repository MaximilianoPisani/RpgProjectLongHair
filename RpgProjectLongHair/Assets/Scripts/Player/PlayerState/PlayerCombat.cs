using Fusion;
using UnityEngine;

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

    private PlayerVFXSync _vfxSync;

    public override void Spawned()
    {
        _vfxSync = GetComponent<PlayerVFXSync>();
    }

    public MeleeAttackData GetCurrentMeleeData()
    {
        switch (CurrentWeapon)
        {
            case WeaponCategory.Axe:
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
            case WeaponCategory.Gatling:
                return RangeData;
        }
        return null;
    }

    public void SpawnSlashVFX(AttackVFXConfig config)
        => _vfxSync?.SpawnSlashVFX(config);

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetCurrentWeapon(WeaponCategory weapon)
    {
        CurrentWeapon = weapon;
    }
}