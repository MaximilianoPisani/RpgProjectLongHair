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

        if (_equipManager.IsEquipped())
        {
            var equipped = _equipManager.GetCurrentEquippedItemSO();
            if (equipped == null)
            {
                SetAttackScriptsActive(false);
                return;
            }

            if (equipped.type == ItemType.Weapon)
            {
                switch (equipped.weaponCategory)
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
            else
            {
                SetAttackScriptsActive(false);
            }
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