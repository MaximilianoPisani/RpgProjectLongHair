using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float speed = 5f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;

    private CharacterController _controller;
    private Vector3 _velocity;
    private bool _isGrounded;

    private bool _jumpRequested;
    private PickupableItem _itemInRange;

    private NetworkInputData _networkInput;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
    }

    public void RequestJump()
    {
        _jumpRequested = true;
    }

    public void SetItemInRange(PickupableItem item)
    {
        _itemInRange = item;

        if (_itemInRange != null)
        {
            var playerInput = GetComponent<PlayerInput>();
            playerInput.actions["Interact"].performed += ctx =>
            {
                InventoryManager.Instance.PickupItem(_itemInRange);
                _itemInRange = null;
            };
        }
    }

    public override void FixedUpdateNetwork()
    {

        if (!GetInput<NetworkInputData>(out var inputData))
            return;

        _networkInput = inputData;

        Vector3 horizontalMove = new Vector3(_networkInput.move.x, 0, _networkInput.move.y);
        _controller.Move(horizontalMove * speed * Runner.DeltaTime);

        _velocity.y += gravity * Runner.DeltaTime;
        _controller.Move(new Vector3(0, _velocity.y, 0) * Runner.DeltaTime);

        _isGrounded = _controller.isGrounded;
        if (_isGrounded && _velocity.y < 0)
            _velocity.y = -2f;

        if ((_networkInput.jump || _jumpRequested) && _isGrounded)
        {
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            Debug.Log("[Player] Jump executed!");
        }

        _jumpRequested = false;
    }
}