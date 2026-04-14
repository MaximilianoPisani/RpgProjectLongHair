using Fusion;
using UnityEngine;

public class EnemyRangedController : EnemyBaseController
{
    [Header("Ranged Settings")]
    public RangedAttackData RangedAttackData;
    public float PreferredMinRange = 4f;
    public float PreferredMaxRange = 7f;
    public float NextRangedAttackTime { get; set; } = 0f;

    [Header("Animation")]
    [SerializeField] private EnemyAnimationController animationController;
    public EnemyAnimationController AnimationController => animationController;

    public override void Spawned()
    {
        base.Spawned();

        if (animationController == null)
            animationController = GetComponent<EnemyAnimationController>();
    }

    protected override void InitStateMachine()
    {
        ChangeState(new EnemyIdleRangedState(this));
    }

    protected override IEnemyState GetIdleState() => new EnemyIdleRangedState(this);

    /// <summary>
    /// Dispara la animación ranged pasando ambos VFX configs desde el data.
    /// El timing de cada VFX viene embebido en su AttackVFXConfig.
    /// </summary>
    public void TriggerRangedAttackAnimation()
    {
        if (animationController == null || RangedAttackData == null) return;

        animationController.PlayRangedAttack(
            RangedAttackData.FireEjectionVFX,
            RangedAttackData.ShellEjectionVFX
        );
    }

    public void TriggerHitAnimation()
    {
        if (animationController != null)
            animationController.PlayHitReaction();
    }

    private void OnValidate()
    {
        if (animationController == null)
            animationController = GetComponent<EnemyAnimationController>();
    }
}