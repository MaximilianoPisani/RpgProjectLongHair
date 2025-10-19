using UnityEngine;
using Fusion;

public class PickupableItem : NetworkBehaviour
{
    [SerializeField] private ItemSO itemData;
    public ItemSO ItemData => itemData;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }
}