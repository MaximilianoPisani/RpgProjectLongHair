using UnityEngine;

[CreateAssetMenu(fileName = "Rage_Data", menuName = "Data/RageAbility")]
public class RageData : ScriptableObject
{
    [Header("Charge")]
    public float maxCharge = 200f;
    public float chargePerDamage = 1f;

    [Header("Charge Modes")]
    public bool useHitsInsteadOfDamage = true;
    public float chargePerHit = 10f;
    public float extraChargeFromDamage = 0.2f;

    [Header("Automatic Weapon Multiplier")]
    [Tooltip("Multiplicador de carga para armas automáticas")]
    [Range(0.05f, 1f)]
    public float automaticWeaponChargeMultiplier = 0.35f;

    [Header("Active Buff")]
    public float damageMultiplier = 1.75f;
    public float activeDuration = 8f;
    public KeyCode activationKey = KeyCode.Q;

    [Header("VFX - Per Weapon")]
    public RageWeaponVFXConfig[] weaponVFXConfigs;

    public RageWeaponVFXConfig GetConfigForWeapon(WeaponCategory weapon)
    {
        if (weaponVFXConfigs == null)
            return null;

        foreach (var config in weaponVFXConfigs)
        {
            if (config.weapon == weapon)
                return config;
        }

        return null;
    }
}