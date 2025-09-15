using UnityEngine;

public class PickupableItem : MonoBehaviour
{
    [SerializeField] private Item _itemData; 
    [SerializeField] private bool _autoPickup = false; 

    private bool playerInRange = false;
    private GameObject player;

    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            Pickup();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            player = other.gameObject;

            if (_autoPickup)
                Pickup();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            player = null;
        }
    }

    private void Pickup()
    {
        if (_itemData == null)
        {
            Debug.LogWarning("PickupableItem has no itemData assigned!");
            return;
        }

        InventoryManager.Instance.AddItem(_itemData);

        if (_itemData.type == ItemType.Weapon || _itemData.type == ItemType.Armor)
        {
            string defaultSlot = _itemData.type == ItemType.Weapon ? "RightHand" : "Body";
            InventoryManager.Instance.EquipItem(defaultSlot, _itemData);
        }

        Debug.Log($"Picked up {_itemData.name}");

        Destroy(gameObject);
    }
}