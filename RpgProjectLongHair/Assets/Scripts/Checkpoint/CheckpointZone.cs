using Fusion;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CheckpointZone : NetworkBehaviour
{
    [SerializeField] private Transform _spawnPoint;

    [Header("Feedback")]
    [SerializeField] private Animator _animator;
    [SerializeField] private ParticleSystem _vfxPrefab;

    // NUEVO: Track de players que ya activaron este checkpoint
    private HashSet<PlayerRef> _activatedPlayers = new HashSet<PlayerRef>();

    private void Awake()
    {
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent<PlayerCheckpoint>(out var checkpoint)) return;
        if (!checkpoint.HasInputAuthority) return;

        // NUEVO: Si ya activó, no hacer nada
        if (_activatedPlayers.Contains(checkpoint.Object.InputAuthority)) return;

        // NUEVO: Marcar como activado
        _activatedPlayers.Add(checkpoint.Object.InputAuthority);

        Vector3 spawnPos = _spawnPoint != null
            ? _spawnPoint.position
            : transform.position;

        checkpoint.SetCheckpoint(spawnPos); 
        checkpoint.PersistCheckpoint(spawnPos); 
        PlayLocalFeedback();
    }

    private void PlayLocalFeedback()
    {
        AudioManager.Instance.PlayCheckPoint();

        if (_animator != null)
            _animator.SetTrigger("Activate");

        if (_vfxPrefab != null)
        {
            Vector3 vfxPosition = _spawnPoint != null
                ? _spawnPoint.position
                : transform.position;

            var vfx = Instantiate(_vfxPrefab, vfxPosition, Quaternion.identity);
            Destroy(vfx.gameObject,
                vfx.main.duration + vfx.main.startLifetime.constantMax);
        }
    }
}