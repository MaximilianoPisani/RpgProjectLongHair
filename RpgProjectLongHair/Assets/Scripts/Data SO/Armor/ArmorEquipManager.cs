using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class ArmorEquipManager : NetworkBehaviour
{
    [Header("References")]
    [Tooltip("SkinnedMeshRenderer del modelo base del personaje")]
    [SerializeField] private SkinnedMeshRenderer _baseSMR;

    [Tooltip("Raíz del armature del personaje (ej: transform 'Armature' o 'Hips')")]
    [SerializeField] private Transform _armatureRoot;

    private PlayerArmorData _armorData;
    private Mesh _originalMesh;
    private Material[] _originalMaterials;

    public PlayerArmorData ArmorData => _armorData;

    public override void Spawned()
    {
        _armorData = GetComponent<PlayerArmorData>();

        if (_armorData == null)
        {
            Debug.LogError("[ArmorEquipManager] PlayerArmorData no encontrado en el GameObject");
            return;
        }

        _armorData.OnArmorChanged += ApplyArmor;

        if (_armorData.EquippedArmorId != 0)
            ApplyArmor(_armorData.EquippedArmorId);
    }

    private void OnDestroy()
    {
        if (_armorData != null)
            _armorData.OnArmorChanged -= ApplyArmor;
    }

    // ?? Equip request ??????????????????????????????????????????????????????

    public void RequestEquipArmor(ItemSO item)
    {
        if (!HasInputAuthority || item == null || item.type != ItemType.Armor) return;

        var sm = GetComponent<PlayerStateMachine>();
        if (sm != null && sm.IsBusy)
        {
            Debug.Log("[Armor] Player ocupado, no se puede equipar ahora");
            return;
        }

        int idToSend = _armorData.EquippedArmorId == item.id ? 0 : item.id;

        if (Object.HasStateAuthority)
            _armorData.SetArmor(idToSend);
        else
            RPC_RequestEquipArmor(idToSend);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestEquipArmor(int itemId, RpcInfo info = default)
    {
        var sm = GetComponent<PlayerStateMachine>();
        if (sm != null && sm.IsBusy)
        {
            Debug.LogWarning("[Armor] Servidor rechazó equip: player ocupado");
            return;
        }

        _armorData.SetArmor(itemId);
    }

    // ?? Render ?????????????????????????????????????????????????????????????

    private void ApplyArmor(int itemId)
    {
        Debug.Log($"[Armor] ApplyArmor — itemId={itemId}");
        ClearActiveArmor();

        if (itemId == 0) return; // ClearActiveArmor ya restauró el mesh base

        ItemSO item = ItemDatabase.GetItemByIdStatic(itemId);
        if (item == null)
        {
            Debug.LogWarning($"[Armor] ItemSO {itemId} no encontrado en ItemDatabase");
            return;
        }

        if (item.armorMesh != null)
            ApplyMeshDirect(item);
        else
            Debug.LogError($"[Armor] {item.itemName} no tiene armorMesh asignado en el ItemSO");
    }

    private void ApplyMeshDirect(ItemSO item)
    {
        if (_baseSMR == null)
        {
            Debug.LogError("[Armor] _baseSMR no asignado en el inspector");
            return;
        }

        // Guardar mesh y materiales originales solo la primera vez
        if (_originalMesh == null)
        {
            _originalMesh = _baseSMR.sharedMesh;
            _originalMaterials = _baseSMR.sharedMaterials;
        }

        // Swapear directamente en el SMR existente — bones y armature intactos
        _baseSMR.sharedMesh = item.armorMesh;
        _baseSMR.sharedMaterials = item.armorMaterials != null && item.armorMaterials.Length > 0
            ? item.armorMaterials
            : _originalMaterials;

        _baseSMR.enabled = true;
    }

    private void ClearActiveArmor()
    {
        if (_baseSMR == null) return;

        // Restaurar mesh original si había un swap activo
        if (_originalMesh != null)
        {
            _baseSMR.sharedMesh = _originalMesh;
            _baseSMR.sharedMaterials = _originalMaterials;
            _baseSMR.enabled = true;
            _originalMesh = null;
            _originalMaterials = null;
        }
    }
}