using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI nameText;

    public void SetData(ItemSO item)
    {
        if (item == null) return;

        if (icon != null)
            icon.sprite = item.icon;

        if (nameText != null)
            nameText.text = item.itemName;
    }
}