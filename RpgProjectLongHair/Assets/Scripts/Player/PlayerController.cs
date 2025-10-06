using UnityEngine;
using UnityEngine.InputSystem;
using Fusion;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(NetworkCharacterController))]
[RequireComponent(typeof(NetworkedInventory))]
public class PlayerController : NetworkBehaviour
{
    [Header("Move")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _turnLerp = 0.2f; // rotación suave hacia el movimiento

    [Header("Pickup Settings")]
    [SerializeField] private float _pickupRange = 2f;

    private PlayerInput _playerInput;
    private NetworkCharacterController _characterController;
    private NetworkedInventory _inventory;
    [SerializeField] private Transform _cam; // opcional: asignar desde el inspector

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        _characterController = GetComponent<NetworkCharacterController>();
        _inventory = GetComponent<NetworkedInventory>();
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

    // Llamá esto al spawnear si tenés una cámara follow dedicada
    public void SetCamera(Transform cam) => _cam = cam;

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData input)) return;

        // 1) Input en plano XZ (x=derecha, z=adelante)
        Vector3 inputDir = new Vector3(input.moveDirection.x, 0f, input.moveDirection.z);

        // 2) Resolver cámara (solo una vez para el local), fallback a Camera.main
        if (_cam == null && HasInputAuthority && Camera.main != null)
            _cam = Camera.main.transform;

        // 3) Calcular dirección mundo relativa a la cámara (solo yaw)
        Vector3 worldDir;
        if (_cam != null)
        {
            Vector3 camF = Vector3.ProjectOnPlane(_cam.forward, Vector3.up).normalized;
            Vector3 camR = Vector3.ProjectOnPlane(_cam.right, Vector3.up).normalized;
            worldDir = (camF * inputDir.z + camR * inputDir.x);
        }
        else
        {
            // Fallback a ejes de mundo si no hay cámara
            worldDir = inputDir;
        }

        // 4) Mover (normalizar evita más velocidad en diagonales)
        Vector3 move = worldDir.sqrMagnitude > 1e-4f ? worldDir.normalized : Vector3.zero;
        _characterController.Move(move * _moveSpeed * Runner.DeltaTime);

        // 5) Rotar el personaje hacia la dirección de movimiento (suave)
        if (move.sqrMagnitude > 1e-4f)
        {
            Vector3 look = move; look.y = 0f;
            if (look.sqrMagnitude > 1e-4f)
                transform.forward = Vector3.Slerp(transform.forward, look, _turnLerp);
        }

        // 6) Interactuar (si viene flag en el input)
        if (input.interact)
            TryPickupItem();
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!HasInputAuthority) return;
        TryPickupItem();
    }

    private void TryPickupItem()
    {
        if (!Object.HasStateAuthority) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, _pickupRange);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<PickupableItem>(out var pickup))
            {
                int newItemId = Random.Range(1, 999999);
                if (_inventory.AddItem(pickup.ToItemData(newItemId)))
                    Runner.Despawn(pickup.Object);
                break;
            }
        }
    }
}
