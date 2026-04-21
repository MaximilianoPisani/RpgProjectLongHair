using Fusion;
using UnityEngine;

public class PlayerNetworkSync : NetworkBehaviour
{
    [Networked] private Vector3 SyncedPosition { get; set; }
    [Networked] private Quaternion SyncedRotation { get; set; }

    [Networked] private float SyncedSpeed { get; set; }
    [Networked] private NetworkBool SyncedIsJumping { get; set; }
    [Networked] private NetworkBool SyncedIsReloading { get; set; }
    [Networked] private NetworkBool SyncedIsFalling { get; set; }
    [Networked] private NetworkBool SyncedIsLanding { get; set; }  

    [Networked] private int SyncedMeleeTrigger { get; set; }
    [Networked] private int SyncedShootTrigger { get; set; }
    [Networked] private int SyncedJumpTrigger { get; set; }
    [Networked] private int SyncedDieTrigger { get; set; }
    [Networked] private int SyncedComboIndex { get; set; }
    [Networked] private int SyncedFallTrigger { get; set; }
    [Networked] private int SyncedLandTrigger { get; set; }     

    private int _lastMelee;
    private int _lastShoot;
    private int _lastJump;
    private int _lastDie;
    private int _lastFall;
    private int _lastLand;  

    private Animator _animator;
    private ChangeDetector _changes;

    [SerializeField] private float _interpolationSpeed = 15f;

    public override void Spawned()
    {
        _animator = GetComponent<Animator>();
        _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (Object.HasStateAuthority)
        {
            SyncedPosition = transform.position;
            SyncedRotation = transform.rotation;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        SyncedPosition = transform.position;
        SyncedRotation = transform.rotation;

        if (_animator == null) return;

        SyncedSpeed = _animator.GetFloat("speed");
        SyncedIsJumping = _animator.GetBool("isJumping");
        SyncedIsReloading = _animator.GetBool("IsReloading");
        SyncedComboIndex = _animator.GetInteger("ComboIndex");
        SyncedIsFalling = _animator.GetBool("isFalling");
        SyncedIsLanding = _animator.GetBool("isLanding"); 
    }

    public override void Render()
    {
        if (Object.HasInputAuthority) return;

        // Interpolar POSICIÓN Y ROTACIÓN
        transform.position = Vector3.Lerp(
            transform.position,
            SyncedPosition,
            _interpolationSpeed * Time.deltaTime
        );

        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            SyncedRotation,
            _interpolationSpeed * Time.deltaTime
        );

        if (_animator == null) return;

        _animator.SetFloat("speed", SyncedSpeed);
        _animator.SetBool("isJumping", SyncedIsJumping);
        _animator.SetBool("IsReloading", SyncedIsReloading);
        _animator.SetInteger("ComboIndex", SyncedComboIndex);
        _animator.SetBool("isFalling", SyncedIsFalling);
        _animator.SetBool("isLanding", SyncedIsLanding);   

        foreach (var change in _changes.DetectChanges(this, out var prev, out var current))
        {
            if (change == nameof(SyncedMeleeTrigger) && _lastMelee != SyncedMeleeTrigger)
            {
                _lastMelee = SyncedMeleeTrigger;
                _animator.SetTrigger("Melee");
            }
            if (change == nameof(SyncedShootTrigger) && _lastShoot != SyncedShootTrigger)
            {
                _lastShoot = SyncedShootTrigger;
                _animator.SetTrigger("Shoot");
            }
            if (change == nameof(SyncedJumpTrigger) && _lastJump != SyncedJumpTrigger)
            {
                _lastJump = SyncedJumpTrigger;
                _animator.SetTrigger("Jump");
            }
            if (change == nameof(SyncedDieTrigger) && _lastDie != SyncedDieTrigger)
            {
                _lastDie = SyncedDieTrigger;
                _animator.SetTrigger("Die");
            }
            if (change == nameof(SyncedFallTrigger) && _lastFall != SyncedFallTrigger)
            {
                _lastFall = SyncedFallTrigger;
                _animator.SetTrigger("Fall");
            }
            if (change == nameof(SyncedLandTrigger) && _lastLand != SyncedLandTrigger)
            {
                _lastLand = SyncedLandTrigger;
                _animator.SetTrigger("Land");
            }
        }
    }

    public void TriggerLand()
    {
        if (Object.HasStateAuthority)
        {
            SyncedLandTrigger++;
            _animator?.SetTrigger("Land");
        }
        else if (Object.HasInputAuthority)
        {
            _animator?.SetTrigger("Land");
            RPC_TriggerLand();
        }
    }

    public void TriggerJump()
    {
        if (Object.HasStateAuthority)
        {
            SyncedJumpTrigger++;
            _animator?.SetTrigger("Jump");
        }
        else if (Object.HasInputAuthority)
        {
            _animator?.SetTrigger("Jump");
            RPC_TriggerJump();
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
        }
        else if (Object.HasInputAuthority)
        {
            _animator?.SetTrigger("Fall");
            RPC_TriggerFall();
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    private void RPC_TriggerLand() => SyncedLandTrigger++;  

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    private void RPC_TriggerFall() => SyncedFallTrigger++;

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    private void RPC_TriggerJump() => SyncedJumpTrigger++;

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    private void RPC_TriggerMelee() => SyncedMeleeTrigger++;

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    private void RPC_TriggerShoot() => SyncedShootTrigger++;

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    private void RPC_TriggerDie() => SyncedDieTrigger++;
}