using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Fusion;

public class PlayerInventoryData : NetworkBehaviour
{
    private string LocalKey => $"inventory_{Runner?.UserId ?? "local"}";

    public List<ItemData> Items { get; private set; } = new List<ItemData>();
    public event Action OnInventoryChanged;

    private PlayerCloudSave _cloudSave;

    private PlayerSaveData _cachedSaveData = new PlayerSaveData();

    public override void Spawned()
    {
        if (!HasInputAuthority) return;
        _cloudSave = GetComponent<PlayerCloudSave>()
                  ?? gameObject.AddComponent<PlayerCloudSave>();

        PlayerPrefs.DeleteKey(LocalKey);
        PlayerPrefs.Save();

        _ = LoadFromCloud();
    }

    private async Task LoadFromCloud()
    {
        _cachedSaveData = await _cloudSave.LoadPlayerData();

        Items.Clear();
        if (_cachedSaveData.inventoryItemIds != null)
        {
            foreach (int id in _cachedSaveData.inventoryItemIds)
                if (id != 0)
                    Items.Add(new ItemData { id = id, type = ItemType.Weapon });
        }

        if (Items.Count == 0)
            LoadFromPrefs();

        OnInventoryChanged?.Invoke();
        Debug.Log($"[Inventory] Cargado: {Items.Count} items");
    }

    public bool AddItem(ItemData item)
    {
        if (!HasInputAuthority)
            return false;

        ItemSO itemSO = ItemDatabase.GetItemByIdStatic(item.id);

        if (itemSO != null &&
            itemSO.weaponCategory == WeaponCategory.CraftItem)
        {
            bool alreadyExists = Items.Exists(x => x.id == item.id);

            if (alreadyExists)
            {
                Debug.LogWarning($"[Inventory] Craft item duplicado bloqueado: {item.id}");
                return false;
            }
        }

        Items.Add(item);

        Debug.Log($"[Inventory] Item agregado: {item.id}");

        _ = SaveToCloud();
        OnInventoryChanged?.Invoke();

        return true;
    }

    public bool RemoveItem(int id)
    {
        if (!HasInputAuthority)
            return false;

        int index = Items.FindIndex(x => x.id == id);

        if (index == -1)
        {
            Debug.LogWarning($"[Inventory] RemoveItem: id {id} no encontrado");
            return false;
        }

        Items.RemoveAt(index);

        Debug.Log($"[Inventory] Item removido: {id}");

        _ = SaveToCloud();
        OnInventoryChanged?.Invoke();

        return true;
    }

    public bool HasItem(int id) => Items.Exists(x => x.id == id);
    public bool HasCraftItem(int id) => HasItem(id);

    public bool HasAllCraftItems(List<CraftItemSO> required)
    {
        foreach (var item in required)
            if (!HasItem(item.id)) return false;
        return true;
    }

    public async Task SaveToCloud()
    {
        if (_cloudSave == null) return;

        _cachedSaveData.inventoryItemIds = new int[Items.Count];
        for (int i = 0; i < Items.Count; i++)
            _cachedSaveData.inventoryItemIds[i] = Items[i].id;

        await _cloudSave.SavePlayerData(_cachedSaveData);
        SaveToPrefs();
    }

    private void SaveToPrefs()
    {
        var wrapper = new ItemDataListWrapper { items = Items };
        PlayerPrefs.SetString(LocalKey, JsonUtility.ToJson(wrapper));
        PlayerPrefs.Save();
    }

    private void LoadFromPrefs()
    {
        string json = PlayerPrefs.GetString(LocalKey, "");
        if (string.IsNullOrEmpty(json)) return;
        var wrapper = JsonUtility.FromJson<ItemDataListWrapper>(json);
        if (wrapper?.items != null)
            Items = wrapper.items;
    }

    [Serializable]
    private class ItemDataListWrapper { public List<ItemData> items; }
}