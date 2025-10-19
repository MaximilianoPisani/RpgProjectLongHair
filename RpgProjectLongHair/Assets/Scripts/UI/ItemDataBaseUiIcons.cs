using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ItemDatabaseUIIcons", menuName = "Inventory/ItemDatabaseUIIcons")]
public class ItemDatabaseUIIcons : ScriptableObject
{
    [Serializable]
    public struct IconData
    {
        public ItemType type;
        public Sprite icon;
    }

    [SerializeField] private IconData[] icons;

    private Dictionary<ItemType, Sprite> _iconLookup;

    private void OnEnable()
    {
        BuildDictionary();
    }

    private void BuildDictionary()
    {
        _iconLookup = new Dictionary<ItemType, Sprite>();

        foreach (var entry in icons)
        {
            if (!_iconLookup.ContainsKey(entry.type))
                _iconLookup.Add(entry.type, entry.icon);
        }
    }

    public Sprite GetIcon(ItemType type)
    {
        if (_iconLookup == null || _iconLookup.Count == 0)
            BuildDictionary();

        if (_iconLookup.TryGetValue(type, out var sprite))
            return sprite;

        Debug.LogWarning($"No icon assigned for ItemType: {type}");
        return null;
    }
}