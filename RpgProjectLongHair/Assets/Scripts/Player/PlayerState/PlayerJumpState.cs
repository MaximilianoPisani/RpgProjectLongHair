using Fusion;
using UnityEngine;
public class PlayerJumpState : IPlayerState
{
    private PlayerStateMachine _sm;
    private float _airTime;
    private const float MinAirTimeBeforeFall = 0.1f; // Tiempo antes de poder caer
    private const float MinAirTimeBeforeLand = 0.2f;
    public PlayerJumpState(PlayerStateMachine sm) => _sm = sm;

    public void Enter()
    {
        _airTime = 0f;
        _sm.IsJumping = true;

        _sm.Player.Jump();

        _sm.GetComponent<PlayerNetworkSync>()?.TriggerJump();

        if (_sm.Animator != null)
        {
            _sm.Animator.SetBool("isLanding", false);
            _sm.Animator.SetBool("isJumping", true);
            _sm.Animator.SetBool("isFalling", false);
        }

        _sm.GetComponent<PlayerNetworkSync>()?.TriggerJump();
        Debug.Log("[JUMP] Enter - Starting jump");
    }

    public void Exit()
    {
        if (_sm.Animator != null)
            _sm.Animator.SetBool("isJumping", false);

        Debug.Log($"[JUMP] Exit - Air time was: {_airTime:F2}s");
    }

    public void Tick(NetworkInputData input)
    {
        _airTime += _sm.Runner.DeltaTime;
        var ncc = _sm.GetComponent<NetworkCharacterController>();

        if (ncc != null)
        {
            Debug.Log($"[JUMP] Tick - AirTime: {_airTime:F2}, Grounded: {_sm.Player.IsPhysicallyGroundedPublic()}, VelY: {ncc.Velocity.y:F2}");
        }
        if (_airTime >= MinAirTimeBeforeFall && PlayerFallState.ShouldFall(_sm))
        {
            _sm.ChangeState(new PlayerFallState(_sm));
            return;
        }

        // Prevenir aterrizaje prematuro en colinas
        if (_airTime >= MinAirTimeBeforeLand && _sm.Player.IsPhysicallyGroundedPublic())
        {
            // Verificar que realmente aterrizamos (velocidad Y cerca de 0 o negativa)
            if (ncc != null && ncc.Velocity.y <= 0.5f)
            {
                _sm.ChangeState(new PlayerLandState(_sm));
            }
        }
    }
}