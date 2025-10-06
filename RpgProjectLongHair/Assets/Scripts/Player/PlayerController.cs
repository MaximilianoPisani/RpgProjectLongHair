using UnityEngine;
using UnityEngine.InputSystem;
using Fusion;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(NetworkCharacterController))]
[RequireComponent(typeof(NetworkedInventory))]
public class PlayerController : NetworkBehaviour
{
    [Header("Move")]
    [SerializeField] private float _moveSpeed = 5f;

    [Header("Pickup Settings")]
    [SerializeField] private float _pickupRange = 2f;

    private PlayerInput _playerInput;
    private NetworkCharacterController _characterController;
    private NetworkedInventory _inventory;
    private Transform _cam;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        _characterController = GetComponent<NetworkCharacterController>();
        _inventory = GetComponent<NetworkedInventory>();
    }

    private void OnEnable()
    {
        if (_playerInput != null)
        {
            _playerInput.actions["Interact"].performed += OnInteract;
        }
    }

    private void OnDisable()
    {
        if (_playerInput != null)
        {
            _playerInput.actions["Interact"].performed -= OnInteract;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData input)) return;

        Vector3 inputDir = new Vector3(input.moveDirection.x, 0f, input.moveDirection.z);

        if (_cam == null && HasInputAuthority && Camera.main != null)
            _cam = Camera.main.transform;

        Vector3 worldDir = inputDir;

        if (_cam != null)
        {
            Vector3 camF = _cam.forward; camF.y = 0f; camF.Normalize();
            Vector3 camR = _cam.right; camR.y = 0f; camR.Normalize();
            worldDir = camF * inputDir.z + camR * inputDir.x; 
        }
        else
        {
            worldDir = inputDir;
        }

        _characterController.Move(worldDir * _moveSpeed * Runner.DeltaTime);

        if (input.interact)
            TryPickupItem();
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!HasInputAuthority) return;

        Debug.Log("[Player] Interact pressed via Input System!");
        TryPickupItem();
    }

    private void TryPickupItem()
    {
        if (!Object.HasStateAuthority) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, _pickupRange);
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