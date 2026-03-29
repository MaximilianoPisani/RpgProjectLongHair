using UnityEngine;

public class PlayerWeaponHandler : MonoBehaviour
{
    private EquipManager _equipManager;

    public bool IsMelee { get; private set; }
    public bool IsRanged { get; private set; }

    private void Awake()
    {
        _equipManager = GetComponentInChildren<EquipManager>();
    }

    private void OnEnable()
    {
        if (_equipManager != null)
            _equipManager.OnEquippedChanged += OnWeaponChanged;
    }

    private void OnDisable()
    {
        if (_equipManager != null)
            _equipManager.OnEquippedChanged -= OnWeaponChanged;
    }

    private void OnWeaponChanged(int equippedId)
    {
        if (equippedId == 0)
        {
            SetWeapon(false, false);
            return;
        }

        ItemSO item = ItemDatabase.GetItemByIdStatic(equippedId);

        if (item == null || item.type != ItemType.Weapon)
        {
            SetWeapon(false, false);
            return;
        }

        switch (item.weaponCategory)
        {
            case WeaponCategory.Melee:
                SetWeapon(true, false);
                break;

            case WeaponCategory.Ranged:
                SetWeapon(false, true);
                break;
        }
    }

    private void SetWeapon(bool melee, bool ranged)
    {
        IsMelee = melee;
        IsRanged = ranged;
    }
}