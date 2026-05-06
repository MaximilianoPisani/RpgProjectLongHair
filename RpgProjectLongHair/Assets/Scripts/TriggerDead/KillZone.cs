using System.Collections.Generic;
using UnityEngine;
using Fusion;

[RequireComponent(typeof(Collider))]
public class KillZone : NetworkBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private int _instantKillDamage = 99999;

    private HashSet<PlayerHealth> _recentlyKilled = new HashSet<PlayerHealth>();

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health == null) return;

        if (!health.HasStateAuthority) return;

        if (health.IsDead) return;

        if (_recentlyKilled.Contains(health)) return;

        Debug.Log("[KillZone] Player entered kill zone");

        _recentlyKilled.Add(health);

        health.TakeDamage(_instantKillDamage, other.transform.position);

        StartCoroutine(RemoveFromListAfterDelay(health, 2.5f));
    }

    private System.Collections.IEnumerator RemoveFromListAfterDelay(PlayerHealth health, float delay)
    {
        yield return new WaitForSeconds(delay);

        _recentlyKilled.Remove(health);
    }
}