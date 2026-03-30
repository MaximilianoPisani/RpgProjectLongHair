using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkCharacterController))]
public class Player : NetworkBehaviour
{
    private NetworkCharacterController _ncc;

    public float rotationSpeed = 10f;
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float sprintSpeed = 5f;
    public float SprintSpeed => sprintSpeed;

    public override void Spawned()
    {
        _ncc = GetComponent<NetworkCharacterController>();
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData input)) return;

        float targetSpeed = input.sprint ? sprintSpeed : walkSpeed;
        _ncc.maxSpeed = targetSpeed;

        Vector3 moveDir = new Vector3(input.moveDirection.x, 0f, input.moveDirection.z);
        _ncc.Move(moveDir);

        if (moveDir.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(moveDir),
                rotationSpeed * Runner.DeltaTime
            );
        }

        if (input.jump && _ncc.Grounded)
            _ncc.Jump();

        if (input.interact && Object.HasInputAuthority)
            RPC_RequestPickup();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestPickup(RpcInfo info = default)
    {
        GetComponent<PlayerInventoryController>()?.TryPickupItem();
    }
    public void TeleportTo(Vector3 position)
    {
        if (_ncc != null)
        {
            _ncc.Velocity = Vector3.zero;
            _ncc.Teleport(position);
        }
        else
            transform.position = position;
    }
    public float GetHorizontalSpeed()
    {
        if (_ncc == null) return 0f;

        Vector3 vel = _ncc.Velocity;
        return new Vector3(vel.x, 0, vel.z).magnitude;
    }
    public void Move(Vector3 dir) { } 
    public void Jump() => _ncc.Jump();
    public bool IsGrounded() => _ncc != null && _ncc.Grounded;
}