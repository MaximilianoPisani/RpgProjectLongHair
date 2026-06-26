using UnityEngine;

public class EnemyDeathState : IEnemyState
{
    private readonly EnemyBaseController _enemy;
    private bool _hasTriggeredDeath = false;

    public EnemyDeathState(EnemyBaseController enemy)
    {
        _enemy = enemy;
    }

    public void EnterState()
    {
        if (_hasTriggeredDeath) return;
        _hasTriggeredDeath = true;

        Debug.Log($"[Death] {_enemy.name} entering death state.");

        // Desactivar NavMeshAgent solo en el host (clientes ya lo tienen desactivado)
        if (_enemy.Agent != null)
            _enemy.Agent.enabled = false;

        // TriggerDeath maneja todo via RPCs:
        // - RPC_ActivateRagdoll → activa ragdoll en TODOS los clientes
        // - RPC_StartFadeOut   → fade + despawn en TODOS los clientes
        var networkSync = _enemy.GetComponent<EnemyNetworkSync>();
        networkSync?.TriggerDeath();
    }

    public void ExitState() { }
    public void UpdateState() { }
}