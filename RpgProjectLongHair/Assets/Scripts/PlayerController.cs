using Fusion;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    private NetworkCharacterController _characterController;
    [SerializeField] private Renderer _renderer;

    public float moveSpeed = 5f;
    public float pickupRange = 2f;

    private void Awake()
    {
        _characterController = GetComponent<NetworkCharacterController>();
    }

    public override void Spawned()
    {
        if (HasInputAuthority)
            _renderer.material.color = Color.white;
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData input))
        {
            Vector3 move = input.moveDirection.normalized;
            _characterController.Move(move * moveSpeed * Runner.DeltaTime);

            if (input.interact)
            {
                AttemptPickup();
            }
        }
    }

    private void AttemptPickup()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, pickupRange);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<PickupableItem>(out var item))
            {
                if (item.Object.HasStateAuthority) 
                {
                    Runner.Despawn(item.Object);
                    Debug.Log($"Item {item.GetItemData().name} despawned by {name}");
                }
            }
        }
    }
}