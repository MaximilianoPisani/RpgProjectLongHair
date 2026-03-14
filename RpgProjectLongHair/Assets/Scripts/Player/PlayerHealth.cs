using System.Collections;
using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(PlayerCheckpoint))]
[RequireComponent(typeof(NetworkCharacterController))]
public class PlayerHealth : NetworkBehaviour
{
    [Header("Health")]
    [SerializeField] private int _maxHealth = 100;

    [Networked] private int CurrentHealth { get; set; }

    [Header("Respawn")]
    [SerializeField] private float _respawnDelay = 2f;

    [Header("Flash Effect")]
    [SerializeField] private Renderer _meshRenderer;
    [SerializeField] private Color _flashColor = Color.red;
    [SerializeField] private int _flashCount = 3;
    [SerializeField] private float _flashDuration = 0.1f;

    private Color _originalColor;
    private Coroutine _flashCoroutine;
    private PlayerCheckpoint _checkpoint;
    private NetworkCharacterController _networkCC;

    public override void Spawned()
    {
        _checkpoint = GetComponent<PlayerCheckpoint>();
        _networkCC = GetComponent<NetworkCharacterController>();

        if (HasStateAuthority)
            CurrentHealth = _maxHealth;

        if (_meshRenderer != null)
            _originalColor = _meshRenderer.material.color;
    }

    public void TakeDamage(int damage, Vector3 attackerPosition)
    {
        if (!HasStateAuthority) return;
        if (damage <= 0) return;
        if (CurrentHealth <= 0) return;

        CurrentHealth -= damage;
        Debug.Log($"[Player] {CurrentHealth}/{_maxHealth} HP");

        if (CurrentHealth > 0)
            RPC_Flash();
        else
            Die();
    }

    private void Die()
    {
        if (!HasStateAuthority) return;

        Debug.Log("[Player] Player died");

        CurrentHealth = 0;

        if (_flashCoroutine != null)
            StopCoroutine(_flashCoroutine);

        if (_meshRenderer != null)
            _meshRenderer.material.color = _originalColor;

        Vector3 respawnPosition = _checkpoint != null
            ? _checkpoint.LastCheckpoint
            : transform.position;

        StartCoroutine(RespawnRoutine(respawnPosition));
    }

    private IEnumerator RespawnRoutine(Vector3 respawnPosition)
    {
        yield return new WaitForSeconds(_respawnDelay);

        _networkCC.Teleport(respawnPosition);

        CurrentHealth = _maxHealth;

        Debug.Log("[Player] Revived at checkpoint with inventory intact");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_Flash()
    {
        if (_meshRenderer == null)
            return;

        if (_flashCoroutine != null)
            StopCoroutine(_flashCoroutine);

        _flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        for (int i = 0; i < _flashCount; i++)
        {
            _meshRenderer.material.color = _flashColor;
            yield return new WaitForSeconds(_flashDuration);

            _meshRenderer.material.color = _originalColor;
            yield return new WaitForSeconds(_flashDuration);
        }
    }
}