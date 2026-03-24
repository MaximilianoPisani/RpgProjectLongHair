using UnityEngine;

public class EnemyKamikazeController : EnemyBaseController
{
    [Header("Kamikaze")]
    public KamikazeAttackData KamikazeData;

    protected override void InitStateMachine()
    {
        ChangeState(new EnemyKamikazeIdleState(this));
    }

    protected override IEnemyState GetIdleState() => new EnemyKamikazeIdleState(this);
}