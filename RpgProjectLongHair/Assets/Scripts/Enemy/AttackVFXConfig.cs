using UnityEngine;

/// <summary>
/// Configuración de VFX para un ataque. Única fuente de verdad para prefab, timing y offset.
/// Usada tanto en MeleeAttackData como en RangedAttackData.
/// </summary>
[System.Serializable]
public class AttackVFXConfig
{
    [Header("VFX Settings")]
    [Tooltip("Prefab del efecto visual")]
    public GameObject vfxPrefab;

    [Tooltip("Tiempo desde el inicio de la animación para spawnear el VFX")]
    public float vfxSpawnTime = 0.25f;

    [Tooltip("Offset local desde el punto de spawn")]
    public Vector3 localOffset = Vector3.zero;

    [Tooltip("Si debe seguir al transform o quedarse en el mundo")]
    public bool followTransform = false;

    [Tooltip("Duración antes de auto-destruirse (0 = usar duración del particle system)")]
    public float customDuration = 0f;
}