using Fusion;
using UnityEngine;
[RequireComponent(typeof(NetworkObject))]
public class EnemyMeleeAttack : NetworkBehaviour 
{ 
    [SerializeField] private MeleeAttackData _attackData; 
    [SerializeField] private LayerMask _playerLayer; 
    [SerializeField] private Transform _attackOrigin; 
    private float _nextAttackTime; 
    private void TryAttack() 
    { 
        if (Time.time < _nextAttackTime) return; 

        Collider[] hits = Physics.OverlapSphere(_attackOrigin.position, _attackData.AttackRange, _playerLayer); 
        foreach (var hit in hits) 
        { 
            if (hit.CompareTag("Player") && hit.TryGetComponent<PlayerHealth>(out var playerHealth)) 
            { 
                playerHealth.TakeDamage(_attackData.Damage); 
                Debug.Log($"[Enemy] Hit player for {_attackData.Damage} damage."); 
                _nextAttackTime = Time.time + _attackData.Cooldown;
            } 
        } 
    } 
    private void Update() 
    { 
        if (!Object.HasStateAuthority) return; TryAttack(); 
    } 
}