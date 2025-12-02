using UnityEngine;

public class WeaponAttackToggler : MonoBehaviour
{
    private EquipManager _equipManager;
    private PlayerMeleeAttack _meleeAttack;
    private PlayerRangeAttack _rangeAttack;

    private void Awake()
    {
        _equipManager = GetComponentInChildren<EquipManager>();
        _meleeAttack = GetComponent<PlayerMeleeAttack>();
        _rangeAttack = GetComponent<PlayerRangeAttack>();
    }

    private void Start()
    {
        SetAttackScriptsActive(false);
    }

    private void Update()
    {
        if (_equipManager == null) return;

        int equippedId = _equipManager.EquippedItemId;

        if (equippedId == 0)
        {
            SetAttackScriptsActive(false);
            return;
        }

        ItemSO equippedItem = ItemDatabase.GetItemByIdStatic(equippedId);

        if (equippedItem == null)
        {
            SetAttackScriptsActive(false);
            return;
        }

        if (equippedItem.type != ItemType.Weapon)
        {
            SetAttackScriptsActive(false);
            return;
        }

        switch (equippedItem.weaponCategory)
        {
            case WeaponCategory.Melee:
                SetAttackScriptsActive(true, isMelee: true);
                break;

            case WeaponCategory.Ranged:
                SetAttackScriptsActive(true, isMelee: false);
                break;

            default:
                SetAttackScriptsActive(false);
                break;
        }
    }

    private void SetAttackScriptsActive(bool enabled, bool isMelee = false)
    {
        if (_meleeAttack != null)
            _meleeAttack.enabled = enabled && isMelee;

        if (_rangeAttack != null)
            _rangeAttack.enabled = enabled && !isMelee;
    }
}