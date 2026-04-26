using Fusion;
using UnityEngine;

public class PlayerFallState : IPlayerState
{
    private PlayerStateMachine _sm;
    private TickTimer _fallTimer;

    public PlayerFallState(PlayerStateMachine sm)
    {
        _sm = sm;
    }

    public void Enter()
    {
        _fallTimer = TickTimer.CreateFromSeconds(_sm.Runner, 0f); // empieza expirado, solo para tracking
        _sm.GetComponent<PlayerNetworkSync>()?.TriggerFall();

        if (_sm.Animator != null)
        {
            _sm.Animator.SetBool("isFalling", true);
            _sm.Animator.SetBool("isJumping", false);
            _sm.Animator.SetBool("isLanding", false);
        }

        Debug.Log("[FALL] Enter - Starting fall");
    }

    public void Exit()
    {
        if (_sm.Animator != null)
            _sm.Animator.SetBool("isFalling", false);


        Debug.Log("[FALL] Exit");
    }

    public void Tick(NetworkInputData input)
    {
        var ncc = _sm.GetComponent<NetworkCharacterController>();

        if (_sm.Player.IsPhysicallyGroundedPublic())
        {
            if (ncc != null && ncc.Velocity.y <= 0.5f)
            {
                _sm.ChangeState(new PlayerLandState(_sm));
            }
        }
    }

    public static bool ShouldFall(PlayerStateMachine sm)
    {
        if (sm.Player == null) return false;
        var ncc = sm.GetComponent<NetworkCharacterController>();
        if (ncc == null) return false;
        return !sm.Player.IsPhysicallyGroundedPublic() && ncc.Velocity.y < -1f;
    }
}