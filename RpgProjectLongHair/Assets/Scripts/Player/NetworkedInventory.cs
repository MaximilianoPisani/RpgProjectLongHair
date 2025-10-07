using Fusion;
using UnityEngine;

public class NetworkedInventory : NetworkBehaviour
{
    [Networked, Capacity(20)]
    public NetworkArray<ItemData> Items => default;

    [HideInInspector] public Transform EquipPoint; // Se asigna desde PlayerController

    private GameObject _currentEquipped;

    // Agrega item al inventario
    public bool AddItem(ItemData item)
    {
        if (!Object.HasStateAuthority) return false;

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

    // Equipa item en equipPoint
    public void EquipItem(int slot, GameObject prefab)
    {
        if (prefab.TryGetComponent<NetworkObject>(out var netObj))
        {
            if (Runner != null)
            {
                NetworkObject spawned = Runner.Spawn(netObj, EquipPoint.position, EquipPoint.rotation);
                spawned.transform.SetParent(EquipPoint, worldPositionStays: true);
            }
        }
        else
        {
            GameObject go = Instantiate(prefab, EquipPoint.position, EquipPoint.rotation);
            go.transform.SetParent(EquipPoint, worldPositionStays: false);
        }


    }

    // Verifica si el item ya está en el inventario
    public bool HasItem(int itemId)
    {
        for (int i = 0; i < Items.Length; i++)
        {
            if (Items[i].id == itemId) return true;
        }
        return false;
    }

}