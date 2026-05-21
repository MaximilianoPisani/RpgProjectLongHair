using UnityEngine;

/// <summary>
/// Poner en el mismo objeto que DamageableObject (o en cualquier hijo).
/// Conectar DamageableObject.OnActivated ? DoorOpenListener.Execute()
/// </summary>
public class DoorOpenListener : MonoBehaviour
{
    [SerializeField] private NetworkDoor door;

    public void Execute()
    {
        door?.Open();
    }
}