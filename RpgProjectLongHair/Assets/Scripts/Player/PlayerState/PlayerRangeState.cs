using Fusion;
using UnityEngine;

public class PlayerRangeState : IPlayerState
{
    private PlayerStateMachine _sm;

    public PlayerRangeState(PlayerStateMachine sm)
    {
        _sm = sm;
    }

    public void Enter()
    {
        var weapon = _sm.GetComponent<PlayerWeaponHandler>();
        if (weapon == null || !weapon.IsRanged)
        {
            _sm.ChangeState(new PlayerIdleState(_sm));
            return;
        }

        var settings = _sm.Combat;
        if (settings.rangeData == null || settings.rangeData.ProjectilePrefab == null)
        {
            _sm.ChangeState(new PlayerIdleState(_sm));
            return;
        }

        if (!_sm.AttackCooldown.ExpiredOrNotRunning(_sm.Runner))
        {
            _sm.ChangeState(new PlayerIdleState(_sm));
            return;
        }

        _sm.AttackCooldown = TickTimer.CreateFromSeconds(_sm.Runner, settings.rangeData.Cooldown);

        Vector3 direction = _sm.LastShootDirection.sqrMagnitude > 0.01f ? _sm.LastShootDirection : _sm.transform.forward;

        Vector3 flatDir = new Vector3(direction.x, 0f, direction.z).normalized;
        if (flatDir.sqrMagnitude > 0.01f)
            _sm.transform.rotation = Quaternion.LookRotation(flatDir);

        Vector3 spawnPos =
            (settings.shootPoints != null && settings.shootPoints.Length > 0)
            ? settings.shootPoints[0].position
            : _sm.transform.position + _sm.transform.forward + Vector3.up;

        PlayerRef attacker = _sm.Object.InputAuthority;
        RangedAttackData rangeData = settings.rangeData;

        _sm.Runner.Spawn(
            rangeData.ProjectilePrefab,
            spawnPos,
            Quaternion.LookRotation(direction),
            attacker,
            onBeforeSpawned: (runner, obj) =>
            {
                var projectile = obj.GetComponent<Projectile>();
                if (projectile != null)
                    projectile.InitServer(direction, rangeData, attacker, spawnPos);
            }
        );

        if (_sm.Animator != null)
            _sm.Animator.SetTrigger("Range");

        _sm.ChangeState(new PlayerIdleState(_sm));
    }
    private Vector3 GetShootDirection()
    {
        Camera cam = Camera.main;
        if (cam == null) return _sm.transform.forward;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, 200f))
        {
            Vector3 dir = (hit.point - _sm.transform.position).normalized;
            return dir;
        }

        return ray.direction.normalized;
    }

    public void Exit() { }

    public void Tick(NetworkInputData input) { }
}