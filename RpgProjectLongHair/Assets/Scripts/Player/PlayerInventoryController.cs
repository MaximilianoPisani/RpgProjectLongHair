using UnityEngine;

public class PlayerInventoryController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInventoryData _inventoryData;
    [SerializeField] private InventoryUiManager _uiManager;
    [SerializeField] private EquipManager _equipManager;
    [SerializeField] private Transform _inventoryContent;
    [SerializeField] private GameObject _inventoryPanel;

    [Header("Pickup")]
    [SerializeField] private float _pickupRange = 2f;

    private bool _isOpen = false;

    private void Start()
    {
        if (_uiManager != null && _inventoryContent != null)
            _uiManager.SetContent(_inventoryContent);

        if (_inventoryData != null)
            _inventoryData.OnInventoryChanged += RefreshInventoryUI;

        if (_equipManager != null)
            _equipManager.OnEquippedChanged += RefreshEquipState;
    }
    private void Update()
    {
        bool togglePressed = Input.GetKeyDown(KeyCode.I) || (Input.GetKeyDown(KeyCode.Escape) && _isOpen);
        if (togglePressed)
        {
            _isOpen = Input.GetKeyDown(KeyCode.I) ? !_isOpen : false;
            RunnerManager.SetInventoryOpen(_isOpen);
            _inventoryPanel.SetActive(_isOpen);
        }
    }

    private void OnDestroy()
    {
        if (_inventoryData != null)
            _inventoryData.OnInventoryChanged -= RefreshInventoryUI;

        if (_equipManager != null)
            _equipManager.OnEquippedChanged -= RefreshEquipState;
    }

    private void RefreshInventoryUI()
    {
        if (_uiManager == null || _inventoryData == null)
            return;

        _uiManager.Clear();

        foreach (var data in _inventoryData.Items)
        {
            if (data.id == 0)
                continue;

            ItemSO itemSO = ItemDatabase.GetItemByIdStatic(data.id);

            if (itemSO != null)
                _uiManager.AddItem(itemSO, OnInventorySlotClicked);
        }

        RefreshEquipState(_equipManager.EquippedItemId);
    }

    private void OnInventorySlotClicked(ItemSO item)
    {
        _equipManager?.OnSlotClicked(item);
    }

    private void RefreshEquipState(int equippedId)
    {
        _uiManager?.HighlightEquipped(equippedId);
    }

    public void TryPickupItem()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _pickupRange);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<PickupableItem>(out var pickup))
            {
                if (pickup.Object == null || !pickup.Object.IsValid) continue;

                bool added = _inventoryData.AddItem(pickup.ItemData);
                if (added)
                {
                    pickup.RPC_RequestDespawn(); 
                }
                return;
            }
        }
    }
}
