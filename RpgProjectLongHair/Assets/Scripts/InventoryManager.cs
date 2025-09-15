using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }


    private Dictionary<string, Item> _equippedItems = new Dictionary<string, Item>();


    private List<Item> _inventoryItems = new List<Item>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddItem(Item newItem)
    {
        if (newItem == null) return;

        _inventoryItems.Add(newItem);
        Debug.Log($"Added {newItem.name} to inventory");
    }

    public void RemoveItem(Item item)
    {
        if (item == null) return;

        _inventoryItems.Remove(item);
        Debug.Log($"Removed {item.name} from inventory");
    }

    public void EquipItem(string slot, Item item)
    {
        if (item == null) return;

        _equippedItems[slot] = item;
        Debug.Log($"Equipped {item.name} in slot {slot}");
    }

    public void UnequipItem(string slot)
    {
        if (_equippedItems.ContainsKey(slot))
        {
            Debug.Log($"Unequipped {_equippedItems[slot].name} from slot {slot}");
            _equippedItems[slot] = null;
        }
    }

    public Item GetEquippedItem(string slot)
    {
        return _equippedItems.ContainsKey(slot) ? _equippedItems[slot] : null;
    }


    public List<Item> GetInventoryItems()
    {
        return _inventoryItems;
    }
}
