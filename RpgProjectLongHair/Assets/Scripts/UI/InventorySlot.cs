using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    private ItemSO currentItem;

    public void SetData(ItemSO item)
    {
        currentItem = item;
        if (iconImage != null)
            iconImage.sprite = item.icon;
    }

    public ItemSO GetItem() => currentItem;
}