using Fusion;
using UnityEngine;

public class PlayerIdleState : IPlayerState
{
    private PlayerStateMachine _sm;

    public PlayerIdleState(PlayerStateMachine sm)
    {
        _sm = sm;
    }

    public void Enter()
    {
        if (_sm.Animator != null)
            _sm.Animator.SetFloat("speed", 0f);
    }

    public void Exit() { }

    public void Tick(NetworkInputData input)
    {
        if (input.jump && !_sm.IsJumping && _sm.Player.IsGrounded())
        {
            _sm.IsJumping = true;
            _sm.ChangeState(new PlayerJumpState(_sm));
            return;
        }

        var weapon = _sm.GetComponent<PlayerWeaponHandler>();

        if (input.attackJustPressed && weapon != null && weapon.IsMelee)
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
        {
            _sm.ChangeState(new PlayerMoveState(_sm));
            return;
        }
    }
}