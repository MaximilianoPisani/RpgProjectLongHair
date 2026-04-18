using UnityEngine;

[System.Serializable]
public class RageWeaponVFXConfig
{
    public WeaponCategory weapon;

    [Header("Melee - un VFX por cada golpe del combo (mismo orden que ComboAttacks)")]
    public AttackVFXConfig[] comboVFX;

    [Header("Ranged")]
    public AttackVFXConfig rageFireEjectionVFX;
    public AttackVFXConfig rageShellEjectionVFX;

    public AttackVFXConfig GetComboVFX(int comboIndex)
    {
        int i = comboIndex - 1;
        if (comboVFX == null || i < 0 || i >= comboVFX.Length) return null;
        return comboVFX[i];
    }
}