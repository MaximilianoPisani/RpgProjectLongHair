using System.Collections.Generic;
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
    [Networked] private int SyncedExplodeTrigger { get; set; }

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
    private int _lastExplode;

    private Coroutine _flashLoopCoroutine;

    // ===== PARÁMETROS VÁLIDOS =====
    private HashSet<int> _validParams;

    // Hashes pre-calculados
    private static readonly int _speedHash = Animator.StringToHash("Speed");
    private static readonly int _attackIndexHash = Animator.StringToHash("AttackIndex");
    private static readonly int _idleIndexHash = Animator.StringToHash("IdleIndex");
    private static readonly int _meleeHash = Animator.StringToHash("MeleeAttack");
    private static readonly int _rangedHash = Animator.StringToHash("RangedAttack");
    private static readonly int _reloadHash = Animator.StringToHash("Reload");
    private static readonly int _hitHash = Animator.StringToHash("Hit");
    private static readonly int _deathHash = Animator.StringToHash("Death");
    private static readonly int _explodeHash = Animator.StringToHash("Explode");

    public override void Spawned()
    {
        _animController = GetComponent<EnemyAnimationController>();
        _animator = GetComponentInChildren<Animator>();
        _vfxController = GetComponent<EnemyVFXController>();
        _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);

        CacheValidParameters();
    }

    private void CacheValidParameters()
    {
        _validParams = new HashSet<int>();
        if (_animator == null) return;

        foreach (AnimatorControllerParameter param in _animator.parameters)
            _validParams.Add(param.nameHash);
    }

    private void SafeSetTrigger(int hash)
    {
        if (_validParams.Contains(hash)) _animator.SetTrigger(hash);
    }

    private void SafeSetFloat(int hash, float value)
    {
        if (_validParams.Contains(hash)) _animator.SetFloat(hash, value);
    }

    private void SafeSetInteger(int hash, int value)
    {
        if (_validParams.Contains(hash)) _animator.SetInteger(hash, value);
    }

    private float SafeGetFloat(int hash)
    {
        return _validParams.Contains(hash) ? _animator.GetFloat(hash) : 0f;
    }

    private int SafeGetInteger(int hash)
    {
        return _validParams.Contains(hash) ? _animator.GetInteger(hash) : 0;
    }

    // ===== HOST ESCRIBE EN FixedUpdate =====
    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (_animator == null) return;

        SyncedSpeed = SafeGetFloat(_speedHash);
        SyncedAttackIndex = (byte)SafeGetInteger(_attackIndexHash);
        SyncedIdleIndex = (byte)SafeGetInteger(_idleIndexHash);
        SyncedIsDead = _animController != null && _animController.IsDead;
    }

    // ===== CLIENTES LEEN EN Render =====
    public override void Render()
    {
        if (Object.HasStateAuthority) return;
        if (_animator == null) return;

        // Valores continuos
        SafeSetFloat(_speedHash, SyncedSpeed);
        SafeSetInteger(_attackIndexHash, SyncedAttackIndex);

        // IdleIndex solo cuando cambia
        if (SyncedIdleIndex != _lastIdleIndex)
        {
            _lastIdleIndex = SyncedIdleIndex;
            SafeSetInteger(_idleIndexHash, SyncedIdleIndex);
        }

        _vfxController?.UpdateThrusters(SyncedSpeed);

        // Triggers
        foreach (var change in _changes.DetectChanges(this))
        {
            if (change == nameof(SyncedMeleeTrigger) && _lastMelee != SyncedMeleeTrigger)
            {
                _lastMelee = SyncedMeleeTrigger;
                SafeSetInteger(_attackIndexHash, SyncedAttackIndex);
                SafeSetTrigger(_meleeHash);
            }
            if (change == nameof(SyncedRangedTrigger) && _lastRanged != SyncedRangedTrigger)
            {
                _lastRanged = SyncedRangedTrigger;
                SafeSetTrigger(_rangedHash);
            }
            if (change == nameof(SyncedReloadTrigger) && _lastReload != SyncedReloadTrigger)
            {
                _lastReload = SyncedReloadTrigger;
                SafeSetTrigger(_reloadHash);
            }
            if (change == nameof(SyncedHitTrigger) && _lastHit != SyncedHitTrigger)
            {
                _lastHit = SyncedHitTrigger;
                SafeSetTrigger(_hitHash);
            }
            if (change == nameof(SyncedDeathTrigger) && _lastDeath != SyncedDeathTrigger)
            {
                _lastDeath = SyncedDeathTrigger;
                SafeSetTrigger(_deathHash);
            }
            if (change == nameof(SyncedExplodeTrigger) && _lastExplode != SyncedExplodeTrigger)
            {
                _lastExplode = SyncedExplodeTrigger;
                SafeSetTrigger(_explodeHash);
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

        RPC_ActivateRagdoll(Vector3.zero);
    }

    public void TriggerExplode()
    {
        if (!Object.HasStateAuthority) return;

        SyncedExplodeTrigger++;
        _animController?.PlayExplode(null);
    }

    public void StartExplosionFlash(float duration, float interval, Color emissionColor, float intensity)
    {
        if (!Object.HasStateAuthority) return;
        RPC_StartFlashLoop(duration, interval, emissionColor, intensity);
    }

    public void StopExplosionFlash()
    {
        if (!Object.HasStateAuthority) return;
        RPC_StopFlashLoop();
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

        if (fireVFX?.vfxPrefab != null)
            RPC_SpawnMuzzleFlash(fireVFX.vfxPrefab.name, fireVFX.vfxSpawnTime);

        if (shellVFX?.vfxPrefab != null)
            RPC_SpawnShellEjection(shellVFX.vfxPrefab.name, shellVFX.vfxSpawnTime);
    }

    public void SyncSlashVFX(AttackVFXConfig config)
    {
        if (!Object.HasStateAuthority) return;
        if (config?.vfxPrefab == null) return;

        RPC_SpawnSlashVFX(config.vfxPrefab.name, config.vfxSpawnTime,
                          config.localOffset, config.followTransform, config.customDuration);
    }

    public void SyncExplosionVFX(GameObject vfxPrefab, Vector3 position)
    {
        if (!Object.HasStateAuthority) return;
        if (vfxPrefab == null) return;

        RPC_SpawnExplosionVFX(position);
    }

    public void TriggerKamikazeExplosionSound()
    {
        if (!Object.HasStateAuthority) return;
        RPC_PlayKamikazeExplosionSound();
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

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)]
    private void RPC_SpawnExplosionVFX(Vector3 position)
    {
        var kamikaze = GetComponent<EnemyKamikazeController>();
        if (kamikaze?.KamikazeData?.ExplosionVFXPrefab == null) return;

        GameObject vfx = GameObject.Instantiate(
            kamikaze.KamikazeData.ExplosionVFXPrefab,
            position,
            Quaternion.identity
        );
        GameObject.Destroy(vfx, 3f);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_StartFlashLoop(float duration, float interval, Color emissionColor, float intensity)
    {
        if (_flashLoopCoroutine != null)
            StopCoroutine(_flashLoopCoroutine);

        _flashLoopCoroutine = StartCoroutine(
            FlashLoopRoutine(duration, interval, emissionColor, intensity)
        );
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_StopFlashLoop()
    {
        if (_flashLoopCoroutine != null)
        {
            StopCoroutine(_flashLoopCoroutine);
            _flashLoopCoroutine = null;
        }

        var meshRenderer = GetComponentInChildren<Renderer>();
        if (meshRenderer == null) return;

        var mat = meshRenderer.material;
        if (_emissionCached)
            mat.SetColor("_EmissionColor", _originalEmission);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)]
    private void RPC_ActivateRagdoll(Vector3 deathForce)
    {
        var ragdoll = GetComponent<EnemyRagdoll>();
        ragdoll?.ActivateRagdoll(deathForce);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayKamikazeExplosionSound()
    {
        AudioManager.Instance.PlayAttackEnemyKamikaze();
    }

    // ===== HELPERS =====

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

    private Color _originalEmission;
    private bool _emissionCached = false;

    private System.Collections.IEnumerator FlashLoopRoutine(float duration, float interval,
                                                             Color emissionColor, float intensity)
    {
        var meshRenderer = GetComponentInChildren<Renderer>();
        if (meshRenderer == null) yield break;

        var mat = meshRenderer.material;
        mat.EnableKeyword("_EMISSION");

        if (!_emissionCached)
        {
            _originalEmission = mat.GetColor("_EmissionColor");
            _emissionCached = true;
        }

        float elapsed = 0f;
        bool isFlashing = false;

        while (elapsed < duration)
        {
            isFlashing = !isFlashing;
            mat.SetColor("_EmissionColor",
                isFlashing ? emissionColor * intensity : _originalEmission);

            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }

        mat.SetColor("_EmissionColor", _originalEmission);
        _flashLoopCoroutine = null;
    }
}