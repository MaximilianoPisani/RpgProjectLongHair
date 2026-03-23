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

    protected override void InitStateMachine()
    {
        ChangeState(new EnemyIdleRangedState(this));
    }

    protected override IEnemyState GetIdleState() => new EnemyIdleRangedState(this);
}