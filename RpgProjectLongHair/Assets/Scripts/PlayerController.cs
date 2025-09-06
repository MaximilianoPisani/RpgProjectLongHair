using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float jumpForce = 250f, force = 10f;

    private Rigidbody _rb;
    private PlayerInput _playerInput;
    private Vector3 _input;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _playerInput = GetComponent<PlayerInput>();
    }

    void Update()
    {
        Vector2 moveInput = _playerInput.actions["Move"].ReadValue<Vector2>();
        _input = new Vector3(moveInput.x, 0f, moveInput.y);

        Debug.Log(_input);
    }

    private void FixedUpdate()
    {
        _rb.AddForce(_input * force);
    }

    public void Jump(InputAction.CallbackContext callbackContext)
    {
        if (callbackContext.performed)
        {
            _rb.AddForce(Vector3.up * jumpForce);
            Debug.Log("Jump");
            Debug.Log(callbackContext.phase);
        }
    }
}