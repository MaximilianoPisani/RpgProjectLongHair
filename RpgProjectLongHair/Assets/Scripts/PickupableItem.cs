using Fusion;
using UnityEngine;

public class PickupableItem : NetworkBehaviour
{
    [SerializeField] private Item _itemData;

    public Item GetItemData() => _itemData;

}