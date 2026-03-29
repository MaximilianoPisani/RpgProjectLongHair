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
            Debug.Log($"[Checkpoint] Initial position: {LastCheckpoint}");
        }
    }

    public void SetCheckpoint(Vector3 position)
    {
        RPC_SaveCheckpoint(position);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SaveCheckpoint(Vector3 position)
    {
        LastCheckpoint = position;
        Debug.Log($"[Checkpoint] Saved at {position}");
    }
}