using System.Collections.Generic;
using UnityEngine;
using Fusion;

[System.Serializable]
public class ItemSpawnData
{
    public NetworkObject Prefab;
    public Transform[] SpawnPoints;
    public int Count = 1;

    [Header("Quest Spawn")]
    public bool SpawnOnQuestStart;
    public string QuestId;
}
public class ItemSpawner : MonoBehaviour
{
    public static ItemSpawner Instance { get; private set; }

    [SerializeField]
    private List<ItemSpawnData> _spawnDatas = new();

    private readonly List<NetworkObject> _spawnedItems = new();

    private readonly Dictionary<string, List<NetworkObject>>
        _questSpawnedItems = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        MissionEvents.OnMissionStart += OnMissionStart;
        MissionEvents.OnMissionComplete += OnMissionFinished;
        MissionEvents.OnMissionFailed += OnMissionFinished;
    }

    private void OnDisable()
    {
        MissionEvents.OnMissionStart -= OnMissionStart;
        MissionEvents.OnMissionComplete -= OnMissionFinished;
        MissionEvents.OnMissionFailed -= OnMissionFinished;
    }

    public void SpawnItems(NetworkRunner runner)
    {
        if (!runner.IsServer)
            return;

        Debug.Log(
            $"[ItemSpawner] SpawnItems llamado - items={_spawnDatas.Count}");

        foreach (var data in _spawnDatas)
        {
            if (data.SpawnOnQuestStart)
                continue;

            SpawnItemGroup(runner, data, null);
        }
    }

    private void OnMissionStart(QuestDataSO quest)
    {
        var runner = FindFirstObjectByType<NetworkRunner>();

        if (runner == null || !runner.IsServer)
            return;

        Debug.Log(
            $"[ItemSpawner] OnMissionStart => {quest.questId}");

        foreach (var data in _spawnDatas)
        {
            if (!data.SpawnOnQuestStart)
                continue;

            if (data.QuestId != quest.questId)
                continue;

            SpawnItemGroup(
                runner,
                data,
                quest.questId);
        }
    }

    private void OnMissionFinished(QuestDataSO quest)
    {
        var runner = FindFirstObjectByType<NetworkRunner>();

        if (runner == null || !runner.IsServer)
            return;

        if (!_questSpawnedItems.TryGetValue(
                quest.questId,
                out var items))
            return;

        Debug.Log(
            $"[ItemSpawner] Despawneando items de misión {quest.questId}");

        foreach (var item in items)
        {
            if (item != null && item.IsValid)
                runner.Despawn(item);
        }

        _questSpawnedItems.Remove(quest.questId);
    }

    private void SpawnItemGroup(
        NetworkRunner runner,
        ItemSpawnData data,
        string questId)
    {
        if (data.Prefab == null)
            return;

        if (data.SpawnPoints == null ||
            data.SpawnPoints.Length == 0)
            return;

        List<NetworkObject> questItems = null;

        if (!string.IsNullOrEmpty(questId))
        {
            if (!_questSpawnedItems.TryGetValue(
                    questId,
                    out questItems))
            {
                questItems = new List<NetworkObject>();

                _questSpawnedItems.Add(
                    questId,
                    questItems);
            }
        }

        for (int i = 0; i < data.Count; i++)
        {
            Transform spawnPoint =
                data.SpawnPoints[
                    i % data.SpawnPoints.Length];

            Vector3 pos = spawnPoint.position;
            Quaternion rot = spawnPoint.rotation;

            NetworkObject itemObj =
                runner.Spawn(
                    data.Prefab,
                    pos,
                    rot,
                    PlayerRef.None);

            if (itemObj == null)
                continue;

            _spawnedItems.Add(itemObj);

            if (questItems != null)
                questItems.Add(itemObj);

            Debug.Log(
                $"[ItemSpawner] Spawn item {itemObj.name}");
        }
    }

    public List<NetworkObject> GetItems()
    {
        return _spawnedItems;
    }

    public void RemoveItem(
        NetworkRunner runner,
        NetworkObject item)
    {
        if (item == null)
            return;

        _spawnedItems.Remove(item);

        if (runner != null &&
            runner.IsServer)
        {
            runner.Despawn(item);
        }
    }
}