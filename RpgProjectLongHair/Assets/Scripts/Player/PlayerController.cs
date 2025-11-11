using UnityEngine;
using UnityEngine.InputSystem;
using Fusion;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(NetworkCharacterController))]
[RequireComponent(typeof(Inventory))]
public class PlayerController : NetworkBehaviour
{
    [Header("UI")]
    [SerializeField] private InventoryUiManager _uiManager;
    [SerializeField] private Transform _inventoryContent;
    [SerializeField] private UnityEngine.UI.Button _inventoryToggleButton;
    [SerializeField] private UnityEngine.UI.Button _cancelButton;

    private Transform _cam;
    private PlayerInput _playerInput;
    private NetworkCharacterController _characterController;
    private Inventory _inventory;
    private EquipManager _equipManager;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        _characterController = GetComponent<NetworkCharacterController>();
        _inventory = GetComponent<Inventory>();
        _equipManager = GetComponentInChildren<EquipManager>();
    }

    private void OnEnable()
    {
        if (_playerInput != null)
            _playerInput.actions["Interact"].performed += OnInteract;
    }

    private void OnDisable()
    {
        if (_playerInput != null)
            _playerInput.actions["Interact"].performed -= OnInteract;
    }

    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            if (_uiManager != null)
                _uiManager.SetContent(_inventoryContent);
        }
        else
        {
            if (_uiManager != null)
                Destroy(_uiManager.gameObject);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData input))
            return;

        if (GetInput(out NetworkInputData data))
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                data.aimRotation,
                Runner.DeltaTime * 10f
            );
        }

        Vector3 inputDir = new Vector3(input.moveDirection.x, 0f, input.moveDirection.z);

        if (_cam == null && HasInputAuthority && Camera.main != null)
            _cam = Camera.main.transform;

        Vector3 moveDir = inputDir;
        moveDir.y = 0;
        moveDir.Normalize();

        _characterController.Move(moveDir);

        if (moveDir.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(moveDir),
                10f * Runner.DeltaTime
            );
        }

        if (input.jump && Mathf.Abs(_characterController.Velocity.y) < 0.05f)
            _characterController.Jump();

        if (input.interact)
            TryPickupItem();
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (!HasInputAuthority) return;
        TryPickupItem();
    }

    private void TryPickupItem()
    {
        if (!HasInputAuthority) return;

        if (_inventory != null && _equipManager != null && _uiManager != null)
        {
            var inventoryHandler = GetComponent<PlayerInventoryUIHandler>();
            if (inventoryHandler != null)
                inventoryHandler.TryPickupItem();
        }
    }
}