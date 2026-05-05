using UnityEngine;
using Fusion;

public class PickupableCraftItem : NetworkBehaviour
{
    [SerializeField] private CraftItemSO _craftItemSO;
    [SerializeField] private Transform _visualRoot;

    public CraftItemSO CraftItemSO => _craftItemSO;
    public int ItemId => _craftItemSO != null ? _craftItemSO.id : 0;

    public override void Spawned()
    {
        SetupVisual();
    }

    private void SetupVisual()
    {
        if (_craftItemSO == null || _visualRoot == null) return;
        if (_craftItemSO.visualPrefab == null) return;

        foreach (Transform child in _visualRoot)
            Destroy(child.gameObject);

        Instantiate(_craftItemSO.visualPrefab, _visualRoot);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestDespawn()
    {
        if (Object == null || !Object.IsValid) return;
        Runner.Despawn(Object);
    }
}