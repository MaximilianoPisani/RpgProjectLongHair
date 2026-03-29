using Fusion;
using UnityEngine;

public class PlayerMeleeState : IPlayerState
{
    private PlayerStateMachine _sm;

    public PlayerMeleeState(PlayerStateMachine sm)
    {
        _sm = sm;
    }

    public void Enter()
    {
        var weapon = _sm.GetComponent<PlayerWeaponHandler>();

        if (weapon == null || !weapon.IsMelee)
        {
            _sm.ChangeState(new PlayerIdleState(_sm));
            return;
        }

        var settings = _sm.Combat;

        if (!_sm.AttackCooldown.ExpiredOrNotRunning(_sm.Runner))
        {
            _sm.ChangeState(new PlayerIdleState(_sm));
            return;
        }

        _sm.AttackCooldown = TickTimer.CreateFromSeconds(
            _sm.Runner,
            settings.meleeData.Cooldown
        );

        Vector3 origin = settings.meleeOrigin != null
            ? settings.meleeOrigin.position
            : _sm.transform.position + Vector3.up;

        Collider[] hits = Physics.OverlapSphere(
            origin,
            settings.meleeData.HitRadius,
            settings.enemyLayer
        );

        foreach (var hit in hits)
        {
            var enemyHealth = hit.GetComponentInParent<EnemyHealth>();

            if (enemyHealth != null && enemyHealth.Object.HasStateAuthority)
            {
                enemyHealth.ApplyDamageServer(
                    settings.meleeData.Damage,
                    _sm.Object.InputAuthority
                );
            }
        }

        if (_sm.Animator != null)
            _sm.Animator.SetTrigger("Melee");

        _sm.ChangeState(new PlayerIdleState(_sm));
    }

    public void Exit() { }

    public void Tick(NetworkInputData input) { }
}