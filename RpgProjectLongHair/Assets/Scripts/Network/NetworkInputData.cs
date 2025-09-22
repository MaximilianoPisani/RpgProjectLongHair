using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector3 moveDirection;
    public bool interact;
}