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

    [Header("Jump Settings")]
    [SerializeField] private float baseJumpImpulse = 8f;      // Salto base
    [SerializeField] private float runningJumpBonus = 2f;     // Bonus al correr
    [SerializeField] private float airControl = 0.3f;

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
        bool isInAir = sm != null && (sm.CurrentState is PlayerJumpState || sm.CurrentState is PlayerFallState);
        bool isAiming = false;
        if (sm?.CurrentState is PlayerRangeState rangeState)
            isAiming = !rangeState.IsReloading;

        _ncc.LockRotation = isAiming;

        float targetSpeed = input.sprint ? sprintSpeed : walkSpeed;
        _ncc.maxSpeed = targetSpeed;

        // Delegar control de rotación al estado activo
        _ncc.LockRotation = isAiming;

        Vector3 moveDir = new Vector3(input.moveDirection.x, 0f, input.moveDirection.z);

        if (Object.HasStateAuthority)
        {
            float originalAcceleration = _ncc.acceleration;

            if (isInAir)
            {
                _ncc.acceleration = originalAcceleration * airControl;
                _ncc.Move(moveDir);
                _ncc.acceleration = originalAcceleration;
            }
            else
            {
                _ncc.Move(moveDir); // NCC mueve pero NO rota si LockRotation = true
            }
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
    public void Jump()
    {
        if (_ncc != null)
        {
            bool useCoyoteTime = _coyoteTimer > 0f;

            // Calcular impulso dinámico basado en velocidad actual
            float currentSpeed = GetHorizontalSpeed();
            float speedRatio = Mathf.Clamp01(currentSpeed / sprintSpeed);

            // Más impulso si está corriendo
            float jumpImpulse = baseJumpImpulse + (runningJumpBonus * speedRatio);

            // Agregar impulso horizontal en la dirección del movimiento
            Vector3 currentVelocity = _ncc.Velocity;
            Vector3 horizontalVel = new Vector3(currentVelocity.x, 0, currentVelocity.z);

            // Preservar momentum horizontal (hace el salto más dinámico)
            if (horizontalVel.magnitude > 0.1f)
            {
                Vector3 jumpDirection = horizontalVel.normalized;
                // Pequeño boost horizontal en la dirección del movimiento
                _ncc.Velocity = new Vector3(
                    currentVelocity.x * 1.1f, // 10% boost horizontal
                    currentVelocity.y,
                    currentVelocity.z * 1.1f
                );
            }

            _ncc.Jump(ignoreGrounded: useCoyoteTime, overrideImpulse: jumpImpulse);

            Debug.Log($"[JUMP PHYSICS] Impulse: {jumpImpulse:F1}, Speed: {currentSpeed:F1}, Grounded: {_ncc.Grounded}, FinalVel: {_ncc.Velocity}");
        }
    }
    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position + Vector3.up * (groundCheckRadius + 0.05f);
        Vector3 endPoint = origin + Vector3.down * (groundCheckDistance + 0.05f);

        // Esfera inicial del cast
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, groundCheckRadius);

        // Esfera final del cast
        bool grounded = IsPhysicallyGrounded();
        Gizmos.color = grounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(endPoint, groundCheckRadius);

        // Línea que conecta ambas esferas
        Gizmos.color = Color.white;
        Gizmos.DrawLine(origin, endPoint);
    }
}