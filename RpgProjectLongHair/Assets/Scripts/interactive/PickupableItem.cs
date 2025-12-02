using UnityEngine;
using Fusion;

// Componente item que puede ser recogido
public class PickupableItem : NetworkBehaviour
{
    [SerializeField] private ItemSO itemDataSO;
    public ItemSO ItemDataSO => itemDataSO;

    public ItemData ItemData => new ItemData // Datos que se envían al inventario (NetworkArray)
    {
        id = itemDataSO != null ? itemDataSO.id : 0,
        type = itemDataSO != null ? itemDataSO.type : ItemType.Consumable
    };

    private void Reset() // Asegura que el collider sea trigger si fue agregado al prefab
    {
        var col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }
}