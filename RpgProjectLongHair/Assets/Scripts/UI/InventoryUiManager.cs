using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUiManager : MonoBehaviour
{
    [SerializeField] private Transform _contentParent;          // Contenedor de slots
    [SerializeField] private Button _toggleButton;              // Botón para abrir/cerrar
    [SerializeField] private Button _cancelButton;              // Botón para cerrar panel
    [SerializeField] private GameObject _panel;                // Panel completo de inventario

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

    public void AddItem(ItemSO item)
    {
        if (item == null || _contentParent == null) return;
        if (collectedItems.Contains(item)) return;

        collectedItems.Add(item);

        if (item.slotPrefab == null)
        {
            Debug.LogWarning($"Item {item.itemName} has no slotPrefab assigned!");
            return;
        }

        GameObject slotObj = Instantiate(item.slotPrefab, _contentParent);
        slotObj.name = item.itemName + "_Slot";

        InventorySlot slot = slotObj.GetComponent<InventorySlot>();
        if (slot != null)
            slot.SetData(item);
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