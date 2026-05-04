using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CraftingUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject _panel;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI _txtRecipeName;
    [SerializeField] private TextMeshProUGUI _txtItems;

    [Header("Buttons")]
    [SerializeField] private Button _btnCraft;
    [SerializeField] private Button _btnClose;

    private CraftRecipeSO _recipe;
    private PlayerInventoryData _playerInventory;

    public bool IsOpen => _panel.activeSelf;

    private void Start()
    {
        _btnCraft.onClick.AddListener(OnCraft);
        _btnClose.onClick.AddListener(Hide);
        _panel.SetActive(false);
    }

    public void Show(CraftRecipeSO recipe, PlayerInventoryData inventory)
    {
        _recipe = recipe;
        _playerInventory = inventory;
        _txtRecipeName.text = _recipe.recipeName;
        RefreshItemList();
        _panel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void RefreshItemList()
    {
        string itemsText = "";
        foreach (var item in _recipe.requiredItems)
        {
            bool tiene = _playerInventory.HasCraftItem(item.id);
            string estado = tiene ? "[OK]- " : "[X]- ";
            itemsText += $"{estado} {item.itemName}\n";
        }
        _txtItems.text = itemsText;

        // Habilitar botón solo si tiene todos los items
        _btnCraft.interactable = _playerInventory.HasAllCraftItems(_recipe.requiredItems);
    }

    private void OnCraft()
    {
        if (_recipe.resultItem == null)
        {
            Debug.LogError("[CraftingUI] resultItem no asignado en la receta");
            return;
        }

        // Consumir los 3 items del inventario
        foreach (var item in _recipe.requiredItems)
            _playerInventory.RemoveItem(item.id);

        // Agregar el arma crafteada al inventario
        var weaponData = new ItemData
        {
            id = _recipe.resultItem.id,
            type = _recipe.resultItem.type
        };
        _playerInventory.AddItem(weaponData);

        Debug.Log($"[CraftingUI] Crafteado: {_recipe.resultItem.itemName}");
        Hide();
    }

    private void Hide()
    {
        _panel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
    }
}