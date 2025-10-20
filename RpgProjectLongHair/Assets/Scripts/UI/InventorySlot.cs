using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Text nameText;

    public void SetData(ItemSO item)
    {
        if (iconImage != null)
            iconImage.sprite = item.icon;

        if (nameText != null)
            nameText.text = item.itemName;
    }
}