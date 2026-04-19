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
            _sm.Animator.SetBool("isLanding", false);
        }

        _sm.GetComponent<PlayerNetworkSync>()?.TriggerFall();

        Debug.Log("[FALL] Enter - Starting fall");
    }

    public void Exit()
    {
        if (_sm.Animator != null)
            _sm.Animator.SetBool("isFalling", false);

        Debug.Log($"[FALL] Exit - Fall time was: {_fallTime:F2}s");
    }

    public void Tick(NetworkInputData input)
    {
        _fallTime += _sm.Runner.DeltaTime;

        var ncc = _sm.GetComponent<NetworkCharacterController>();

        if (ncc != null)
        {
            Debug.Log($"[FALL] Tick - FallTime: {_fallTime:F2}, Grounded: {_sm.Player.IsPhysicallyGroundedPublic()}, VelY: {ncc.Velocity.y:F2}");
        }

        // Solo aterrizar si velocidad Y es negativa o cercana a 0
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

        //Caer si NO está en el suelo Y la velocidad Y es negativa
        return !sm.Player.IsPhysicallyGroundedPublic() && ncc.Velocity.y < -1f;
    }
}