using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }


    public void PickupItem(PickupableItem item)
    {
        if (item == null || item.GetItemData() == null)
            return;

        Debug.Log($"Take: {item.GetItemData().name}");


        if (item.Runner != null)
            item.Runner.Despawn(item.Object);
        else
            Destroy(item.gameObject);
    }

    public UnityEngine.Events.UnityAction GetPickupAction(PickupableItem item)
    {
        return () => PickupItem(item);
    }
}