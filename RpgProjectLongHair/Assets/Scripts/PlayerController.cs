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

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput<NetworkInputData>(out var inputData))
        {

            Vector3 move = new Vector3(inputData.move.x, 0, inputData.move.y);
            _controller.Move(move * speed * Runner.DeltaTime);


            _isGrounded = _controller.isGrounded;
            if (_isGrounded && _velocity.y < 0)
                _velocity.y = -2f;


            if (inputData.jump && _isGrounded)
            {
                _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }

        _velocity.y += gravity * Runner.DeltaTime;
        _controller.Move(_velocity * Runner.DeltaTime);
    }
}