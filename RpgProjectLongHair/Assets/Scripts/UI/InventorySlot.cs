using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    [SerializeField] private Image iconImage;

    private ItemSO _currentItem;

    private static readonly Color EquippedColor = Color.white;
    private static readonly Color UnequippedColor = new Color(1f, 1f, 1f, 0.35f);

    public void SetData(ItemSO item)
    {
        _currentItem = item;

        if (iconImage != null)
            iconImage.sprite = item.icon;

        SetEquipped(false);
    }

    public void SetEquipped(bool equipped)
    {
        if (iconImage == null)
            return;

        iconImage.color = equipped ? EquippedColor : UnequippedColor;
    }

    public ItemSO GetItem() => _currentItem;
}