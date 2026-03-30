using Fusion;
using UnityEngine;

public class PlayerMoveState : IPlayerState
{
    private PlayerStateMachine _sm;



    public PlayerMoveState(PlayerStateMachine sm)
    {
        _sm = sm;
    }

    public void Enter()
    {
        if (_sm.Animator != null)
            _sm.Animator.SetBool("isMoving", true);
    }

    public void Exit()
    {
        if (_sm.Animator != null)
            _sm.Animator.SetBool("isMoving", false);
    }

    public void Tick(NetworkInputData input)
    {
        var weapon = _sm.GetComponent<PlayerWeaponHandler>();

        if (input.attack && weapon != null && weapon.IsMelee)
        { _sm.ChangeState(new PlayerMeleeState(_sm)); return; }

        if (input.attackRange && weapon != null && weapon.IsRanged)
        { _sm.ChangeState(new PlayerRangeState(_sm)); return; }

        //CALCULAR SPEED
        float speed = input.moveDirection.magnitude;

        if (_sm.Animator != null)
            _sm.Animator.SetFloat("speed", speed);

        // CAMBIO A IDLE SI NO SE MUEVE
        if (speed < 0.01f)
        {
            _sm.ChangeState(new PlayerIdleState(_sm));
            return;
        }
    }
}