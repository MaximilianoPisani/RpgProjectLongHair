using Fusion;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PickupableItem : NetworkBehaviour
{
    [SerializeField] private Item _itemData;

    public ItemData ToItemData(int id)
    {
        return new ItemData
        {
            id = id,
            type = _itemData.type
        };
    }

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }
}