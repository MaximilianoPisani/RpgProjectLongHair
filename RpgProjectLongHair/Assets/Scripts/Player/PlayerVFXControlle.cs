using UnityEngine;

public class PlayerVFXController : MonoBehaviour
{
    [Header("Melee VFX")]
    [SerializeField] private Transform vfxSpawnPoint;
    [SerializeField] private Transform meleeOrigin;

    [Header("Ranged VFX")]
    [SerializeField] private Transform shellEjectionPoint;
    [SerializeField] private Transform fireEjectionPoint;
    [SerializeField] private Transform[] shootPoints;

    public void SpawnSlashVFX(AttackVFXConfig config)
    {
        if (config == null || config.vfxPrefab == null) return;
        Transform point = vfxSpawnPoint != null ? vfxSpawnPoint : meleeOrigin;
        SpawnVFXLocal(config, point ?? transform);
    }

    public void SpawnShellEjectionVFX(AttackVFXConfig config)
    {
        if (config == null || config.vfxPrefab == null) return;
        Transform point = shellEjectionPoint;
        if (point == null && shootPoints != null && shootPoints.Length > 0)
            point = shootPoints[0];
        SpawnVFXLocal(config, point ?? transform);
    }

    public void SpawnFireEjectionVFX(AttackVFXConfig config)
    {
        if (config == null || config.vfxPrefab == null) return;
        Transform point = fireEjectionPoint;
        if (point == null && shootPoints != null && shootPoints.Length > 0)
            point = shootPoints[0];
        SpawnVFXLocal(config, point ?? transform);
    }

    public void SpawnVFXFromName(string prefabName, Vector3 position, Quaternion rotation,
                                  Vector3 localOffset, bool followTransform, float customDuration,
                                  PlayerCombat combat)
    {
        GameObject prefab = FindVFXPrefabByName(prefabName, combat);
        if (prefab == null)
        {
            Debug.LogWarning($"[PlayerVFXController] Prefab no encontrado: {prefabName}");
            return;
        }

        Vector3 spawnPos = position + rotation * localOffset;
        GameObject vfxInstance = Instantiate(prefab, spawnPos, rotation);

        if (followTransform)
        {
            Transform closest = FindClosestTransform(position);
            if (closest != null)
                vfxInstance.transform.SetParent(closest, true);
        }

        if (customDuration > 0f)
            Destroy(vfxInstance, customDuration);
    }

    private void SpawnVFXLocal(AttackVFXConfig config, Transform spawnTransform)
    {
        Vector3 spawnPos = spawnTransform.position
            + spawnTransform.TransformDirection(config.localOffset);

        GameObject vfxInstance = Instantiate(config.vfxPrefab, spawnPos, spawnTransform.rotation);

        if (config.followTransform)
            vfxInstance.transform.SetParent(spawnTransform, true);

        if (config.customDuration > 0f)
            Destroy(vfxInstance, config.customDuration);
    }

    private GameObject FindVFXPrefabByName(string name, PlayerCombat combat)
    {
        if (combat.meleeData?.ComboAttacks != null)
        {
            foreach (var combo in combat.meleeData.ComboAttacks)
            {
                if (combo.attackVFX?.vfxPrefab?.name == name)
                    return combo.attackVFX.vfxPrefab;
            }
        }

        if (combat.RangeData?.ShellEjectionVFX?.vfxPrefab?.name == name)
            return combat.RangeData.ShellEjectionVFX.vfxPrefab;

        if (combat.RangeData?.FireEjectionVFX?.vfxPrefab?.name == name)
            return combat.RangeData.FireEjectionVFX.vfxPrefab;

        return null;
    }

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

    public Transform GetSlashSpawnPoint() => vfxSpawnPoint != null ? vfxSpawnPoint : meleeOrigin;
    public Transform GetShellSpawnPoint() => shellEjectionPoint != null ? shellEjectionPoint
                                           : (shootPoints?.Length > 0 ? shootPoints[0] : null);
    public Transform GetFireSpawnPoint() => fireEjectionPoint != null ? fireEjectionPoint
                                           : (shootPoints?.Length > 0 ? shootPoints[0] : null);
}