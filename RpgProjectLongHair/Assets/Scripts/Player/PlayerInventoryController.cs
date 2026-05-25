using Fusion;
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
        var netObj = GetComponentInParent<NetworkObject>();
        if (netObj != null && !netObj.HasInputAuthority)
        {
            Destroy(this);
            return;
        }

        if (_uiManager != null && _inventoryContent != null)
            _uiManager.SetContent(_inventoryContent);

        if (_inventoryData != null)
            _inventoryData.OnInventoryChanged += RefreshInventoryUI;

        if (_equipManager != null)
            _equipManager.OnEquippedChanged += _ => RefreshEquipState();

        var armorData = GetComponent<PlayerArmorData>();
        if (armorData != null)
            armorData.OnArmorChanged += _ => RefreshEquipState();

        RefreshInventoryUI();
    }
    private void Update()
    {
        if (_isOpen && !_inventoryPanel.activeSelf)
        {
            _isOpen = false;
            UiStateManager.CloseBlockingUI();
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            if (!_isOpen && UiStateManager.HasBlockingUI)
                return;

            _isOpen = !_isOpen;

            _inventoryPanel.SetActive(_isOpen);

            if (_isOpen)
                UiStateManager.OpenBlockingUI();
            else
                UiStateManager.CloseBlockingUI();
        }

        if (Input.GetKeyDown(KeyCode.Escape) && _isOpen)
        {
            _isOpen = false;

            _inventoryPanel.SetActive(false);

            UiStateManager.CloseBlockingUI();
        }
    }

    private void OnDestroy()
    {
        if (_inventoryData != null)
            _inventoryData.OnInventoryChanged -= RefreshInventoryUI;

        if (_equipManager != null)
            _equipManager.OnEquippedChanged -= _ => RefreshEquipState();

        var armorData = GetComponent<PlayerArmorData>();
        if (armorData != null)
            armorData.OnArmorChanged -= _ => RefreshEquipState();
    }

    private void RefreshInventoryUI()
    {
        if (_uiManager == null || _inventoryData == null) return;

        _uiManager.Clear();

        foreach (var data in _inventoryData.Items)
        {

            if (data.id == 0) continue;

            ItemSO itemSO = ItemDatabase.GetItemByIdStatic(data.id);

            if (itemSO != null)
                _uiManager.AddItem(itemSO, OnInventorySlotClicked);
        }

        RefreshEquipState();
    }

    private void OnInventorySlotClicked(ItemSO item)
    {
        _equipManager?.OnSlotClicked(item);
    }

    private void RefreshEquipState()
    {
        if (_uiManager == null) return;

        int weaponId = _equipManager != null ? _equipManager.EquippedItemId : 0;

        var armorData = GetComponent<PlayerArmorData>();
        int armorId = armorData != null ? armorData.EquippedArmorId : 0;

        _uiManager.HighlightEquippedMultiple(weaponId, armorId);
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

            if (hit.TryGetComponent<PickupableCraftItem>(out var craftPickup))
            {
                if (craftPickup.Object == null || !craftPickup.Object.IsValid) continue;

                var characterData = GetComponent<PlayerCharacterData>();
                if (characterData == null) return;
                if (craftPickup.CraftItemSO.owner != characterData.characterType)
                {
                    craftPickup.ShowFeedback("¡ESTE ITEM NO TE PERTENECE!");
                    return;
                }

                if (!craftPickup.TryMarkPicked())
                    return;

                if (_inventoryData.HasItem(craftPickup.ItemId))
                {
                    Debug.Log($"[Pickup] Ya tenés el item {craftPickup.CraftItemSO.itemName}");
                    return;
                }

                var craftItemData = new ItemData
                {
                    id = craftPickup.ItemId,
                    type = ItemType.QuestItem
                };
                bool added = _inventoryData.AddItem(craftItemData);
                if (added)
                {
                    craftPickup.RPC_RequestDespawn();
                }
                return;
            }

        }
    }
    public void ForceClose()
    {
        if (!_isOpen)
            return;

        _isOpen = false;
        _inventoryPanel.SetActive(false);

        UiStateManager.CloseBlockingUI();
    }
}
