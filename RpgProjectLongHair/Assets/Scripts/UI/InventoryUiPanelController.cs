using UnityEngine;
using Fusion;

public class InventoryUIPanelController : NetworkBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject _inventoryPanel;

    private bool _initialized = false;
    private bool _isOpen = false;

    public override void Spawned()
    {
        if (!HasInputAuthority)
        {
            if (_inventoryPanel != null) Destroy(_inventoryPanel.gameObject);
            Destroy(this);
            return;
        }
        SetupLocalUI();
    }

    private void Update()
    {
        if (!HasInputAuthority) return;

        bool togglePressed = Input.GetKeyDown(KeyCode.I) || (Input.GetKeyDown(KeyCode.Escape) && _isOpen);
        if (togglePressed)
        {
            _isOpen = Input.GetKeyDown(KeyCode.I) ? !_isOpen : false;
            if (_inventoryPanel != null)
                _inventoryPanel.SetActive(_isOpen);
            PlayerCamera.Local?.SetCursorLocked(!_isOpen);
        }
    }

    private void SetupLocalUI()
    {
        if (_initialized) return;
        _initialized = true;

        if (_inventoryPanel != null)
            _inventoryPanel.SetActive(false);
    }
}