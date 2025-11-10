using UnityEngine;

public class EquipManager : MonoBehaviour
{
    [SerializeField] private Transform _equipPoint;
    private GameObject _currentEquipped;

    public bool IsEquipped() => _currentEquipped != null;

    public void EquipItemFromSlot(ItemSO item)
    {
        if (_currentEquipped != null)
            UnequipCurrent();

        if (item == null || item.equipPrefab == null)
        {
            Debug.LogWarning($"Cannot equip {item?.itemName}");
            return;
        }

        GameObject obj = Instantiate(item.equipPrefab, _equipPoint);
        obj.name = item.itemName + "_Equipped";
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        _currentEquipped = obj;

        Collider col = obj.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Debug.Log($"Equipped {item.itemName}");
    }

    public ItemSO GetCurrentEquippedItemSO()
    {
        if (_currentEquipped == null) return null;
        var pickup = _currentEquipped.GetComponent<PickupableItem>();
        return pickup != null ? pickup.ItemDataSO : null;
    }

    public void UnequipCurrent()
    {
        if (_currentEquipped != null)
        {
            Destroy(_currentEquipped);
            _currentEquipped = null;
        }
    }
}