using Fusion;
using UnityEngine;

public enum PlayerStateId : byte
{
    Idle, Move, Jump, Fall, Land, Melee, Range, Dead
}

public class PlayerStateMachine : NetworkBehaviour
{
    private IPlayerState _currentState;
    public IPlayerState CurrentState => _currentState;

    [Header("References")]
    public Animator Animator;
    public PlayerCombat Combat;
    public PlayerHealth Health;
    public Player Player;

    public bool IsBusy => _currentState is PlayerMeleeState || _currentState is PlayerRangeState;
    [Networked] public TickTimer AttackCooldown { get; set; }
    [Networked] public int NetworkedComboIndex { get; set; }
    [Networked] public TickTimer LandTickTimer { get; set; }
    public bool IsJumping { get; set; } = false;
    public Vector3 LastShootDirection { get; set; } = Vector3.forward;
    public bool IsJumpLocked => _currentState is PlayerRangeState;

    public NetworkInputData InputData { get; private set; }

    [Networked] public PlayerStateId NetworkedStateId { get; set; }
    private ChangeDetector _stateChangeDetector;

    // === estados que solo el host resuelve ====================
    private static bool IsAirborneState(IPlayerState s) =>
        s is PlayerJumpState
        || s is PlayerFallState
        || s is PlayerLandState;

    public override void Spawned()
    {
        Player = GetComponent<Player>();
        IsJumping = false;
        _stateChangeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        ChangeState(new PlayerIdleState(this));
    }

    public override void FixedUpdateNetwork()
    {
        if (Player.IsPhysicallyGroundedPublic() && !(_currentState is PlayerJumpState))
            IsJumping = false;

        if (_currentState is PlayerDeadState)
        {
            if (Object.HasStateAuthority)
                _currentState.Tick(default);
            return;
        }

        // Cliente remoto: nunca tickea
        if (!Object.HasInputAuthority && !Object.HasStateAuthority)
            return;

        if (!GetInput(out NetworkInputData input)) return;
        InputData = input;
        LastShootDirection = input.shootDirection;

        // Estados aéreos: solo el host tickea
        if (IsAirborneState(_currentState))
        {
            if (Object.HasStateAuthority)
            {
                var blockedInput = input;
                blockedInput.attack = false;
                blockedInput.attackJustPressed = false;
                blockedInput.attackRange = false;
                _currentState.Tick(blockedInput);
            }
            return;
        }

        // Caída: solo host inicia
        if (Object.HasStateAuthority)
        {
            if ((_currentState is PlayerIdleState || _currentState is PlayerMoveState)
                && PlayerFallState.ShouldFall(this))
            {
                ChangeState(new PlayerFallState(this));
                return;
            }
        }

        // Salto: el input viene del cliente, el host ejecuta la física
        bool canJump = _currentState is PlayerIdleState || _currentState is PlayerMoveState;
        if (input.jump && canJump && Player.IsGrounded() && !IsJumpLocked)
        {
            IsJumping = true;
            ChangeState(new PlayerJumpState(this));
            return;
        }

        _currentState?.Tick(input);
    }

    public override void Render()
    {
        // El host ya tiene el estado correcto, solo necesita sincronizar clientes
        foreach (var change in _stateChangeDetector.DetectChanges(this))
        {
            if (change != nameof(NetworkedStateId)) continue;

            if (Object.HasStateAuthority) continue;

            if (Object.HasInputAuthority)
            {
                // Cliente local: solo sincronizar estados aéreos y muerte
                // Los estados predichos (Idle/Move/Melee/Range) no se pisan

                bool currentIsAirborne = _currentState is PlayerJumpState
                                 || _currentState is PlayerFallState
                                 || _currentState is PlayerLandState;

                bool shouldSync = NetworkedStateId == PlayerStateId.Jump
                          || NetworkedStateId == PlayerStateId.Fall
                          || NetworkedStateId == PlayerStateId.Land
                          || NetworkedStateId == PlayerStateId.Dead
                          // Salir del ciclo aéreo cuando el host confirma tierra
                          || (currentIsAirborne && (
                              NetworkedStateId == PlayerStateId.Idle
                           || NetworkedStateId == PlayerStateId.Move));
                if (shouldSync)
            {
                // Resetear IsJumping cuando volvemos a tierra
                if (NetworkedStateId == PlayerStateId.Idle
                 || NetworkedStateId == PlayerStateId.Move)
                    IsJumping = false;

                SyncStateOnClient(NetworkedStateId);
            }
            }
            else
            {
                // Cliente remoto: sincronizar todo
                SyncStateOnClient(NetworkedStateId);
            }
        }
    }

    // ===Cambia estado y notifica al host ====================
    public void ChangeState(IPlayerState newState)
    {
        Debug.Log($"[SM] {GetStateId(_currentState)} ? {GetStateId(newState)} | StateAuth:{Object.HasStateAuthority} | InputAuth:{Object.HasInputAuthority}");
        _currentState?.Exit();
        _currentState = newState;
        _currentState.Enter();

        if (Object.HasStateAuthority)
            NetworkedStateId = GetStateId(newState);
    }

    // ===Sincroniza estado en el cliente sin tocar NetworkedStateId ====================
    private void SyncStateOnClient(PlayerStateId stateId)
    {
        Debug.Log($"[SYNC CLIENT] Aplicando {stateId} | currentState:{GetStateId(_currentState)}");
        IPlayerState newState = stateId switch
        {
            PlayerStateId.Idle => new PlayerIdleState(this),
            PlayerStateId.Move => new PlayerMoveState(this),
            PlayerStateId.Jump => new PlayerJumpState(this),
            PlayerStateId.Fall => new PlayerFallState(this),
            PlayerStateId.Land => new PlayerLandState(this),
            PlayerStateId.Melee => new PlayerMeleeState(this),
            PlayerStateId.Range => new PlayerRangeState(this),
            PlayerStateId.Dead => new PlayerDeadState(this),
            _ => new PlayerIdleState(this)
        };

        _currentState?.Exit();
        _currentState = newState;
        _currentState.Enter();
    }

    private static PlayerStateId GetStateId(IPlayerState state) => state switch
    {
        PlayerIdleState => PlayerStateId.Idle,
        PlayerMoveState => PlayerStateId.Move,
        PlayerJumpState => PlayerStateId.Jump,
        PlayerFallState => PlayerStateId.Fall,
        PlayerLandState => PlayerStateId.Land,
        PlayerMeleeState => PlayerStateId.Melee,
        PlayerRangeState => PlayerStateId.Range,
        PlayerDeadState => PlayerStateId.Dead,
        _ => PlayerStateId.Idle
    };

    public Vector3 GetSpawnPosition()
    {
        var checkpoint = GetComponent<PlayerCheckpoint>();
        return checkpoint != null ? checkpoint.LastCheckpoint : Vector3.zero;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    public void RPC_RequestMeleeDamage(Vector3 origin, Vector3 forward, int damage)
    {
        if (Combat == null || Combat.meleeData == null)
            return;

        Vector3 attackOrigin = Combat.meleeOrigin != null
            ? Combat.meleeOrigin.position
            : origin + forward * 0.5f + Vector3.up;

        int damageableLayer = LayerMask.GetMask("Damageable");
        LayerMask combinedMask = Combat.enemyLayer | damageableLayer;

        Collider[] hits = Physics.OverlapSphere(
            attackOrigin,
            Combat.meleeData.HitRadius,
            combinedMask
        );

        foreach (var hit in hits)
        {
            var enemyHealth = hit.GetComponentInParent<EnemyHealth>();

            if (enemyHealth != null && enemyHealth.Object.HasStateAuthority)
            {
                var stats = GetComponent<PlayerStats>();

                int finalDamage = stats != null
                    ? stats.CurrentDamage
                    : damage;

                enemyHealth.ApplyDamageServer(finalDamage, Object.InputAuthority);

                PlayerRageHandler.NotifyDamageDealt(
                    Object.InputAuthority,
                    finalDamage
                );

                Debug.Log($"[RPC Melee] Hit enemy - Damage: {finalDamage}");
                continue;
            }

            var chainAnchor = hit.GetComponentInParent<DamageableChainAnchor>();
                if (chainAnchor != null && chainAnchor.Object.HasStateAuthority)
                {
                    var stats = GetComponent<PlayerStats>();
                    int finalDamage = stats != null ? stats.CurrentDamage : damage;

                    chainAnchor.ApplyDamageServer(finalDamage, Object.InputAuthority);

                    Debug.Log($"[RPC Melee] Hit chain anchor - Damage: {finalDamage}");
                    continue;
                }
            }
        }

    private void OnDrawGizmosSelected()
    {
        if (Combat == null || Combat.meleeData == null) return;

        Vector3 direction = Vector3.right;
        Vector3 origin = Combat.meleeOrigin != null
            ? Combat.meleeOrigin.position
            : transform.position + direction * 0.5f + Vector3.up;

        float radius = Combat.meleeData.HitRadius;
        Gizmos.color = _currentState is PlayerMeleeState ? Color.red : new Color(1, 0, 0, 0.2f);
        Gizmos.DrawWireSphere(origin, radius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, origin + direction * 1.5f);
    }
}