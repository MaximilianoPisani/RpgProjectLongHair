using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class ItemSpawner : MonoBehaviour
{
    public static ItemSpawner Instance { get; private set; }

    [SerializeField] private NetworkObject _itemPrefab; 
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private int _itemCount = 5;

    private List<NetworkObject> _spawnedItems = new List<NetworkObject>();

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

        for (int i = 0; i < _itemCount; i++)
        {
            Transform spawnPoint = _spawnPoints[i % _spawnPoints.Length];
            Vector3 pos = spawnPoint.position;

            NetworkObject item = runner.Spawn(_itemPrefab, pos, Quaternion.identity);

            if (item != null)
            {
                _spawnedItems.Add(item);

                var pickup = item.GetComponent<PickupableItem>();
                if (pickup != null)
                {
                    pickup.SetRunner(runner);
                }
            }
        }
    }

    public List<NetworkObject> GetItems() => _spawnedItems;

    public void RemoveItem(NetworkRunner runner, NetworkObject item)
    {
        if (_spawnedItems.Contains(item))
        {
            _spawnedItems.Remove(item);
            runner.Despawn(item); 
        }
    }
}