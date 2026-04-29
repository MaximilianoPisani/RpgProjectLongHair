using Fusion;
using UnityEngine;

public class PlayerMeleeState : IPlayerState
{
    private PlayerStateMachine _sm;

    private int _comboIndex = 0;
    private int _lastAttackTick = -1;
    private bool _inputBuffered = false;
    private bool _isAttacking = false;
    private bool _queueNextAttack = false;

    private TickTimer _attackTickTimer;
    private TickTimer _comboResetTickTimer;
    private TickTimer _postComboCooldownTimer;

    private bool _hitFrameExecuted = false;
    private bool _vfxSpawned = false;
    private bool _comboWindowOpened = false;
    private bool _comboWindowClosed = false;

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

        _isAttacking = true;
        StartNextAttack();

        _lastAttackTick = _sm.Runner.Tick;
    }

    public void Tick(NetworkInputData input)
    {
        if (!_postComboCooldownTimer.ExpiredOrNotRunning(_sm.Runner))
        {
            Debug.Log("[Melee] Post-combo cooldown...");
            return;
        }

        if (!_isAttacking)
        {
            float speed = _sm.Player.GetHorizontalSpeed();
            if (speed >= 0.01f)
            {
                _sm.ChangeState(new PlayerMoveState(_sm));
                return;
            }
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

        float speed = _sm.Player.GetHorizontalSpeed();
        float normalizedSpeed = speed / _sm.Player.SprintSpeed;

        if (_sm.Animator != null)
            _sm.Animator.SetFloat("speed", normalizedSpeed);

        if (_comboResetTickTimer.ExpiredOrNotRunning(_sm.Runner))
        {
            if (speed < 0.01f)
            {
                _sm.ChangeState(new PlayerIdleState(_sm));
                return;
            }
            else
            {
                // FIX: si el jugador está en movimiento, salir al estado de locomoción
                _sm.ChangeState(new PlayerMoveState(_sm));
                return;
            }
        }
    }

    // ==================== ATTACK LOGIC ====================

    private void UpdateAttackLogic(NetworkInputData input)
    {
        if (_currentAttackConfig == null) return;

        float elapsed = _currentAttackConfig.attackDuration
                      - (_attackTickTimer.RemainingTime(_sm.Runner) ?? 0f);

        ExecuteTimedEvents(elapsed);

        bool attackPressed = input.attackJustPressed;
        int currentTick = _sm.Runner.Tick;
        if (attackPressed && !_inputBuffered && currentTick != _lastAttackTick)
        {
            _lastAttackTick = currentTick;
            HandleAttackInput();
        }

        if (_attackTickTimer.Expired(_sm.Runner))
            EndCurrentAttack();
    }

    private void ExecuteTimedEvents(float elapsed)
    {
        if (!_vfxSpawned
            && _currentAttackConfig.attackVFX != null
            && elapsed >= _currentAttackConfig.attackVFX.vfxSpawnTime)
        {
            _vfxSpawned = true;
            SpawnSlashVFX();
        }

        if (!_hitFrameExecuted && elapsed >= _currentAttackConfig.hitFrameTime)
        {
            _hitFrameExecuted = true;
            ExecuteHitFrame();
        }

        if (!_comboWindowOpened && elapsed >= _currentAttackConfig.comboWindowOpenTime)
        {
            _comboWindowOpened = true;
            OpenComboWindow();
        }

        if (!_comboWindowClosed && elapsed >= _currentAttackConfig.comboWindowCloseTime)
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

        _attackTickTimer = TickTimer.CreateFromSeconds(_sm.Runner, _currentAttackConfig.attackDuration);
        _comboResetTickTimer = TickTimer.CreateFromSeconds(_sm.Runner, _meleeData.ComboResetTime);

        _lastAttackTick = -1;
        _hitFrameExecuted = false;
        _vfxSpawned = false;
        _comboWindowOpened = false;
        _comboWindowClosed = false;
        _inputBuffered = false;

        _sm.NetworkedComboIndex = _comboIndex;

        if (_sm.Animator != null)
        {
            _sm.Animator.SetFloat("speed", 0f);
            _sm.Animator.SetInteger("ComboIndex", _comboIndex);
        }

        _sm.GetComponent<PlayerNetworkSync>()?.TriggerMelee();

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

        bool isLastAttack = _comboIndex >= _meleeData.MaxComboCount;
        if (isLastAttack)
        {
            _isAttacking = false;
            _currentAttackConfig = null;
            StartPostComboCooldown();
            return;
        }

        _isAttacking = false;
        _currentAttackConfig = null;
    }

    private void StartPostComboCooldown()
    {
        _postComboCooldownTimer = TickTimer.CreateFromSeconds(_sm.Runner, _meleeData.Cooldown);
        ResetCombo();

        if (_sm.Animator != null)
            _sm.Animator.SetInteger("ComboIndex", 0);

        Debug.Log($"[Melee] Post-combo cooldown iniciado: {_meleeData.Cooldown}s");
    }

    private void ResetCombo()
    {
        _comboIndex = 0;
        _inputBuffered = false;
        _queueNextAttack = false;

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

        var rage = _sm.GetComponent<PlayerRageHandler>();

        if (rage != null && rage.IsRageActive)
        {
            var rageConfig = rage.RageData?.GetConfigForWeapon(_sm.Combat.CurrentWeapon);
            var rageVFX = rageConfig?.GetComboVFX(_comboIndex);

            if (rageVFX != null)
                _sm.Combat?.SpawnSlashVFX(rageVFX);
            else
                Debug.LogWarning($"[Melee][Rage] No rage VFX for combo {_comboIndex}");

            return;
        }

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
                int damage = _currentAttackConfig.damage;

                var rage = _sm.GetComponent<PlayerRageHandler>();
                if (rage != null)
                    damage = Mathf.RoundToInt(damage * rage.GetDamageMultiplier());

                enemyHealth.ApplyDamageServer(damage, _sm.Object.InputAuthority);
                PlayerRageHandler.NotifyDamageDealt(_sm.Object.InputAuthority, damage);

                Debug.Log($"[Melee] Hit enemy - Damage: {damage}");
            }
        }

        PlayHitFeedback();
    }

    private void PlayHitFeedback() { }
}