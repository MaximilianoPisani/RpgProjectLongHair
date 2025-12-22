using System.Collections;
using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(PlayerCheckpoint))]
public class PlayerHealth : NetworkBehaviour
{
    [Header("Health")]
    [SerializeField] private int _maxHealth = 100;

    [Networked] private int CurrentHealth { get; set; }

    [Header("Respawn")]
    [SerializeField] private float _respawnDelay = 2f;
    [SerializeField] private NetworkPrefabRef _playerPrefab;

    [Header("Flash Effect")]
    [SerializeField] private Renderer _meshRenderer;
    [SerializeField] private Color _flashColor = Color.red;
    [SerializeField] private int _flashCount = 3;
    [SerializeField] private float _flashDuration = 0.1f;

    private Color _originalColor;
    private Coroutine _flashCoroutine;
    private PlayerCheckpoint _checkpoint;

    public override void Spawned()
    {
        _checkpoint = GetComponent<PlayerCheckpoint>();

        if (HasStateAuthority)
            CurrentHealth = _maxHealth;

        if (_meshRenderer != null)
            _originalColor = _meshRenderer.material.color;
    }
    public void TakeDamage(int damage)
    {
        if (!HasStateAuthority) return;
        if (damage <= 0) return;
        if (CurrentHealth <= 0) return;

        CurrentHealth -= damage;
        Debug.Log($"[Player] {CurrentHealth}/{_maxHealth} HP");

        if (CurrentHealth > 0)
        {
            RPC_Flash();
        }
        else
        {
            Die();
        }
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

        PlayerRef playerRef = Object.InputAuthority;

        StartCoroutine(RespawnRoutine(respawnPosition, playerRef));
    }

    private IEnumerator RespawnRoutine(Vector3 respawnPosition, PlayerRef playerRef)
    {
        yield return new WaitForSeconds(_respawnDelay);

        Runner.Spawn(
            _playerPrefab,
            respawnPosition,
            Quaternion.identity,
            playerRef,
            (runner, obj) =>
            {
                var health = obj.GetComponent<PlayerHealth>();
                if (health != null)
                    health.CurrentHealth = health._maxHealth;
            });

        Runner.Despawn(Object);
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