using Fusion;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CheckpointZone : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent<PlayerCheckpoint>(out var checkpoint))
            return;

        checkpoint.SetCheckpoint(transform.position);
        Debug.Log("[CheckpointZone] Player entered checkpoint zone");
    }
}