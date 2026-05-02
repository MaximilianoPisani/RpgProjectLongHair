using Fusion;
using UnityEngine;

public class PlayerMoveState : IPlayerState
{
    private PlayerStateMachine _sm;

    private const float AccelDamp = 0.08f;  // idle walk/run
    private const float DecelDamp = 0.08f;   // walk/run idle

    public PlayerMoveState(PlayerStateMachine sm)
    {
        _sm = sm;
    }

    public void Enter() { }

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
        { _sm.ChangeState(new PlayerRangeState(_sm)); return; }

        //CALCULAR SPEED
        float speed = _sm.Player.GetHorizontalSpeed();

        float normalizedSpeed = speed / _sm.Player.SprintSpeed;

        if (_sm.Animator != null)
        {
            float currentAnim = _sm.Animator.GetFloat("speed");

            // Damp asimétrico rápido al subir, lento al bajar
            float damp = normalizedSpeed > currentAnim ? AccelDamp : DecelDamp;

            // deltaTime como tercer parámetro activa el suavizado nativo del Animator
            _sm.Animator.SetFloat("speed", normalizedSpeed, damp, _sm.Runner.DeltaTime); 
        }
        // CAMBIO A IDLE SI NO SE MUEVE
        if (speed < 0.01f)
        {
            _sm.ChangeState(new PlayerIdleState(_sm));
            return;
        }
    }
}