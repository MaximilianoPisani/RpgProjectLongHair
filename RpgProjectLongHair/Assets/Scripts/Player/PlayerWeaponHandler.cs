using UnityEngine;

public class PlayerWeaponHandler : MonoBehaviour
{
    private EquipManager _equipManager;
    private PlayerCombat _combat;

    public bool IsMelee { get; private set; }
    public bool IsRanged { get; private set; }

    private void Awake()
    {
        _equipManager = GetComponentInChildren<EquipManager>();
        _combat = GetComponentInParent<PlayerCombat>();

        if (_combat == null)
            Debug.LogError("PlayerCombat no encontrado en el padre!");
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
            case WeaponCategory.Axe:
                SetWeapon(true, false);
                SetCurrentWeapon(WeaponCategory.Axe);
                break;

            case WeaponCategory.Hammer:
                SetWeapon(true, false);
                SetCurrentWeapon(WeaponCategory.Hammer);
                break;

            case WeaponCategory.Rifle:
                SetWeapon(false, true);
                SetCurrentWeapon(WeaponCategory.Rifle);
                break;

            case WeaponCategory.Gatling:
                SetWeapon(false, true);
                SetCurrentWeapon(WeaponCategory.Gatling);
                break;
        }
    }

    private void SetCurrentWeapon(WeaponCategory weapon)
    {
        if (_combat != null && _combat.HasStateAuthority)
            _combat.CurrentWeapon = weapon;
    }

    private void SetWeapon(bool melee, bool ranged)
    {
        IsMelee = melee;
        IsRanged = ranged;

        var animator = GetComponentInParent<Animator>();
        if (animator != null)
        {
            animator.SetBool("IsGunEquipped", ranged);
            animator.SetBool("IsAxeEquipped", melee);
        }
    }

    public IWeaponAnimatable GetCurrentWeaponAnimatable()
    {
        if (_equipManager == null) return null;

        // El EquipManager instancia el prefab directamente como hijo de los
        // equip points (_rangedPoint, _meleePointA, etc.), así que buscamos
        // en sus hijos con GetComponentInChildren.
        // includeInactive: false  solo armas activas en escena.
        return _equipManager.GetComponentInChildren<IWeaponAnimatable>(false);
    }
}