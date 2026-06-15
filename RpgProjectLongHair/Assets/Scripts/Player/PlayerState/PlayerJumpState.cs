using Fusion;
using UnityEngine;

public class PlayerJumpState : IPlayerState
{
    private PlayerStateMachine _sm;
    private TickTimer _minAirTimer;
    private TickTimer _minLandTimer;

    public PlayerJumpState(PlayerStateMachine sm) => _sm = sm;

    public void Enter()
    {
        _sm.IsJumping = true;

        if (_sm.Object.HasStateAuthority || _sm.Object.HasInputAuthority)
        {
            _sm.Player.Jump();
        }

        if (_sm.Object.HasStateAuthority)
        {
            _minAirTimer = TickTimer.CreateFromSeconds(_sm.Runner, 0.1f);
            _minLandTimer = TickTimer.CreateFromSeconds(_sm.Runner, 0.3f);
        }

        if (_sm.Animator != null)
        {
            _sm.Animator.SetBool("isLanding", false);
            _sm.Animator.SetBool("isJumping", true);
            _sm.Animator.SetBool("isFalling", false);
        }

        var netSync = _sm.GetComponent<PlayerNetworkSync>();
        if (netSync != null)
        {
            netSync.SetJumpingFlag(true);
            netSync.TriggerJump();
        }
    }

    public void Exit()
    {
        if (_sm.Animator != null)
            _sm.Animator.SetBool("isJumping", false);

        // NUEVO: Limpiar flag al salir
        var netSync = _sm.GetComponent<PlayerNetworkSync>();
        if (netSync != null)
            netSync.SetJumpingFlag(false);
    }

    public void Tick(NetworkInputData input)
    {
        // Solo el host resuelve transiciones
        if (!_sm.Object.HasStateAuthority) return;

        if (_minAirTimer.Expired(_sm.Runner) && PlayerFallState.ShouldFall(_sm))
        {
            _sm.ChangeState(new PlayerFallState(_sm));
            return;
        }

        if (_minLandTimer.Expired(_sm.Runner) && _sm.Player.IsPhysicallyGroundedPublic())
        {
            var ncc = _sm.GetComponent<NetworkCharacterController>();
            if (ncc != null && ncc.Velocity.y <= 0.5f)
                _sm.ChangeState(new PlayerLandState(_sm));
        }
    }
}