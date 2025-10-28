using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUiManager : MonoBehaviour
{
    [SerializeField] private Transform _contentParent;
    [SerializeField] private Button _toggleButton;
    [SerializeField] private Button _cancelButton;
    [SerializeField] private GameObject _panel;

    private readonly List<ItemSO> collectedItems = new List<ItemSO>();

    private void Awake()
    {
        if (_toggleButton != null)
            _toggleButton.onClick.AddListener(TogglePanel);

        if (_cancelButton != null)
            _cancelButton.onClick.AddListener(ClosePanel);
    }

    public void SetContent(Transform content)
    {
        _contentParent = content;
    }

    public void AddItem(ItemSO item, Action<ItemSO> onClick = null)
    {
        if (item == null || _contentParent == null) return;
        if (collectedItems.Contains(item)) return;

        collectedItems.Add(item);

        if (item.slotPrefab == null)
        {
            Debug.LogWarning($"Item {item.itemName} no tiene slotPrefab asignado!");
            return;
        }

        GameObject slotObj = Instantiate(item.slotPrefab, _contentParent);
        slotObj.name = item.itemName + "_Slot";

        InventorySlot slot = slotObj.GetComponent<InventorySlot>();
        if (slot != null)
            slot.SetData(item);

        Button slotButton = slotObj.GetComponent<Button>();
        if (slotButton != null && onClick != null)
        {
            slotButton.onClick.RemoveAllListeners();
            slotButton.onClick.AddListener(() => onClick.Invoke(item));
        }
    }

    public void Clear()
    {
        collectedItems.Clear();
        if (_contentParent == null) return;

        foreach (Transform child in _contentParent)
            Destroy(child.gameObject);
    }

    private void TogglePanel()
    {
        if (_panel == null) return;
        _panel.SetActive(!_panel.activeSelf);
    }

    private void ClosePanel()
    {
        if (_panel == null) return;
        _panel.SetActive(false);
    }
}