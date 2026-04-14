using UnityEngine;

public class PickupVFXController : MonoBehaviour
{
    private GameObject _vfxInstance;
    private Transform _visualRoot;
    private ItemVFXConfig _config;

    private float _floatingTime;
    private Vector3 _initialPosition;
    private Light[] _lights;

    public void Initialize(Transform visualRoot, ItemVFXConfig config)
    {
        _visualRoot = visualRoot;
        _config = config;

        if (_config == null || _config.vfxPrefab == null)
        {
            Debug.LogWarning("[PickupVFXController] No VFX config or prefab assigned");
            return;
        }

        SpawnVFX();
        _initialPosition = transform.position;
    }

    private void SpawnVFX()
    {
        // Instanciar el VFX como hijo del item
        _vfxInstance = Instantiate(_config.vfxPrefab, transform);
        _vfxInstance.transform.localPosition = _config.vfxOffset;
        _vfxInstance.transform.localScale = _config.vfxScale;

        // Cachear luces si hay pulse habilitado
        if (_config.enablePulse)
        {
            _lights = _vfxInstance.GetComponentsInChildren<Light>();
        }

        // Iniciar partículas en loop
        ParticleSystem[] particles = _vfxInstance.GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in particles)
        {
            var main = ps.main;
            main.loop = true;
            if (!ps.isPlaying)
                ps.Play();
        }
    }

    private void Update()
    {
        if (_config == null || _vfxInstance == null) return;

        // Floating animation
        if (_config.enableFloating)
        {
            _floatingTime += Time.deltaTime * _config.floatingSpeed;
            float newY = _initialPosition.y + Mathf.Sin(_floatingTime) * _config.floatingHeight;
            transform.position = new Vector3(
                _initialPosition.x,
                newY,
                _initialPosition.z
            );
        }

        // Rotation animation
        if (_config.enableRotation)
        {
            _vfxInstance.transform.Rotate(_config.rotationSpeed * Time.deltaTime, Space.Self);
        }

        // Pulse effect on lights
        if (_config.enablePulse && _lights != null && _lights.Length > 0)
        {
            float pulse = Mathf.Lerp(
                _config.pulseMinIntensity,
                _config.pulseMaxIntensity,
                (Mathf.Sin(Time.time * _config.pulseSpeed) + 1f) / 2f
            );

            foreach (var light in _lights)
            {
                if (light != null)
                    light.intensity = pulse;
            }
        }
    }

    public void DestroyVFX()
    {
        if (_vfxInstance != null)
        {
            // Detener partículas
            ParticleSystem[] particles = _vfxInstance.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particles)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            Destroy(_vfxInstance);
        }

        Destroy(this);
    }
}
