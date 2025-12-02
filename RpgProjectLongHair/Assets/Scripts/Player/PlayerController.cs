using UnityEngine;
using UnityEngine.InputSystem;
using Fusion;
using UnityEngine.Windows;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(NetworkCharacterController))]
public class PlayerController : NetworkBehaviour
{
    private Transform _cam;
    private PlayerInput _playerInput;
    private NetworkCharacterController _characterController;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        _characterController = GetComponent<NetworkCharacterController>();
    }

    private void OnEnable()
    {
        if (_playerInput != null)
            _playerInput.actions["Interact"].performed += OnInteract;
    }

    private void OnDisable()
    {
        if (_playerInput != null)
            _playerInput.actions["Interact"].performed -= OnInteract;
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData input))
            return;

        if (GetInput(out NetworkInputData data))
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                data.aimRotation,
                Runner.DeltaTime * 10f
            );
        }

        Vector3 inputDir = new Vector3(input.moveDirection.x, 0f, input.moveDirection.z);

        if (_cam == null && HasInputAuthority && Camera.main != null)
            _cam = Camera.main.transform;

        Vector3 moveDir = inputDir;
        moveDir.y = 0;
        moveDir.Normalize();

        _characterController.Move(moveDir);

        if (moveDir.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(moveDir),
                10f * Runner.DeltaTime
            );
        }

        if (input.jump && Mathf.Abs(_characterController.Velocity.y) < 0.05f)
            _characterController.Jump();

        if (input.interact)

            GetComponent<PlayerInventoryController>().TryPickupItem();
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (!HasInputAuthority) return;
        GetComponent<PlayerInventoryController>().TryPickupItem();
    }
}