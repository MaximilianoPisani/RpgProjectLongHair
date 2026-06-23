using Fusion;
using UnityEngine;

public class MinimapIconRotation : NetworkBehaviour // Hereda de NetworkBehaviour
{
    [SerializeField] private Vector3 rotationOffset = new Vector3(90f, 0f, 0f);
    private Transform _myPlayerTransform;

    public override void Spawned()
    {
        // Solo el jugador que es dueño de este objeto busca su propio transform
        if (Object.HasInputAuthority)
        {
            _myPlayerTransform = transform.root;
        }
    }

    private void LateUpdate()
    {
        // Si no soy el dueño, no hago nada
        if (_myPlayerTransform == null) return;

        transform.rotation = Quaternion.Euler(
            rotationOffset.x,
            _myPlayerTransform.eulerAngles.y + rotationOffset.y,
            rotationOffset.z
        );
    }
}