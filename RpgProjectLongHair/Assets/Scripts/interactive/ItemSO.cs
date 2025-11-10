using UnityEngine;

public enum WeaponCategory { None, Melee, Ranged }

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
    public GameObject equipPrefab;
}