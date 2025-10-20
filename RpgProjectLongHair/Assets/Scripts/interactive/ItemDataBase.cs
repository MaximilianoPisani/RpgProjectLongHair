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
            if (_instance == null)
                _instance = Resources.Load<ItemDatabase>("ItemDatabase"); 
            return _instance;
        }
    }

    public static ItemSO GetItemByIdStatic(int id)
    {
        return Instance?.GetItemById(id);
    }
}