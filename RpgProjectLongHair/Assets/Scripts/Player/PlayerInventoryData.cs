using System.Collections.Generic;
using UnityEngine;
using System;
using Fusion;

public class PlayerInventoryData : NetworkBehaviour
{
    private string SaveKey => $"inventory_{Runner.UserId}";

    public List<ItemData> Items { get; private set; } = new List<ItemData>();
    public event Action OnInventoryChanged;

    public override void Spawned()
    {
        if (!HasInputAuthority) return;
        Load();
    }

    public bool AddItem(ItemData item)
    {
        if (!HasInputAuthority) return false;

        Items.Add(item);
        Save();
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool RemoveItem(int id)
    {
        if (!HasInputAuthority) return false;

        var found = Items.Find(x => x.id == id);
        if (found.id == 0) return false;
        Items.Remove(found);
        Save();
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool HasItem(int id) => Items.Exists(x => x.id == id);

    private void Save()
    {
        var wrapper = new ItemDataListWrapper { items = Items };
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(wrapper));
        PlayerPrefs.Save();
    }

    private void Load()
    {
        string json = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrEmpty(json)) return;

        var wrapper = JsonUtility.FromJson<ItemDataListWrapper>(json);
        if (wrapper?.items != null)
        {
            Items = wrapper.items;
            OnInventoryChanged?.Invoke();
        }
    }

    [Serializable]
    private class ItemDataListWrapper
    {
        public List<ItemData> items;
    }
}