using Fusion;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CheckpointZone : NetworkBehaviour
{
    [SerializeField] private Transform _spawnPoint;

    [Header("Feedback")]
    [SerializeField] private Animator _animator;
    [SerializeField] private ParticleSystem _vfxPrefab;

    private void Awake()
    {
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent<PlayerCheckpoint>(out var checkpoint))
            return;

        if (!checkpoint.HasInputAuthority) return;

        Vector3 spawnPos = _spawnPoint != null
            ? _spawnPoint.position
            : transform.position;

        checkpoint.SetCheckpoint(spawnPos);

        PlayLocalFeedback();
    }

    private void PlayLocalFeedback()
    {
        if (_animator != null)
            _animator.SetTrigger("Activate");

        if (_vfxPrefab != null)
        {
            Vector3 vfxPosition = _spawnPoint != null
                ? _spawnPoint.position
                : transform.position;

            var vfx = Instantiate(_vfxPrefab, vfxPosition, Quaternion.identity);
            Destroy(vfx.gameObject, vfx.main.duration + vfx.main.startLifetime.constantMax);
        }
    }
}