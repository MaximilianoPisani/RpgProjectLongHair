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
            UpdateLocomotion(input);
        else
            UpdateAttackLogic(input);
    }

    public void Exit()
    {
        ResetCombo();

        if (_sm.Animator != null)
            _sm.Animator.SetFloat("speed", 0f);
    }

    // ==================== LOCOMOTION ====================

    private void UpdateLocomotion(NetworkInputData input)
    {
        var weapon = _sm.GetComponent<PlayerWeaponHandler>();

        if (weapon == null || !weapon.IsMelee)
        {
            _sm.ChangeState(new PlayerIdleState(_sm));
            return;
        }

        bool attackPressed = input.attack && !_lastAttackInput;
        _lastAttackInput = input.attack;

        if (attackPressed)
        {
            _isAttacking = true;
            StartNextAttack();
            return;
        }

        if (_comboResetTimer > 0f)
        {
            _comboResetTimer -= _sm.Runner.DeltaTime;
            if (_comboResetTimer <= 0f)
                ResetCombo();
        }

        float speed = _sm.Player.GetHorizontalSpeed();
        float normalizedSpeed = speed / _sm.Player.SprintSpeed;

        if (_sm.Animator != null)
            _sm.Animator.SetFloat("speed", normalizedSpeed);

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

        _attackTimer += _sm.Runner.DeltaTime;

        ExecuteTimedEvents();

        bool attackPressed = input.attack && !_lastAttackInput;
        _lastAttackInput = input.attack;

        if (attackPressed && !_inputBuffered)
            HandleAttackInput();

        if (_attackTimer >= _currentAttackConfig.attackDuration)
            EndCurrentAttack();
    }

    private void ExecuteTimedEvents()
    {
        // VFX — usa el spawn time del AttackVFXConfig embebido en el combo
        if (!_vfxSpawned
            && _currentAttackConfig.attackVFX != null
            && _attackTimer >= _currentAttackConfig.attackVFX.vfxSpawnTime)
        {
            _vfxSpawned = true;
            SpawnSlashVFX();
        }

        if (!_hitFrameExecuted && _attackTimer >= _currentAttackConfig.hitFrameTime)
        {
            _hitFrameExecuted = true;
            ExecuteHitFrame();
        }

        if (!_comboWindowOpened && _attackTimer >= _currentAttackConfig.comboWindowOpenTime)
        {
            _comboWindowOpened = true;
            OpenComboWindow();
        }

        if (!_comboWindowClosed && _attackTimer >= _currentAttackConfig.comboWindowCloseTime)
        {
            _comboWindowClosed = true;
            CloseComboWindow();
        }
    }

    private void StartNextAttack()
    {
        _comboIndex++;
        if (_comboIndex > _meleeData.MaxComboCount)
            _comboIndex = 1;

        _currentAttackConfig = _meleeData.ComboAttacks[_comboIndex - 1];

        _attackTimer = 0f;
        _hitFrameExecuted = false;
        _vfxSpawned = false;
        _comboWindowOpened = false;
        _comboWindowClosed = false;
        _inputBuffered = false;
        _comboResetTimer = _meleeData.ComboResetTime;

        _sm.NetworkedComboIndex = _comboIndex;

        if (_sm.Animator != null)
        {
            _sm.Animator.SetFloat("speed", 0f);
            _sm.Animator.SetInteger("ComboIndex", _comboIndex);

            if (_comboIndex == 1)
                _sm.GetComponent<PlayerNetworkSync>()?.TriggerMelee();
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

        if (_comboWindowOpened && !_comboWindowClosed)
        {
            _queueNextAttack = true;
            Debug.Log("[Melee] Combo window open - Queuing next attack");
            return;
        }

        if (!_inputBuffered)
        {
            _inputBuffered = true;
            Debug.Log("[Melee] Attack buffered");
        }
    }

    private void EndCurrentAttack()
    {
        Debug.Log($"[Melee] Attack {_comboIndex} ended - Queued: {_queueNextAttack}");

        if (_queueNextAttack)
        {
            _queueNextAttack = false;
            StartNextAttack();
            return;
        }

        _isAttacking = false;
        _currentAttackConfig = null;
    }

    private void ResetCombo()
    {
        _comboIndex = 0;
        _inputBuffered = false;
        _comboResetTimer = 0f;

        if (_sm.Animator != null)
            _sm.Animator.SetInteger("ComboIndex", 0);

        Debug.Log("[Melee] Combo reset");
    }

    // ==================== COMBO WINDOW ====================

    private void OpenComboWindow()
    {
        Debug.Log($"[Melee] Combo window opened (Attack {_comboIndex})");

        if (_inputBuffered)
        {
            _inputBuffered = false;
            _queueNextAttack = true;
            Debug.Log("[Melee] Buffered input detected - Queuing next attack");
        }
    }

    private void CloseComboWindow()
    {
        Debug.Log($"[Melee] Combo window closed (Attack {_comboIndex})");

        if (_inputBuffered)
        {
            _inputBuffered = false;
            Debug.Log("[Melee] Input buffered but window closed - Input discarded");
        }
    }

    // ==================== VFX ====================

    private void SpawnSlashVFX()
    {
        if (_currentAttackConfig?.attackVFX == null) return;

        _sm.Combat?.SpawnSlashVFX(_currentAttackConfig.attackVFX);

        Debug.Log($"[Melee] VFX spawned for combo {_comboIndex}");
    }

    // ==================== HIT DETECTION ====================

    private void ExecuteHitFrame()
    {
        Debug.Log($"[Melee] Hit frame executed (Attack {_comboIndex})");

        if (!_sm.Object.HasInputAuthority) return;

        if (_sm.Object.HasStateAuthority)
            ApplyMeleeDamage();
        else
            _sm.RPC_RequestMeleeDamage(
                _sm.transform.position,
                _sm.transform.forward,
                _currentAttackConfig.damage
            );
    }

    private void ApplyMeleeDamage()
    {
        var settings = _sm.Combat;
        if (settings == null) return;

        Vector3 origin = settings.meleeOrigin != null
            ? settings.meleeOrigin.position
            : _sm.transform.position + _sm.transform.forward * 0.5f + Vector3.up;

        Collider[] hits = Physics.OverlapSphere(origin, _meleeData.HitRadius, settings.enemyLayer);

        foreach (var hit in hits)
        {
            var enemyHealth = hit.GetComponentInParent<EnemyHealth>();

            if (enemyHealth != null && _sm.Object.HasStateAuthority)
            {
                enemyHealth.ApplyDamageServer(_currentAttackConfig.damage, _sm.Object.InputAuthority);
                Debug.Log($"[Melee] Hit enemy - Damage: {_currentAttackConfig.damage}");
            }
        }

        PlayHitFeedback();
    }

    private void PlayHitFeedback()
    {
        // TODO: sonido, vibración, etc.
    }
}