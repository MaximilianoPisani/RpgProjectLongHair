using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using UnityEngine;
using Fusion;

public class PlayerInventoryData : NetworkBehaviour
{
    private string LocalKey => $"inventory_{Runner?.UserId ?? "local"}";

    public List<ItemData> Items { get; private set; } = new List<ItemData>();
    public event Action OnInventoryChanged;

    private PlayerCloudSave _cloudSave;

    public override void Spawned()
    {
        if (!HasInputAuthority) return;

        _cloudSave = GetComponent<PlayerCloudSave>();
        if (_cloudSave == null)
            _cloudSave = gameObject.AddComponent<PlayerCloudSave>();

        _ = LoadFromCloud();
    }

    private async Task LoadFromCloud()
    {
        PlayerSaveData saveData = await _cloudSave.LoadPlayerData(); 

        Items.Clear();

        if (saveData.inventoryItemIds != null)
        {
            foreach (int id in saveData.inventoryItemIds)
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
        if (!HasInputAuthority) return false;
        Items.Add(item);
        _ = SaveToCloud();
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool RemoveItem(int id)
    {
        if (!HasInputAuthority) return false;
        var found = Items.Find(x => x.id == id);
        if (found.id == 0) return false;
        Items.Remove(found);
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

        var saveData = await _cloudSave.LoadPlayerData(); 

        saveData.inventoryItemIds = new int[Items.Count];
        for (int i = 0; i < Items.Count; i++)
            saveData.inventoryItemIds[i] = Items[i].id;

        await _cloudSave.SavePlayerData(saveData);
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