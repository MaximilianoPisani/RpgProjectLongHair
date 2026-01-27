using UnityEngine;
using Fusion;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(NetworkObject))]
public class EnemyHealth : NetworkBehaviour
{
    [Header("Life")]
    [SerializeField] private int _maxHealth = 100;

    [Networked, HideInInspector] public int currentHealth { get; set; }

    public int MaxHealth => _maxHealth;

    [Header("Feedback")]
    [SerializeField] private Renderer _meshRenderer;
    [SerializeField] private Color _flashColor = Color.red;
    [SerializeField] private float _flashDuration = 0.1f;

    [Header("Reward")]
    [SerializeField] private ExpConfigSO _expConfig;

    public event Action<int, int> OnHealthChanged;
    public event Action OnDeath;

    private Color _originalColor;
    private Coroutine _flashCoroutine;

    private readonly HashSet<PlayerRef> _participants = new();

    private ChangeDetector _changeDetector;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            currentHealth = _maxHealth;
            _participants.Clear();
        }

        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (_meshRenderer != null)
            _originalColor = _meshRenderer.material.color;

        OnHealthChanged?.Invoke(currentHealth, _maxHealth);
    }

    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            if (change == nameof(currentHealth))
            {
                OnHealthChanged?.Invoke(currentHealth, _maxHealth);

                if (currentHealth <= 0)
                    OnDeath?.Invoke();
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    public void RPC_ApplyDamage(int damage, RpcInfo info = default)
    {
        TakeDamageServer(damage, info.Source);
    }

    public void ApplyDamageServer(int damage, PlayerRef attacker)
    {
        TakeDamageServer(damage, attacker);
    }

    private void TakeDamageServer(int damage, PlayerRef attacker)
    {
        if (!Object.HasStateAuthority) return;
        if (damage <= 0) return;
        if (currentHealth <= 0)
        {
            if (TryGetComponent<EnemyController>(out var controller))
                controller.ChangeState(new EnemyDeathState(controller));
            else
                Runner.Despawn(Object);
            return;
        }

        if (attacker != PlayerRef.None)
            _participants.Add(attacker);

        currentHealth = Mathf.Max(0, currentHealth - damage);

        Debug.Log($"{Object.name} received {damage}. Remaining life: {currentHealth}");

        RPC_Flash();

        if (currentHealth <= 0)
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

    public static bool TryApplyFromHitbox(Hitbox hb, int damage, PlayerRef attacker)
    {
        if (hb == null || hb.Root == null) return false;

        var health = hb.Root.GetComponentInChildren<EnemyHealth>();
        if (health == null) return false;
        if (!health.Object || !health.Object.HasStateAuthority) return false;

        health.ApplyDamageServer(damage, attacker);
        return true;
    }
}