using Fusion;
using UnityEngine;

public class EnemyMeleeController : EnemyBaseController
{
    [Header("Melee Settings")]
    [SerializeField] private EnemyMeleeAttackData meleeAttackData;
    [SerializeField] private float attackRange = 2f;

    [Header("Animation & VFX")]
    [SerializeField] private EnemyAnimationController animationController;
    [SerializeField] private EnemyVFXController vfxController;

    [Networked] public float NextMeleeAttackTime { get; set; }
    [Networked] public int CurrentAttackIndex { get; set; }

    // Propiedades públicas
    public EnemyMeleeAttackData MeleeAttackData => meleeAttackData;  // tipo concreto, no base
    public float AttackRange => attackRange;
    public EnemyAnimationController AnimationController => animationController;
    public EnemyVFXController VFXController => vfxController;

    private EnemyNetworkSync _networkSync;

    public override void Spawned()
    {
        base.Spawned();
        if (animationController == null)
            animationController = GetComponent<EnemyAnimationController>();

        if (vfxController == null)
            vfxController = GetComponent<EnemyVFXController>();

        _networkSync = GetComponent<EnemyNetworkSync>();

        CurrentAttackIndex = 0;
    }

    protected override void InitStateMachine() => ChangeState(new EnemyMeleeIdleState(this));
    protected override IEnemyState GetIdleState() => new EnemyMeleeIdleState(this);

    public int ExecuteCurrentAttack()
    {
        if (meleeAttackData == null) return -1;

        int attackIndex = CurrentAttackIndex;

        // FIX: instancia, no clase estática
        var vfxConfig = meleeAttackData.GetVFXConfig(attackIndex);
        _networkSync?.TriggerMeleeAttack(attackIndex, vfxConfig);

        _networkSync?.SyncAttackIndicator();
        _networkSync?.SyncSlashVFX(vfxConfig);

        // Avanza secuencia usando AttackCount del data, no un campo manual
        CurrentAttackIndex = (CurrentAttackIndex + 1) % meleeAttackData.AttackCount;

        NextMeleeAttackTime = Runner.SimulationTime + meleeAttackData.Cooldown;

        return attackIndex;
    }

    public void ApplyDamageToTarget()
    {
        if (TargetPlayer == null || meleeAttackData == null) return;

        float dist = Vector3.Distance(transform.position, TargetPlayer.position);
        if (dist > attackRange) return;

        if (TargetPlayer.TryGetComponent<PlayerHealth>(out var playerHealth))
            playerHealth.TakeDamage(meleeAttackData.Damage, transform.position);
    }

    public void TriggerHitAnimation() => _networkSync?.TriggerHit();
    public void SyncHit(Vector3 hitPos, Vector3 hitNormal)
    {
        _networkSync?.SyncHitVFX(hitPos, hitNormal);
    }

    private void OnValidate()
    {
        if (animationController == null)
            animationController = GetComponent<EnemyAnimationController>();
        if (vfxController == null)
            vfxController = GetComponent<EnemyVFXController>();
    }
}