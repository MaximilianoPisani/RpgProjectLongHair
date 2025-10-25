using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class PlayerUIController : NetworkBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;

    private bool initialized = false;

    public override void Spawned()
    {
        if (!HasInputAuthority)
        {
            if (inventoryPanel != null) Destroy(inventoryPanel.gameObject);
            if (openButton != null) Destroy(openButton.gameObject);
            if (closeButton != null) Destroy(closeButton.gameObject);
            Destroy(this);
            return;
        }

        SetupLocalUI();
    }

    private void SetupLocalUI()
    {
        if (initialized) return;
        initialized = true;

        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        if (openButton != null)
        {
            openButton.onClick.RemoveAllListeners();
            openButton.onClick.AddListener(() =>
            {
                if (inventoryPanel != null)
                    inventoryPanel.SetActive(true);
            });
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() =>
            {
                if (inventoryPanel != null)
                    inventoryPanel.SetActive(false);
            });
        }
    }
}