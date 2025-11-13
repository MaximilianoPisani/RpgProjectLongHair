using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    public List<ItemSO> items = new List<ItemSO>();

    public ItemSO GetItemById(int id)
    {
        return items.Find(i => i.id == id);
    }

    private static ItemDatabase _instance;
    public static ItemDatabase Instance
    {
        get
        {
            return _instance ?? Resources.Load<ItemDatabase>("Items/ItemDatabase");
        }
    }

    public static ItemSO GetItemByIdStatic(int id)
    {
        return Instance?.GetItemById(id);
    }
}