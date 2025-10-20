using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class Inventory : NetworkBehaviour
{
    [Networked, Capacity(20)]public NetworkArray<ItemData> Items => default; 
    public bool AddItem(ItemData item) 
    { 
        if (!Object.HasStateAuthority) return false; 

        for (int i = 0; i < Items.Length; i++) 
        {
            if (Items[i].id == 0) 
            { 
                Items.Set(i, item); return true; 
            } 

        }

        Debug.LogWarning("Inventory full"); return false;
    
    } 
}