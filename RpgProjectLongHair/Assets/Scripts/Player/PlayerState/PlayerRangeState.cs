using Fusion;
using UnityEngine;

public class PlayerRangeState : IPlayerState
{
    private PlayerStateMachine _sm;
    private RangedAttackData _rangeData;
    private IWeaponAnimatable _weaponAnim;

    private enum ShootPhase
    {
        Idle,
        Shooting,
        Reloading,
        AutomaticFire
    }

    private ShootPhase _currentPhase = ShootPhase.Idle;

    // Timers
    private TickTimer _shootTickTimer;
    private TickTimer _reloadTickTimer;
    private TickTimer _fireRateTickTimer;
    private TickTimer _continuousFireTickTimer;

    // Flags
    private bool _projectileSpawned = false;
    private bool _shellEjectionSpawned = false;
    private bool _fireEjectionSpawned = false;
    private bool _attackButtonReleased = true;
    private bool _needsReload = false;

    public PlayerRangeState(PlayerStateMachine sm)
    {
        _sm = sm;
    }
    public bool IsLockingInput =>
    _currentPhase == ShootPhase.Shooting ||
    _currentPhase == ShootPhase.Reloading ||
    _currentPhase == ShootPhase.AutomaticFire;
    public void Enter()
    {
        var weapon = _sm.GetComponent<PlayerWeaponHandler>();
        if (weapon == null || !weapon.IsRanged)
        {
            _sm.ChangeState(new PlayerIdleState(_sm));
            return;
        }

        _rangeData = _sm.Combat?.GetCurrentRangeData();
        if (_rangeData == null)
        {
            Debug.LogError("[Range] RangedAttackData no configurado!");
            _sm.ChangeState(new PlayerIdleState(_sm));
            return;
        }

        if (!_sm.AttackCooldown.ExpiredOrNotRunning(_sm.Runner))
        {
            _sm.ChangeState(new PlayerIdleState(_sm));
            return;
        }

        _weaponAnim = weapon.GetCurrentWeaponAnimatable();
        _currentPhase = ShootPhase.Idle;
        _needsReload = false;

        Debug.Log($"[Range] Entered - Fire Mode: {_rangeData.Mode}");
    }

    public void Tick(NetworkInputData input)
    {
        switch (_currentPhase)
        {
            case ShootPhase.Idle: UpdateIdlePhase(input); break;
            case ShootPhase.Shooting: UpdateShootingPhase(input); break;
            case ShootPhase.AutomaticFire: UpdateAutomaticFirePhase(input); break;
            case ShootPhase.Reloading: UpdateReloadingPhase(input); break;
        }

        UpdateMovement();
    }

    public void Exit()
    {
        if (_sm.Animator != null)
        {
            _sm.Animator.SetBool("IsReloading", false);
            _sm.Animator.SetBool("IsShooting", false); 
            _sm.Animator.SetFloat("speed", 0f);

            _sm.Animator.ResetTrigger("Shoot");
        }

        Debug.Log("[Range] Exited");
    }

    // ==================== IDLE ====================

    private void UpdateIdlePhase(NetworkInputData input)
    {
        if (!input.attackRange)
            _attackButtonReleased = true;

        bool canShoot = _rangeData.RequireReleaseToShootAgain ? _attackButtonReleased : true;

        if (input.attack && canShoot)
            StartShooting();
        else if (!input.attack)
            _sm.ChangeState(new PlayerIdleState(_sm));
    }

    // ==================== SHOOTING ====================

    private void StartShooting()
    {
        _currentPhase = ShootPhase.Shooting;
        _shootTickTimer = TickTimer.CreateFromSeconds(_sm.Runner, _rangeData.ShootDuration);
        _projectileSpawned = false;
        _shellEjectionSpawned = false;
        _fireEjectionSpawned = false;
        _attackButtonReleased = false;

        RotateToShootDirection();
        _sm.GetComponent<PlayerNetworkSync>()?.TriggerShoot();
        _weaponAnim?.PlayShoot();

        Debug.Log("[Range] Started shooting");
    }

    private void UpdateShootingPhase(NetworkInputData input)
    {
        float elapsed = _rangeData.ShootDuration
                - (_shootTickTimer.RemainingTime(_sm.Runner) ?? 0f);

        ExecuteShootTimedEvents(elapsed);

        if (_shootTickTimer.Expired(_sm.Runner))
            OnShootAnimationEnd(input);
    }

    private void OnShootAnimationEnd(NetworkInputData input)
    {
        if (_rangeData.Mode == FireMode.Automatic)
        {
            if (input.attackRange)
            {
                _currentPhase = ShootPhase.AutomaticFire;
                _fireRateTickTimer = TickTimer.CreateFromSeconds(_sm.Runner, _rangeData.FireRate);
                _continuousFireTickTimer = TickTimer.CreateFromSeconds(_sm.Runner, _rangeData.MaxContinuousFireTime);
                Debug.Log("[Range] Entering automatic fire mode");
            }
            else
            {
                StartReloading();
            }
        }
        else
        {
            StartReloading();
        }
    }

    /// <summary>
    /// Ejecuta los eventos de VFX y proyectil según los tiempos embebidos en cada AttackVFXConfig.
    /// </summary>
    private void ExecuteShootTimedEvents(float elapsed)
    {
        // Shell ejection — tiempo viene de ShellEjectionVFX.vfxSpawnTime
        if (!_shellEjectionSpawned
            && _rangeData.ShellEjectionVFX != null
            && elapsed >= _rangeData.ShellEjectionVFX.vfxSpawnTime)
        {
            _shellEjectionSpawned = true;
            SpawnShellEjectionVFX();
        }

        // Fire ejection — tiempo viene de FireEjectionVFX.vfxSpawnTime
        if (!_fireEjectionSpawned
            && _rangeData.FireEjectionVFX != null
            && elapsed >= _rangeData.FireEjectionVFX.vfxSpawnTime)
        {
            _fireEjectionSpawned = true;
            SpawnFireEjectionVFX();
        }

        // Proyectil — tiempo propio del data
        if (!_projectileSpawned && elapsed >= _rangeData.ShootFrameTime)
        {
            _projectileSpawned = true;
            SpawnProjectile();
        }
    }

    // ==================== AUTOMATIC FIRE ====================

    private void UpdateAutomaticFirePhase(NetworkInputData input)
    {
        if (!input.attack)
        {
            Debug.Log("[Range] Button released - Starting reload");
            StartReloading();
            return;
        }

        if (_rangeData.MaxContinuousFireTime > 0f
            && _continuousFireTickTimer.Expired(_sm.Runner))
        {
            Debug.Log("[Range] Max continuous fire time reached - Forcing reload");
            _needsReload = true;
            StartReloading();
            return;
        }

        if (_fireRateTickTimer.Expired(_sm.Runner))
        {
            _fireRateTickTimer = TickTimer.CreateFromSeconds(_sm.Runner, _rangeData.FireRate);
            _projectileSpawned = false;
            _shellEjectionSpawned = false;
            _fireEjectionSpawned = false;

            SpawnProjectile();
            SpawnShellEjectionVFX();
            SpawnFireEjectionVFX();
            _weaponAnim?.PlayShoot();

            Debug.Log($"[Range] Auto-fire shot");
        }
    }

    // ==================== RELOADING ====================

    private void StartReloading()
    {
        _currentPhase = ShootPhase.Reloading;
        _reloadTickTimer = TickTimer.CreateFromSeconds(_sm.Runner, _rangeData.ReloadDuration);

        if (_sm.Animator != null)
        {
            _sm.Animator.SetBool("IsReloading", true);
            _weaponAnim?.PlayReload();
        }

        Debug.Log($"[Range] Started reloading - Duration: {_rangeData.ReloadDuration}s");
    }
    private void UpdateReloadingPhase(NetworkInputData input)
    {
        if (_reloadTickTimer.Expired(_sm.Runner))
            OnReloadComplete(input);
    }

    private void OnReloadComplete(NetworkInputData input)
    {
        Debug.Log("[Range] Reload complete");

        if (_sm.Animator != null)
            _sm.Animator.SetBool("IsReloading", false);

        _sm.AttackCooldown = TickTimer.CreateFromSeconds(_sm.Runner, _rangeData.Cooldown);

        if (_rangeData.Mode == FireMode.Automatic && input.attack && !_needsReload)
        {
            Debug.Log("[Range] Reload complete - Resuming automatic fire");
            StartShooting();
        }
        else
        {
            _sm.ChangeState(new PlayerIdleState(_sm));
        }
    }

    // ==================== VFX ====================

    private void SpawnShellEjectionVFX()
    {
        if (_rangeData.ShellEjectionVFX == null) return;

        var rage = _sm.GetComponent<PlayerRageHandler>();

        if (rage != null && rage.IsRageActive)
        {
            var rageConfig = rage.RageData?.GetConfigForWeapon(_sm.Combat.CurrentWeapon);
            if (rageConfig?.rageShellEjectionVFX != null)
                _sm.Combat?.SpawnShellEjectionVFX(rageConfig.rageShellEjectionVFX);
            else
                Debug.LogWarning("[Range][Rage] No rage shell ejection VFX configured");
            return;
        }

        _sm.Combat?.SpawnShellEjectionVFX(_rangeData.ShellEjectionVFX);
        Debug.Log("[Range] Shell ejection VFX spawned");
    }

    private void SpawnFireEjectionVFX()
    {
        if (_rangeData.FireEjectionVFX == null) return;

        var rage = _sm.GetComponent<PlayerRageHandler>();

        if (rage != null && rage.IsRageActive)
        {
            var rageConfig = rage.RageData?.GetConfigForWeapon(_sm.Combat.CurrentWeapon);
            if (rageConfig?.rageFireEjectionVFX != null)
                _sm.Combat?.SpawnFireEjectionVFX(rageConfig.rageFireEjectionVFX);
            else
                Debug.LogWarning("[Range][Rage] No rage fire ejection VFX configured");
            return;
        }

        _sm.Combat?.SpawnFireEjectionVFX(_rangeData.FireEjectionVFX);
        Debug.Log("[Range] Fire ejection VFX spawned");
    }

    // ==================== PROJECTILE ====================
    private void SpawnProjectile()
    {
        Debug.Log("[Range] Spawning projectile");

        // El proyectil siempre se spawnea, rage no lo afecta
        if (_sm.Object.HasInputAuthority && !_sm.Object.HasStateAuthority)
            RPC_SpawnProjectile();

        if (_sm.Object.HasStateAuthority)
            ExecuteSpawnProjectile();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    private void RPC_SpawnProjectile()
    {
        ExecuteSpawnProjectile();
    }

    private void ExecuteSpawnProjectile()
    {
        var settings = _sm.Combat;
        if (_rangeData == null) return;

        Vector3 direction = GetShootDirection();

        Vector3 spawnPos = (settings.shootPoints != null && settings.shootPoints.Length > 0)
            ? settings.shootPoints[0].position
            : _sm.transform.position + _sm.transform.forward + Vector3.up;

        PlayerRef attacker = _sm.Object.InputAuthority;

        _sm.Runner.Spawn(
            _rangeData.ProjectilePrefab,
            spawnPos,
            Quaternion.LookRotation(direction),
            attacker,
            onBeforeSpawned: (runner, obj) =>
            {
                var projectile = obj.GetComponent<Projectile>();
                if (projectile != null)
                    projectile.InitServer(direction, _rangeData, attacker, spawnPos);
            }
        );

        Debug.Log($"[Range] Projectile spawned at {spawnPos}");
    }

    // ==================== MOVEMENT & ROTATION ====================

    private void UpdateMovement()
    {
        if (_sm.Animator == null) return;

        float speed = _sm.Player.GetHorizontalSpeed();
        float normalized = speed / _sm.Player.SprintSpeed;

        if (IsLockingInput)
            normalized = 0f;
        else if (_currentPhase == ShootPhase.AutomaticFire)
            normalized *= 0.5f;

        _sm.Animator.SetFloat("speed", normalized);
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

    private Vector3 GetShootDirection()
    {
        if (_sm.LastShootDirection.sqrMagnitude > 0.01f)
            return _sm.LastShootDirection.normalized;

        Camera cam = Camera.main;
        if (cam == null) return _sm.transform.forward;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, 200f))
            return (hit.point - _sm.transform.position).normalized;

        return ray.direction.normalized;
    }
}