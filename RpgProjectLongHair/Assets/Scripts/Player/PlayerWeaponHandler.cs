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
        var networkSync = GetComponentInParent<PlayerNetworkSync>();
        networkSync?.ResetAllAnimations();

        var sm = GetComponentInParent<PlayerStateMachine>();
        if (sm != null && sm.CurrentState is PlayerRangeState)
            sm.ChangeState(new PlayerIdleState(sm));


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
        if (_combat == null) return;

        // Host: setea directo
        if (_combat.HasStateAuthority)
        {
            _combat.CurrentWeapon = weapon;
            return;
        }

        // Cliente: pide al host via RPC
        if (_combat.HasInputAuthority)
            _combat.RPC_SetCurrentWeapon(weapon);
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

        return _equipManager.GetComponentInChildren<IWeaponAnimatable>(false);
    }
}