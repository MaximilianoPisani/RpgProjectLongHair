using Fusion;
using UnityEngine;

public class PlayerVFXSync : NetworkBehaviour
{
    private PlayerVFXController _vfxController;
    private PlayerCombat _combat;

    public override void Spawned()
    {
        _vfxController = GetComponent<PlayerVFXController>();
        _combat = GetComponent<PlayerCombat>();
    }

    // ===== API PÚBLICA =====

    public void SpawnSlashVFX(AttackVFXConfig config)
    {
        if (config == null || config.vfxPrefab == null) return;

        // Obtener posición real del spawn point
        Transform point = _vfxController.GetSlashSpawnPoint();
        Vector3 worldPos = point != null ? point.position : transform.position;
        Quaternion worldRot = point != null ? point.rotation : transform.rotation;

        _vfxController?.SpawnSlashVFX(config); // local inmediato

        if (Object.HasStateAuthority)
            RPC_SpawnSlashVFX(config.vfxPrefab.name, worldPos, worldRot,
                              config.localOffset, config.followTransform, config.customDuration);
        else if (Object.HasInputAuthority)
            RPC_RequestSlashVFX(config.vfxPrefab.name, worldPos, worldRot,
                                config.localOffset, config.followTransform, config.customDuration);
    }

    public void SpawnShellEjectionVFX(AttackVFXConfig config)
    {
        if (config == null || config.vfxPrefab == null) return;

        Transform point = _vfxController.GetShellSpawnPoint();
        Vector3 worldPos = point != null ? point.position : transform.position;
        Quaternion worldRot = point != null ? point.rotation : transform.rotation;

        _vfxController?.SpawnShellEjectionVFX(config);

        if (Object.HasStateAuthority)
            RPC_SpawnShellEjectionVFX(config.vfxPrefab.name, worldPos, worldRot,
                                      config.localOffset, config.followTransform, config.customDuration);
        else if (Object.HasInputAuthority)
            RPC_RequestShellEjectionVFX(config.vfxPrefab.name, worldPos, worldRot,
                                        config.localOffset, config.followTransform, config.customDuration);
    }

    public void SpawnFireEjectionVFX(AttackVFXConfig config)
    {
        if (config == null || config.vfxPrefab == null) return;

        Transform point = _vfxController.GetFireSpawnPoint();
        Vector3 worldPos = point != null ? point.position : transform.position;
        Quaternion worldRot = point != null ? point.rotation : transform.rotation;

        _vfxController?.SpawnFireEjectionVFX(config);

        if (Object.HasStateAuthority)
            RPC_SpawnFireEjectionVFX(config.vfxPrefab.name, worldPos, worldRot,
                                     config.localOffset, config.followTransform, config.customDuration);
        else if (Object.HasInputAuthority)
            RPC_RequestFireEjectionVFX(config.vfxPrefab.name, worldPos, worldRot,
                                       config.localOffset, config.followTransform, config.customDuration);
    }

    // ===== RPCs SLASH =====

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestSlashVFX(string prefabName, Vector3 pos, Quaternion rot,
                                      Vector3 offset, bool follow, float duration)
        => RPC_SpawnSlashVFX(prefabName, pos, rot, offset, follow, duration);

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)] //Proxies, no All
    private void RPC_SpawnSlashVFX(string prefabName, Vector3 pos, Quaternion rot,
                                    Vector3 offset, bool follow, float duration)
    {
        _vfxController?.SpawnVFXFromName(prefabName, pos, rot, offset, follow, duration, _combat);
    }

    // ===== RPCs SHELL =====

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestShellEjectionVFX(string prefabName, Vector3 pos, Quaternion rot,
                                          Vector3 offset, bool follow, float duration)
    => RPC_SpawnShellEjectionVFX(prefabName, pos, rot, offset, follow, duration);

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)]
    private void RPC_SpawnShellEjectionVFX(string prefabName, Vector3 pos, Quaternion rot,
                                            Vector3 offset, bool follow, float duration)
        => _vfxController?.SpawnVFXFromName(prefabName, pos, rot, offset, follow, duration, _combat);

    // ===== RPCs FIRE =====

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestFireEjectionVFX(string prefabName, Vector3 pos, Quaternion rot,
                                          Vector3 offset, bool follow, float duration)
     => RPC_SpawnFireEjectionVFX(prefabName, pos, rot, offset, follow, duration);

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)]
    private void RPC_SpawnFireEjectionVFX(string prefabName, Vector3 pos, Quaternion rot,
                                           Vector3 offset, bool follow, float duration)
        => _vfxController?.SpawnVFXFromName(prefabName, pos, rot, offset, follow, duration, _combat);
}