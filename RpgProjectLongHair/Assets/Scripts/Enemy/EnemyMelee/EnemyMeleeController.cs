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

    private void OnValidate()
    {
        if (animationController == null)
            animationController = GetComponent<EnemyMinionAnimationController>();
    }
}