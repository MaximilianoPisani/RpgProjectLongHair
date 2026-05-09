using Fusion;
using UnityEngine;

public class PlayerIdleState : IPlayerState
{
    private PlayerStateMachine _sm;
    private const float DecelDamp = 0.08f;
    public PlayerIdleState(PlayerStateMachine sm)
    {
        _sm = sm;
    }

    public void Enter()
    {
        var weapon = _sm.GetComponent<PlayerWeaponHandler>();
        if (_sm.Animator != null && weapon != null)
        {
            _sm.Animator.SetBool("IsGunEquipped", weapon.IsRanged);
            _sm.Animator.SetBool("IsAxeEquipped", weapon.IsMelee);
        }

        // Resetear flags aéreos por si quedaron sucios
        if (_sm.Animator != null)
        {
            _sm.Animator.SetBool("isJumping", false);
            _sm.Animator.SetBool("isFalling", false);
            _sm.Animator.SetBool("isLanding", false);
        }
    }

    public void Exit() { }

    public void Tick(NetworkInputData input)
    {
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
        if (_sm.Animator != null)
            _sm.Animator.SetFloat("speed", 0f, DecelDamp, Time.deltaTime);
        if (input.moveDirection.sqrMagnitude > 0.01f)
        {
            _sm.ChangeState(new PlayerMoveState(_sm));
            return;
        }
    }
}