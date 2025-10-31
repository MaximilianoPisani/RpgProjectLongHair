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

        Vector3 moveDir =  inputDir;

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

    private void SetupLocalUI()
    {
        if (!HasInputAuthority || _uiManager == null)
            return;

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

        var eventSystem = UnityEngine.EventSystems.EventSystem.current;
        var uiModule = eventSystem?.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

        if (uiModule != null && _playerInput != null)
            uiModule.actionsAsset = _playerInput.actions;

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
                {
                    _uiManager.AddItem(itemSO, OnSlotClicked);
                }
            }
        }
    }

    private void OnSlotClicked(ItemSO item)
    {
        if (!HasInputAuthority) return;

        if (_equipManager == null)
        {
            Debug.LogWarning("EquipManager not found on player.");
            return;
        }

        if (_equipManager.IsEquipped())
        {
            Debug.Log("An item is already equipped, it will be unequipped first..");
            _equipManager.UnequipCurrent();
        }

        bool equipped = _equipManager.EquipItem(item);
        if (equipped)
            Debug.Log($"{item.itemName} properly equipped.");
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
                {
                    AddItemToOwnerRpc(pickup.ItemData.id);
                }

                if (HasInputAuthority && added && _uiManager != null)
                {
                    _uiManager.AddItem(pickup.ItemDataSO, OnSlotClicked);
                }
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
        if (itemSO == null)
        {
            Debug.LogWarning($"No ItemSO found with id {itemId}");
            return;
        }

        if (_uiManager != null)
            _uiManager.AddItem(itemSO, OnSlotClicked);
    }
}
