using Fusion;
using System;
using System.Threading.Tasks;
using UnityEngine;

public class PlayerArmorData : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(OnArmorChangedRender))]
    public int EquippedArmorId { get; set; }

    public event Action<int> OnArmorChanged;

    private PlayerCloudSave _cloudSave;
    private PlayerSaveData _cachedSaveData;

    public override void Spawned()
    {
        if (!HasInputAuthority) return;

        _cloudSave = GetComponent<PlayerCloudSave>()
                  ?? gameObject.AddComponent<PlayerCloudSave>();

        _ = LoadFromCloud();
    }

    private async Task LoadFromCloud()
    {
        _cachedSaveData = await _cloudSave.LoadPlayerData();
        OnArmorChanged?.Invoke(EquippedArmorId);
    }

    public void SetArmor(int itemId)
    {
        if (EquippedArmorId == itemId) return; // evitar doble disparo

        EquippedArmorId = itemId;

        // OnChangedRender no trigerea en host — invocar manualmente
        if (Object.HasStateAuthority)
            OnArmorChanged?.Invoke(itemId);

        _ = SaveToCloud();
    }

    private void OnArmorChangedRender()
    {
        // Para clientes remotos
        OnArmorChanged?.Invoke(EquippedArmorId);
    }

    private async Task SaveToCloud()
    {
        if (_cloudSave == null || _cachedSaveData == null) return;
        await _cloudSave.SavePlayerData(_cachedSaveData);
    }
}