using Fusion;
using UnityEngine;

public class EnemyRangedController : EnemyBaseController
{
    [Header("Ranged Settings")]
    public RangedAttackData RangedAttackData;

    [Tooltip("Distancia mínima que mantiene del player")]
    public float PreferredMinRange = 4f;

    [Tooltip("Distancia máxima antes de acercarse")]
    public float PreferredMaxRange = 7f;

    public float NextRangedAttackTime { get; set; } = 0f;

    [Header("Animation")]
    [SerializeField] private EnemyRangedAnimationController animationController;

    public EnemyRangedAnimationController AnimationController => animationController;

    public override void Spawned()
    {
        base.Spawned();

        // Auto-encontrar el controlador de animaciones si no está asignado
        if (animationController == null)
            animationController = GetComponent<EnemyRangedAnimationController>();
    }

    protected override void InitStateMachine()
    {
        ChangeState(new EnemyIdleRangedState(this));
    }

    protected override IEnemyState GetIdleState() => new EnemyIdleRangedState(this);

    public void TriggerRangedAttackAnimation()
    {
        if (animationController != null)
            animationController.PlayRangedAttack();
    }
    public void TriggerHitAnimation()
    {
        if (animationController != null)
            animationController.PlayHitReaction();
    }
    private void OnValidate()
    {
        if (animationController == null)
            animationController = GetComponent<EnemyRangedAnimationController>();
    }

}