using Fusion;
using UnityEngine;

public class EnemyMeleeController : EnemyBaseController
{
    [Header("Melee")]
    public MeleeAttackData MeleeAttackData;
    public float StoppingDistance = 1f;

    [SerializeField] private EnemyAnimationController animationController; // Cambiado
    public EnemyAnimationController AnimationController => animationController;

    private int currentComboIndex = 0;

    public override void Spawned()
    {
        base.Spawned();
        if (Agent != null)
            Agent.stoppingDistance = StoppingDistance;

        if (animationController == null)
            animationController = GetComponent<EnemyAnimationController>();
    }

    protected override void InitStateMachine()
    {
        ChangeState(new EnemyMeleeIdleState(this));
    }

    protected override IEnemyState GetIdleState() => new EnemyMeleeIdleState(this);

    public void TriggerMeleeAttackAnimation()
    {
        if (animationController == null || MeleeAttackData == null) return;

        // Obtener VFX del combo actual
        AttackVFXConfig vfxConfig = null;
        if (MeleeAttackData.ComboAttacks != null && MeleeAttackData.ComboAttacks.Length > 0)
        {
            int index = Mathf.Clamp(currentComboIndex, 0, MeleeAttackData.ComboAttacks.Length - 1);
            vfxConfig = MeleeAttackData.ComboAttacks[index].attackVFX;
        }

        animationController.PlayMeleeAttack(vfxConfig);
    }

    public void TriggerHitAnimation()
    {
        if (animationController != null)
            animationController.PlayHitReaction();
    }

    public void OnMeleeHitFrame()
    {
        if (!Object.HasStateAuthority) return;
        if (TargetPlayer == null) return;

        Collider[] hits = Physics.OverlapSphere(
            AttackOrigin.position,
            MeleeAttackData.HitRadius,
            PlayerLayer
        );

        var alreadyHit = new System.Collections.Generic.HashSet<PlayerHealth>();

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;

            var playerHealth = hit.GetComponent<PlayerHealth>()
                            ?? hit.GetComponentInParent<PlayerHealth>();

            if (playerHealth == null) continue;
            if (playerHealth.IsDead) continue;
            if (!alreadyHit.Add(playerHealth)) continue;

            playerHealth.TakeDamage(MeleeAttackData.Damage, transform.position);
        }
    }

    private void OnValidate()
    {
        if (animationController == null)
            animationController = GetComponent<EnemyAnimationController>();
    }
}