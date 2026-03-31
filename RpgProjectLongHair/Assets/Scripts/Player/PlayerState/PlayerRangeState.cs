using Fusion;
using UnityEngine;

public class PlayerRangeState : IPlayerState, IAnimationEventReceiver
{
    private PlayerStateMachine _sm;

    private enum ShootPhase
    {
        Shooting,   // Reproduciendo animación de disparo
        Reloading   // Reproduciendo animación de recarga
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
        if (settings.rangeData == null || settings.rangeData.ProjectilePrefab == null)
        {
            Debug.LogError("RangedData o ProjectilePrefab no configurado");
            _sm.ChangeState(new PlayerIdleState(_sm));
            return;
        }

        if (!_sm.AttackCooldown.ExpiredOrNotRunning(_sm.Runner))
        {
            Debug.Log("Arma en cooldown");
            _sm.ChangeState(new PlayerIdleState(_sm));
            return;
        }
        _currentPhase = ShootPhase.Shooting;
        _hasSpawnedProjectile = false;

        RotateToShootDirection();

         if (_sm.Animator != null)
         {
            _sm.Animator.SetTrigger("Shoot");
            _sm.Animator.SetBool("IsReloading", false);
         }

        Debug.Log("Iniciando disparo");

    }

    public void Tick(NetworkInputData input)
    {
        HandleGunMovement();
    }

    public void Exit()
    {
        // Limpiar parámetros del animator
        if (_sm.Animator != null)
        {
            _sm.Animator.SetBool("IsReloading", false);
        }

        Debug.Log("Saliendo del estado ranged");
    }

    // ==================== EVENTOS DE ANIMACIÓN ====================

    public void OnShootFrame()
    {
        if (_hasSpawnedProjectile) return; // Evitar spawns duplicados

        _hasSpawnedProjectile = true;
        SpawnProjectile();

        Debug.Log("Proyectil disparado");
    }

    public void OnShootAnimationEnd()
    {
        // Transicionar a recarga
        _currentPhase = ShootPhase.Reloading;

        if (_sm.Animator != null)
        {
            _sm.Animator.SetBool("IsReloading", true);
        }

        Debug.Log("Iniciando recarga");
    }

    // Llamado al final de la animación de recarga
    public void OnReloadComplete()
    {
        var settings = _sm.Combat;

        // Activar cooldown después de completar la recarga
        _sm.AttackCooldown = TickTimer.CreateFromSeconds(
            _sm.Runner,
            settings.rangeData.Cooldown
        );

        Debug.Log($"Recarga completa - Cooldown: {settings.rangeData.Cooldown}s");

        // Volver a idle
        _sm.ChangeState(new PlayerIdleState(_sm));
    }

    public void OpenComboWindow() { }
    public void CloseComboWindow() { }
    public void EndAttack() { }
    public void OnHitFrame() { }

    // ==================== LÓGICA DE DISPARO ====================

    private void SpawnProjectile()
    {
        var settings = _sm.Combat;

        // Obtener dirección de disparo
        Vector3 direction = GetShootDirection();

        // Obtener posición de spawn
        Vector3 spawnPos = (settings.shootPoints != null && settings.shootPoints.Length > 0)
            ? settings.shootPoints[0].position
            : _sm.transform.position + _sm.transform.forward + Vector3.up;

        // Spawn del proyectil
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

        // Opcional: Feedback visual/audio
        PlayShootFeedback();
    }

    private Vector3 GetShootDirection()
    {
        // Si tenemos dirección guardada del input, usarla
        if (_sm.LastShootDirection.sqrMagnitude > 0.01f)
        {
            return _sm.LastShootDirection.normalized;
        }

        // Sino, disparar desde el centro de la cámara
        Camera cam = Camera.main;
        if (cam == null)
            return _sm.transform.forward;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, 200f))
        {
            Vector3 dir = (hit.point - _sm.transform.position).normalized;
            return dir;
        }

        return ray.direction.normalized;
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

    private void HandleGunMovement()
    {
        if (_sm.Animator == null) return;

        if (_currentPhase == ShootPhase.Shooting)
        {
            _sm.Animator.SetFloat("Speed", 0f);
            return;
        }

        var player = _sm.GetComponent<Player>();
        if (player == null) return;

        float speed = player.GetHorizontalSpeed();

        // Normalizar para tu blend tree (0 , 1)
        float normalized = speed / player.SprintSpeed;
        float normalizedSpeed = speed / _sm.Player.SprintSpeed;

        if (_sm.Animator != null)
        {
            _sm.Animator.SetFloat("speed", normalized);
            _sm.Animator.SetBool("isMoving", speed > 0.05f);
        }
    }
    private void PlayShootFeedback()
    {
        // TODO: 
        // Sonido de disparo
        // Efecto de muzzle flash
        // Vibración del controlador
        // Efecto de retroceso de cámara
    }
}