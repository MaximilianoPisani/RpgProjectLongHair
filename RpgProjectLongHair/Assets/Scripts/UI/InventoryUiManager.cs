using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Manager local para mostrar el inventario
public class InventoryUiManager : MonoBehaviour
{
    [SerializeField] private Transform _contentParent;
    private readonly Dictionary<int, InventorySlot> _slotsById = new();
    public void SetContent(Transform content)
    {
        _contentParent = content;
    }
    public void AddItem(ItemSO item, Action<ItemSO> onClick)
    {
        if (item == null || _contentParent == null)
            return;
        if (_slotsById.ContainsKey(item.id))
            return;
        GameObject slotObj = Instantiate(item.slotPrefab, _contentParent);
        slotObj.name = item.itemName + "_Slot";
        InventorySlot slot = slotObj.GetComponent<InventorySlot>();
        slot.SetData(item);
        _slotsById[item.id] = slot;
        Button button = slotObj.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(item));
        }
    }
    public void HighlightEquipped(int equippedId)
    {
        foreach (var kv in _slotsById)
        {
            bool isEquipped = kv.Key == equippedId;
            kv.Value.SetEquipped(isEquipped);
        }
    }
    public void Clear()
    {
        _slotsById.Clear();
        if (_contentParent == null) return;
        foreach (Transform child in _contentParent)
            Destroy(child.gameObject);
    }
}