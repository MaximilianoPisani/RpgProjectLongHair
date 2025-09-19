using Fusion;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    private NetworkCharacterController _characterController;
    [SerializeField] private Renderer _renderer;

    public float moveSpeed = 5f;

    private void Awake()
    {
        _characterController = GetComponent<NetworkCharacterController>();
    }

    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            _renderer.material.color = Color.white;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData inputPlayer))
        {
            Vector3 move = inputPlayer.moveDirection.normalized;
            _characterController.Move(move * moveSpeed * Runner.DeltaTime);
        }
    }
}