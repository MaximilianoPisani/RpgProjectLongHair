using System.Collections;
using Fusion;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(NetworkObject))]
public class PlayerHealth : NetworkBehaviour
{
    [Header("Health")]
    [SerializeField] private int _maxHealth = 100;
    [Networked] private int NetworkedCurrentHealth { get; set; }

    [Header("Flash Effect")]
    [SerializeField] private Color _flashColor = Color.red;
    [SerializeField] private int _flashCount = 3;
    [SerializeField] private float _flashDuration = 0.1f;

    private SkinnedMeshRenderer[] _skinnedRenderers;
    private Color[] _originalColors;
    private Coroutine _flashCoroutine;

    // NUEVO: Para detectar cambios
    private ChangeDetector _changeDetector;
    private int _previousHealth;

    // Eventos para notificar cambios de salud
    public UnityEvent<int, int> OnHealthChanged;
    public UnityEvent OnPlayerDied;

    // Propiedades públicas
    public bool IsDead => NetworkedCurrentHealth <= 0;
    public int CurrentHealth => NetworkedCurrentHealth;
    public int MaxHealth => _maxHealth;
    public float HealthPercentage => _maxHealth > 0 ? (float)NetworkedCurrentHealth / _maxHealth : 0f;

    public override void Spawned()
    {
        if (HasStateAuthority)
            NetworkedCurrentHealth = _maxHealth;

        _skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        _originalColors = new Color[_skinnedRenderers.Length];

        for (int i = 0; i < _skinnedRenderers.Length; i++)
            _originalColors[i] = _skinnedRenderers[i].material.color;

        Debug.Log($"[PlayerHealth] Encontró {_skinnedRenderers.Length} SkinnedMeshRenderers");

        // Inicializar ChangeDetector
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        _previousHealth = NetworkedCurrentHealth;

        // Notificar estado inicial
        OnHealthChanged?.Invoke(NetworkedCurrentHealth, _maxHealth);
    }

    // NUEVO: Detectar cambios en cada tick de red
    public override void Render()
    {
        // Detectar cambios en la salud
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(NetworkedCurrentHealth):
                    OnHealthChangedDetected();
                    break;
            }
        }
    }

    private void OnHealthChangedDetected()
    {
        int currentHealth = NetworkedCurrentHealth;

        Debug.Log($"[PlayerHealth] Health changed: {currentHealth}/{_maxHealth} (IsServer: {HasStateAuthority})");

        // Invocar evento
        OnHealthChanged?.Invoke(currentHealth, _maxHealth);

        // Si murió (pasó de >0 a 0)
        if (currentHealth <= 0 && _previousHealth > 0)
        {
            HandleDeathOnClient();
        }

        _previousHealth = currentHealth;
    }

    private void HandleDeathOnClient()
    {
        Debug.Log($"[PlayerHealth] HandleDeathOnClient (IsServer: {HasStateAuthority})");

        OnPlayerDied?.Invoke();
        GetComponent<PlayerNetworkSync>()?.ResetAllAnimations();

        var sm = GetComponent<PlayerStateMachine>();
        if (sm != null)
            sm.ChangeState(new PlayerDeadState(sm));

        if (HasStateAuthority)
        {
            var questController = GetComponent<QuestController>();
            if (questController?.CurrentQuest != null)
                questController.HandlePlayerDeath();
        }
    }

    public void TakeDamage(int damage, Vector3 hitPoint)
    {
        if (!HasStateAuthority) return;
        if (damage <= 0) return;
        if (IsDead) return;

        NetworkedCurrentHealth -= damage;
        NetworkedCurrentHealth = Mathf.Max(0, NetworkedCurrentHealth);

        Debug.Log($"[Player] {NetworkedCurrentHealth}/{_maxHealth} HP");

        // Notificar al sistema de regeneración
        var regen = GetComponent<PlayerHealthRegeneration>();
        if (regen != null)
            regen.OnDamageTaken();

        // Flash solo si no murió
        if (!IsDead)
            RPC_Flash();
    }

    public void Heal(int amount)
    {
        if (!HasStateAuthority) return;
        if (amount <= 0) return;
        if (IsDead) return;

        int previousHealth = NetworkedCurrentHealth;
        NetworkedCurrentHealth += amount;
        NetworkedCurrentHealth = Mathf.Min(NetworkedCurrentHealth, _maxHealth);

        if (NetworkedCurrentHealth != previousHealth)
        {
            Debug.Log($"[Player] Healed: {NetworkedCurrentHealth}/{_maxHealth} HP");
        }
    }

    public void ResetHealth()
    {
        if (!HasStateAuthority) return;

        NetworkedCurrentHealth = _maxHealth;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_Flash()
    {
        if (_skinnedRenderers == null || _skinnedRenderers.Length == 0) return;

        if (_flashCoroutine != null)
            StopCoroutine(_flashCoroutine);

        _flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        for (int i = 0; i < _flashCount; i++)
        {
            foreach (var r in _skinnedRenderers)
                r.material.color = _flashColor;

            yield return new WaitForSeconds(_flashDuration);

            for (int j = 0; j < _skinnedRenderers.Length; j++)
                _skinnedRenderers[j].material.color = _originalColors[j];

            yield return new WaitForSeconds(_flashDuration);
        }
    }

    public void SetMaxHealth(int newMaxHealth)
    {
        if (!HasStateAuthority) return;

        _maxHealth = Mathf.Max(1, newMaxHealth);
        NetworkedCurrentHealth = Mathf.Min(NetworkedCurrentHealth, _maxHealth);

        OnHealthChanged?.Invoke(NetworkedCurrentHealth, _maxHealth);
    }
}