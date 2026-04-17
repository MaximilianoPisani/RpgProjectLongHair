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

    [Header("Ground Detection")]
    [SerializeField] private float groundCheckRadius = 0.25f;
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundLayers = -1;

    [Header("Coyote Time")]
    [SerializeField] private float coyoteTime = 0.12f;
    private float _coyoteTimer = 0f;

    public override void Spawned()
    {
        _ncc = GetComponent<NetworkCharacterController>();
        var cam = GetComponentInChildren<PlayerCamera>();
        var cc = GetComponent<CharacterController>();

        if (Object.HasInputAuthority)
        {
            if (cam != null) cam.Init(transform);
            if (cc != null) cc.enabled = true;
        }
        else
        {
            if (cam != null) cam.gameObject.SetActive(false);
            if (cc != null) cc.enabled = false;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData input)) return;

        UpdateCoyoteTimer(Runner.DeltaTime);

        var sm = GetComponent<PlayerStateMachine>();
        bool inputLocked = sm != null && sm.IsInputLocked;

        float targetSpeed = input.sprint ? sprintSpeed : walkSpeed;
        _ncc.maxSpeed = targetSpeed;

        Vector3 moveDir = inputLocked
            ? Vector3.zero
            : new Vector3(input.moveDirection.x, 0f, input.moveDirection.z);

        if (Object.HasStateAuthority)
        {
            _ncc.Move(moveDir);
        }

        if (moveDir.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(moveDir),
                rotationSpeed * Runner.DeltaTime
            );
        }

        if (input.interact && Object.HasInputAuthority)
            GetComponent<PlayerInventoryController>()?.TryPickupItem();
    }

    private void UpdateCoyoteTimer(float deltaTime)
    {
        if (IsPhysicallyGrounded())
            _coyoteTimer = coyoteTime;
        else
            _coyoteTimer -= deltaTime;
    }

    private bool IsPhysicallyGrounded()
    {
        if (_ncc != null && _ncc.Grounded) return true;

        Vector3 origin = transform.position + Vector3.up * (groundCheckRadius + 0.05f);

        return Physics.SphereCast(
            origin,
            groundCheckRadius,
            Vector3.down,
            out _,
            groundCheckDistance + 0.05f,
            groundLayers,
            QueryTriggerInteraction.Ignore
        );
    }

    public bool IsGrounded() => _coyoteTimer > 0f;

    public bool IsPhysicallyGroundedPublic() => IsPhysicallyGrounded();

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
}