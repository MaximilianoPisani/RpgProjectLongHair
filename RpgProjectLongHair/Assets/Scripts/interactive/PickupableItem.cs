using UnityEngine;
using Fusion;

public class PickupableItem : NetworkBehaviour
{
    [SerializeField] private ItemSO itemDataSO;
    public ItemSO ItemDataSO => itemDataSO;

    public ItemData ItemData => new ItemData
    {
        id = itemDataSO.id,
        type = itemDataSO.type
    };

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    //Test quest
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<ItemSO>(out var item))
        {
            //TrackEvents.OnTrackEvent?.Invoke($"Pick_Item_{item.itemName}, {item.amount}"); error de dato en invoke!
        }
    }
}