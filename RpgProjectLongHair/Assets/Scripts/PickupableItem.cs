using Fusion;
using UnityEngine;

public class PickupableItem : NetworkBehaviour
{
    [SerializeField] private Item _itemData;
    [SerializeField] private bool _autoPickup = false;

    private bool _playerInRange = false;
    private GameObject _player;

    private void Update()
    {
        if (_playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            TryPickup();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInRange = true;
            _player = other.gameObject;

            if (_autoPickup)
                TryPickup();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInRange = false;
            _player = null;
        }
    }

    private void TryPickup()
    {
        if (Object.HasStateAuthority)
        {
            Pickup();
        }
        else
        {
            RPC_RequestPickup();
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestPickup()
    {
        Pickup();
    }

    private void Pickup()
    {
        if (_itemData == null)
        {
            Debug.LogWarning("PickupableItem has no itemData assigned!");
            return;
        }

        InventoryManager.Instance.AddItem(_itemData);

        if (_itemData.type == ItemType.Weapon || _itemData.type == ItemType.Armor)
        {
            string defaultSlot = _itemData.type == ItemType.Weapon ? "RightHand" : "Body";
            InventoryManager.Instance.EquipItem(defaultSlot, _itemData);
        }

        Debug.Log($"Picked up {_itemData.name}");

        Runner.Despawn(Object);
    }
}