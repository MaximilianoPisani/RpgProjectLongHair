using Fusion;
using UnityEngine;

public class PlayerRangeState : IPlayerState
{
    private PlayerStateMachine _sm;
    private RangedAttackData _rangeData;

    private IWeaponAnimatable _weaponAnim;

    public bool IsLockingInput =>
    _currentPhase == ShootPhase.Shooting ||
    _currentPhase == ShootPhase.Reloading ||
    _currentPhase == ShootPhase.AutomaticFire;


    // Estado del disparo
    private enum ShootPhase
    {
        Idle,           // Esperando input
        Shooting,       // Ejecutando animación de disparo
        Reloading,      // Recargando
        AutomaticFire   // Disparando en modo automático (loop)
    }

    private ShootPhase _currentPhase = ShootPhase.Idle;

    // Timers
    private float _shootTimer = 0f;
    private float _reloadTimer = 0f;
    private float _fireRateTimer = 0f;
    private float _continuousFireTimer = 0f;

    // Flags
    private bool _projectileSpawned = false;
    private bool _attackButtonReleased = true;
    private bool _needsReload = false;

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
        _rangeData = settings?.GetCurrentRangeData();

        if (_rangeData == null)
        {
            Debug.LogError("[Range] RangedAttackData no configurado!");
            _sm.ChangeState(new PlayerIdleState(_sm));
            return;
        }

        // Verificar cooldown
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
            case ShootPhase.Idle:
                UpdateIdlePhase(input);
                break;

            case ShootPhase.Shooting:
                UpdateShootingPhase(input);
                break;

            case ShootPhase.AutomaticFire:
                UpdateAutomaticFirePhase(input);
                break;

            case ShootPhase.Reloading:
                UpdateReloadingPhase(input);
                break;
        }

        UpdateMovement();
    }

    public void Exit()
    {
        if (_sm.Animator != null)
        {
            _sm.Animator.SetBool("IsReloading", false);
            _sm.Animator.SetFloat("speed", 0f);
        }

        Debug.Log("[Range] Exited");
    }

    // ==================== IDLE PHASE ====================

    private void UpdateIdlePhase(NetworkInputData input)
    {
        // Detectar si se soltó el botón
        if (!input.attackRange)
        {
            _attackButtonReleased = true;
        }

        // Verificar si puede disparar
        bool canShoot = _rangeData.RequireReleaseToShootAgain
            ? _attackButtonReleased
            : true;

        if (input.attackRange && canShoot)
        {
            StartShooting();
        }
        else if (!input.attackRange)
        {
            // Si no hay input, volver a idle
            _sm.ChangeState(new PlayerIdleState(_sm));
        }
    }

    // ==================== SHOOTING PHASE ====================

    private void StartShooting()
    {
        _currentPhase = ShootPhase.Shooting;
        _shootTimer = 0f;
        _projectileSpawned = false;
        _attackButtonReleased = false;

        RotateToShootDirection();

        // Trigger de animación
        _sm.GetComponent<PlayerNetworkSync>()?.TriggerShoot();

        _weaponAnim?.PlayShoot();

        Debug.Log("[Range] Started shooting");
    }

    private void UpdateShootingPhase(NetworkInputData input)
    {
        _shootTimer += _sm.Runner.DeltaTime;

        // Spawn del proyectil en el momento exacto
        if (!_projectileSpawned && _shootTimer >= _rangeData.ShootFrameTime)
        {
            _projectileSpawned = true;
            SpawnProjectile();
        }

        // Fin de la animación de disparo
        if (_shootTimer >= _rangeData.ShootDuration)
        {
            OnShootAnimationEnd(input);
        }
    }

    private void OnShootAnimationEnd(NetworkInputData input)
    {
        Debug.Log($"[Range] Shoot animation ended - Mode: {_rangeData.Mode}");

        // Decidir qué hacer según el modo de fuego
        if (_rangeData.Mode == FireMode.Automatic)
        {
            // Si sigue presionado, continuar disparando
            if (input.attackRange)
            {
                _currentPhase = ShootPhase.AutomaticFire;
                _fireRateTimer = 0f;
                _continuousFireTimer = 0f;
                Debug.Log("[Range] Entering automatic fire mode");
            }
            else
            {
                StartReloading();
            }
        }
        else // SingleShot
        {
            StartReloading();
        }
    }

    // ==================== AUTOMATIC FIRE PHASE ====================

    private void UpdateAutomaticFirePhase(NetworkInputData input)
    {
        _fireRateTimer += _sm.Runner.DeltaTime;
        _continuousFireTimer += _sm.Runner.DeltaTime;

        // Verificar si se soltó el botón
        if (!input.attackRange)
        {
            Debug.Log("[Range] Button released - Starting reload");
            StartReloading();
            return;
        }

        // Verificar tiempo máximo de disparo continuo
        if (_rangeData.MaxContinuousFireTime > 0f &&
            _continuousFireTimer >= _rangeData.MaxContinuousFireTime)
        {
            Debug.Log("[Range] Max continuous fire time reached - Forcing reload");
            _needsReload = true;
            StartReloading();
            return;
        }

        // Disparar según la cadencia
        if (_fireRateTimer >= _rangeData.FireRate)
        {
            _fireRateTimer = 0f;
            _projectileSpawned = false;
            _shootTimer = 0f;

            // Reiniciar el ciclo de disparo
            SpawnProjectile();

            _weaponAnim?.PlayShoot();

            Debug.Log($"[Range] Auto-fire shot - Continuous time: {_continuousFireTimer:F2}s");
        }
    }

    // ==================== RELOADING PHASE ====================

    private void StartReloading()
    {
        _currentPhase = ShootPhase.Reloading;
        _reloadTimer = 0f;

        if (_sm.Animator != null)
        {
            _sm.Animator.SetBool("IsReloading", true);
            _weaponAnim?.PlayReload();
        }

        Debug.Log($"[Range] Started reloading - Duration: {_rangeData.ReloadDuration}s");
    }

    private void UpdateReloadingPhase(NetworkInputData input)
    {
        _reloadTimer += _sm.Runner.DeltaTime;

        if (_reloadTimer >= _rangeData.ReloadDuration)
        {
            OnReloadComplete(input);
        }
    }

    private void OnReloadComplete(NetworkInputData input)
    {
        Debug.Log("[Range] Reload complete");

        if (_sm.Animator != null)
        {
            _sm.Animator.SetBool("IsReloading", false);
        }

        // Setear cooldown
        _sm.AttackCooldown = TickTimer.CreateFromSeconds(
            _sm.Runner,
            _rangeData.Cooldown
        );

        // Si es automático y sigue presionado (y no necesita reload forzado), continuar disparando
        if (_rangeData.Mode == FireMode.Automatic && input.attackRange && !_needsReload)
        {
            Debug.Log("[Range] Reload complete - Resuming automatic fire");
            StartShooting();
        }
        else
        {
            // Volver a idle
            _sm.ChangeState(new PlayerIdleState(_sm));
        }
    }

    // ==================== PROJECTILE SPAWNING ====================

    private void SpawnProjectile()
    {
        Debug.Log("[Range] Spawning projectile");

        if (_sm.Object.HasInputAuthority && !_sm.Object.HasStateAuthority)
        {
            RPC_SpawnProjectile();
        }

        if (_sm.Object.HasStateAuthority)
        {
            ExecuteSpawnProjectile();
        }
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
                {
                    projectile.InitServer(direction, _rangeData, attacker, spawnPos);
                }
            }
        );

        Debug.Log($"[Range] Projectile spawned at {spawnPos}");
    }

    // ==================== MOVEMENT & ROTATION ====================

    private void UpdateMovement()
    {
        if (_sm.Animator == null) return;

        if (IsLockingInput)
        {
            _sm.Animator.SetFloat("speed", 0f);
            _sm.Animator.SetBool("isMoving", false);
            return;
        }

        float speed = _sm.Player.GetHorizontalSpeed();
        float normalized = speed / _sm.Player.SprintSpeed;
        _sm.Animator.SetFloat("speed", normalized);
        _sm.Animator.SetBool("isMoving", speed > 0.05f);
    }

    private void RotateToShootDirection()
    {
        Vector3 direction = _sm.LastShootDirection.sqrMagnitude > 0.01f
            ? _sm.LastShootDirection
            : _sm.transform.forward;

        Vector3 flatDir = new Vector3(direction.x, 0f, direction.z).normalized;

        if (flatDir.sqrMagnitude > 0.01f)
        {
            _sm.transform.rotation = Quaternion.LookRotation(flatDir);
        }
    }

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
}