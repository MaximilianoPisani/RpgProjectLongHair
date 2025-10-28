using UnityEngine;

public class EquipManager : MonoBehaviour
{
    [SerializeField] private Transform _equipPoint;
    private GameObject _currentEquipped;

    public bool IsEquipped() => _currentEquipped != null;

    public bool EquipItem(ItemSO item)
    {
        if (_currentEquipped != null)
        {
            Debug.Log("YAn item is already equipped..");
            return false;
        }

        if (item == null)
        {
            Debug.LogWarning("Null item passed to EquipItem!");
            return false;
        }

        if (item.equipPrefab == null)
        {
            Debug.LogWarning($"The item {item.itemName} has no equipPrefab assigned!");
            return false;
        }

        if (_equipPoint == null)
        {
            Debug.LogError("EquipPoint not assigned!");
            return false;
        }

        GameObject obj = Instantiate(item.equipPrefab, _equipPoint);
        obj.name = item.itemName + "_Equipped";
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        _currentEquipped = obj;

        Collider col = obj.GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        Debug.Log($"Equipped: {item.itemName} in {_equipPoint.name}");

        return true;
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