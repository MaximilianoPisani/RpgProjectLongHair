using UnityEngine;

public class AutoDestroyVFX : MonoBehaviour
{
    [SerializeField] private float lifetime = 2f;
    [SerializeField] private bool useParticleSystemDuration = true;

    private void Start()
    {
        if (useParticleSystemDuration)
        {
            // Obtener la duración del sistema de partículas
            ParticleSystem ps = GetComponent<ParticleSystem>();
            if (ps != null)
            {
                lifetime = ps.main.duration + ps.main.startLifetime.constantMax;
            }
        }

        Destroy(gameObject, lifetime);
    }
}