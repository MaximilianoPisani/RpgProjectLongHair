using UnityEngine;
using Fusion;

// Componente item que puede ser recogido
public class PickupableItem : NetworkBehaviour
{
    [SerializeField] private ItemSO itemDataSO;
    [SerializeField] private Transform _visualRoot;
    public ItemSO ItemDataSO => itemDataSO;

    public override void Spawned()
    {
        SetupVisual();
    }

    private void SetupVisual()
    {
        if (itemDataSO == null) return;

        // Limpiar hijos
        foreach (Transform child in _visualRoot)
            Destroy(child.gameObject);

        if (itemDataSO.isDualWield)
        {
            Instantiate(itemDataSO.rightHandPrefab, _visualRoot);
            Instantiate(itemDataSO.leftHandPrefab, _visualRoot);
        }
        else if (itemDataSO.equipPrefab != null)
        {
            Instantiate(itemDataSO.equipPrefab, _visualRoot);
        }
    }
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

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestDespawn()
    {
        if (Object == null || !Object.IsValid) return;
        Runner.Despawn(Object);
    }
}