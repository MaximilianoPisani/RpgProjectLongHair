using System.Collections;
using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class PlayerHealth : NetworkBehaviour
{
    [Header("Health")]
    [SerializeField] private int _maxHealth = 100;

    [Networked] private int CurrentHealth { get; set; }

    [Header("Flash Effect")]
    [SerializeField] private Renderer _meshRenderer;
    [SerializeField] private Color _flashColor = Color.red;
    [SerializeField] private int _flashCount = 3;
    [SerializeField] private float _flashDuration = 0.1f;

    private Color _originalColor;
    private Coroutine _flashCoroutine;

    public bool IsDead => CurrentHealth <= 0;

    public override void Spawned()
    {
        if (HasStateAuthority)
            CurrentHealth = _maxHealth;

        if (_meshRenderer != null)
            _originalColor = _meshRenderer.material.color;
    }

    public void TakeDamage(int damage, Vector3 hitPoint)
    {
        if (!HasStateAuthority) return;
        if (damage <= 0) return;
        if (IsDead) return;

        CurrentHealth -= damage;

        Debug.Log($"[Player] {CurrentHealth}/{_maxHealth} HP");


        if (!IsDead)
        {
            RPC_Flash();
        }
        else
        {
            OnDeath();
        }
    }

    private void OnDeath()
    {
        Debug.Log("[Player] Dead");
        var sm = GetComponent<PlayerStateMachine>();
        if (sm != null)
            sm.ChangeState(new PlayerDeadState(sm));

        RPC_OnDeath();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnDeath()
    {
        var sm = GetComponent<PlayerStateMachine>();
        if (sm?.Animator != null)
            sm.Animator.SetTrigger("Die");
    }

    public void ResetHealth()
    {
        if (!HasStateAuthority) return;

        CurrentHealth = _maxHealth;
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