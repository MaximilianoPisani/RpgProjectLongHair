using Fusion;
using UnityEngine;

public class PlayerStateMachine : NetworkBehaviour
{
    private IPlayerState _currentState;

    [Header("References")]
    public Animator Animator;
    public PlayerCombat Combat;
    public PlayerHealth Health;
    public Player Player; 

    [Header("Config")]
    public float moveSpeed = 5f;

    [Networked] public TickTimer AttackCooldown { get; set; }
    public bool IsJumping { get; set; } = false;
    public Vector3 LastShootDirection { get; set; } = Vector3.forward;

    public override void Spawned()
    {
        Player = GetComponent<Player>();
        IsJumping = false;
        ChangeState(new PlayerIdleState(this));
    }

    public override void FixedUpdateNetwork()
    {
        if (Player.IsGrounded())
            IsJumping = false;

        if (_currentState is PlayerDeadState)
        {
            _currentState.Tick(default);
            return;
        }

        if (!GetInput(out NetworkInputData input)) return;

        LastShootDirection = input.shootDirection;

        if (input.jump && !IsJumping && Player.IsGrounded())
        {
            IsJumping = true;
            ChangeState(new PlayerJumpState(this));
            return;
        }

        _currentState?.Tick(input);
    }

    public void ChangeState(IPlayerState newState)
    {
        _currentState?.Exit();
        _currentState = newState;
        _currentState.Enter();
    }

    public Vector3 GetSpawnPosition()
    {
        var checkpoint = GetComponent<PlayerCheckpoint>();
        return checkpoint != null ? checkpoint.LastCheckpoint : Vector3.zero;
    }
}