using Fusion;
using UnityEngine;

public class PlayerNetworkSync : NetworkBehaviour
{
    [Networked] private Vector3 SyncedPosition { get; set; }
    [Networked] private Quaternion SyncedRotation { get; set; }

    [Networked] private float SyncedSpeed { get; set; }
    [Networked] private NetworkBool SyncedIsMoving { get; set; }
    [Networked] private NetworkBool SyncedIsJumping { get; set; }
    [Networked] private NetworkBool SyncedIsReloading { get; set; }

    [Networked] private int SyncedMeleeTrigger { get; set; }
    [Networked] private int SyncedShootTrigger { get; set; }
    [Networked] private int SyncedJumpTrigger { get; set; }
    [Networked] private int SyncedDieTrigger { get; set; }
    [Networked] private int SyncedComboIndex { get; set; }

    private int _lastMelee;
    private int _lastShoot;
    private int _lastJump;
    private int _lastDie;

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
        SyncedIsMoving = _animator.GetBool("isMoving");
        SyncedIsJumping = _animator.GetBool("isJumping");
        SyncedIsReloading = _animator.GetBool("IsReloading");
        SyncedComboIndex = _animator.GetInteger("ComboIndex");
    }

    public override void Render()
    {
        if (Object.HasStateAuthority) return;

        transform.position = Vector3.Lerp(transform.position, SyncedPosition, _interpolationSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, SyncedRotation, _interpolationSpeed * Time.deltaTime);

        if (_animator == null) return;

        _animator.SetFloat("speed", SyncedSpeed);
        _animator.SetBool("isMoving", SyncedIsMoving);
        _animator.SetBool("isJumping", SyncedIsJumping);
        _animator.SetBool("IsReloading", SyncedIsReloading);
        _animator.SetInteger("ComboIndex", SyncedComboIndex);

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
        }
    }

    public void TriggerJump()
    {
        if (!Object.HasStateAuthority) return;
        SyncedJumpTrigger++;
        _animator?.SetTrigger("Jump"); 
    }

    public void TriggerMelee()
    {
        if (!Object.HasStateAuthority) return;
        SyncedMeleeTrigger++;
        _animator?.SetTrigger("Melee");
    }

    public void TriggerShoot()
    {
        if (!Object.HasStateAuthority) return;
        SyncedShootTrigger++;
        _animator?.SetTrigger("Shoot"); 
    }

    public void TriggerDie()
    {
        if (!Object.HasStateAuthority) return;
        SyncedDieTrigger++;
        _animator?.SetTrigger("Die"); 
    }
}