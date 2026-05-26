using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class QuestEnemyPool : MonoBehaviour
{
    [SerializeField] private string _missionId;
    [SerializeField] private List<SpawnData> _spawnDatas = new();
    private List<NetworkObject> _spawnedEnemies = new();

    private void OnEnable()
    {
        MissionEvents.OnMissionStart += OnMissionStart;
        MissionEvents.OnMissionComplete += OnMissionComplete;
        MissionEvents.OnMissionFailed += OnMissionFailed;
    }

    private void OnDisable()
    {
        MissionEvents.OnMissionStart    -= OnMissionStart;
        MissionEvents.OnMissionComplete -= OnMissionComplete;
        MissionEvents.OnMissionFailed   -= OnMissionFailed;
    }

    private void OnMissionStart(QuestDataSO data)
    {
        Debug.Log(
            $"[QuestEnemyPool] Evento recibido {data.questId} / esperado {_missionId}");

        if (data.questId != _missionId)
            return;

        if (_spawnedEnemies.Count > 0)
            return;

        var runner = FindFirstObjectByType<NetworkRunner>();

        if (runner == null || !runner.IsServer)
            return;

        foreach (var spawnData in _spawnDatas)
        {
            if (spawnData.Prefab == null)
                continue;

            for (int i = 0; i < spawnData.Count; i++)
            {
                Transform point =
                    spawnData.SpawnPoints[
                        i % spawnData.SpawnPoints.Length];

                NetworkObject enemy =
                    runner.Spawn(
                        spawnData.Prefab,
                        point.position,
                        Quaternion.identity);

                if (enemy != null)
                    _spawnedEnemies.Add(enemy);
            }
        }
    }

    private void OnMissionComplete(QuestDataSO data)
    {
        if (data.questId != _missionId)
            return;

        Debug.Log(
            $"[QuestEnemyPool] MissionComplete {_missionId}");

        DespawnEnemies();

        _spawnedEnemies.Clear();
    }

    private void OnMissionFailed(QuestDataSO data)
    {
        if (data.questId != _missionId)
            return;

        Debug.Log(
            $"[QuestEnemyPool] MissionFailed {_missionId}");

        DespawnEnemies();

        _spawnedEnemies.Clear();
    }

    private void DespawnEnemies()
    {
        Debug.Log($"[QuestEnemyPool] DespawnEnemies llamado. IsServer={FindFirstObjectByType<NetworkRunner>()?.IsServer}");
        var runner = FindFirstObjectByType<NetworkRunner>();
        if (runner == null || !runner.IsServer) return;

        foreach (var enemy in _spawnedEnemies)
        {
            if (enemy != null && enemy.IsValid)
                runner.Despawn(enemy);
        }
        _spawnedEnemies.Clear();
    }

}