using Fusion;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Componente genérico reutilizable.
/// Recibe daño y dispara OnActivated cuando llega a 0.
/// Poner en cualquier objeto con layer Damageable + NetworkObject.
/// </summary>
public class DamageableObject : NetworkBehaviour
{
    [Header("Vida")]
    [SerializeField] private float maxHealth = 100f;

    [Header("Evento — conectar desde el inspector")]
    public UnityEvent OnActivated;

    [Networked] private float CurrentHealth { get; set; }
    [Networked] private NetworkBool IsActivated { get; set; }

    private ChangeDetector _changeDetector;
    private bool _activationExecuted = false;

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (Object.HasStateAuthority)
        {
            CurrentHealth = maxHealth;
            IsActivated = false;
        }
    }

    // Corre en todos los clientes — dispara el evento local cuando IsActivated cambia
    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            if (change == nameof(IsActivated) && IsActivated && !_activationExecuted)
            {
                _activationExecuted = true;
                OnActivated?.Invoke();

                AudioManager.Instance.PlayStone();
            }
        }
    }

    // Llamar desde Projectile, RPC_RequestMeleeDamage y ApplyMeleeDamage
    public void ApplyDamageServer(float amount, PlayerRef attacker)
    {
        if (!Object.HasStateAuthority || IsActivated) return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);

        if (CurrentHealth <= 0f)
            IsActivated = true;
    }

    public float HealthPercent => CurrentHealth / maxHealth;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.15f);
    }
}