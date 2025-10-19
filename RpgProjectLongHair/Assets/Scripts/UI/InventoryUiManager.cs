using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUiManager : MonoBehaviour
{
    public static InventoryUiManager Instance;

    [Header("Referencias")]
    [SerializeField] private Transform contentParent; 

    private readonly List<ItemSO> collectedItems = new List<ItemSO>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void AddItem(ItemSO item)
    {
        if (item == null) return;

        if (collectedItems.Contains(item))
        {
            Debug.Log($"Ya tienes el item: {item.itemName}");
            return;
        }

        collectedItems.Add(item);

        if (item.slotPrefab == null)
        {
            Debug.LogWarning($"El item {item.itemName} no tiene un slotPrefab asignado!");
            return;
        }

        GameObject slotObj = Instantiate(item.slotPrefab, contentParent);
        var slot = slotObj.GetComponent<InventorySlot>();
        slot?.SetData(item);

        Debug.Log($"Item agregado al inventario: {item.itemName}");
    }
}