using UnityEngine;
using Fusion;

public class PlayerCombat : NetworkBehaviour
{
    [Networked] public WeaponCategory CurrentWeapon { get; set; }

    [Header("Melee")]
    public MeleeAttackData meleeData;

    [Header("Common Melee")]
    public Transform meleeOrigin;
    public LayerMask enemyLayer;

    [Header("VFX Settings - Melee")]
    [Tooltip("Punto de spawn para los efectos visuales (si es null, usa meleeOrigin)")]
    public Transform vfxSpawnPoint;

    [Header("Range")]
    public RangedAttackData RangeData;

    [Header("Common Range")]
    public Transform[] shootPoints;

    [Header("VFX Settings - Ranged")]
    [Tooltip("Punto de spawn para los casquillos/balas expulsados")]
    public Transform shellEjectionPoint;

    [Tooltip("Punto de spawn para el fuego expulsado del cañón")]
    public Transform fireEjectionPoint;

    public MeleeAttackData GetCurrentMeleeData()
    {
        switch (CurrentWeapon)
        {
            case WeaponCategory.Axe:
            case WeaponCategory.Hammer:
                return meleeData;
        }
        return null;
    }

    public RangedAttackData GetCurrentRangeData()
    {
        switch (CurrentWeapon)
        {
            case WeaponCategory.Rifle:
            case WeaponCategory.Gatling:
                return RangeData;
        }
        return null;
    }

    // ==================== VFX MELEE ====================

    /// <summary>
    /// Spawnea el VFX de slash - sincronizado en red
    /// </summary>
    public void SpawnSlashVFX(AttackVFXConfig config)
    {
        if (config == null || config.vfxPrefab == null)
        {
            Debug.LogWarning("[PlayerCombat] SpawnSlashVFX: config o prefab nulo");
            return;
        }

        Transform spawnTransform = vfxSpawnPoint != null ? vfxSpawnPoint : meleeOrigin;
        if (spawnTransform == null) spawnTransform = transform;

        // Spawn local inmediato (predicción)
        SpawnVFXLocal(config, spawnTransform);

        // Sincroniza con otros clientes
        if (Object.HasStateAuthority)
        {
            RPC_SpawnSlashVFX(config.vfxPrefab.name, spawnTransform.position, spawnTransform.rotation,
                              config.localOffset, config.followTransform, config.customDuration);
        }
        else if (Object.HasInputAuthority)
        {
            RPC_RequestSlashVFX(config.vfxPrefab.name, spawnTransform.position, spawnTransform.rotation,
                                config.localOffset, config.followTransform, config.customDuration);
        }

        Debug.Log($"[PlayerCombat] Slash VFX spawned: {config.vfxPrefab.name}");
    }

    // ==================== VFX RANGED ====================

    /// <summary>
    /// Spawnea el VFX de expulsión de casquillo - sincronizado en red
    /// </summary>
    public void SpawnShellEjectionVFX(AttackVFXConfig config)
    {
        if (config == null || config.vfxPrefab == null)
        {
            Debug.LogWarning("[PlayerCombat] SpawnShellEjectionVFX: config o prefab nulo");
            return;
        }

        Transform spawnTransform = shellEjectionPoint;
        if (spawnTransform == null && shootPoints != null && shootPoints.Length > 0)
            spawnTransform = shootPoints[0];
        if (spawnTransform == null) spawnTransform = transform;

        SpawnVFXLocal(config, spawnTransform);

        if (Object.HasStateAuthority)
        {
            RPC_SpawnShellEjectionVFX(config.vfxPrefab.name, spawnTransform.position, spawnTransform.rotation,
                                      config.localOffset, config.followTransform, config.customDuration);
        }
        else if (Object.HasInputAuthority)
        {
            RPC_RequestShellEjectionVFX(config.vfxPrefab.name, spawnTransform.position, spawnTransform.rotation,
                                        config.localOffset, config.followTransform, config.customDuration);
        }

        Debug.Log($"[PlayerCombat] Shell ejection VFX spawned: {config.vfxPrefab.name}");
    }

    /// <summary>
    /// Spawnea el VFX de fogonazo/fire ejection - sincronizado en red
    /// </summary>
    public void SpawnFireEjectionVFX(AttackVFXConfig config)
    {
        if (config == null || config.vfxPrefab == null)
        {
            Debug.LogWarning("[PlayerCombat] SpawnFireEjectionVFX: config o prefab nulo");
            return;
        }

        Transform spawnTransform = fireEjectionPoint;
        if (spawnTransform == null && shootPoints != null && shootPoints.Length > 0)
            spawnTransform = shootPoints[0];
        if (spawnTransform == null) spawnTransform = transform;

        SpawnVFXLocal(config, spawnTransform);

        if (Object.HasStateAuthority)
        {
            RPC_SpawnFireEjectionVFX(config.vfxPrefab.name, spawnTransform.position, spawnTransform.rotation,
                                     config.localOffset, config.followTransform, config.customDuration);
        }
        else if (Object.HasInputAuthority)
        {
            RPC_RequestFireEjectionVFX(config.vfxPrefab.name, spawnTransform.position, spawnTransform.rotation,
                                       config.localOffset, config.followTransform, config.customDuration);
        }

        Debug.Log($"[PlayerCombat] Fire ejection VFX spawned: {config.vfxPrefab.name}");
    }

    // ==================== HELPERS ====================

    /// <summary>
    /// Spawn local del VFX (sin red)
    /// </summary>
    private void SpawnVFXLocal(AttackVFXConfig config, Transform spawnTransform)
    {
        Vector3 spawnPosition = spawnTransform.position
            + spawnTransform.TransformDirection(config.localOffset);
        Quaternion spawnRotation = spawnTransform.rotation;

        GameObject vfxInstance = Instantiate(config.vfxPrefab, spawnPosition, spawnRotation);

        if (config.followTransform)
            vfxInstance.transform.SetParent(spawnTransform, true);

        if (config.customDuration > 0f)
            Destroy(vfxInstance, config.customDuration);
    }

    /// <summary>
    /// Spawn VFX desde el nombre del prefab (usado en RPCs)
    /// </summary>
    private void SpawnVFXFromName(string prefabName, Vector3 position, Quaternion rotation,
                                   Vector3 localOffset, bool followTransform, float customDuration)
    {
        // Busca el prefab por nombre en tus configs
        GameObject prefab = FindVFXPrefabByName(prefabName);
        if (prefab == null)
        {
            Debug.LogWarning($"[PlayerCombat] VFX prefab no encontrado: {prefabName}");
            return;
        }

        Vector3 spawnPosition = position + rotation * localOffset;
        GameObject vfxInstance = Instantiate(prefab, spawnPosition, rotation);

        if (followTransform)
        {
            // Si debe seguir, busca el transform más cercano
            Transform closestTransform = FindClosestTransform(position);
            if (closestTransform != null)
                vfxInstance.transform.SetParent(closestTransform, true);
        }

        if (customDuration > 0f)
            Destroy(vfxInstance, customDuration);
    }

    /// <summary>
    /// Encuentra el prefab VFX por nombre
    /// </summary>
    private GameObject FindVFXPrefabByName(string name)
    {
        // Slash VFX
        if (meleeData?.ComboAttacks != null)
        {
            foreach (var combo in meleeData.ComboAttacks)
            {
                if (combo.attackVFX?.vfxPrefab != null && combo.attackVFX.vfxPrefab.name == name)
                    return combo.attackVFX.vfxPrefab;
            }
        }

        // Ranged VFX
        if (RangeData?.ShellEjectionVFX?.vfxPrefab?.name == name)
            return RangeData.ShellEjectionVFX.vfxPrefab;

        if (RangeData?.FireEjectionVFX?.vfxPrefab?.name == name)
            return RangeData.FireEjectionVFX.vfxPrefab;

        return null;
    }

    /// <summary>
    /// Encuentra el transform más cercano para parenting
    /// </summary>
    private Transform FindClosestTransform(Vector3 position)
    {
        float minDist = float.MaxValue;
        Transform closest = null;

        Transform[] candidates = { vfxSpawnPoint, shellEjectionPoint, fireEjectionPoint };

        foreach (var t in candidates)
        {
            if (t == null) continue;
            float dist = Vector3.Distance(t.position, position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = t;
            }
        }

        return closest ?? transform;
    }

    // ==================== RPCs - MELEE ====================

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestSlashVFX(string prefabName, Vector3 pos, Quaternion rot,
                                     Vector3 offset, bool follow, float duration)
    {
        RPC_SpawnSlashVFX(prefabName, pos, rot, offset, follow, duration);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SpawnSlashVFX(string prefabName, Vector3 pos, Quaternion rot,
                                   Vector3 offset, bool follow, float duration)
    {
        // No spawnear en quien lo pidió (ya lo hizo local)
        if (Object.HasInputAuthority) return;

        SpawnVFXFromName(prefabName, pos, rot, offset, follow, duration);
    }

    // ==================== RPCs - SHELL EJECTION ====================

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestShellEjectionVFX(string prefabName, Vector3 pos, Quaternion rot,
                                             Vector3 offset, bool follow, float duration)
    {
        RPC_SpawnShellEjectionVFX(prefabName, pos, rot, offset, follow, duration);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SpawnShellEjectionVFX(string prefabName, Vector3 pos, Quaternion rot,
                                           Vector3 offset, bool follow, float duration)
    {
        if (Object.HasInputAuthority) return;

        SpawnVFXFromName(prefabName, pos, rot, offset, follow, duration);
    }

    // ==================== RPCs - FIRE EJECTION ====================

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestFireEjectionVFX(string prefabName, Vector3 pos, Quaternion rot,
                                            Vector3 offset, bool follow, float duration)
    {
        RPC_SpawnFireEjectionVFX(prefabName, pos, rot, offset, follow, duration);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SpawnFireEjectionVFX(string prefabName, Vector3 pos, Quaternion rot,
                                          Vector3 offset, bool follow, float duration)
    {
        if (Object.HasInputAuthority) return;

        SpawnVFXFromName(prefabName, pos, rot, offset, follow, duration);
    }
}