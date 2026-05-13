using UnityEngine;

public class PlayerDeadState : IPlayerState
{
    private PlayerStateMachine _sm;
    private float _timer;

    public PlayerDeadState(PlayerStateMachine sm)
    {
        _sm = sm;
    }

    public void Enter()
    {
        var sync = _sm.GetComponent<PlayerNetworkSync>();
        sync?.ResetAllAnimations();
        sync?.SetIsReloading(false);
        sync?.SetSpeed(0f);

        ResetAnimations();

        if (_sm.Object.HasStateAuthority)
        {
            _timer = 0f;

            // CRÍTICO: Solo el servidor incrementa el trigger
            // Esto se sincronizará automáticamente a todos los clientes
            sync?.TriggerDie();

            if (_sm.Object.HasInputAuthority)
                RunnerManager.SetInputBlocked(true);
        }
    }

    private void ResetAnimations()
    {
        var animator = _sm.Animator;
        if (animator == null) return;

        animator.SetBool("IsReloading", false);
        animator.SetBool("isJumping", false);
        animator.SetBool("isFalling", false);
        animator.SetBool("isLanding", false);
        animator.SetFloat("speed", 0f);
        animator.SetInteger("ComboIndex", 0);
        animator.SetBool("IsDead", true);

        animator.ResetTrigger("Shoot");
        animator.ResetTrigger("Melee");
        animator.ResetTrigger("Jump");
        animator.ResetTrigger("Fall");
        animator.ResetTrigger("Land");

        _sm.GetComponent<PlayerNetworkSync>()?.TriggerDie();
    }

    public void Exit()
    {
        var animator = _sm.Animator;
        if (animator != null)
        {
            animator.ResetTrigger("Die");
            animator.SetBool("IsDead", false);
        }

        if (!_sm.Object.HasInputAuthority) return;
        RunnerManager.SetInputBlocked(false);
    }

    public void Tick(NetworkInputData input)
    {
        if (!_sm.Object.HasStateAuthority) return;
        _timer += _sm.Runner.DeltaTime;

        if (_timer > 2f)
        {
            var checkpoint = _sm.GetComponent<PlayerCheckpoint>();
            Vector3 spawnPos = checkpoint != null ? checkpoint.LastCheckpoint : _sm.transform.position;
            _sm.Player.TeleportTo(spawnPos);

            var health = _sm.GetComponent<PlayerHealth>();
            if (health != null)
                health.ResetHealth();

            _sm.ChangeState(new PlayerIdleState(_sm));
        }
    }
}