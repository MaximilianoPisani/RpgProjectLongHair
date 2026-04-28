using UnityEngine;

public class EnemyKamikazeController : EnemyBaseController
{
    [Header("Kamikaze")]
    public KamikazeAttackData KamikazeData;

    [Header("Layers")]
    [SerializeField] private LayerMask _enemyLayer;
    protected override void InitStateMachine()
    {
        ChangeState(new EnemyKamikazeIdleState(this));
    }

    protected override IEnemyState GetIdleState() => new EnemyKamikazeIdleState(this);
    public LayerMask EnemyLayer => _enemyLayer;
}