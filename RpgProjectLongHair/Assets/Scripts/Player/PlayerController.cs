using UnityEngine;
using UnityEngine.InputSystem;
using Fusion;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(NetworkCharacterController))]
[RequireComponent(typeof(Inventory))]
public class PlayerController : NetworkBehaviour
{
    [Header("Move")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _turnLerp = 0.2f;

    [Header("Pickup")]
    [SerializeField] private float _pickupRange = 2f;

    [Header("UI")]
    [SerializeField] private InventoryUiManager uiManager;
    [SerializeField] private Transform _inventoryContent;

    private Transform _cam;
    private PlayerInput _playerInput;
    private NetworkCharacterController _characterController;
    private Inventory _inventory;

    // Referencia al runner
    private NetworkRunner _runner;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        _characterController = GetComponent<NetworkCharacterController>();
        _inventory = GetComponent<Inventory>();
    }

    public void SetRunner(NetworkRunner runner)
    {
        _runner = runner;
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

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData input)) return;

        Vector3 inputDir = new Vector3(input.moveDirection.x, 0f, input.moveDirection.z);

        if (_cam == null && HasInputAuthority && Camera.main != null)
            _cam = Camera.main.transform;

        Vector3 worldDir = _cam != null
            ? Vector3.ProjectOnPlane(_cam.forward, Vector3.up).normalized * inputDir.z +
              Vector3.ProjectOnPlane(_cam.right, Vector3.up).normalized * inputDir.x
            : inputDir;

        Vector3 move = worldDir.sqrMagnitude > 1e-4f ? worldDir.normalized : Vector3.zero;
        _characterController.Move(move * _moveSpeed * Runner.DeltaTime);

        if (move.sqrMagnitude > 1e-4f)
        {
            Vector3 look = move; look.y = 0f;
            transform.forward = Vector3.Slerp(transform.forward, look, _turnLerp);
        }

        if (input.interact)
            TryPickupItem();
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!HasInputAuthority) return;
        TryPickupItem();
    }

    private void TryPickupItem()
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
    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            uiManager.SetContent(_inventoryContent);
            RefreshInventoryUI();
        }
    }

    private void RefreshInventoryUI()
    {
        uiManager.Clear(); 
        for (int i = 0; i < _inventory.Items.Length; i++)
        {
            var itemData = _inventory.Items[i];
            if (itemData.id != 0)
            {
                ItemSO itemSO = ItemDatabase.GetItemByIdStatic(itemData.id);
                if (itemSO != null)
                    uiManager.AddItem(itemSO);
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void PickupItemRpc(NetworkObject itemNetObj, RpcInfo info = default)
    {
        if (itemNetObj != null && itemNetObj.TryGetComponent<PickupableItem>(out var pickup))
        {
            _inventory.AddItem(pickup.ItemData);

            if (HasInputAuthority)
                uiManager.AddItem(pickup.ItemDataSO);

            Runner.Despawn(itemNetObj);
            ItemSpawner.Instance.RemoveItem(Runner, itemNetObj);
        }
    }
}