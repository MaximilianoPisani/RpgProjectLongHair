using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector3 moveDirection;
    public bool interact;
    public bool jump;
    public int equipSlot;
    public Quaternion aimRotation;
    public NetworkBool LockOnPressed;
    public NetworkBool attack;
    public NetworkBool attackRange;
    public Vector3 shootDirection;
    public bool sprint;
    public NetworkBool attackJustPressed;
}