using System.Collections.Generic;
using UnityEngine;
using Fusion;
using static Unity.Collections.Unicode;

[System.Serializable]
public class ItemSpawnData
{
    public NetworkObject Prefab;
    public Transform[] SpawnPoints;
    public int Count = 1;
}

public class ItemSpawner : MonoBehaviour
{
    public static ItemSpawner Instance { get; private set; }

    [SerializeField] private List<ItemSpawnData> _spawnDatas = new List<ItemSpawnData>();
    private readonly List<NetworkObject> _spawnedItems = new List<NetworkObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    public void SpawnItems(NetworkRunner runner)
    {
        if (!runner.IsServer) return;

        foreach (var data in _spawnDatas)
        {
            if (data.Prefab == null || data.SpawnPoints.Length == 0) continue;

            for (int i = 0; i < data.Count; i++)
            {
                Transform spawnPoint = data.SpawnPoints[i % data.SpawnPoints.Length];
                Vector3 pos = spawnPoint.position;

                NetworkObject itemObj = runner.Spawn(data.Prefab, pos, Quaternion.identity, null);
                if (itemObj != null)
                    _spawnedItems.Add(itemObj);
            }
        }
    }

    public List<NetworkObject> GetItems() => _spawnedItems;

    public void RemoveItem(NetworkRunner runner, NetworkObject item)
    {
        if (item == null) return;

        if (_spawnedItems.Contains(item))
            _spawnedItems.Remove(item);

        if (runner != null && item != null && runner.IsServer)
            runner.Despawn(item); 
    }
}