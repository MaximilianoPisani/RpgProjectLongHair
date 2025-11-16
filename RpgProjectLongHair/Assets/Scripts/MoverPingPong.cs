using UnityEngine;

public class MoverPingPong : MonoBehaviour
{
    public Transform puntoA;
    public Transform puntoB;
    public float velocidad = 5f;

    private Vector3 destinoActual;

    void Start()
    {
        transform.position = puntoA.position;
        destinoActual = puntoB.position;
    }

    void Update()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            destinoActual,
            velocidad * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, destinoActual) < 0.1f)
        {
            destinoActual = destinoActual == puntoA.position ? puntoB.position : puntoA.position;
        }
    }
}

