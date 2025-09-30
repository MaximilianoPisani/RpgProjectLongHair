using UnityEngine;
using Fusion;

[RequireComponent(typeof(NetworkObject))]
public class EnemyHealth : NetworkBehaviour
{
    [SerializeField] private int _maxHealth = 100;
    [Networked] private int _currentHealth { get; set; }

    [SerializeField] private Renderer _meshRenderer;
    [SerializeField] private Color _flashColor = Color.red;
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

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ApplyDamage(int damage)
    {
        TakeDamage(damage);
    }

    private void TakeDamage(int damage)
    {
        if (!Object.HasStateAuthority) return;
        if (damage <= 0) return;

        _currentHealth -= damage;
        Debug.Log($"{Object.name} recibió {damage}. Vida restante: {_currentHealth}");

        RPC_Flash();

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{Object.name} murió.");
        if (Runner != null && Object.HasStateAuthority)
            Runner.Despawn(Object);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_Flash()
    {
        if (_meshRenderer == null) return;

        if (_flashCoroutine != null)
            StopCoroutine(_flashCoroutine);

        _flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private System.Collections.IEnumerator FlashRoutine()
    {
        _meshRenderer.material.color = _flashColor;
        yield return new WaitForSeconds(_flashDuration);
        _meshRenderer.material.color = _originalColor;
    }
}