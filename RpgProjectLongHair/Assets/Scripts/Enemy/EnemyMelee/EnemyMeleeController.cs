using UnityEngine;
using UnityEngine.AI;

public class EnemyMeleeController : EnemyBaseController
{
    [Header("Melee")]
    public MeleeAttackData MeleeAttackData;
    public float StoppingDistance = 1f;

    [SerializeField] private EnemyMinionAnimationController animationController;
    public EnemyMinionAnimationController AnimationController => animationController;

    public override void Spawned()
    {
        base.Spawned();
        if (Agent != null)
            Agent.stoppingDistance = StoppingDistance;

        if (animationController == null)
            animationController = GetComponent<EnemyMinionAnimationController>();
    }

    protected override void InitStateMachine()
    {
        ChangeState(new EnemyMeleeIdleState(this));
    }

    protected override IEnemyState GetIdleState() => new EnemyMeleeIdleState(this);

    public void TriggerMeleeAttackAnimation()
    {
        if (animationController != null)
            animationController.PlayMeleeAttack();
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
            Debug.Log($"[Enemy] Hit frame ? {MeleeAttackData.Damage} dmg");
        }
    }
    private void OnValidate()
    {
        if (animationController == null)
            animationController = GetComponent<EnemyMinionAnimationController>();
    }
}