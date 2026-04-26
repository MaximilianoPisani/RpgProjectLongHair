using Fusion;
using UnityEngine;

public class PlayerLandState : IPlayerState
{
    private PlayerStateMachine _sm;
    private TickTimer _landTickTimer;

    public PlayerLandState(PlayerStateMachine sm)
    {
        _sm = sm;
    }

    public void Enter()
    {
        _sm.IsJumping = false;
        _landTickTimer = TickTimer.CreateFromSeconds(_sm.Runner, 0.2f);

        if (_sm.Animator != null)
        {
            _sm.Animator.SetBool("isJumping", false);
            _sm.Animator.SetBool("isFalling", false);
            _sm.Animator.SetBool("isLanding", true);

            float speed = _sm.Player.GetHorizontalSpeed();
            float normalizedSpeed = speed / _sm.Player.SprintSpeed;
            _sm.Animator.SetFloat("speed", normalizedSpeed);
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
        if (!_landTickTimer.Expired(_sm.Runner))
            return;

        var weapon = _sm.GetComponent<PlayerWeaponHandler>();

        if (input.attack && weapon != null && weapon.IsMelee)
        {
            _sm.ChangeState(new PlayerMeleeState(_sm));
            return;
        }

        if (input.attackRange && weapon != null && weapon.IsRanged)
        {
            _sm.ChangeState(new PlayerRangeState(_sm));
            return;
        }

        if (input.moveDirection.sqrMagnitude > 0.01f)
            _sm.ChangeState(new PlayerMoveState(_sm));
        else
            _sm.ChangeState(new PlayerIdleState(_sm));
    }
}