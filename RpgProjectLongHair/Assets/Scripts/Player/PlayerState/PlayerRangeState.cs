using Fusion;
using UnityEngine;

public class PlayerRangeState : IPlayerState, IAnimationEventReceiver
{
    private PlayerStateMachine _sm;

    private enum ShootPhase
    {
        Shooting,
        Reloading
    }

    private ShootPhase _currentPhase;
    private bool _hasSpawnedProjectile;

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

        if (!_sm.AttackCooldown.ExpiredOrNotRunning(_sm.Runner))
        {
            _sm.ChangeState(new PlayerIdleState(_sm));
            return;
        }

        _currentPhase = ShootPhase.Shooting;
        _hasSpawnedProjectile = false;

        RotateToShootDirection();

        _sm.GetComponent<PlayerNetworkSync>()?.TriggerShoot();
    }

    public void Tick(NetworkInputData input)
    {
        HandleGunMovement();
    }

    public void Exit()
    {
        if (_sm.Animator != null)
            _sm.Animator.SetBool("IsReloading", false);
    }

    public void OnShootFrame()
    {
        if (_hasSpawnedProjectile) return;

        _hasSpawnedProjectile = true;

        if (_sm.Object.HasInputAuthority && !_sm.Object.HasStateAuthority)
        {
            RPC_SpawnProjectile();
        }

        if (_sm.Object.HasStateAuthority)
        {
            SpawnProjectile();
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SpawnProjectile()
    {
        SpawnProjectile();
    }


    private void SpawnProjectile()
    {
        var settings = _sm.Combat;

        Vector3 direction = GetShootDirection();

        Vector3 spawnPos = (settings.shootPoints != null && settings.shootPoints.Length > 0)
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
                {
                    projectile.InitServer(direction, rangeData, attacker, spawnPos);
                }
            }
        );
    }

    public void OnShootAnimationEnd()
    {
        _currentPhase = ShootPhase.Reloading;

        if (_sm.Animator != null)
            _sm.Animator.SetBool("IsReloading", true);
    }

    public void OnReloadComplete()
    {
        var settings = _sm.Combat;

        _sm.AttackCooldown = TickTimer.CreateFromSeconds(
            _sm.Runner,
            settings.rangeData.Cooldown
        );

        _sm.ChangeState(new PlayerIdleState(_sm));
    }

    public void OpenComboWindow() { }
    public void CloseComboWindow() { }
    public void EndAttack() { }
    public void OnHitFrame() { }


    private Vector3 GetShootDirection()
    {
        if (_sm.LastShootDirection.sqrMagnitude > 0.01f)
            return _sm.LastShootDirection.normalized;

        Camera cam = Camera.main;
        if (cam == null)
            return _sm.transform.forward;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, 200f))
            return (hit.point - _sm.transform.position).normalized;

        return ray.direction.normalized;
    }

    private void RotateToShootDirection()
    {
        Vector3 direction = _sm.LastShootDirection.sqrMagnitude > 0.01f
            ? _sm.LastShootDirection
            : _sm.transform.forward;

        Vector3 flatDir = new Vector3(direction.x, 0f, direction.z).normalized;

        if (flatDir.sqrMagnitude > 0.01f)
            _sm.transform.rotation = Quaternion.LookRotation(flatDir);
    }

    private void HandleGunMovement()
    {
        if (_sm.Animator == null) return;

        if (_currentPhase == ShootPhase.Shooting)
        {
            _sm.Animator.SetFloat("Speed", 0f);
            return;
        }

        float speed = _sm.Player.GetHorizontalSpeed();
        float normalized = speed / _sm.Player.SprintSpeed;

        _sm.Animator.SetFloat("speed", normalized);
        _sm.Animator.SetBool("isMoving", speed > 0.05f);
    }
}