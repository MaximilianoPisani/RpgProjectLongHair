using UnityEngine;
using UnityEngine.UI;

public class WeaponHUDIcon : MonoBehaviour
{
    [SerializeField] private Image _weaponIconImage;

    private void OnEnable()
    {
        EquipManager.OnLocalWeaponEquipped += HandleWeaponEquipped;
        HandleWeaponEquipped(null); 
    }

    private void OnDisable()
    {
        EquipManager.OnLocalWeaponEquipped -= HandleWeaponEquipped;
    }

    private void HandleWeaponEquipped(ItemSO item)
    {
        if (item == null)
        {
            _weaponIconImage.enabled = false;
            _weaponIconImage.sprite = null;
            return;
        }

        _weaponIconImage.sprite = item.icon;
        _weaponIconImage.enabled = true;
    }
}