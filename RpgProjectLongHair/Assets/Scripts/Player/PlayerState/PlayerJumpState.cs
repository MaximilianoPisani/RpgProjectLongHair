using Fusion;
using UnityEngine;

public class PlayerJumpState : IPlayerState
{
    private PlayerStateMachine _sm;
    private float _airTime;
    private const float MinAirTime = 0.15f;

    public PlayerJumpState(PlayerStateMachine sm)
    {
        _sm = sm;
    }

    public void Enter()
    {
        _airTime = 0f;
        _sm.Player.Jump();
        _sm.GetComponent<PlayerNetworkSync>()?.TriggerJump();
    }

    public void Exit()
    {
        if (_sm.Animator != null)
            _sm.Animator.SetBool("isJumping", false);
    }

    public void Tick(NetworkInputData input)
    {
        _airTime += _sm.Runner.DeltaTime;

        if (_airTime < MinAirTime) return;

        if (_sm.Player.IsGrounded())
        {
            if (input.moveDirection.sqrMagnitude > 0.01f)
                _sm.ChangeState(new PlayerMoveState(_sm));
            else
                _sm.ChangeState(new PlayerIdleState(_sm));
        }
    }
}