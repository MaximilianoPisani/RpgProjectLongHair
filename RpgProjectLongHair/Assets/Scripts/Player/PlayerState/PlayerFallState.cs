using Fusion;
using UnityEngine;

public class PlayerFallState : IPlayerState
{
    private PlayerStateMachine _sm;
    private float _fallTime;
    private const float FallVelocityThreshold = -0.5f;

    public PlayerFallState(PlayerStateMachine sm)
    {
        _sm = sm;
    }

    public void Enter()
    {
        _fallTime = 0f;
        _sm.GetComponent<PlayerNetworkSync>()?.TriggerFall();

        if (_sm.Animator != null)
        {
            _sm.Animator.SetBool("isFalling", true);
            _sm.Animator.SetBool("isJumping", false);
        }
    }

    public void Exit()
    {
        if (_sm.Animator != null)
            _sm.Animator.SetBool("isFalling", false);
    }

    public void Tick(NetworkInputData input)
    {
        _fallTime += _sm.Runner.DeltaTime;

        if (_sm.Player.IsPhysicallyGroundedPublic())
        {
            _sm.ChangeState(new PlayerLandState(_sm));
            return;
        }
    }

    public static bool ShouldFall(PlayerStateMachine sm)
    {
        var ncc = sm.GetComponent<NetworkCharacterController>();
        if (ncc == null) return false;
        return !sm.Player.IsGrounded() && ncc.Velocity.y < FallVelocityThreshold;
    }
}