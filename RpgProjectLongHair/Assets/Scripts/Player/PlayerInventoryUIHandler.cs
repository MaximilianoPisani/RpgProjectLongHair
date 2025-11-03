using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Fusion;

public class PlayerInventoryUIHandler : NetworkBehaviour
{
    [SerializeField] private Inventory _inventory;
    [SerializeField] private InventoryUiManager _uiManager;
    [SerializeField] private EquipManager _equipManager;
    [SerializeField] private float _pickupRange = 2f;

    public void TryPickupItem()
    {
        if (!HasInputAuthority) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, _pickupRange);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<PickupableItem>(out var pickup))
            {
                PickupItemRpc(pickup.Object);
                break;
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void PickupItemRpc(NetworkObject itemNetObj, RpcInfo info = default)
    {
        if (itemNetObj.TryGetComponent<PickupableItem>(out var pickup))
        {
            if (_inventory.HasStateAuthority)
            {
                bool added = _inventory.AddItem(pickup.ItemData);

                if (!HasInputAuthority && added)
                    AddItemToOwnerRpc(pickup.ItemData.id);

                if (HasInputAuthority && added && _uiManager != null)
                    _uiManager.AddItem(pickup.ItemDataSO, _equipManager.EquipItemFromSlot);
            }

            Runner.Despawn(itemNetObj);
            ItemSpawner.Instance.RemoveItem(Runner, itemNetObj);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void AddItemToOwnerRpc(int itemId, RpcInfo info = default)
    {
        if (!HasInputAuthority) return;

        ItemSO itemSO = ItemDatabase.GetItemByIdStatic(itemId);
        if (itemSO != null)
            _uiManager.AddItem(itemSO, _equipManager.EquipItemFromSlot);
    }
}