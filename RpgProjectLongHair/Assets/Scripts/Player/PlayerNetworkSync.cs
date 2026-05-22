using Fusion;
using UnityEngine;

public class PlayerNetworkSync : NetworkBehaviour
{
    [Networked] private float SyncedSpeed { get; set; }
    [Networked] private byte AnimationFlags { get; set; }

    [Networked] private int SyncedMeleeTrigger { get; set; }
    [Networked] private int SyncedShootTrigger { get; set; }
    [Networked] private int SyncedJumpTrigger { get; set; }
    [Networked] private int SyncedDieTrigger { get; set; }
    [Networked] private int SyncedFallTrigger { get; set; }
    [Networked] private int SyncedLandTrigger { get; set; }

    [Networked] private int SyncedComboIndex { get; set; }
    [Networked] private int SyncedShellVFXTrigger { get; set; }
    [Networked] private int SyncedFireVFXTrigger { get; set; }

    private const byte FLAG_JUMPING = 1 << 0;
    private const byte FLAG_RELOADING = 1 << 1;
    private const byte FLAG_FALLING = 1 << 2;
    private const byte FLAG_LANDING = 1 << 3;

    private Animator _animator;
    private ChangeDetector _changes;

    private int _lastMelee;
    private int _lastShoot;
    private int _lastJump;
    private int _lastDie;
    private int _lastFall;
    private int _lastLand;
    private int _lastComboIndex = 0;

    private int _lastShellVFX;
    private int _lastFireVFX;

    private const float AccelDamp = 0.08f;
    private const float DecelDamp = 0.08f;


    public override void Spawned()
    {
        _animator = GetComponent<Animator>();
        _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (_animator == null) return;

        float currentSpeed = _animator.GetFloat("speed");
        SyncedSpeed = Mathf.Round(currentSpeed * 10f) / 10f;

        var sm = GetComponent<PlayerStateMachine>();
        SyncedComboIndex = sm != null ? sm.NetworkedComboIndex : _animator.GetInteger("ComboIndex");

        byte flags = 0;
        if (_animator.GetBool("isJumping")) flags |= FLAG_JUMPING;
        if (_animator.GetBool("IsReloading")) flags |= FLAG_RELOADING;
        if (_animator.GetBool("isFalling")) flags |= FLAG_FALLING;
        if (_animator.GetBool("isLanding")) flags |= FLAG_LANDING;

        AnimationFlags = flags;
    }

    public override void Render()
    {
        if (_animator == null) return;
        if (!Object.HasInputAuthority)
        {
            float currentAnim = _animator.GetFloat("speed");
            float damp = SyncedSpeed > currentAnim ? AccelDamp : DecelDamp;
            _animator.SetFloat("speed", SyncedSpeed, damp, Time.deltaTime);
            _animator.SetInteger("ComboIndex", SyncedComboIndex);
            _animator.SetBool("isJumping", (AnimationFlags & FLAG_JUMPING) != 0);
            _animator.SetBool("IsReloading", (AnimationFlags & FLAG_RELOADING) != 0);
            _animator.SetBool("isFalling", (AnimationFlags & FLAG_FALLING) != 0);
            _animator.SetBool("isLanding", (AnimationFlags & FLAG_LANDING) != 0);

            if (SyncedComboIndex > 0 && SyncedComboIndex != _lastComboIndex)
                _animator.SetTrigger("Melee");

            _lastComboIndex = SyncedComboIndex;
        }

        foreach (var change in _changes.DetectChanges(this))
        {
            if (change == nameof(SyncedMeleeTrigger) && _lastMelee != SyncedMeleeTrigger)
            {
                _lastMelee = SyncedMeleeTrigger;
                if (!Object.HasInputAuthority)
                    _animator.SetTrigger("Melee");
            }
            if (change == nameof(SyncedShootTrigger) && _lastShoot != SyncedShootTrigger)
            {
                _lastShoot = SyncedShootTrigger;
                if (!Object.HasInputAuthority)
                    _animator.SetTrigger("Shoot");
            }
            if (change == nameof(SyncedJumpTrigger) && _lastJump != SyncedJumpTrigger)
            {
                _lastJump = SyncedJumpTrigger;
                if (!Object.HasInputAuthority)
                    _animator.SetTrigger("Jump");
            }
            if (change == nameof(SyncedDieTrigger) && _lastDie != SyncedDieTrigger)
            {
                _lastDie = SyncedDieTrigger;
                if (!Object.HasInputAuthority)
                    _animator.SetTrigger("Die");
            }
            if (change == nameof(SyncedFallTrigger) && _lastFall != SyncedFallTrigger)
            {
                _lastFall = SyncedFallTrigger;
                if (!Object.HasInputAuthority)
                    _animator.SetTrigger("Fall");
            }
            if (change == nameof(SyncedLandTrigger) && _lastLand != SyncedLandTrigger)
            {
                _lastLand = SyncedLandTrigger;
                if (!Object.HasInputAuthority)
                    _animator.SetTrigger("Land");
            }
            if (change == nameof(SyncedShellVFXTrigger) && _lastShellVFX != SyncedShellVFXTrigger)
            {
                _lastShellVFX = SyncedShellVFXTrigger;
                GetComponent<PlayerVFXSync>()?.OnShellVFXTriggered();
            }

            if (change == nameof(SyncedFireVFXTrigger) && _lastFireVFX != SyncedFireVFXTrigger)
            {
                _lastFireVFX = SyncedFireVFXTrigger;
                GetComponent<PlayerVFXSync>()?.OnFireVFXTriggered();
            }
        }
    }

    // ========== NUEVOS MÉTODOS PARA ACTUALIZAR FLAGS DIRECTAMENTE ==========

    public void SetJumpingFlag(bool value)
    {
        byte flags = AnimationFlags;
        if (value)
            flags |= FLAG_JUMPING;
        else
            flags &= unchecked((byte)~FLAG_JUMPING);

        if (Object.HasStateAuthority)
            AnimationFlags = flags;
        else if (Object.HasInputAuthority)
            RPC_SetJumpingFlag(value);

        _animator?.SetBool("isJumping", value);
    }

    public void SetFallingFlag(bool value)
    {
        byte flags = AnimationFlags;
        if (value)
            flags |= FLAG_FALLING;
        else
            flags &= unchecked((byte)~FLAG_FALLING);

        if (Object.HasStateAuthority)
            AnimationFlags = flags;
        else if (Object.HasInputAuthority)
            RPC_SetFallingFlag(value);

        _animator?.SetBool("isFalling", value);
    }

    public void SetLandingFlag(bool value)
    {
        byte flags = AnimationFlags;
        if (value)
            flags |= FLAG_LANDING;
        else
            flags &= unchecked((byte)~FLAG_LANDING);

        if (Object.HasStateAuthority)
            AnimationFlags = flags;
        else if (Object.HasInputAuthority)
            RPC_SetLandingFlag(value);

        _animator?.SetBool("isLanding", value);
    }

    // ========== MÉTODOS EXISTENTES MODIFICADOS ==========

    public void SetIsReloading(bool value)
    {
        byte flags = AnimationFlags;
        if (value)
            flags |= FLAG_RELOADING;
        else
            flags &= unchecked((byte)~FLAG_RELOADING);

        if (Object.HasStateAuthority)
            AnimationFlags = flags;
        else if (Object.HasInputAuthority)
            RPC_SetIsReloading(value);

        _animator?.SetBool("IsReloading", value);
    }

    public void SetSpeed(float speed)
    {
        if (Object.HasStateAuthority)
            SyncedSpeed = speed;
        else if (Object.HasInputAuthority)
            RPC_SetSpeed(speed);

        _animator?.SetFloat("speed", speed);
    }

    public void ResetAllAnimations()
    {
        if (_animator != null)
        {
            _animator.SetBool("IsReloading", false);
            _animator.SetBool("isJumping", false);
            _animator.SetBool("isFalling", false);
            _animator.SetBool("isLanding", false);
            _animator.SetFloat("speed", 0f);
            _animator.SetInteger("ComboIndex", 0);
            _animator.ResetTrigger("Shoot");
            _animator.ResetTrigger("Melee");
            _animator.ResetTrigger("Jump");
            _animator.ResetTrigger("Fall");
            _animator.ResetTrigger("Land");
        }

        if (Object.HasStateAuthority)
        {
            AnimationFlags = 0;
            SyncedSpeed = 0f;
            SyncedComboIndex = 0;
        }
        else if (Object.HasInputAuthority)
        {
            RPC_ForceResetFlags();
        }
    }

    public void TriggerMelee()
    {
        if (Object.HasStateAuthority)
        {
            SyncedMeleeTrigger++;
            _animator?.SetTrigger("Melee");
        }
        else if (Object.HasInputAuthority)
        {
            _animator?.SetTrigger("Melee");
            RPC_TriggerMelee();
        }
    }

    public void TriggerShoot()
    {
        if (Object.HasStateAuthority)
        {
            SyncedShootTrigger++;
            _animator?.SetTrigger("Shoot");
        }
        else if (Object.HasInputAuthority)
        {
            _animator?.SetTrigger("Shoot");
            RPC_TriggerShoot();
        }
    }

    public void TriggerJump()
    {
        if (Object.HasStateAuthority)
        {
            SyncedJumpTrigger++;
            _animator?.SetTrigger("Jump");
            // MODIFICADO: Forzar actualización inmediata del flag
            byte flags = AnimationFlags;
            flags |= FLAG_JUMPING;
            AnimationFlags = flags;
        }
        else if (Object.HasInputAuthority)
        {
            _animator?.SetTrigger("Jump");
            RPC_TriggerJump();
        }
    }

    public void TriggerDie()
    {
        if (Object.HasStateAuthority)
        {
            SyncedDieTrigger++;
            _animator?.SetTrigger("Die");
        }
        else if (Object.HasInputAuthority)
        {
            _animator?.SetTrigger("Die");
            RPC_TriggerDie();
        }
    }

    public void TriggerFall()
    {
        if (Object.HasStateAuthority)
        {
            SyncedFallTrigger++;
            _animator?.SetTrigger("Fall");
            // MODIFICADO: Forzar actualización inmediata del flag
            byte flags = AnimationFlags;
            flags |= FLAG_FALLING;
            flags &= unchecked((byte)~FLAG_JUMPING); // Quitar jumping
            AnimationFlags = flags;
        }
        else if (Object.HasInputAuthority)
        {
            _animator?.SetTrigger("Fall");
            RPC_TriggerFall();
        }
    }

    public void TriggerLand()
    {
        if (Object.HasStateAuthority)
        {
            SyncedLandTrigger++;
            _animator?.SetTrigger("Land");
            // MODIFICADO: Forzar actualización inmediata del flag
            byte flags = AnimationFlags;
            flags |= FLAG_LANDING;
            flags &= unchecked((byte)~FLAG_FALLING); // Quitar falling
            flags &= unchecked((byte)~FLAG_JUMPING); // Quitar jumping
            AnimationFlags = flags;
        }
        else if (Object.HasInputAuthority)
        {
            _animator?.SetTrigger("Land");
            RPC_TriggerLand();
        }
    }

    public void TriggerShellVFX()
    {
        if (Object.HasStateAuthority)
            SyncedShellVFXTrigger++;
        else if (Object.HasInputAuthority)
            RPC_TriggerShellVFX();
    }

    public void TriggerFireVFX()
    {
        if (Object.HasStateAuthority)
            SyncedFireVFXTrigger++;
        else if (Object.HasInputAuthority)
            RPC_TriggerFireVFX();
    }

    // ========== RPCs EXISTENTES ==========

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_TriggerMelee() => SyncedMeleeTrigger++;

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_TriggerShoot() => SyncedShootTrigger++;

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_TriggerJump()
    {
        SyncedJumpTrigger++;
        // MODIFICADO: Forzar actualización inmediata del flag
        byte flags = AnimationFlags;
        flags |= FLAG_JUMPING;
        AnimationFlags = flags;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_TriggerDie() => SyncedDieTrigger++;

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_TriggerFall()
    {
        SyncedFallTrigger++;
        // MODIFICADO: Forzar actualización inmediata del flag
        byte flags = AnimationFlags;
        flags |= FLAG_FALLING;
        flags &= unchecked((byte)~FLAG_JUMPING);
        AnimationFlags = flags;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_TriggerLand()
    {
        SyncedLandTrigger++;
        // MODIFICADO: Forzar actualización inmediata del flag
        byte flags = AnimationFlags;
        flags |= FLAG_LANDING;
        flags &= unchecked((byte)~FLAG_FALLING);
        flags &= unchecked((byte)~FLAG_JUMPING);
        AnimationFlags = flags;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetIsReloading(bool value)
    {
        byte flags = AnimationFlags;
        if (value)
            flags |= FLAG_RELOADING;
        else
            flags &= unchecked((byte)~FLAG_RELOADING);
        AnimationFlags = flags;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetSpeed(float speed)
    {
        SyncedSpeed = speed;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_ForceResetFlags()
    {
        AnimationFlags = 0;
        SyncedSpeed = 0f;
        SyncedComboIndex = 0;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_TriggerShellVFX() => SyncedShellVFXTrigger++;

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_TriggerFireVFX() => SyncedFireVFXTrigger++;

    // ========== NUEVOS RPCs PARA FLAGS ==========

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetJumpingFlag(bool value)
    {
        byte flags = AnimationFlags;
        if (value)
            flags |= FLAG_JUMPING;
        else
            flags &= unchecked((byte)~FLAG_JUMPING);
        AnimationFlags = flags;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetFallingFlag(bool value)
    {
        byte flags = AnimationFlags;
        if (value)
            flags |= FLAG_FALLING;
        else
            flags &= unchecked((byte)~FLAG_FALLING);
        AnimationFlags = flags;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetLandingFlag(bool value)
    {
        byte flags = AnimationFlags;
        if (value)
            flags |= FLAG_LANDING;
        else
            flags &= unchecked((byte)~FLAG_LANDING);
        AnimationFlags = flags;
    }
}