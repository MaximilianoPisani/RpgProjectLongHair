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

    public void OnShellVFXTriggered()
    {
        var config = GetCurrentShellConfig();
        if (config != null)
            _vfxController?.SpawnShellEjectionVFX(config);
    }

    public void OnFireVFXTriggered()
    {
        var config = GetCurrentFireConfig();
        if (config != null)
            _vfxController?.SpawnFireEjectionVFX(config);
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

    private AttackVFXConfig GetCurrentShellConfig()
    {
        var rage = GetComponent<PlayerRageHandler>();
        if (rage != null && rage.IsRageActive)
        {
            var rageConfig = rage.RageData?.GetConfigForWeapon(_combat.CurrentWeapon);
            if (rageConfig?.rageShellEjectionVFX != null)
                return rageConfig.rageShellEjectionVFX;
        }
        return _combat?.RangeData?.ShellEjectionVFX;
    }

    private AttackVFXConfig GetCurrentFireConfig()
    {
        var rage = GetComponent<PlayerRageHandler>();
        if (rage != null && rage.IsRageActive)
        {
            var rageConfig = rage.RageData?.GetConfigForWeapon(_combat.CurrentWeapon);
            if (rageConfig?.rageFireEjectionVFX != null)
                return rageConfig.rageFireEjectionVFX;
        }
        return _combat?.RangeData?.FireEjectionVFX;
    }
}