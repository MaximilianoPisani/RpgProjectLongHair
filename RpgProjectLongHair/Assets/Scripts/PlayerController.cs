using Fusion;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    public float speed = 5f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;

    private CharacterController _controller;
    private Vector3 _velocity;
    private bool _isGrounded;

    private bool _jumpRequested;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
    }

    public void RequestJump()
    {
        _jumpRequested = true;
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput<NetworkInputData>(out var inputData)) return;

        Vector3 horizontalMove = new Vector3(inputData.move.x, 0, inputData.move.y);
        _controller.Move(horizontalMove * speed * Runner.DeltaTime);

        _velocity.y += gravity * Runner.DeltaTime;
        _controller.Move(new Vector3(0, _velocity.y, 0) * Runner.DeltaTime);

        _isGrounded = _controller.isGrounded;
        if (_isGrounded && _velocity.y < 0)
            _velocity.y = -2f; 

        if ((inputData.jump || _jumpRequested) && _isGrounded)
        {
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            Debug.Log("[Player] Jump executed!");
        }

        _jumpRequested = false;

         
    }
}