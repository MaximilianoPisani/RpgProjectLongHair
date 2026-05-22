using Fusion;
using UnityEngine;

public class PlayerFallState : IPlayerState
{
    private PlayerStateMachine _sm;
    private TickTimer _minFallTimer;

    public PlayerFallState(PlayerStateMachine sm) => _sm = sm;

    public void Enter()
    {
        if (_sm.Object.HasStateAuthority)
            _minFallTimer = TickTimer.CreateFromSeconds(_sm.Runner, 0.15f);

        if (_sm.Animator != null)
        {
            _sm.Animator.SetBool("isFalling", true);
            _sm.Animator.SetBool("isJumping", false);
            _sm.Animator.SetBool("isLanding", false);
        }

        var netSync = _sm.GetComponent<PlayerNetworkSync>();
        if (netSync != null)
        {
            netSync.SetFallingFlag(true);
            netSync.TriggerFall();
        }
    }

    public void Exit()
    {
        if (_sm.Animator != null)
            _sm.Animator.SetBool("isFalling", false);

        // NUEVO: Limpiar flag al salir
        var netSync = _sm.GetComponent<PlayerNetworkSync>();
        if (netSync != null)
            netSync.SetFallingFlag(false);
    }

    public void Tick(NetworkInputData input)
    {
        if (!_sm.Object.HasStateAuthority) return;
        if (!_minFallTimer.Expired(_sm.Runner)) return;

        var ncc = _sm.GetComponent<NetworkCharacterController>();
        if (_sm.Player.IsPhysicallyGroundedPublic() && ncc != null && ncc.Velocity.y <= 0.5f)
            _sm.ChangeState(new PlayerLandState(_sm));
    }

    public static bool ShouldFall(PlayerStateMachine sm)
    {
        if (sm.Player == null) return false;
        var ncc = sm.GetComponent<NetworkCharacterController>();
        if (ncc == null) return false;
        return !sm.Player.IsPhysicallyGroundedPublic() && ncc.Velocity.y < -1f;
    }
}