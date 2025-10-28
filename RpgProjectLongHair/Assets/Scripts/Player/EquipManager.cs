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
            Debug.Log("Ya hay un item equipado.");
            return false;
        }

        if (item == null)
        {
            Debug.LogWarning("Item nulo pasado a EquipItem!");
            return false;
        }

        if (item.equipPrefab == null)
        {
            Debug.LogWarning($"El item {item.itemName} no tiene equipPrefab asignado!");
            return false;
        }

        if (_equipPoint == null)
        {
            Debug.LogError("EquipPoint no asignado!");
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

        Debug.Log($"Equipado: {item.itemName} en {_equipPoint.name}");

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