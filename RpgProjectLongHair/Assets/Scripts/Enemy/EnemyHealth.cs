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

    private PlayerRef _lastAttacker;

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
        Debug.Log($"[EnemyHealth] Damage {damage} from {attacker}, authority={Object.HasStateAuthority}");

        if (!Object.HasStateAuthority) return;

        if (damage <= 0) return;

        if (currentHealth <= 0) return;

        if (attacker != PlayerRef.None)
        {
          _participants.Add(attacker);
          _lastAttacker = attacker; 
        }

        currentHealth = Mathf.Max(0, currentHealth - damage);

        RPC_Flash();

        if (currentHealth <= 0)
        {
          GiveKillExp();  
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
    private void GiveKillExp()
    {
        Debug.Log($"[EnemyHealth] GiveKillExp called. LastAttacker={_lastAttacker}");

        if (_lastAttacker == PlayerRef.None)
        {
            Debug.Log("[EnemyHealth] LastAttacker is NONE");
            return;
        }

        if (!Runner.TryGetPlayerObject(_lastAttacker, out NetworkObject playerObj))
        {
            Debug.Log("[EnemyHealth] PlayerObject NOT FOUND");
            return;
        }

        Debug.Log($"[EnemyHealth] PlayerObject found: {playerObj.name}");
        
        var playerExp = playerObj.GetComponent<PlayerExp>();
        if (playerExp != null)
        {
            int exp = _expConfig.GetExp(ExpEvent.Kill);
                playerExp.AddExperience(exp);
        }

        Debug.Log($"[EnemyHealth] TrackEvents suscriptores: {TrackEvents.OnTrackEvent?.GetInvocationList().Length ?? 0}");
        TrackEvents.OnTrackEvent?.Invoke("Kill_Enemy", 1); // Disparar evento de tracking para misiones
    }
}