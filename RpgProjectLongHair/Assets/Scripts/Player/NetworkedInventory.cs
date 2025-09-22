using Fusion;
using UnityEngine;

public class NetworkedInventory : NetworkBehaviour
{
    [Networked, Capacity(20)]
    public NetworkArray<ItemData> Items => default;

    public bool AddItem(ItemData item)
    {
        if (!Object.HasStateAuthority)
            return false;

        for (int i = 0; i < Items.Length; i++)
        {
            if (Items[i].id == 0) 
            {
                Items.Set(i, item); 
                Debug.Log($"Item added to slot {i} for {Object.InputAuthority}");
                return true;
            }
        }

        Debug.LogWarning("Inventory full");
        return false;
    }

    public void RemoveItem(int itemId)
    {
        if (!Object.HasStateAuthority) return;

        for (int i = 0; i < Items.Length; i++)
        {
            if (Items[i].id == itemId)
            {
                Items.Set(i, default); 
                return;
            }
        }
    }
}