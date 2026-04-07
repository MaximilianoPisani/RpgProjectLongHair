using UnityEngine;

public enum WeaponCategory
{
    Axe,
    Hammer,
    Rifle,
    Gatling
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class ItemSO : ScriptableObject
{
    public int id;
    public string itemName;
    public Sprite icon;
    public ItemType type;
    public WeaponCategory weaponCategory; 
    public int amount; // for quest

    public GameObject slotPrefab;

    public bool isDualWield;

    [Header("Single")]
    public GameObject equipPrefab;

    [Header("Dual Wield")]
    public GameObject rightHandPrefab;
    public GameObject leftHandPrefab;
}