using UnityEngine;

public class CraftingTable : MonoBehaviour
{
    [Header("Receta")]
    [SerializeField] private CraftRecipeSO _recipe;

    [Header("Detección")]
    [SerializeField] private float _interactRadius = 3f;

    [Header("UI")]
    [SerializeField] private CraftingUI _craftingUI;
    [SerializeField] private GameObject _canvasTable; // panel "presioná E"

    private PlayerInventoryData _playerInventory;
    private bool _playerInRange;

    private void Update()
    {
        if (_playerInventory == null) return;

        float dist = Vector3.Distance(transform.position, _playerInventory.transform.position);
        bool cerca = dist <= _interactRadius;

        if (!cerca)
        {
            _playerInventory = null;
            _canvasTable.SetActive(false);
            return;
        }

        _canvasTable.SetActive(!_craftingUI.IsOpen);

        if (!Input.GetKeyDown(KeyCode.E)) return;

        _craftingUI.Show(_recipe, _playerInventory);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        var inventory = other.GetComponent<PlayerInventoryData>();
        if (inventory == null) return;
        _playerInventory = inventory;
        _playerInRange = true;
    }
}