using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUiManager : MonoBehaviour
{
    [SerializeField] private Transform contentParent;          // Contenedor de slots
    [SerializeField] private Button toggleButton;              // Botón para abrir/cerrar
    [SerializeField] private Button cancelButton;              // Botón para cerrar panel
    [SerializeField] private GameObject panel;                // Panel completo de inventario

    private readonly List<ItemSO> collectedItems = new List<ItemSO>();

    private void Awake()
    {
        if (toggleButton != null)
            toggleButton.onClick.AddListener(TogglePanel);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(ClosePanel);
    }

    public void SetContent(Transform content)
    {
        contentParent = content;
    }

    public void AddItem(ItemSO item)
    {
        if (item == null || contentParent == null) return;
        if (collectedItems.Contains(item)) return;

        collectedItems.Add(item);

        if (item.slotPrefab == null)
        {
            Debug.LogWarning($"Item {item.itemName} has no slotPrefab assigned!");
            return;
        }

        GameObject slotObj = Instantiate(item.slotPrefab, contentParent);
        slotObj.name = item.itemName + "_Slot";

        InventorySlot slot = slotObj.GetComponent<InventorySlot>();
        if (slot != null)
            slot.SetData(item);
    }

    public void Clear()
    {
        collectedItems.Clear();
        if (contentParent == null) return;
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);
    }

    private void TogglePanel()
    {
        if (panel == null) return;
        panel.SetActive(!panel.activeSelf);
    }

    private void ClosePanel()
    {
        if (panel == null) return;
        panel.SetActive(false);
    }
}