using System.Collections;
using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private int _maxHealth = 100;
    [Networked] private int _currentHealth { get; set; }

    [Header("Flash Effect")]
    [SerializeField] private Renderer _meshRenderer;
    [SerializeField] private Color _flashColor = Color.red;
    [SerializeField] private int _flashCount = 3;
    [SerializeField] private float _flashDuration = 0.1f;

    private Color _originalColor;
    private Coroutine _flashCoroutine;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
            _currentHealth = _maxHealth;

        if (_meshRenderer != null)
            _originalColor = _meshRenderer.material.color;
    }

    public void TakeDamage(int damage)
    {
        if (!Object.HasStateAuthority) return;
        if (damage <= 0) return;

        _currentHealth -= damage;
        Debug.Log($"[Player] {_currentHealth}/{_maxHealth} HP after taking {damage} damage.");

        if (_currentHealth > 0)
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
        Debug.Log("[Player] Player has died!");

        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
            _flashCoroutine = null;
        }

        if (_meshRenderer != null)
            _meshRenderer.material.color = _originalColor;

        if (Object.HasStateAuthority)
        {
            Runner.Despawn(Object);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_Flash()
    {
        if (_meshRenderer == null) return;

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
