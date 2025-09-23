using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector3 moveDirection;
    //public Vector3 look;  nouse delta? para rotar la c�mara?
    public bool interact;
}