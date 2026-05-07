using Fusion;
using UnityEngine;

public enum PlayerStateId : byte
{
    Idle,
    Move,
    Jump,
    Fall,
    Land,
    Melee,
    Range,
    Dead
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
    private PlayerStateId _lastKnownStateId;
    public override void Spawned()
    {
        Player = GetComponent<Player>();
        IsJumping = false;
        _stateChangeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        _lastKnownStateId = PlayerStateId.Idle;
        ChangeState(new PlayerIdleState(this));
    }

    public override void FixedUpdateNetwork()
    {
        if (Player.IsPhysicallyGroundedPublic())
            IsJumping = false;

        if (_currentState is PlayerDeadState)
        {
            _currentState.Tick(default);
            return;
        }

        if (!GetInput(out NetworkInputData input)) return;

        InputData = input;
        LastShootDirection = input.shootDirection;


        if (_currentState is PlayerIdleState || _currentState is PlayerMoveState)
        {
            if (PlayerFallState.ShouldFall(this))
            {
                ChangeState(new PlayerFallState(this));
                return;
            }
        }

        bool canJump = _currentState is PlayerIdleState
            || _currentState is PlayerMoveState;

        if (input.jump && canJump && Player.IsGrounded() && !IsJumpLocked)
        {
            IsJumping = true;
            ChangeState(new PlayerJumpState(this));
            return;
        }

        bool isInAir = _currentState is PlayerFallState
                    || _currentState is PlayerLandState
                    || _currentState is PlayerJumpState
                    || PlayerFallState.ShouldFall(this);

        if (isInAir)
        {
            var blockedInput = input;
            blockedInput.attack = false;
            blockedInput.attackJustPressed = false;
            blockedInput.attackRange = false;
            _currentState?.Tick(blockedInput);
            return;
        }

        _currentState?.Tick(input);
    }

    public void ChangeState(IPlayerState newState)
    {
        _currentState?.Exit();
        _currentState = newState;
        _currentState.Enter();

        if (Object.HasStateAuthority)
            NetworkedStateId = GetStateId(newState);
    }

    public void UpdateState(NetworkInputData input)
    {
        _currentState?.Tick(input);
    }

    public override void Render()
    {
        if (Object.HasStateAuthority) return;

        foreach (var change in _stateChangeDetector.DetectChanges(this))
        {
            if (change == nameof(NetworkedStateId))
            {
                if (Object.HasInputAuthority)
                {
                    // Jugador local: SOLO sincronizar el respawn (Dead  vivo)
                    // El combo y movimiento los maneja la predicción local
                    bool isRespawn = _currentState is PlayerDeadState
                                     && NetworkedStateId != PlayerStateId.Dead;
                    if (isRespawn)
                    {
                        _lastKnownStateId = PlayerStateId.Dead;
                        SyncStateOnClient(NetworkedStateId);
                    }
                }
                else
                {
                    // Jugador remoto: sincronizar todo
                    SyncStateOnClient(NetworkedStateId);
                }
            }
        }
    }

    private void SyncStateOnClient(PlayerStateId stateId)
    {
        if (_lastKnownStateId == stateId) return;
        _lastKnownStateId = stateId;

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

        // Cambia el estado localmente sin modificar NetworkedStateId
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
        if (Combat == null || Combat.meleeData == null) return;

        Vector3 attackOrigin = Combat.meleeOrigin != null
            ? Combat.meleeOrigin.position
            : origin + forward * 0.5f + Vector3.up;

        Collider[] hits = Physics.OverlapSphere(
            attackOrigin,
            Combat.meleeData.HitRadius,
            Combat.enemyLayer
        );

        foreach (var hit in hits)
        {
            var enemyHealth = hit.GetComponentInParent<EnemyHealth>();
            if (enemyHealth != null && enemyHealth.Object.HasStateAuthority)
            {
                enemyHealth.ApplyDamageServer(damage, Object.InputAuthority);
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

        if (_currentState is PlayerMeleeState)
            Gizmos.color = Color.red;
        else
            Gizmos.color = new Color(1, 0, 0, 0.2f);

        Gizmos.DrawWireSphere(origin, radius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, origin + direction * 1.5f);
    }
}