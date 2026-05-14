using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(EquipManager))]
public class PlayerStats : MonoBehaviour
{
    [Header("Base Stats")]
    [SerializeField] private int baseDamage = 50;

    [Header("Extra Buffs")]
    [SerializeField] private int bonusDamage = 0;

    public int CurrentDamage { get; private set; }

    public UnityEvent<int> OnDamageChanged;

    private EquipManager _equipManager;
    private PlayerRageHandler _rageHandler;

    private void Awake()
    {
        _equipManager = GetComponent<EquipManager>();
        _rageHandler = GetComponent<PlayerRageHandler>();
    }

    private void OnEnable()
    {
        if (_equipManager != null)
            _equipManager.OnEquippedChanged += UpdateDamage;

        UpdateDamage(_equipManager != null
            ? _equipManager.EquippedItemId
            : 0);
    }

    private void OnDisable()
    {
        if (_equipManager != null)
            _equipManager.OnEquippedChanged -= UpdateDamage;
    }

    private void Update()
    {
        UpdateDamage(_equipManager != null
            ? _equipManager.EquippedItemId
            : 0);
    }

    private void UpdateDamage(int equippedId)
    {
        float finalDamage = baseDamage;

        if (equippedId != 0)
        {
            ItemSO item = ItemDatabase.GetItemByIdStatic(equippedId);

            if (item != null && item.type == ItemType.Weapon)
            {
                finalDamage += item.baseDamage;
            }
        }

        finalDamage += bonusDamage;

        if (_rageHandler != null)
        {
            finalDamage *= _rageHandler.GetDamageMultiplier();
        }

        CurrentDamage = RoundToNearest10(finalDamage);

        CurrentDamage = Mathf.Max(0, CurrentDamage);

        OnDamageChanged?.Invoke(CurrentDamage);
    }

    private int RoundToNearest10(float value)
    {
        return Mathf.RoundToInt(value / 10f) * 10;
    }

    public void AddBonusDamage(int amount)
    {
        bonusDamage += amount;
    }

    public void RemoveBonusDamage(int amount)
    {
        bonusDamage -= amount;
    }
}