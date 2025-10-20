using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUiManager : MonoBehaviour
{
    [SerializeField] private Transform contentParent;

    private readonly List<ItemSO> collectedItems = new List<ItemSO>();

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
            Debug.LogWarning($"Item {item.itemName} no tiene slotPrefab asignado!");
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
}