using Fusion;
using UnityEngine;

public class PlayerLandState : IPlayerState
{
    private PlayerStateMachine _sm;

    public PlayerLandState(PlayerStateMachine sm) => _sm = sm;

    public void Enter()
    {
        _sm.IsJumping = false;

        if (_sm.Object.HasStateAuthority)
            _sm.LandTickTimer = TickTimer.CreateFromSeconds(_sm.Runner, 0.2f);

        if (_sm.Animator != null)
        {
            _sm.Animator.SetBool("isJumping", false);
            _sm.Animator.SetBool("isFalling", false);
            _sm.Animator.SetBool("isLanding", true);
            float speed = _sm.Player.GetHorizontalSpeed();
            _sm.Animator.SetFloat("speed", speed / _sm.Player.SprintSpeed);
        }

        var netSync = _sm.GetComponent<PlayerNetworkSync>();
        if (netSync != null)
        {
            netSync.SetLandingFlag(true);
            netSync.TriggerLand();
        }
    }

    public void Exit()
    {
        var netSync = _sm.GetComponent<PlayerNetworkSync>();
        if (netSync != null)
            netSync.SetLandingFlag(false);
    }

    public void Tick(NetworkInputData input)
    {
        if (!_sm.Object.HasStateAuthority) return;
        if (!_sm.LandTickTimer.Expired(_sm.Runner)) return;

        var weapon = _sm.GetComponent<PlayerWeaponHandler>();
        if (input.attackJustPressed && weapon != null && weapon.IsMelee)
        {
            _sm.ChangeState(new PlayerMeleeState(_sm));
            return;
        }
        if (input.attack && weapon != null && weapon.IsRanged)
        {
            _sm.ChangeState(new PlayerRangeState(_sm));
            return;
        }

        _sm.ChangeState(input.moveDirection.sqrMagnitude > 0.01f
            ? new PlayerMoveState(_sm)
            : new PlayerIdleState(_sm));
    }
}