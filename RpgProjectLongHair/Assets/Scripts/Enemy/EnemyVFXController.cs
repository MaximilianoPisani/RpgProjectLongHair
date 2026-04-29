using UnityEngine;

/// <summary>
/// Maneja todos los VFX de enemigos: ataques, propulsores, impactos
/// </summary>
public class EnemyVFXController : MonoBehaviour
{
    [Header("Spawn Points")]
    [SerializeField] private Transform attackVfxPoint;
    [SerializeField] private Transform muzzlePoint; // Para ranged
    [SerializeField] private Transform thrusterPoint; // Para propulsores

    [Header("Thruster VFX")]
    [Tooltip("Prefab de partículas de propulsores")]
    [SerializeField] private ParticleSystem thrusterVfxPrefab;

    [Tooltip("Velocidad mínima para activar propulsores")]
    [SerializeField] private float thrusterActivationSpeed = 0.5f;

    [Header("Hit VFX")]
    [Tooltip("VFX cuando recibe daño")]
    [SerializeField] private AttackVFXConfig hitReactionVFX;

    [Tooltip("Transform del modelo que se mueve con animaciones (ej: mesh root). Si está vacío usa el transform principal")]
    [SerializeField] private Transform animatedModelTransform;

    [Header("Attack Indicator VFX")]
    [Tooltip("VFX de advertencia que se muestra antes del ataque")]
    [SerializeField] private AttackVFXConfig attackIndicatorVFX;

    [Tooltip("Punto de spawn del indicador (si está vacío usa attackVfxPoint)")]
    [SerializeField] private Transform attackIndicatorPoint;

    // Runtime - Propulsores
    private ParticleSystem activeThrusterVfx;
    private bool thrustersActive = false;

    /// <summary>
    /// Actualiza los propulsores basado en velocidad
    /// </summary>
    public void UpdateThrusters(float currentSpeed)
    {
        bool shouldBeActive = currentSpeed >= thrusterActivationSpeed;

        if (shouldBeActive && !thrustersActive)
        {
            ActivateThrusters();
        }
        else if (!shouldBeActive && thrustersActive)
        {
            DeactivateThrusters();
        }
    }

    private void ActivateThrusters()
    {
        if (thrusterVfxPrefab == null) return;

        Vector3 spawnPos = thrusterPoint != null
            ? thrusterPoint.position
            : transform.position;

        Quaternion spawnRot = thrusterPoint != null
            ? thrusterPoint.rotation
            : transform.rotation;

        activeThrusterVfx = Instantiate(thrusterVfxPrefab, spawnPos, spawnRot);

        // Parentear al punto de spawn
        if (thrusterPoint != null)
            activeThrusterVfx.transform.SetParent(thrusterPoint);
        else
            activeThrusterVfx.transform.SetParent(transform);

        activeThrusterVfx.Play();
        thrustersActive = true;
    }

    private void DeactivateThrusters()
    {
        if (activeThrusterVfx != null)
        {
            activeThrusterVfx.Stop();
            Destroy(activeThrusterVfx.gameObject, activeThrusterVfx.main.duration);
            activeThrusterVfx = null;
        }
        thrustersActive = false;
    }

    /// <summary>
    /// Spawnea un VFX desde una configuración
    /// </summary>
    public void SpawnVFX(AttackVFXConfig config, Transform spawnPoint = null)
    {
        if (config == null || config.vfxPrefab == null) return;

        Transform point = spawnPoint ?? attackVfxPoint ?? transform;
        Vector3 spawnPos = point.position + point.TransformDirection(config.localOffset);
        Quaternion spawnRot = point.rotation;

        GameObject vfxInstance = Instantiate(config.vfxPrefab, spawnPos, spawnRot);

        if (config.followTransform)
        {
            vfxInstance.transform.SetParent(point);
        }

        float duration = config.customDuration > 0
            ? config.customDuration
            : GetParticleDuration(vfxInstance);

        Destroy(vfxInstance, duration);
    }

    /// <summary>
    /// Spawnea VFX con delay (para sincronizar con animaciones)
    /// </summary>
    public void SpawnVFXDelayed(AttackVFXConfig config, float delay, Transform spawnPoint = null)
    {
        if (delay <= 0f)
        {
            SpawnVFX(config, spawnPoint);
            return;
        }

        StartCoroutine(SpawnVFXCoroutine(config, delay, spawnPoint));
    }

    private System.Collections.IEnumerator SpawnVFXCoroutine(AttackVFXConfig config, float delay, Transform spawnPoint)
    {
        yield return new WaitForSeconds(delay);
        SpawnVFX(config, spawnPoint);
    }

    /// <summary>
    /// Spawnea VFX de impacto cuando recibe daño
    /// </summary>
    public void SpawnHitVFX(Vector3 hitPosition, Vector3 hitNormal)
    {
        if (hitReactionVFX == null || hitReactionVFX.vfxPrefab == null) return;

        // Usar el transform del modelo animado si está asignado, sino usar el transform principal
        Transform targetTransform = animatedModelTransform != null ? animatedModelTransform : transform;

        // Usar la posición del modelo (que se mueve con animaciones)
        Vector3 spawnPos = targetTransform.position;

        // Aplicar offset local si está configurado
        spawnPos += targetTransform.TransformDirection(hitReactionVFX.localOffset);

        // Rotación: el VFX mira HACIA AFUERA (en dirección del hit normal)
        Quaternion rotation = hitNormal != Vector3.zero
           ? Quaternion.LookRotation(hitNormal)
           : Quaternion.identity;

        GameObject vfxInstance = Instantiate(hitReactionVFX.vfxPrefab, spawnPos, rotation);

        // Si followTransform está activo, parentear al modelo animado
        if (hitReactionVFX.followTransform)
        {
            vfxInstance.transform.SetParent(targetTransform);
        }

        float duration = hitReactionVFX.customDuration > 0
            ? hitReactionVFX.customDuration
            : GetParticleDuration(vfxInstance);

        Destroy(vfxInstance, duration);
    }

    /// <summary>
    /// Spawnea VFX en el punto de muzzle (para enemigos ranged)
    /// </summary>
    public void SpawnMuzzleFlash(AttackVFXConfig config)
    {
        SpawnVFX(config, muzzlePoint);
    }

    /// <summary>
    /// Obtiene la duración de un particle system
    /// </summary>
    private float GetParticleDuration(GameObject vfxObject)
    {
        ParticleSystem ps = vfxObject.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            return ps.main.duration + ps.main.startLifetime.constantMax;
        }
        return 2f; // Fallback
    }

    /// <summary>
    /// Fuerza la desactivación de propulsores (útil al morir)
    /// </summary>
    public void ForceStopThrusters()
    {
        if (thrustersActive)
        {
            DeactivateThrusters();
        }
    }

    private void OnDestroy()
    {
        // Limpiar VFX al destruir el enemigo
        if (activeThrusterVfx != null)
            Destroy(activeThrusterVfx.gameObject);
    }

    private void OnValidate()
    {
        if (attackVfxPoint == null)
            Debug.LogWarning($"{name}: Attack VFX Point no asignado");

        if (thrusterPoint == null)
            Debug.LogWarning($"{name}: Thruster Point no asignado");

        if (animatedModelTransform == null)
            Debug.LogWarning($"{name}: Animated Model Transform no asignado - se usará el transform principal");
    }
    public void SpawnAttackIndicator(float delay = 0f, Transform spawnPoint = null)
    {
        Transform point = attackIndicatorPoint ?? attackVfxPoint;

        if (delay > 0f)
            SpawnVFXDelayed(attackIndicatorVFX, delay, point);
        else
            SpawnVFX(attackIndicatorVFX, point);
    }

}