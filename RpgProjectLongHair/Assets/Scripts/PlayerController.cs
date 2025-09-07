using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float speed = 5f;
    public float jumpForce = 5f;
    public float gravityMultiplier = 2f;

    private Rigidbody _rb;
    private PlayerInput _playerInput;
    private bool _isGrounded;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _playerInput = GetComponent<PlayerInput>();
        _rb.freezeRotation = true;
    }

    void FixedUpdate()
    {

        Vector2 moveInput = _playerInput.actions["Move"].ReadValue<Vector2>();
        Vector3 move = new Vector3(moveInput.x, 0f, moveInput.y).normalized;


        Vector3 displacement = move * speed * Time.fixedDeltaTime;
        _rb.MovePosition(_rb.position + displacement);

   
        if (!_isGrounded)
        {
            _rb.AddForce(Vector3.up * Physics.gravity.y * (gravityMultiplier - 1), ForceMode.Acceleration);
        }
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (context.performed && _isGrounded)
        {
            _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            _isGrounded = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            _isGrounded = true;
        }
    }
}