using Fusion;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PickupableItem : NetworkBehaviour
{
    [SerializeField] private Item _itemData;

    public Item GetItemData() => _itemData;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }
}