using Fusion;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PickupableItem : NetworkBehaviour
{
    [SerializeField] private Item _itemData;
    private NetworkRunner _runner;

    public ItemData ToItemData(int id)
    {
        return new ItemData
        {
            id = id,
            type = _itemData.type
        };
    }

    public void SetRunner(NetworkRunner runner)
    {
        _runner = runner;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Object.HasInputAuthority) return;

        var inventory = other.GetComponent<NetworkedInventory>();
        if (inventory == null) return;

        ItemData data = ToItemData(GetInstanceID()); 
        if (inventory.AddItem(data))
        {
            if (_runner != null)
            {
                ItemSpawner.Instance.RemoveItem(_runner, Object);
            }
        }
    }

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }
}