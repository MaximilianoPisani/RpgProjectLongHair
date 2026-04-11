using Fusion;
using UnityEngine;

public class PlayerMeleeState : IPlayerState
{
    private PlayerStateMachine _sm;

    // Estado del combo
    private int _comboIndex = 0;
    private bool _inputBuffered = false;
    private bool _isAttacking = false;
    private bool _queueNextAttack = false;

    // Timers para el ataque actual
    private float _attackTimer = 0f;
    private float _comboResetTimer = 0f;

    // Flags para eventos ya ejecutados
    private bool _hitFrameExecuted = false;
    private bool _vfxSpawned = false;
    private bool _comboWindowOpened = false;
    private bool _comboWindowClosed = false;
    private bool _lastAttackInput = false;

    // Referencia a los datos de melee
    private MeleeAttackData _meleeData;
    private ComboAttackConfig _currentAttackConfig;

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

        // Obtener referencia a MeleeData
        _meleeData = _sm.Combat?.meleeData as MeleeAttackData;
        if (_meleeData == null)
        {
            Debug.LogError("MeleeAttackData no configurado!");
            _sm.ChangeState(new PlayerIdleState(_sm));
            return;
        }

        _isAttacking = false;
        _comboResetTimer = 0f;
    }

    public void Tick(NetworkInputData input)
    {
        if (!_isAttacking)
        {
            UpdateLocomotion(input);
        }
        else
        {
            UpdateAttackLogic(input);
        }
    }

    public void Exit()
    {
        ResetCombo();

        if (_sm.Animator != null)
        {
            _sm.Animator.SetFloat("speed", 0f);
        }
    }

    // ==================== LOCOMOTION ====================

    private void UpdateLocomotion(NetworkInputData input)
    {
        var weapon = _sm.GetComponent<PlayerWeaponHandler>();

        // Si cambia de arma, salir del estado
        if (weapon == null || !weapon.IsMelee)
        {
            _sm.ChangeState(new PlayerIdleState(_sm));
            return;
        }

        bool attackPressed = input.attack && !_lastAttackInput;
        _lastAttackInput = input.attack;

        // Si ataca, iniciar combo
        if (attackPressed)
        {
            _isAttacking = true;
            StartNextAttack();
            return;
        }

        // Actualizar timer de reset de combo
        if (_comboResetTimer > 0f)
        {
            _comboResetTimer -= _sm.Runner.DeltaTime;
            if (_comboResetTimer <= 0f)
            {
                ResetCombo();
            }
        }

        // Calcular velocidad
        float speed = _sm.Player.GetHorizontalSpeed();
        float normalizedSpeed = speed / _sm.Player.SprintSpeed;

        if (_sm.Animator != null)
        {
            _sm.Animator.SetFloat("speed", normalizedSpeed);
        }

        // Si no se mueve, volver a idle
        if (speed < 0.01f)
        {
            _sm.ChangeState(new PlayerIdleState(_sm));
            return;
        }
    }

    // ==================== ATTACK LOGIC ====================

    private void UpdateAttackLogic(NetworkInputData input)
    {
        if (_currentAttackConfig == null) return;

        // Incrementar timer del ataque
        _attackTimer += _sm.Runner.DeltaTime;

        // Ejecutar eventos basados en tiempo
        ExecuteTimedEvents();

        bool attackPressed = input.attack && !_lastAttackInput;
        _lastAttackInput = input.attack;
        // Procesar input de ataque
        if (attackPressed && !_inputBuffered)
        {
            HandleAttackInput();
        }

        // Verificar si el ataque terminó
        if (_attackTimer >= _currentAttackConfig.attackDuration)
        {
            EndCurrentAttack();
        }
    }

    private void ExecuteTimedEvents()
    {
        // VFX Spawn - Ejecutar ANTES del hit frame para mejor feedback visual
        if (!_vfxSpawned && _attackTimer >= _currentAttackConfig.vfxSpawnTime)
        {
            _vfxSpawned = true;
            SpawnSlashVFX();
        }

        // Hit Frame - Aplicar daño
        if (!_hitFrameExecuted && _attackTimer >= _currentAttackConfig.hitFrameTime)
        {
            _hitFrameExecuted = true;
            ExecuteHitFrame();
        }

        // Abrir ventana de combo
        if (!_comboWindowOpened && _attackTimer >= _currentAttackConfig.comboWindowOpenTime)
        {
            _comboWindowOpened = true;
            OpenComboWindow();
        }

        // Cerrar ventana de combo
        if (!_comboWindowClosed && _attackTimer >= _currentAttackConfig.comboWindowCloseTime)
        {
            _comboWindowClosed = true;
            CloseComboWindow();
        }
    }

    private void StartNextAttack()
    {
        // Incrementar combo
        _comboIndex++;
        if (_comboIndex > _meleeData.MaxComboCount)
        {
            _comboIndex = 1; // Reiniciar combo
        }

        // Obtener configuración del ataque actual
        _currentAttackConfig = _meleeData.ComboAttacks[_comboIndex - 1];

        // Resetear flags y timers
        _attackTimer = 0f;
        _hitFrameExecuted = false;
        _vfxSpawned = false;
        _comboWindowOpened = false;
        _comboWindowClosed = false;
        _inputBuffered = false;
        _comboResetTimer = _meleeData.ComboResetTime;

        // Sincronizar por red
        _sm.NetworkedComboIndex = _comboIndex;

        // Actualizar animator
        if (_sm.Animator != null)
        {
            _sm.Animator.SetFloat("speed", 0f);
            _sm.Animator.SetInteger("ComboIndex", _comboIndex);

            // Solo trigger en el primer ataque
            if (_comboIndex == 1)
            {
                _sm.GetComponent<PlayerNetworkSync>()?.TriggerMelee();
            }
        }

        Debug.Log($"[Melee] Combo Attack {_comboIndex} started - Duration: {_currentAttackConfig.attackDuration}s");
    }

    private void HandleAttackInput()
    {
        if (_comboIndex >= _meleeData.MaxComboCount)
        {
            Debug.Log("[Melee] Max combo reached - Input ignored");
            return;
        }

        // Si la ventana está abierta, atacar inmediatamente
        if (_comboWindowOpened && !_comboWindowClosed)
        {
            Debug.Log("[Melee] Combo window open - Executing next attack");
            _queueNextAttack = true;
            return;
        }
        // Si no, almacenar input para cuando se abra
        else if (!_inputBuffered)
        {
            _inputBuffered = true;
            Debug.Log("[Melee] Attack buffered");
        }
    }

    private void EndCurrentAttack()
    {
        Debug.Log($"[Melee] Attack {_comboIndex} ended - Buffered: {_inputBuffered}");

        // Si hay input buffereado, se ejecutará en la siguiente ventana
        // pero esto no debería pasar porque ya deberíamos haber iniciado el siguiente ataque
        if (_queueNextAttack)
        {
            Debug.Log("Ejecutando siguiente ataque DESPUÉS de terminar animación");

            _queueNextAttack = false;
            StartNextAttack();
            return;
        }

        // Volver a modo locomoción
        _isAttacking = false;
        _currentAttackConfig = null;
    }

    private void ResetCombo()
    {
        _comboIndex = 0;
        _inputBuffered = false;
        _comboResetTimer = 0f;

        if (_sm.Animator != null)
        {
            _sm.Animator.SetInteger("ComboIndex", 0);
        }

        Debug.Log("[Melee] Combo reset");
    }

    // ==================== COMBO WINDOW LOGIC ====================

    private void OpenComboWindow()
    {
        Debug.Log($"[Melee] Combo window opened (Attack {_comboIndex})");

        // Si había input buffereado, ejecutarlo
        if (_inputBuffered)
        {
            Debug.Log("[Melee] Buffered input detected - Executing next attack");
            _inputBuffered = false;
            _queueNextAttack = true;
        }
    }

    private void CloseComboWindow()
    {
        Debug.Log($"[Melee] Combo window closed (Attack {_comboIndex})");

        // Si había input buffereado pero se cerró la ventana, descartarlo
        if (_inputBuffered)
        {
            Debug.Log("[Melee] Input buffered but window closed - Input discarded");
            _inputBuffered = false;
        }
    }

    // ==================== VFX ====================

    private void SpawnSlashVFX()
    {
        if (_currentAttackConfig == null || _currentAttackConfig.slashVFXPrefab == null)
        {
            return;
        }

        // Solo spawner VFX localmente (no es necesario sincronizar por red)
        // Cada cliente spawneará su propio VFX
        _sm.Combat?.SpawnSlashVFX(_currentAttackConfig.slashVFXPrefab);

        Debug.Log($"[Melee] VFX spawned for combo {_comboIndex}");
    }

    // ==================== HIT DETECTION ====================

    private void ExecuteHitFrame()
    {
        Debug.Log($"[Melee] Hit frame executed (Attack {_comboIndex})");

        if (!_sm.Object.HasInputAuthority) return;

        if (_sm.Object.HasStateAuthority)
        {
            ApplyMeleeDamage();
        }
        else
        {
            _sm.RPC_RequestMeleeDamage(
                _sm.transform.position,
                _sm.transform.forward,
                _currentAttackConfig.damage
            );
        }
    }

    private void ApplyMeleeDamage()
    {
        var settings = _sm.Combat;
        if (settings == null) return;

        Vector3 origin = settings.meleeOrigin != null
            ? settings.meleeOrigin.position
            : _sm.transform.position + _sm.transform.forward * 0.5f + Vector3.up;

        Collider[] hits = Physics.OverlapSphere(
            origin,
            _meleeData.HitRadius,
            settings.enemyLayer
        );

        foreach (var hit in hits)
        {
            var enemyHealth = hit.GetComponentInParent<EnemyHealth>();

            if (enemyHealth != null && _sm.Object.HasStateAuthority)
            {
                enemyHealth.ApplyDamageServer(
                    _currentAttackConfig.damage,
                    _sm.Object.InputAuthority
                );

                Debug.Log($"[Melee] Hit enemy with combo {_comboIndex} - Damage: {_currentAttackConfig.damage}");
            }
        }

        PlayHitFeedback();
    }

    private void PlayHitFeedback()
    {
        // TODO: Efectos visuales, sonido, vibración, etc.
    }
}