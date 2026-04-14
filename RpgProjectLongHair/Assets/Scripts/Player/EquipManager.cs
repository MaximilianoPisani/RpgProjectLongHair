using UnityEngine;
using Fusion;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(PlayerInventoryData))]
// Componente encargado de manejar qué item está equipado
public class EquipManager : NetworkBehaviour
{
    [Header("Equip Points")]
    [SerializeField] private Transform _meleePointA;
    [SerializeField] private Transform _meleePointB;
    [SerializeField] private Transform _rangedPoint;

    private List<GameObject> _currentEquipped = new List<GameObject>();
    private PlayerInventoryData _inventory;

    public event Action<int> OnEquippedChanged;

    // ID sincronizado del item equipado
    [Networked, OnChangedRender(nameof(OnEquippedChangedRender))]
    public int EquippedItemId { get; set; }

    public override void Spawned()
    {
        _inventory = GetComponent<PlayerInventoryData>();

        if (EquippedItemId != 0)
            RenderEquippedItem();
    }

    public void OnSlotClicked(ItemSO item)
    {
        if (!HasInputAuthority || item == null) return;

        var sm = GetComponent<PlayerStateMachine>();
        if (sm != null && sm.IsBusy)
        {
            Debug.Log("[Equip] Player ocupado, no se puede equipar ahora");
            return;
        }

        if (EquippedItemId == item.id)
            RPC_RequestEquip(0);
        else
            RPC_RequestEquip(item.id);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestEquip(int id, RpcInfo info = default)
    {
        if (_inventory == null) return;

        // Validar en servidor también
        var sm = GetComponent<PlayerStateMachine>();
        if (sm != null && sm.IsBusy)
        {
            Debug.LogWarning("[Equip] Servidor rechazó equip: player ocupado");
            return;
        }

        if (id == 0) { EquippedItemId = 0; return; }

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
        OnEquippedChanged?.Invoke(EquippedItemId);
    }

    private void RenderEquippedItem()
    {
        ClearEquipped();

        if (EquippedItemId == 0)
            return;

        ItemSO item = ItemDatabase.GetItemByIdStatic(EquippedItemId);

        if (item == null)
        {
            Debug.LogWarning($"EquipManager: ItemSO {EquippedItemId} not found");
            return;
        }

        if (item.isDualWield)
        {
            if (_meleePointA == null || _meleePointB == null)
            {
                Debug.LogError("Faltan equip points para dual wield");
                return;
            }

            if (item.rightHandPrefab == null || item.leftHandPrefab == null)
            {
                Debug.LogError($"Item {item.itemName} no tiene prefabs duales");
                return;
            }

            var right = Instantiate(item.rightHandPrefab, _meleePointA);
            Setup(right, item, "Right");

            var left = Instantiate(item.leftHandPrefab, _meleePointB);
            Setup(left, item, "Left");

            _currentEquipped.Add(right);
            _currentEquipped.Add(left);

            return;
        }

        Transform parentPoint = null;

        if (item.type == ItemType.Weapon)
        {
            switch (item.weaponCategory)
            {
                case WeaponCategory.Axe:
                case WeaponCategory.Hammer:
                    parentPoint = _meleePointA;
                    break;

                case WeaponCategory.Rifle:
                case WeaponCategory.Gatling:
                    parentPoint = _rangedPoint;
                    break;
            }
        }

        if (parentPoint == null || item.equipPrefab == null)
        {
            Debug.LogError($"Equip mal configurado en {item.itemName}");
            return;
        }

        var obj = Instantiate(item.equipPrefab, parentPoint);
        Setup(obj, item, "");

        _currentEquipped.Add(obj);
    }

    private void ClearEquipped()
    {
        foreach (var obj in _currentEquipped)
        {
            if (obj != null)
                Destroy(obj);
        }

        _currentEquipped.Clear();
    }

    private void Setup(GameObject obj, ItemSO item, string suffix)
    {
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        if (obj.TryGetComponent<Collider>(out var col))
            col.enabled = false;

        obj.name = $"{item.itemName}_{suffix}";
    }
}