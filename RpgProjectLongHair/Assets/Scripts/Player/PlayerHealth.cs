using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private int _maxHealth = 100;

    [Networked] private int _currentHealth { get; set; }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
            _currentHealth = _maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (!Object.HasStateAuthority) return;
        if (damage <= 0) return;

        _currentHealth -= damage;
        Debug.Log($"[Player] {_currentHealth}/{_maxHealth} HP after taking {damage} damage.");

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("[Player] Player has died!");

       // respawn 

        if (HasInputAuthority)
        {
            Debug.Log("[Player] Local player died - disable input here.");
        }
    }
}