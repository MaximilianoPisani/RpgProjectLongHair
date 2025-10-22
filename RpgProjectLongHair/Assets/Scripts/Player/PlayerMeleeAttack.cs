using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMeleeAttack : NetworkBehaviour
{
    [Header("Datos del ataque")]
    [SerializeField] private MeleeAttackData _attackData;

    [Header("Posición y alcance")]
    [SerializeField] private Transform _attackOrigin;
    [SerializeField] private LayerMask _enemyLayer;

    private PlayerInput _playerInput;

    [Networked] private TickTimer _cooldownTimer { get; set; }

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

    private void OnAttack(InputAction.CallbackContext ctx)
    {
        if (!HasInputAuthority) return;

        if (!_cooldownTimer.ExpiredOrNotRunning(Runner))
            return;

        RPC_RequestMeleeAttack(transform.position, transform.forward);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    private void RPC_RequestMeleeAttack(Vector3 playerPos, Vector3 direction, RpcInfo info = default)
    {
        if (!Runner.IsServer) return;
        if (!_cooldownTimer.ExpiredOrNotRunning(Runner)) return;

        _cooldownTimer = TickTimer.CreateFromSeconds(Runner, _attackData.Cooldown);

        Vector3 origin = _attackOrigin != null ? _attackOrigin.position : playerPos + Vector3.up * 1f;

        Collider[] hits = Physics.OverlapSphere(
            origin,
            _attackData.HitRadius,
            _enemyLayer,
            QueryTriggerInteraction.Collide
        );

        foreach (var hit in hits)
        {
            var enemyHealth = hit.GetComponentInParent<EnemyHealth>();
            if (enemyHealth != null && enemyHealth.Object != null && enemyHealth.Object.HasStateAuthority)
            {
                enemyHealth.ApplyDamageServer(_attackData.Damage, info.Source);
            }
        }

        RPC_PlayMeleeFX(origin, direction);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Unreliable)]
    private void RPC_PlayMeleeFX(Vector3 origin, Vector3 direction)
    {
        // Aquí se puede agregar efectos visuales o de sonido:
        // - animación de ataque
        // - sonido de golpe
    }

}