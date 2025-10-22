using UnityEngine;
using UnityEngine.InputSystem;
using Fusion;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(NetworkCharacterController))]
[RequireComponent(typeof(Inventory))]
public class PlayerController : NetworkBehaviour
{
    [Header("Pickup")]
    [SerializeField] private float _pickupRange = 2f;

    [Header("UI")]
    [SerializeField] private InventoryUiManager _uiManager;
    [SerializeField] private Transform _inventoryContent;
    [SerializeField] private UnityEngine.UI.Button _inventoryToggleButton;
    [SerializeField] private UnityEngine.UI.Button _cancelButton;

    private Transform _cam;
    private PlayerInput _playerInput;
    private NetworkCharacterController _characterController;
    private Inventory _inventory;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        _characterController = GetComponent<NetworkCharacterController>();
        _inventory = GetComponent<Inventory>();
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
            SetupLocalUI();
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

        Vector3 inputDir = new Vector3(input.moveDirection.x, 0f, input.moveDirection.z);

        if (_cam == null && HasInputAuthority && Camera.main != null)
            _cam = Camera.main.transform;

        Vector3 moveDir = _cam != null
            ? (_cam.forward * inputDir.z + _cam.right * inputDir.x)
            : inputDir;

        moveDir.y = 0;
        moveDir.Normalize();

        _characterController.Move(moveDir);

        if (moveDir.sqrMagnitude > 0.001f)
            transform.forward = moveDir;

        if (input.interact)
            TryPickupItem();
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (!HasInputAuthority) return;
        TryPickupItem();
    }

    private void SetupLocalUI()
    {
        if (!HasInputAuthority || _uiManager == null) return;

        _uiManager.gameObject.SetActive(true);
        _uiManager.SetContent(_inventoryContent);

        if (_inventoryToggleButton != null)
        {
            _inventoryToggleButton.onClick.RemoveAllListeners();
            _inventoryToggleButton.onClick.AddListener(ToggleInventory);
        }

        if (_cancelButton != null)
        {
            _cancelButton.onClick.RemoveAllListeners();
            _cancelButton.onClick.AddListener(CloseInventory);
        }

        RefreshInventoryUI();
    }

    private void ToggleInventory()
    {
        if (!HasInputAuthority || _uiManager == null) return;
        _uiManager.gameObject.SetActive(!_uiManager.gameObject.activeSelf);
    }

    private void CloseInventory()
    {
        if (!HasInputAuthority || _uiManager == null) return;
        _uiManager.gameObject.SetActive(false);
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

    private void RefreshInventoryUI()
    {
        if (!HasInputAuthority || _uiManager == null)
            return;

        _uiManager.Clear();

        for (int i = 0; i < _inventory.Items.Length; i++)
        {
            var itemData = _inventory.Items[i];
            if (itemData.id != 0)
            {
                ItemSO itemSO = ItemDatabase.GetItemByIdStatic(itemData.id);
                if (itemSO != null)
                    _uiManager.AddItem(itemSO);
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void PickupItemRpc(NetworkObject itemNetObj, RpcInfo info = default)
    {
        if (itemNetObj.TryGetComponent<PickupableItem>(out var pickup))
        {
            if (_inventory.HasStateAuthority)
                _inventory.AddItem(pickup.ItemData);

            if (HasInputAuthority && _uiManager != null)
                _uiManager.AddItem(pickup.ItemDataSO);

            Runner.Despawn(itemNetObj);
            ItemSpawner.Instance.RemoveItem(Runner, itemNetObj);
        }
    }
}