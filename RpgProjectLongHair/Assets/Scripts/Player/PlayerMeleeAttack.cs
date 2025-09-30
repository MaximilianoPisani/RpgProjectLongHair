using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMeleeAttack : NetworkBehaviour
{
    [SerializeField] private MeleeAttackData _attackData;
    [SerializeField] private Transform _attackOrigin;
    [SerializeField] private LayerMask _enemyLayer;

    private float _nextAttackTime;
    private PlayerInput _playerInput;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        if (_playerInput != null)
            _playerInput.actions["Attack"].performed += OnAttack;
    }

    private void OnDisable()
    {
        if (_playerInput != null)
            _playerInput.actions["Attack"].performed -= OnAttack;
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!HasInputAuthority) return;
        TryAttack();
    }

    private void TryAttack()
    {
        if (Time.time < _nextAttackTime) return;

        _nextAttackTime = Time.time + _attackData.Cooldown;
        RPC_PerformAttack();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_PerformAttack()
    {
        Collider[] hits = Physics.OverlapSphere(_attackOrigin.position, _attackData.AttackRange, _enemyLayer);

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<EnemyHealth>(out var enemyHealth))
            {
                enemyHealth.RPC_ApplyDamage(_attackData.Damage);
                Debug.Log($"[Server] Enemy {hit.name} received {_attackData.Damage} of damage.");
            }
        }
    }

}