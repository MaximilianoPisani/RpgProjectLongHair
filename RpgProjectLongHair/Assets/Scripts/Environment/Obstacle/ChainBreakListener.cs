using UnityEngine;

/// <summary>
/// Poner en el mismo objeto que DamageableObject (o en cualquier hijo).
/// Conectar DamageableObject.OnActivated ? ChainBreakListener.Execute()
/// </summary>
public class ChainBreakListener : MonoBehaviour
{
    [SerializeField] private ChainPhysicsSetup chainSetup;
    [SerializeField] private int breakAtLinkIndex = 2;
    [SerializeField] private Vector3 breakImpulse = new Vector3(0f, -10f, 0f);
    [SerializeField] private float destroyDelay = 5f;

    public void Execute()
    {
        chainSetup?.BreakAtIndex(breakAtLinkIndex, breakImpulse);

        if (destroyDelay > 0f)
            Invoke(nameof(DestroyChain), destroyDelay);
    }

    private void DestroyChain()
    {
        chainSetup?.DestroyAll(0f);
    }
}