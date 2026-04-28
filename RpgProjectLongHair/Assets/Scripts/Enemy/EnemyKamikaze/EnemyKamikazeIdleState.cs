using UnityEngine;

public class EnemyKamikazeIdleState : IEnemyState
{
    private readonly EnemyKamikazeController _enemy;
    private float _searchInterval = 0.3f; // Buscar cada 0.3 segundos en lugar de cada frame
    private float _nextSearchTime = 0f;

    public EnemyKamikazeIdleState(EnemyKamikazeController enemy) => _enemy = enemy;

    public void EnterState()
    {
        // Detener el agente cuando entra en idle
        if (_enemy.Agent != null && _enemy.Agent.isOnNavMesh)
        {
            _enemy.Agent.SetDestination(_enemy.transform.position);
        }

        _nextSearchTime = Time.time;
    }

    public void ExitState() { }

    public void UpdateState()
    {
        if (!_enemy.Object.HasStateAuthority) return;

        // Solo buscar jugadores en intervalos, no cada frame (optimización)
        if (Time.time < _nextSearchTime) return;

        _nextSearchTime = Time.time + _searchInterval;

        // Buscar el jugador más cercano
        Transform closestPlayer = FindClosestValidPlayer();

        if (closestPlayer != null)
        {
            _enemy.SetTarget(closestPlayer);
            _enemy.ChangeState(new EnemyKamikazeChaseState(_enemy));
        }
    }

    /// <summary>
    /// Encuentra el jugador válido (vivo) más cercano dentro del rango de detección.
    /// </summary>
    private Transform FindClosestValidPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        Transform closestPlayer = null;
        float minDistance = float.MaxValue;

        foreach (var playerObj in players)
        {
            // Verificar si el jugador está vivo
            if (!IsPlayerAlive(playerObj))
                continue;

            // Calcular distancia
            float distance = Vector3.Distance(_enemy.transform.position, playerObj.transform.position);

            // Verificar si está dentro del rango de detección y es el más cercano
            if (distance <= _enemy.DetectionRadius && distance < minDistance)
            {
                minDistance = distance;
                closestPlayer = playerObj.transform;
            }
        }

        return closestPlayer;
    }

    /// <summary>
    /// Verifica si un jugador está vivo.
    /// </summary>
    private bool IsPlayerAlive(GameObject playerObj)
    {
        var playerHealth = playerObj.GetComponent<PlayerHealth>()
                        ?? playerObj.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null)
            return true; // Si no tiene componente de salud, asumimos que está vivo

        return !playerHealth.IsDead;
    }
}