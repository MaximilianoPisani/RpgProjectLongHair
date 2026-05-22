using System.Collections.Generic;
using UnityEngine;

public class CraftingTable : MonoBehaviour
{
    [Header("Recetas")]
    [SerializeField] private CraftRecipeSO _recipeFungi;
    [SerializeField] private CraftRecipeSO _recipeMecano;

    [Header("Detección")]
    [SerializeField] private float _interactRadius = 3f;

    [Header("UI")]
    [SerializeField] private CraftingUI _craftingUI;
    [SerializeField] private GameObject _canvasTable;

    private PlayerInventoryData _playerInventory;
    private CraftRecipeSO _currentRecipe;

    private bool _localPlayerRegistered = false;

    public float InteractRadius => _interactRadius;

    private static readonly List<CraftingTable> _registry = new List<CraftingTable>();
    public static IReadOnlyList<CraftingTable> All => _registry;

    private void Update()
    {
        if (!_localPlayerRegistered) return;
        if (_playerInventory == null) return;

        _canvasTable.SetActive(!_craftingUI.IsOpen);

        if (!Input.GetKeyDown(KeyCode.E)) return;
        if (_currentRecipe == null) return;
        if (_craftingUI.IsOpen) return;

        _craftingUI.Show(_currentRecipe, _playerInventory);
    }

    private void OnEnable() => _registry.Add(this);
    private void OnDisable() => _registry.Remove(this);

    public void RegisterLocalPlayer(PlayerInventoryData inventory, CharacterType characterType)
    {
        _playerInventory = inventory;
        _localPlayerRegistered = true; 
        _currentRecipe = characterType == CharacterType.Fungi ? _recipeFungi : _recipeMecano;
        _canvasTable.SetActive(true);
        Debug.Log($"[CraftingTable] Jugador local registrado: {characterType}");
    }

    public void UnregisterLocalPlayer()
    {
        _playerInventory = null;
        _currentRecipe = null;
        _localPlayerRegistered = false; 

        _canvasTable.SetActive(false);

        if (_craftingUI.IsOpen)
            _craftingUI.Hide();
    }
}
