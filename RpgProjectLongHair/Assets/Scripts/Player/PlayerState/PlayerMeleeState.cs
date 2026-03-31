using Fusion;
using UnityEngine;

public class PlayerMeleeState : IPlayerState, IAnimationEventReceiver
{
    private PlayerStateMachine _sm;

    private int _comboIndex = 0;
    private bool _comboWindowOpen = false;
    private bool _inputBuffered = false;
    private float _comboTimer;
    private bool _isAttacking = false;

    private const float COMBO_RESET_TIME = 0.5f;
    private const int MAX_COMBO_COUNT = 3;

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

        _isAttacking = false;
    }

    public void Tick(NetworkInputData input)
    {
        if (!_isAttacking)
        {
            UpdateLocomotion(input);
        }
        else
        {
            // Si estamos atacando, actualizar la lógica del combo
            UpdateComboLogic(input);
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

    private void UpdateLocomotion(NetworkInputData input)
    {
        var weapon = _sm.GetComponent<PlayerWeaponHandler>();

        // Si cambia de arma, salir del estado
        if (weapon == null || !weapon.IsMelee)
        {
            _sm.ChangeState(new PlayerIdleState(_sm));
            return;
        }

        // Si ataca, iniciar combo
        if (input.attack)
        {
            _isAttacking = true;
            StartNextAttack();
            return;
        }

        // CALCULAR SPEED (igual que en MoveState)
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

    // ==================== LÓGICA DE COMBO ====================

    private void UpdateComboLogic(NetworkInputData input)
    {
        // Actualizar timer con DeltaTime de Fusion
        _comboTimer -= _sm.Runner.DeltaTime;

        if (_comboTimer <= 0f)
        {
            Debug.LogWarning("COMBO TIMEOUT - Algo salió mal, forzando reset");

            // Seguridad: Solo forzar reset si realmente pasó mucho tiempo
            // (esto NO debería pasar normalmente, EndAttack debería manejarlo)
            ResetCombo();
            return;
        }

        // Procesar input de ataque
        if (input.attack)
        {
            HandleAttackInput();
        }
    }


    private void StartNextAttack()
    {
        // Incrementar combo
        _comboIndex++;
        if (_comboIndex > MAX_COMBO_COUNT)
            _comboIndex = 1; // Ciclar o podrías resetear a 1

        _sm.NetworkedComboIndex = _comboIndex;
        Debug.Log($"ComboIndex actualizado a: {_comboIndex}");

        // Resetear flags
        _comboWindowOpen = false;
        _inputBuffered = false;
        _comboTimer = COMBO_RESET_TIME;

        // Actualizar animator
        if (_sm.Animator != null)
        {
            // IMPORTANTE: Congelar el speed durante el ataque
            _sm.Animator.SetFloat("speed", 0f);

            _sm.Animator.SetInteger("ComboIndex", _comboIndex);
            Debug.Log($"Animator.ComboIndex seteado a: {_sm.Animator.GetInteger("ComboIndex")}");

            // Solo trigger en el primer ataque
            if (_comboIndex == 1)
            {
                _sm.Animator.SetTrigger("Melee");
                Debug.Log("Trigger 'Melee' activado");
            }
        }
        else
        {
            Debug.LogError("Animator es NULL!"); 
        }

        Debug.Log($"Combo Attack {_comboIndex} started");
    }

    private void HandleAttackInput()
    {
        Debug.Log($"Input de ataque detectado - Ventana abierta: {_comboWindowOpen}");

        if (_comboIndex >= MAX_COMBO_COUNT)
        {
            Debug.Log("Attack 3 activo - Input ignorado");
            return;
        }
        // Si la ventana está abierta, atacar inmediatamente
        if (_comboWindowOpen)
        {
            Debug.Log("Ventana abierta, ejecutando ataque inmediato");
            StartNextAttack();
            return;
        }

        // Si no, almacenar input para cuando se abra la ventana
        if (!_inputBuffered)
        {
            _inputBuffered = true;
            Debug.Log("Attack buffered");
        }
    }

    private void ResetCombo()
    {
        _comboIndex = 0;
        _comboWindowOpen = false;
        _inputBuffered = false;

        if (_sm.Animator != null)
        {
            _sm.Animator.SetInteger("ComboIndex", 0);
        }
    }

    // ==================== EVENTOS DE ANIMACIÓN ====================

    public void OnHitFrame()
    {
        if (!_sm.Object.HasInputAuthority) return;

        ApplyMeleeDamage();
    }

    public void OpenComboWindow()
    {
        Debug.Log($"COMBO WINDOW ABIERTA - ComboIndex actual: {_comboIndex}");
        _comboWindowOpen = true;

        // Si había input buffereado, ejecutarlo
        if (_inputBuffered)
        {
            Debug.Log("Input buffereado detectado, ejecutando ataque");
            _inputBuffered = false;
            StartNextAttack();
        }

        Debug.Log($"Combo window opened (Attack {_comboIndex})");
    }

    public void CloseComboWindow()
    {
        _comboWindowOpen = false;
        Debug.Log($"Combo window closed (Attack {_comboIndex})");
    }

    public void EndAttack()
    {
        Debug.Log($"EndAttack - ComboIndex: {_comboIndex}, Buffered: {_inputBuffered}");

        // Si no hay input buffereado, volver a locomoción melee
        if (!_inputBuffered)
        {
            _isAttacking = false; // Volver a modo locomoción
            ResetCombo();
        }
        // Si hay input buffereado, se ejecutará en la siguiente ventana
    }

    // ==================== APLICAR DAÑO ====================

    private void ApplyMeleeDamage()
    {
        var settings = _sm.Combat;
        if (settings == null || settings.meleeData == null) return;

        Vector3 origin = settings.meleeOrigin != null
            ? settings.meleeOrigin.position
            : _sm.transform.position + _sm.transform.forward * 0.5f + Vector3.up;

        Collider[] hits = Physics.OverlapSphere(
            origin,
            settings.meleeData.HitRadius,
            settings.enemyLayer
        );

        foreach (var hit in hits)
        {
            var enemyHealth = hit.GetComponentInParent<EnemyHealth>();

            // Solo el StateAuthority aplica daño (servidor)
            if (enemyHealth != null && _sm.Object.HasStateAuthority)
            {
                enemyHealth.ApplyDamageServer(
                    settings.meleeData.Damage,
                    _sm.Object.InputAuthority
                );

                Debug.Log($"Hit enemy with combo {_comboIndex} - Damage: {settings.meleeData.Damage}");
            }
        }

        // Opcional: Feedback visual/audio
        PlayHitFeedback();
    }

    private void PlayHitFeedback()
    {
        // TODO: Efectos visuales, sonido, vibración, etc.
    }

    public void OnShootFrame() { }
    public void OnShootAnimationEnd() { }
    public void OnReloadComplete() { }
}