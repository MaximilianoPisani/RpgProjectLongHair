using Fusion;
using UnityEngine;

public class PlayerLandState : IPlayerState
{
    private PlayerStateMachine _sm;
    private float _landTime;
    private const float LandDuration = 0.2f;

    public PlayerLandState(PlayerStateMachine sm)
    {
        _sm = sm;
    }

    public void Enter()
    {
        _landTime = 0f;
        _sm.IsJumping = false;

        if (_sm.Animator != null)
        {
            _sm.Animator.SetBool("isJumping", false);
            _sm.Animator.SetBool("isFalling", false);
            _sm.Animator.SetBool("isLanding", true);
        }

        _sm.GetComponent<PlayerNetworkSync>()?.TriggerLand();
    }

    public void Exit()
    {
        if (_sm.Animator != null)
            _sm.Animator.SetBool("isLanding", false);
    }

    public void Tick(NetworkInputData input)
    {
        _landTime += _sm.Runner.DeltaTime;


        if (!_sm.Player.IsPhysicallyGroundedPublic())
        {
            _sm.ChangeState(new PlayerFallState(_sm));
            return;
        }

        if (_landTime >= LandDuration)
        {
            if (input.moveDirection.sqrMagnitude > 0.01f)
                _sm.ChangeState(new PlayerMoveState(_sm));
            else
                _sm.ChangeState(new PlayerIdleState(_sm));
        }
    }
}