using Fusion;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(NetworkCharacterController))]
[RequireComponent(typeof(NetworkedInventory))]
public class PlayerController : NetworkBehaviour
{
    [SerializeField] private Renderer _renderer;

    private NetworkCharacterController _characterController;
    private NetworkedInventory _inventory;

    public float pickupRange = 2f;

    private void Awake()
    {
        _characterController = GetComponent<NetworkCharacterController>();
        _inventory = GetComponent<NetworkedInventory>();
    }

    public override void Spawned()
    {
        if (HasInputAuthority)
           _renderer.material.color = Color.white;
        Debug.Log("PlayerController Spawned with input authority.");
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData input))
        {
            _characterController.Move(input.moveDirection);

            if (input.interact)
            {
                TryPickupItem();
            }
        }
    }

    private void TryPickupItem()
    {
        if (!Object.HasStateAuthority)
            return;

        Collider[] hits = Physics.OverlapSphere(transform.position, pickupRange);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<PickupableItem>(out var pickup))
            {
                int newItemId = Random.Range(1, 999999);

                if (_inventory.AddItem(pickup.ToItemData(newItemId)))
                {
                    Runner.Despawn(pickup.Object);
                    Debug.Log($"Item {pickup.name} picked and stored in inventory.");
                }
                else
                {
                    Debug.Log("Inventory full, cannot be grabbed.");
                }

                break; 
            }
        }
    }
}