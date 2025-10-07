using System.Collections.Generic;
using UnityEngine;

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance { get; private set; }

    [System.Serializable]
    public class ItemEntry
    {
        public ItemType type; 
        public GameObject prefab;  
    }

    [SerializeField] private List<ItemEntry> _items = new List<ItemEntry>();
    private Dictionary<ItemType, GameObject> _lookup = new Dictionary<ItemType, GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        foreach (var item in _items)
        {
            if (!_lookup.ContainsKey(item.type))
                _lookup[item.type] = item.prefab;
        }
    }

    public GameObject GetPrefab(ItemType type)
    {
        if (_lookup.TryGetValue(type, out var prefab))
            return prefab;
        return null;
    }
}