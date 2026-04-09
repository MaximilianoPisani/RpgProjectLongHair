using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class QuestEnemyPool : MonoBehaviour
{
    [SerializeField] private string _missionId = QuestIds.QUEST_TEST;
    [SerializeField] private List<SpawnData> _spawnDatas = new();
    private List<NetworkObject> _spawnedEnemies = new();

    private void OnEnable()
    {
        MissionEvents.OnMissionStart    += OnMissionStart;    // mision inicia -> spawn enemigos
        MissionEvents.OnMissionComplete += OnMissionComplete; // mision completa -> despawn
        MissionEvents.OnMissionFailed   += OnMissionFailed;   // mision falla -> despawn
    }

    private void OnDisable()
    {
        MissionEvents.OnMissionStart    -= OnMissionStart;
        MissionEvents.OnMissionComplete -= OnMissionComplete;
        MissionEvents.OnMissionFailed   -= OnMissionFailed;
    }

    private void OnMissionStart(QuestDataSO data)
    {
        // Solo activar si es MI mision
        if (data.questId != _missionId) return;

        // Buscar el NetworkRunner en la escena
        var runner = FindFirstObjectByType<NetworkRunner>();
        if (runner == null || !runner.IsServer) return;

        // Spawnear los enemigos de la mision
        foreach (var spawnData in _spawnDatas)
        {
            if (spawnData.Prefab == null) continue;
            for (int i = 0; i < spawnData.Count; i++)
            {
                Transform point = spawnData.SpawnPoints[i % spawnData.SpawnPoints.Length];
                NetworkObject enemy = runner.Spawn(spawnData.Prefab, point.position, Quaternion.identity);
                if (enemy != null)
                    _spawnedEnemies.Add(enemy);
            }
        }
    }

    private void OnMissionComplete(QuestDataSO data)
    {
        if (data.questId != _missionId) return;
        DespawnEnemies();
    }

    private void OnMissionFailed(QuestDataSO data)
    {
        if (data.questId != _missionId) return;
        DespawnEnemies();
    }

    private void DespawnEnemies()
    {
        var runner = FindFirstObjectByType<NetworkRunner>();
        if (runner == null) return;

        foreach (var enemy in _spawnedEnemies)
        {
            if (enemy != null)
                runner.Despawn(enemy);
        }
        _spawnedEnemies.Clear();
    }

}