using Fusion;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CheckpointZone : NetworkBehaviour
{
    [SerializeField] private Transform _spawnPoint;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent<PlayerCheckpoint>(out var checkpoint))
            return;

        Vector3 spawnPos = _spawnPoint != null
            ? _spawnPoint.position
            : transform.position;

        checkpoint.SetCheckpoint(spawnPos);

        Debug.Log("[CheckpointZone] Checkpoint saved");
    }
    private void OnValidate()
    {
        if (_spawnPoint == null)
            Debug.LogWarning($"{name}: SpawnPoint not assigned");
    }

}