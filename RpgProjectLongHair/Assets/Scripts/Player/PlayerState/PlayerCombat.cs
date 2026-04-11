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
}
