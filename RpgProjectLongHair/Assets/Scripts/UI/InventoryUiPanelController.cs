using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class InventoryUIPanelController : NetworkBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject _inventoryPanel;
    [SerializeField] private Button _openButton;
    [SerializeField] private Button _closeButton;

    private bool _initialized = false;

    public override void Spawned() // Se ejecuta cuando el objeto aparece en escena 
    {
        // se elimina UI en clientes que no son el input authority
        if (!HasInputAuthority)
        {
            if (_inventoryPanel != null) Destroy(_inventoryPanel.gameObject);
            if (_openButton != null) Destroy(_openButton.gameObject);
            if (_closeButton != null) Destroy(_closeButton.gameObject);
            Destroy(this);
            return;
        }


        SetupLocalUI();
    }
    private void SetupLocalUI() // Configura los botones y el panel para abrir/cerrar el inventario
    {
        if (_initialized) return;
        _initialized = true;


        if (_inventoryPanel != null)
            _inventoryPanel.SetActive(false);


        if (_openButton != null)
        {
            _openButton.onClick.RemoveAllListeners();
            _openButton.onClick.AddListener(() =>
            {
                if (_inventoryPanel != null)
                    _inventoryPanel.SetActive(true);
            });
        }


        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveAllListeners();
            _closeButton.onClick.AddListener(() =>
            {
                if (_inventoryPanel != null)
                    _inventoryPanel.SetActive(false);
            });
        }
    }
}