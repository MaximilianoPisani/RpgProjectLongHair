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

    private int _lastVFXTick = -1;

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
        _lastVFXTick = -1;

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
        var weapon = _sm.GetComponent<PlayerWeaponHandler>();
        if (weapon == null || !weapon.IsRanged)
        {
            _sm.ChangeState(new PlayerIdleState(_sm));
            return;
        }

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
        // Limpiar el AnimState networked — esto se propaga a todos los clientes
        var sync = _sm.GetComponent<PlayerNetworkSync>();
        if (sync != null)
        {
            sync?.SetIsReloading(false);
            sync?.SetSpeed(0f);
        }

        // También limpiar local por si acaso
        if (_sm.Animator != null)
        {
            _sm.Animator.SetBool("IsReloading", false);
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

        if (input.attackRange && canShoot)
            StartShooting();
        else if (!input.attackRange)
            _sm.ChangeState(new PlayerIdleState(_sm));
    }

    // ==================== SHOOTING ====================

    private void StartShooting()
    {
        _lastVFXTick = -1;
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
        if (!input.attackRange)
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

        RotateToAimDirection(input);

        if (_fireRateTickTimer.Expired(_sm.Runner))
        {
            _fireRateTickTimer = TickTimer.CreateFromSeconds(_sm.Runner, _rangeData.FireRate);
            _projectileSpawned = false;
            _shellEjectionSpawned = false;
            _fireEjectionSpawned = false;

            int currentTick = _sm.Runner.Tick;

            SpawnProjectile();

            if (currentTick != _lastVFXTick)
            {
                _lastVFXTick = currentTick;
                SpawnShellEjectionVFX();
                SpawnFireEjectionVFX();
            }
            _weaponAnim?.PlayShoot();

            Debug.Log($"[Range] Auto-fire shot");
        }
    }

    // ==================== MOVEMENT & ROTATION ====================

    private void RotateToAimDirection(NetworkInputData input)
    {
        Vector3 dir = input.shootDirection;
        Vector3 flatDir = new Vector3(dir.x, 0f, dir.z).normalized;

        if (flatDir.sqrMagnitude > 0.01f)
            _sm.transform.rotation = Quaternion.LookRotation(flatDir);
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

        if (_rangeData.Mode == FireMode.Automatic && input.attackRange && !_needsReload)
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

        Vector3 aimPoint = _sm.InputData.aimPoint;

        Vector3 spawnPos = (settings.shootPoints != null && settings.shootPoints.Length > 0)
            ? settings.shootPoints[0].position
            : _sm.transform.position + _sm.transform.forward + Vector3.up;

        Vector3 direction = (aimPoint - spawnPos).normalized;

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
}