using UnityEngine;

/// <summary>
/// ScriptableObject que contiene la configuración de ataque del enemigo Kamikaze.
/// </summary>
[CreateAssetMenu(fileName = "NewKamikazeAttackData", menuName = "Enemy/Kamikaze Attack Data")]
public class KamikazeAttackData : ScriptableObject
{
    [Header("Damage Settings")]
    [Tooltip("Daño que inflige la explosión")]
    public int Damage = 50;

    [Header("Range Settings")]
    [Tooltip("Radio de explosión en metros")]
    public float HitRadius = 3f;

    [Tooltip("Distancia a la que el kamikaze decide explotar")]
    public float ExplodeDistance = 2f;

    /// <summary>
    /// Alias para ExplodeDistance, para compatibilidad con código existente.
    /// </summary>
    public float AttackRange => ExplodeDistance;

    [Header("Visual Effects")]
    [Tooltip("Prefab del efecto visual de explosión")]
    public GameObject ExplosionVFXPrefab;

    [Header("Animation Timing")]
    [Tooltip("Tiempo de delay antes de la explosión (para sincronizar con animación)")]
    public float ExplosionDelay = 0f;

    [Header("Debug")]
    [Tooltip("Mostrar el radio de explosión en la escena")]
    public bool ShowExplosionRadius = true;

    private void OnValidate()
    {
        // Validar valores
        if (Damage < 0) Damage = 0;
        if (HitRadius < 0.5f) HitRadius = 0.5f;
        if (ExplodeDistance < 0.5f) ExplodeDistance = 0.5f;
        if (ExplosionDelay < 0f) ExplosionDelay = 0f;

        // Asegurar que ExplodeDistance sea menor que HitRadius
        if (ExplodeDistance > HitRadius)
        {
            Debug.LogWarning($"[KamikazeAttackData] ExplodeDistance ({ExplodeDistance}) es mayor que HitRadius ({HitRadius}). Ajustando...");
            ExplodeDistance = HitRadius * 0.8f;
        }
    }

    /// <summary>
    /// Dibuja el radio de explosión en la vista de Scene para debug.
    /// </summary>
    public void DrawExplosionRadius(Vector3 position)
    {
        if (!ShowExplosionRadius) return;

#if UNITY_EDITOR
        // Radio de explosión (rojo)
        UnityEditor.Handles.color = new Color(1f, 0f, 0f, 0.3f);
        UnityEditor.Handles.DrawSolidDisc(position, Vector3.up, HitRadius);

        // Distancia de activación (amarillo)
        UnityEditor.Handles.color = new Color(1f, 1f, 0f, 0.5f);
        UnityEditor.Handles.DrawWireDisc(position, Vector3.up, ExplodeDistance);
#endif
    }
}