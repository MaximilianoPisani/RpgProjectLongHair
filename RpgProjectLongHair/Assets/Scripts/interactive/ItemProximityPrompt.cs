using UnityEngine;
using TMPro;
using Fusion;

public class ItemProximityPrompt : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject _promptPanel;
    [SerializeField] private TextMeshProUGUI _txtPrompt;

    [Header("Settings")]
    [SerializeField] private float _detectionRadius = 2f;
    [SerializeField] private string _keyLabel = "E";

    [Header("References")]
    [SerializeField] private GameObject _inventoryPanel; // ? arrastrá el panel del inventario aquí

    private PickupableItem _nearestItem;
    private PickupableCraftItem _nearestCraftItem;

    private void Start()
    {
        var netObj = GetComponentInParent<NetworkObject>();
        if (netObj != null && !netObj.HasInputAuthority)
        {
            if (_promptPanel != null) Destroy(_promptPanel);
            Destroy(this);
            return;
        }
        HidePrompt();
    }

    private void Update()
    {
        ScanNearbyItems();
    }

    private bool IsInventoryOpen()
    {
        return _inventoryPanel != null && _inventoryPanel.activeSelf;
    }

    private void ScanNearbyItems()
    {
        if (IsInventoryOpen())
        {
            HidePrompt();
            return;
        }

        _nearestItem = null;
        _nearestCraftItem = null;
        float closestDist = _detectionRadius;

        Collider[] hits = Physics.OverlapSphere(transform.position, _detectionRadius);

        foreach (var hit in hits)
        {
            float dist = Vector3.Distance(transform.position, hit.transform.position);

            if (hit.TryGetComponent<PickupableItem>(out var item))
            {
                if (item.Object == null || !item.Object.IsValid) continue;

                if (item.IsAlreadyPicked) continue;
                if (dist < closestDist)
                {
                    closestDist = dist;
                    _nearestItem = item;
                    _nearestCraftItem = null;
                }
                continue;
            }

            if (hit.TryGetComponent<PickupableCraftItem>(out var craftItem))
            {
                if (craftItem.Object == null || !craftItem.Object.IsValid) continue;

                if (craftItem.IsAlreadyPicked) continue;
                if (dist < closestDist)
                {
                    closestDist = dist;
                    _nearestCraftItem = craftItem;
                    _nearestItem = null;
                }
            }
        }

        if (_nearestItem != null)
        {
            string itemName = _nearestItem.ItemDataSO != null
                ? _nearestItem.ItemDataSO.itemName : "Item";
            ShowPrompt($"[{_keyLabel}] Collect {itemName}");
        }
        else if (_nearestCraftItem != null)
        {
            string craftName = _nearestCraftItem.CraftItemSO != null
                ? _nearestCraftItem.CraftItemSO.itemName : "Item";
            ShowPrompt($"[{_keyLabel}] Collect {craftName}");
        }
        else
        {
            HidePrompt();
        }
    }

    private void ShowPrompt(string message)
    {
        if (_promptPanel != null) _promptPanel.SetActive(true);
        if (_txtPrompt != null) _txtPrompt.text = message;
    }

    private void HidePrompt()
    {
        if (_promptPanel != null) _promptPanel.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRadius);
    }
}