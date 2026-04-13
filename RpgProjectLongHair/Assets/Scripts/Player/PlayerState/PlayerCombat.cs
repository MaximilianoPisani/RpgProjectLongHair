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

    [Header("VFX Settings")]
    [Tooltip("Punto de spawn para los efectos visuales (si es null, usa meleeOrigin)")]
    public Transform vfxSpawnPoint;

    [Tooltip("Offset local para el spawn de VFX")]
    public Vector3 vfxOffset = Vector3.zero;

    [Header("Range")]
    public RangedAttackData RangeData;

    [Header("Common Range")]
    public Transform[] shootPoints;

    [Header("VFX Settings - Ranged")]
    [Tooltip("Punto de spawn para los casquillos/balas expulsados (normalmente en el lado del arma)")]
    public Transform shellEjectionPoint;

    [Tooltip("Offset local para el spawn de casquillos")]
    public Vector3 shellEjectionOffset = Vector3.zero;

    [Tooltip("Punto de spawn para el fuego expulsado del cañon")]
    public Transform fireEjectionPoint;

    [Tooltip("Offset local para el spawn de cadencia de fuego")]
    public Vector3 fireEjectionOffset = Vector3.zero;

    public MeleeAttackData GetCurrentMeleeData()
    {
        switch (CurrentWeapon)
        {
            case WeaponCategory.Axe:
                return meleeData;

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
                return RangeData;

            case WeaponCategory.Gatling:
                return RangeData;
        }

        return null;
    }

    public void SpawnSlashVFX(GameObject vfxPrefab)
    {
        if (vfxPrefab == null)
        {
            Debug.LogWarning("[PlayerCombat] No VFX prefab assigned for this attack");
            return;
        }

        // Determinar punto de spawn
        Transform spawnTransform = vfxSpawnPoint != null ? vfxSpawnPoint : meleeOrigin;

        if (spawnTransform == null)
        {
            spawnTransform = transform;
        }

        // Calcular posición y rotación
        Vector3 spawnPosition = spawnTransform.position + spawnTransform.TransformDirection(vfxOffset);
        Quaternion spawnRotation = spawnTransform.rotation;

        // Instanciar VFX
        GameObject vfxInstance = Instantiate(vfxPrefab, spawnPosition, spawnRotation);

        // Opcional: hacer que el VFX siga al jugador si tiene un componente específico
        // vfxInstance.transform.SetParent(spawnTransform);

        Debug.Log($"[PlayerCombat] Spawned slash VFX: {vfxPrefab.name}");
    }

    public void SpawnShellEjectionVFX(GameObject vfxPrefab)
    {
        if (vfxPrefab == null)
        {
            Debug.LogWarning("[PlayerCombat] No shell ejection VFX prefab assigned");
            return;
        }

        // Determinar punto de spawn - preferir shellEjectionPoint, luego shootPoints
        Transform spawnTransform = shellEjectionPoint;

        if (spawnTransform == null && shootPoints != null && shootPoints.Length > 0)
        {
            spawnTransform = shootPoints[0];
        }

        if (spawnTransform == null)
        {
            spawnTransform = transform;
        }

        // Calcular posición y rotación
        Vector3 spawnPosition = spawnTransform.position + spawnTransform.TransformDirection(shellEjectionOffset);
        Quaternion spawnRotation = spawnTransform.rotation;

        // Instanciar VFX
        GameObject vfxInstance = Instantiate(vfxPrefab, spawnPosition, spawnRotation);

        // Opcional: hacer que el VFX siga al arma durante un frame
        // Esto es útil si el jugador se mueve mientras dispara
        // vfxInstance.transform.SetParent(spawnTransform, true);

        Debug.Log($"[PlayerCombat] Spawned shell ejection VFX: {vfxPrefab.name}");
    }

    public void SpawnFireEjectionVFX(GameObject vfxPrefab)
    {
        if (vfxPrefab == null)
        {
            Debug.LogWarning("[PlayerCombat] No fire cadence ejection VFX prefab assigned");
            return;
        }

        // Determinar punto de spawn - preferir fireEjectionPoint, luego shootPoints
        Transform spawnTransform = fireEjectionPoint;

        if (spawnTransform == null && shootPoints != null && shootPoints.Length > 0)
        {
            spawnTransform = shootPoints[0];
        }

        if (spawnTransform == null)
        {
            spawnTransform = transform;
        }

        // Calcular posición y rotación
        Vector3 spawnPosition = spawnTransform.position + spawnTransform.TransformDirection(fireEjectionOffset);
        Quaternion spawnRotation = spawnTransform.rotation;

        // Instanciar VFX
        GameObject vfxInstance = Instantiate(vfxPrefab, spawnPosition, spawnRotation);

        // Opcional: hacer que el VFX siga al arma durante un frame
        // Esto es útil si el jugador se mueve mientras dispara
        // vfxInstance.transform.SetParent(spawnTransform, true);

        Debug.Log($"[PlayerCombat] Spawned shell ejection VFX: {vfxPrefab.name}");
    }
}
