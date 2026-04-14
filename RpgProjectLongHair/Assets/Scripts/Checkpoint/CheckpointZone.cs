using Fusion;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CheckpointZone : NetworkBehaviour
{
    [SerializeField] private Transform _spawnPoint;

    [Header("Feedback")]
    [SerializeField] private Animator _animator;
    [SerializeField] private ParticleSystem _vfxPrefab; // Ahora es prefab

    private bool _isActivated = false; // Previene activaciones múltiples

    private void Awake()
    {
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Solo el servidor procesa los triggers
        if (!Object.HasStateAuthority) return;

        // Evita reactivación si ya fue usado
        if (_isActivated) return;

        if (!other.TryGetComponent<PlayerCheckpoint>(out var checkpoint))
            return;

        Vector3 spawnPos = _spawnPoint != null
            ? _spawnPoint.position
            : transform.position;

        checkpoint.SetCheckpoint(spawnPos);

        _isActivated = true; // Marca como activado

        // El servidor llama al RPC para todos los clientes
        RPC_PlayFeedback();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayFeedback()
    {
        // Animación solo una vez (el trigger se resetea automáticamente)
        if (_animator != null)
            _animator.SetTrigger("Activate");

        // Instancia las partículas en vez de Play()
        if (_vfxPrefab != null)
        {
            Vector3 vfxPosition = _spawnPoint != null
                ? _spawnPoint.position
                : transform.position;

            ParticleSystem vfxInstance = Instantiate(_vfxPrefab, vfxPosition, Quaternion.identity);

            // Destruye automáticamente cuando termine
            Destroy(vfxInstance.gameObject, vfxInstance.main.duration + vfxInstance.main.startLifetime.constantMax);
        }
    }

    // Método público por si quieres resetear el checkpoint
    public void ResetCheckpoint()
    {
        _isActivated = false;
    }

    private void OnValidate()
    {
        if (_spawnPoint == null)
            Debug.LogWarning($"{name}: SpawnPoint not assigned");
    }
}