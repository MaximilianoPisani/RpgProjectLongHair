using Fusion;
using UnityEngine;

public class PlayerMoveState : IPlayerState
{
    private PlayerStateMachine _sm;



    public PlayerMoveState(PlayerStateMachine sm)
    {
        _sm = sm;
    }

    public void Enter() { }

    public void Exit()
    {
        if (_sm.Animator != null)
            _sm.Animator.SetFloat("speed", 0f);
    }

    public void Tick(NetworkInputData input)
    {
        var weapon = _sm.GetComponent<PlayerWeaponHandler>();

        if (input.attackJustPressed && weapon != null && weapon.IsMelee)
        {
            _sm.ChangeState(new PlayerMeleeState(_sm));
            return;
        }

        if (input.attackRange && weapon != null && weapon.IsRanged)
        { _sm.ChangeState(new PlayerRangeState(_sm)); return; }

        //CALCULAR SPEED
        float speed = _sm.Player.GetHorizontalSpeed();

        float normalizedSpeed = speed / _sm.Player.SprintSpeed;

        if (_sm.Animator != null)
        {
            _sm.Animator.SetFloat("speed", normalizedSpeed);
        }
        // CAMBIO A IDLE SI NO SE MUEVE
        if (speed < 0.01f)
        {
            _sm.ChangeState(new PlayerIdleState(_sm));
            return;
        }
    }
}