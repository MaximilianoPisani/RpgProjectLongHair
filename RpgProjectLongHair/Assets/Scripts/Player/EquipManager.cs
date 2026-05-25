using UnityEngine;
using Fusion;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(PlayerInventoryData))]
public class EquipManager : NetworkBehaviour
{
    [Header("Equip Points")]
    [SerializeField] private Transform _meleePointA;
    [SerializeField] private Transform _meleePointB;
    [SerializeField] private Transform _rangedPoint;

    private List<GameObject> _currentEquipped = new List<GameObject>();
    private PlayerInventoryData _inventory;

    public event Action<int> OnEquippedChanged;

    [Networked, OnChangedRender(nameof(OnEquippedChangedRender))]
    public int EquippedItemId { get; set; }

    public override void Spawned()
    {
        _inventory = GetComponent<PlayerInventoryData>();

        if (!HasInputAuthority && EquippedItemId != 0)
            RenderEquippedItem();
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasInputAuthority) return;
        if (!GetInput(out NetworkInputData input)) return;

        if (input.scrollDelta != 0)
            CycleEquip(input.scrollDelta);
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

        if (!_inventory.HasItem(item.id))
        {
            Debug.LogWarning($"[Equip] Item {item.itemName} no está en el inventario local");
            return;
        }

        if (item.type == ItemType.Armor)
        {
            GetComponent<ArmorEquipManager>()?.RequestEquipArmor(item);
            return;
        }

        int idToSend = EquippedItemId == item.id ? 0 : item.id;

        if (Object.HasStateAuthority)
            EquippedItemId = idToSend;
        else
            RPC_RequestEquip(idToSend);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestEquip(int id, RpcInfo info = default)
    {
        var sm = GetComponent<PlayerStateMachine>();
        if (sm != null && sm.IsBusy)
        {
            Debug.LogWarning("[Equip] Servidor rechazó equip: player ocupado");
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

        if (item.type == ItemType.Armor) return;

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

        obj.name = string.IsNullOrEmpty(suffix)
            ? item.itemName
            : $"{item.itemName}_{suffix}";
    }

    public void CycleEquip(int direction)
    {
        var sm = GetComponent<PlayerStateMachine>();
        if (sm != null && sm.IsBusy) return;

        var ids = new List<int> { 0 };
        foreach (var item in _inventory.Items)
            if (item.id != 0) ids.Add(item.id);

        if (ids.Count <= 1) return;

        int currentIndex = ids.IndexOf(EquippedItemId);
        if (currentIndex < 0) currentIndex = 0;

        int nextIndex = (currentIndex + direction + ids.Count) % ids.Count;

        if (Object.HasStateAuthority)
            EquippedItemId = ids[nextIndex];
        else
            RPC_RequestEquip(ids[nextIndex]);
    }
}