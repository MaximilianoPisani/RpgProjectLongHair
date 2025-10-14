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
        RPC_RequestMeleeAttack();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestMeleeAttack(RpcInfo info = default)
    {
        Collider[] hits = Physics.OverlapSphere(
            _attackOrigin.position,
            _attackData.HitRadius,   
            _enemyLayer
        );

        foreach (var hit in hits)
        {
            var enemyHealth = hit.GetComponentInParent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.ApplyDamageServer(_attackData.Damage, info.Source);
            }
        }
    }

}