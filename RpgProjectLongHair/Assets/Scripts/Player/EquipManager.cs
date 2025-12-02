using UnityEngine;
using Fusion;

[RequireComponent(typeof(PlayerInventoryData))]
// Componente encargado de manejar qué item está equipado
public class EquipManager : NetworkBehaviour
{
    [SerializeField] private Transform _equipPoint;

    private GameObject _currentEquipped;
    private PlayerInventoryData _inventory;

    // ID sincronizado del item equipado
    [Networked, OnChangedRender(nameof(OnEquippedChangedRender))] public int EquippedItemId { get; set; }

    public override void Spawned() // Obtiene inventario y renderiza si ya había un item equipado
    {
        _inventory = GetComponent<PlayerInventoryData>();

        if (EquippedItemId != 0)
            RenderEquippedItem();
    }

    public void OnSlotClicked(ItemSO item) // Interpreta clics de UI para equipar o desequipar items
    {
        if (!HasInputAuthority || item == null)
            return;

        if (EquippedItemId == item.id)
            RPC_RequestEquip(0);
        else
            RPC_RequestEquip(item.id);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)] // Solicita al servidor equipar/desequipar
    private void RPC_RequestEquip(int id, RpcInfo info = default)
    {
        if (_inventory == null)
        {
            Debug.LogWarning("EquipManager: missing inventory reference");
            return;
        }

        // Desequipar
        if (id == 0)
        {
            EquippedItemId = 0;
            return;
        }
        if (!_inventory.HasItem(id))
        {
            Debug.LogWarning($"Equip rejected: Player does not own item {id}");
            return;
        }

        EquippedItemId = id;
    }

    public void OnEquippedChangedRender()
    {
        RenderEquippedItem();
    }

    // Instancia o destruye el prefab del item equipado (para la entrega se hizo, esto luego sera mejorado con una pool) 
    private void RenderEquippedItem()
    {
        if (_currentEquipped != null)
        {
            Destroy(_currentEquipped);
            _currentEquipped = null;
        }

        if (EquippedItemId == 0)
            return;

        ItemSO item = ItemDatabase.GetItemByIdStatic(EquippedItemId);

        if (item == null)
        {
            Debug.LogWarning($"EquipManager: ItemSO {EquippedItemId} not found");
            return;
        }

        if (item.equipPrefab == null)
        {
            Debug.LogWarning($"Item {item.itemName} has no equipPrefab!");
            return;
        }

        GameObject obj = Instantiate(item.equipPrefab, _equipPoint);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        if (obj.TryGetComponent<Collider>(out var col))
            col.enabled = false;

        obj.name = item.itemName + "_Equipped";

        _currentEquipped = obj;
    }
}

