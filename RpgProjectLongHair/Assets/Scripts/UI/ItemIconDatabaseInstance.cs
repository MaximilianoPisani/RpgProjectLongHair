using UnityEngine;

public class ItemIconDatabaseInstance : MonoBehaviour
{
    public static ItemIconDatabaseInstance Instance { get; private set; }

    [SerializeField] private ItemDatabaseUIIcons iconDatabase;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public Sprite GetIcon(ItemType type)
    {
        return iconDatabase != null ? iconDatabase.GetIcon(type) : null;
    }
}