using UnityEngine;

public class PlayerInventoryController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInventoryData _inventoryData;
    [SerializeField] private InventoryUiManager _uiManager;
    [SerializeField] private EquipManager _equipManager;
    [SerializeField] private Transform _inventoryContent;

    [Header("Pickup")]
    [SerializeField] private float _pickupRange = 2f;

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
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryPickupItem();
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
                bool added = _inventoryData.AddItem(pickup.ItemData);

                if (added)
                    Destroy(pickup.gameObject);

                return;
            }
        }
    }
}
