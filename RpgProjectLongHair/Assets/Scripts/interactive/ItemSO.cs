using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class ItemSO : ScriptableObject
{
    public int id;
    public string itemName;
    public Sprite icon;
    public ItemType type;

    [Header("UI")]
    public GameObject slotPrefab; 
}