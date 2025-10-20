using UnityEngine;
using Fusion;

public class PickupableItem : NetworkBehaviour
{ [SerializeField] private ItemSO itemDataSO; 
   public ItemSO ItemDataSO => itemDataSO; 
    public ItemData ItemData => new ItemData {id = itemDataSO.id, type = itemDataSO.type};
    private void Reset() 
    { 
        var col = GetComponent<Collider>(); 
        if (col != null) 
            col.isTrigger = true; 
    }
}