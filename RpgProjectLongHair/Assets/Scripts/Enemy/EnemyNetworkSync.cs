using Fusion;
using UnityEngine;

public class EnemyNetworkSync : NetworkBehaviour
{
    // ===== NETWORKED VARS =====
    [Networked] private float SyncedSpeed { get; set; }
    [Networked] private byte SyncedAttackIndex { get; set; }
    [Networked] private byte SyncedIdleIndex { get; set; }
    [Networked] private NetworkBool SyncedIsDead { get; set; }

    // ===== TRIGGERS =====
    [Networked] private int SyncedMeleeTrigger { get; set; }
    [Networked] private int SyncedRangedTrigger { get; set; }
    [Networked] private int SyncedReloadTrigger { get; set; }
    [Networked] private int SyncedHitTrigger { get; set; }
    [Networked] private int SyncedDeathTrigger { get; set; }

    // ===== LOCAL =====
    private EnemyAnimationController _animController;
    private Animator _animator;
    private ChangeDetector _changes;
    private EnemyVFXController _vfxController;

    private int _lastMelee;
    private int _lastRanged;
    private int _lastReload;
    private int _lastHit;
    private int _lastDeath;
    private int _lastIdleIndex = -1;

    public override void Spawned()
    {
        _animController = GetComponent<EnemyAnimationController>();
        _animator = GetComponentInChildren<Animator>();
        _vfxController = GetComponent<EnemyVFXController>();
        _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    // ===== HOST ESCRIBE EN FixedUpdate =====
    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (_animator == null) return;

        SyncedSpeed = _animator.GetFloat("Speed");
        SyncedAttackIndex = (byte)_animator.GetInteger("AttackIndex");
        SyncedIdleIndex = (byte)_animator.GetInteger("IdleIndex");
        SyncedIsDead = _animController != null && _animController.IsDead;
    }

    // ===== CLIENTES LEEN EN Render =====
    public override void Render()
    {
        if (Object.HasStateAuthority) return;
        if (_animator == null) return;

        // Valores continuos
        _animator.SetFloat("Speed", SyncedSpeed);
        _animator.SetInteger("AttackIndex", SyncedAttackIndex);

        // IdleIndex solo cuando cambia
        if (SyncedIdleIndex != _lastIdleIndex)
        {
            _lastIdleIndex = SyncedIdleIndex;
            _animator.SetInteger("IdleIndex", SyncedIdleIndex);
        }

        _vfxController?.UpdateThrusters(SyncedSpeed);

        // Triggers
        foreach (var change in _changes.DetectChanges(this))
        {
            if (change == nameof(SyncedMeleeTrigger) && _lastMelee != SyncedMeleeTrigger)
            {
                _lastMelee = SyncedMeleeTrigger;
                _animator.SetInteger("AttackIndex", SyncedAttackIndex);
                _animator.SetTrigger("MeleeAttack");
            }
            if (change == nameof(SyncedRangedTrigger) && _lastRanged != SyncedRangedTrigger)
            {
                _lastRanged = SyncedRangedTrigger;
                _animator.SetTrigger("RangedAttack");
            }
            if (change == nameof(SyncedReloadTrigger) && _lastReload != SyncedReloadTrigger)
            {
                _lastReload = SyncedReloadTrigger;
                _animator.SetTrigger("Reload");
            }
            if (change == nameof(SyncedHitTrigger) && _lastHit != SyncedHitTrigger)
            {
                _lastHit = SyncedHitTrigger;
                _animator.SetTrigger("Hit");
            }
            if (change == nameof(SyncedDeathTrigger) && _lastDeath != SyncedDeathTrigger)
            {
                _lastDeath = SyncedDeathTrigger;
                _animator.SetTrigger("Death");
            }
        }
    }

    // ===== API PÚBLICA (llamada desde EnemyAI/States) =====

    public void TriggerMeleeAttack(int attackIndex = 0, AttackVFXConfig vfxConfig = null)
    {
        if (!Object.HasStateAuthority) return;

        SyncedAttackIndex = (byte)attackIndex;
        SyncedMeleeTrigger++;
        _animController?.PlayMeleeAttack(attackIndex, vfxConfig);
    }

    public void TriggerRangedAttack(AttackVFXConfig fireVFX = null, AttackVFXConfig shellVFX = null)
    {
        if (!Object.HasStateAuthority) return;

        SyncedRangedTrigger++;
        _animController?.PlayRangedAttack(fireVFX, shellVFX);
    }

    public void TriggerReload()
    {
        if (!Object.HasStateAuthority) return;

        SyncedReloadTrigger++;
        _animController?.PlayReloadAnimation();
    }

    public void TriggerHit()
    {
        if (!Object.HasStateAuthority) return;

        SyncedHitTrigger++;
        _animController?.PlayHitReaction();
    }

    public void TriggerDeath()
    {
        if (!Object.HasStateAuthority) return;

        SyncedDeathTrigger++;
        SyncedIsDead = true;
        _animController?.PlayDeath();
    }


    // ===== API PÚBLICA VFX =====

    public void SyncHitVFX(Vector3 hitPosition, Vector3 hitNormal)
    {
        if (!Object.HasStateAuthority) return;
        _vfxController?.SpawnHitVFX(hitPosition, hitNormal);
        RPC_SpawnHitVFX(hitPosition, hitNormal);
    }

    public void SyncAttackIndicator()
    {
        if (!Object.HasStateAuthority) return;
        _vfxController?.SpawnAttackIndicator();
        RPC_SpawnAttackIndicator();
    }

    public void SyncMuzzleFlash(AttackVFXConfig fireVFX, AttackVFXConfig shellVFX)
    {
        if (!Object.HasStateAuthority) return;

        // Host lo ejecuta local (ya lo hace animationController)
        // Solo necesita notificar proxies
        if (fireVFX?.vfxPrefab != null)
            RPC_SpawnMuzzleFlash(fireVFX.vfxPrefab.name, fireVFX.vfxSpawnTime);

        if (shellVFX?.vfxPrefab != null)
            RPC_SpawnShellEjection(shellVFX.vfxPrefab.name, shellVFX.vfxSpawnTime);
    }

    public void SyncSlashVFX(AttackVFXConfig config)
    {
        if (!Object.HasStateAuthority) return;
        if (config?.vfxPrefab == null) return;

        // Host ya lo spawna via PlayMeleeAttack — solo notificar proxies
        RPC_SpawnSlashVFX(config.vfxPrefab.name, config.vfxSpawnTime,
                          config.localOffset, config.followTransform, config.customDuration);
    }


    // ===== RPCs =====

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)]
    private void RPC_SpawnHitVFX(Vector3 hitPosition, Vector3 hitNormal)
    {
        _vfxController?.SpawnHitVFX(hitPosition, hitNormal);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)]
    private void RPC_SpawnAttackIndicator()
    {
        _vfxController?.SpawnAttackIndicator();
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)]
    private void RPC_SpawnMuzzleFlash(string prefabName, float delay)
    {
        var config = FindVFXConfigByName(prefabName);
        if (config != null)
            _vfxController?.SpawnVFXDelayed(config, delay, null);
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)]
    private void RPC_SpawnShellEjection(string prefabName, float delay)
    {
        var config = FindVFXConfigByName(prefabName);
        if (config != null)
            _vfxController?.SpawnVFXDelayed(config, delay, null);
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)]
    private void RPC_SpawnSlashVFX(string prefabName, float delay, Vector3 offset,
                                bool follow, float duration)
    {
        var config = FindMeleeVFXConfigByName(prefabName);
        if (config != null)
            _vfxController?.SpawnVFXDelayed(config, delay);
    }


    private AttackVFXConfig FindVFXConfigByName(string name)
    {
        var ranged = GetComponent<EnemyRangedController>();
        if (ranged?.RangedAttackData?.FireEjectionVFX?.vfxPrefab?.name == name)
            return ranged.RangedAttackData.FireEjectionVFX;
        if (ranged?.RangedAttackData?.ShellEjectionVFX?.vfxPrefab?.name == name)
            return ranged.RangedAttackData.ShellEjectionVFX;
        return null;
    }
    private AttackVFXConfig FindMeleeVFXConfigByName(string name)
    {
        var melee = GetComponent<EnemyMeleeController>();
        if (melee?.MeleeAttackData?.ComboAttacks == null) return null;

        foreach (var combo in melee.MeleeAttackData.ComboAttacks)
        {
            if (combo.attackVFX?.vfxPrefab?.name == name)
                return combo.attackVFX;
        }
        return null;
    }
}