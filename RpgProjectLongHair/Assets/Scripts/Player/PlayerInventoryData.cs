using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerInventoryData : MonoBehaviour
{
    public List<ItemData> Items = new List<ItemData>();

    public event Action OnInventoryChanged;

    public bool AddItem(ItemData item)
    {
        Items.Add(item);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool RemoveItem(int id)
    {
        var found = Items.Find(x => x.id == id);
        if (found.id == 0) return false;

        Items.Remove(found);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool HasItem(int id)
    {
        return Items.Exists(x => x.id == id);
    }
}