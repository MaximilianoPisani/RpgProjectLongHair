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
        ResetAnimations();

        if (!_sm.Object.HasStateAuthority) return;
        _timer = 0f;

        if (_sm.Object.HasInputAuthority)
            RunnerManager.SetInputBlocked(true);
    }

    private void ResetAnimations()
    {
        var animator = _sm.GetComponentInChildren<Animator>();
        if (animator == null) return;

        animator.SetBool("IsReloading", false);
        animator.SetBool("isJumping", false);
        animator.SetBool("isFalling", false);
        animator.SetBool("isLanding", false);
        animator.SetFloat("speed", 0f);
        animator.SetInteger("ComboIndex", 0);
        animator.ResetTrigger("Shoot");
        animator.ResetTrigger("Melee");
        animator.ResetTrigger("Jump");
        animator.ResetTrigger("Fall");
        animator.ResetTrigger("Land");

        animator.SetBool("IsDead", true);   
        animator.SetTrigger("Die");
    }

    public void Exit()
    {
        var animator = _sm.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.ResetTrigger("Die");
            animator.SetBool("IsDead", false);  
            animator.SetBool("IsReloading", false);
            animator.SetBool("isJumping", false);
            animator.SetBool("isFalling", false);
            animator.SetBool("isLanding", false);
            animator.SetFloat("speed", 0f);
            animator.SetInteger("ComboIndex", 0);
            animator.ResetTrigger("Shoot");
            animator.ResetTrigger("Melee");
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

            if (_sm.Health != null)
                _sm.Health.ResetHealth();

            _sm.ChangeState(new PlayerIdleState(_sm));
        }
    }
}