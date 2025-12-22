using Fusion;
using UnityEngine;

public class PlayerCheckpoint : NetworkBehaviour
{
    [Networked] public Vector3 LastCheckpoint { get; set; }
    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            LastCheckpoint = transform.position;
            Debug.Log($"[Checkpoint] Initial checkpoint set at {LastCheckpoint}");
        }
    }

    public void SetCheckpoint(Vector3 position)
    {
        if (!HasStateAuthority)
            return;

        LastCheckpoint = position;
        Debug.Log($"[Checkpoint] New checkpoint saved at {position}");
    }
}