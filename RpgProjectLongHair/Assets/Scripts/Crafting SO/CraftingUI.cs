using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
        _btnCraft.interactable = _playerInventory.HasAllCraftItems(_recipe.requiredItems);
    }

    private void OnCraft()
    {
        if (_recipe == null || _recipe.resultItem == null)
            return;

        if (_playerInventory == null)
            return;

        if (!_playerInventory.HasAllCraftItems(_recipe.requiredItems))
        {
            Debug.LogWarning("[Crafting] Faltan items");
            return;
        }

        foreach (var item in _recipe.requiredItems)
        {
            bool removed = _playerInventory.RemoveItem(item.id);

            if (!removed)
            {
                Debug.LogError($"[Crafting] No se pudo remover {item.id}");
                return;
            }
        }

        var weaponData = new ItemData
        {
            id = _recipe.resultItem.id,
            type = _recipe.resultItem.type
        };

        bool added = _playerInventory.AddItem(weaponData);

        if (!added)
        {
            Debug.LogError("[Crafting] No se pudo agregar arma");
            return;
        }

        Debug.Log($"[Crafting] Arma creada: {_recipe.resultItem.itemName}");

        Hide();
    }

    private System.Collections.IEnumerator ShowScreenLog(string msg)
    {
        var go = new GameObject("DebugLog");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        go.AddComponent<UnityEngine.UI.CanvasScaler>();
        go.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var text = textGo.AddComponent<TMPro.TextMeshProUGUI>();
        text.text = msg;
        text.fontSize = 18;
        text.color = Color.yellow;
        var rt = textGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(10, 10);
        rt.offsetMax = new Vector2(-10, -10);

        yield return new WaitForSeconds(10f);
        Destroy(go);
    }

    public void Hide()
    {
        _panel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
    }
}