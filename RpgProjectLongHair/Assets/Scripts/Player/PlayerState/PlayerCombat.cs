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
    /// Spawnea el VFX de slash usando la config del combo actual.
    /// El offset viene del AttackVFXConfig, no del inspector.
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

        SpawnVFX(config, spawnTransform);
        Debug.Log($"[PlayerCombat] Slash VFX spawned: {config.vfxPrefab.name}");
    }

    // ==================== VFX RANGED ====================

    /// <summary>
    /// Spawnea el VFX de expulsión de casquillo.
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

        SpawnVFX(config, spawnTransform);
        Debug.Log($"[PlayerCombat] Shell ejection VFX spawned: {config.vfxPrefab.name}");
    }

    /// <summary>
    /// Spawnea el VFX de fogonazo/fire ejection.
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

        SpawnVFX(config, spawnTransform);
        Debug.Log($"[PlayerCombat] Fire ejection VFX spawned: {config.vfxPrefab.name}");
    }

    // ==================== HELPERS ====================

    /// <summary>
    /// Lógica común de spawn: aplica offset del config, respeta followTransform y customDuration.
    /// </summary>
    private void SpawnVFX(AttackVFXConfig config, Transform spawnTransform)
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
}