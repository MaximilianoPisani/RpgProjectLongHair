using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Manager local para mostrar el inventario
public class InventoryUiManager : MonoBehaviour
{
    [SerializeField] private Transform _contentParent;

    private readonly List<ItemSO> _collected = new List<ItemSO>();

    public void SetContent(Transform content)
    {
        _contentParent = content;
    }

    public void AddItem(ItemSO item, Action<ItemSO> onClick) // Crea visualmente el slot en la UI para un item y asigna el callback de click
    {
        if (item == null || _contentParent == null) return;
        if (_collected.Contains(item)) return;

        _collected.Add(item);

        if (item.slotPrefab == null)
        {
            Debug.LogWarning($"Item {item.itemName} has no slotPrefab!");
            return;
        }

        GameObject slotObj = Instantiate(item.slotPrefab, _contentParent);
        slotObj.name = item.itemName + "_Slot";

        InventorySlot slot = slotObj.GetComponent<InventorySlot>();
        if (slot != null) slot.SetData(item);

        Button button = slotObj.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(item));
        }
    }

    public void Clear() // Limpia todos los slots de la UI
    {
        _collected.Clear();

        if (_contentParent == null) return;

        foreach (Transform child in _contentParent)
            Destroy(child.gameObject);
    }
}
