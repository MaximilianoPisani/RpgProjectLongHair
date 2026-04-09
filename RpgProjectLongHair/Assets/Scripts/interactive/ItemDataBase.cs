using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    public List<ItemSO> items = new List<ItemSO>();

    public ItemSO GetItemById(int id)
    {
        return items.Find(i => i != null && i.id == id);
    }

    private static ItemDatabase _instance;

    public static ItemDatabase Instance
    {
        get
        {
            if (_instance == null)
                _instance = Resources.Load<ItemDatabase>("Items/ItemDatabase");

            if (_instance == null)
                Debug.LogError("[ItemDatabase] No se encontró el ScriptableObject en Resources/Items/ItemDatabase");

            return _instance;
        }
    }

    public static ItemSO GetItemByIdStatic(int id)
    {
        if (Instance == null) return null;
        return Instance.GetItemById(id);
    }

    public void CleanNullEntries()
    {
        int removed = items.RemoveAll(i => i == null);
        if (removed > 0)
            Debug.LogWarning($"[ItemDatabase] Se eliminaron {removed} entradas nulas");
    }

    private void OnEnable()
    {
        CleanNullEntries();
    }
}