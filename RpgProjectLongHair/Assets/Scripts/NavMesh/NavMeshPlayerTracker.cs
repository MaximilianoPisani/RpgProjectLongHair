using Fusion;
using UnityEngine;

public class NavMeshPlayerTracker : NetworkBehaviour
{
    [Header("Área de influencia")]
    [SerializeField] private float activationDistance = 80f;
    [SerializeField] private float deactivationDistance = 120f;

    public float ActivationDistance => activationDistance;
    public float DeactivationDistance => deactivationDistance;

    public override void Spawned()
    {
        NavMeshTileManager.Instance?.RegisterPlayer(this);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        NavMeshTileManager.Instance?.UnregisterPlayer(this);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = new Color(1f, 1f, 0f, 0.08f);
        Gizmos.DrawSphere(transform.position, activationDistance);
        Gizmos.color = new Color(1f, 1f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, activationDistance);

        Gizmos.color = new Color(1f, 0f, 0f, 0.04f);
        Gizmos.DrawSphere(transform.position, deactivationDistance);
        Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, deactivationDistance);

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 1.5f);
    }
#endif
}