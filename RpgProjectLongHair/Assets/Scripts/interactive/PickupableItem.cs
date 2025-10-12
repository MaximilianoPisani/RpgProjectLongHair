using Fusion;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PickupableItem : NetworkBehaviour
{
    [SerializeField] private Item _itemData; 
    private NetworkRunner _localRunner;

    public ItemData ToItemData(int id)
    {
        if (_itemData == null)
        {
            Debug.LogError($"PickupableItem {name} not assigned _itemData!");
            return default;
        }

        return new ItemData
        {
            id = id,
            type = _itemData.type
        };
    }

    public void SetRunner(NetworkRunner runner)
    {
        _localRunner = runner;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Object.HasInputAuthority) return;

        if (_itemData == null)
        {
            Debug.LogError($"{name} PickupableItem has _itemData not assigned!");
            return;
        }

        var inventory = other.GetComponentInParent<NetworkedInventory>();
        if (inventory == null)
        {
            Debug.Log($"{name} collided with {other.name} but no NetworkedInventory found.");
            return;
        }

        if (!inventory.HasItem(GetInstanceID()))
        {
            ItemData data = ToItemData(GetInstanceID());
            if (inventory.AddItem(data))
            {
                if (_localRunner != null && ItemSpawner.Instance != null)
                    ItemSpawner.Instance.RemoveItem(_localRunner, Object);
            }
        }
    }

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }
}