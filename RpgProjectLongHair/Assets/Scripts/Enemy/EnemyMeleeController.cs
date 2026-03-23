using UnityEngine;
using UnityEngine.AI;

public class EnemyMeleeController : EnemyBaseController
{
    [Header("Melee")]
    public MeleeAttackData MeleeAttackData;
    public float StoppingDistance = 1f;

    public override void Spawned()
    {
        base.Spawned();
        if (Agent != null)
            Agent.stoppingDistance = StoppingDistance;
    }

    protected override void InitStateMachine()
    {
        ChangeState(new EnemyMeleeIdleState(this));
    }

    protected override IEnemyState GetIdleState() => new EnemyMeleeIdleState(this);
}