using Fusion;
using UnityEngine;
using System.Threading.Tasks;

public class PlayerCheckpoint : NetworkBehaviour
{
    [Networked] public Vector3 LastCheckpoint { get; set; }

    private PlayerCloudSave _cloudSave;

    public override void Spawned()
    {
        if (HasStateAuthority)
            LastCheckpoint = transform.position;

        if (HasInputAuthority)
        {
            _cloudSave = GetComponent<PlayerCloudSave>();
            if (_cloudSave == null)
                _cloudSave = gameObject.AddComponent<PlayerCloudSave>();
        }
    }

    public void SetCheckpoint(Vector3 position)
    {
        if (HasInputAuthority)
            RPC_SaveCheckpoint(position);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SaveCheckpoint(Vector3 position)
    {
        LastCheckpoint = position;
        Debug.Log($"[Checkpoint] Guardado en red: {position}");
    }

    public void PersistCheckpoint(Vector3 position)
    {
        if (!HasInputAuthority) return;
        _ = SaveCheckpointToCloud(position);
    }

    private async Task SaveCheckpointToCloud(Vector3 position)
    {
        var saveData = await _cloudSave.LoadPlayerData(); 
        saveData.SetCheckpoint(position);
        await _cloudSave.SavePlayerData(saveData);
        Debug.Log($"[Checkpoint] Persistido en cloud: {position}");
    }
}