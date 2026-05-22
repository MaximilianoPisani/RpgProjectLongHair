using UnityEngine;
using Fusion;

public class CraftingTableDetector : NetworkBehaviour
{
    private PlayerInventoryData _inventory;
    private PlayerCharacterData _characterData;
    private CraftingTable _currentTable;

    public override void Spawned()
    {
        if (!HasInputAuthority)
        {
            Destroy(this); 
            return;
        }

        _inventory = GetComponent<PlayerInventoryData>();
        _characterData = GetComponent<PlayerCharacterData>();
        Debug.Log($"[CraftingTableDetector] Spawned local player. Mesas registradas: {CraftingTable.All.Count}");
    }

    private void Update()
    {
        if (_inventory == null || _characterData == null) return;

        CraftingTable nearest = null;
        float minDist = float.MaxValue;

        foreach (var table in CraftingTable.All)
        {
            if (table == null) continue;
            float dist = Vector3.Distance(transform.position, table.transform.position);
            if (dist <= table.InteractRadius && dist < minDist)
            {
                minDist = dist;
                nearest = table;
            }
        }

        if (nearest != null && nearest != _currentTable)
        {
            if (_currentTable != null)
                _currentTable.UnregisterLocalPlayer();

            _currentTable = nearest;
            _currentTable.RegisterLocalPlayer(_inventory, _characterData.characterType);
            Debug.Log($"[CraftingTableDetector] Entró a mesa: {nearest.name}");
        }
        else if (nearest == null && _currentTable != null)
        {
            _currentTable.UnregisterLocalPlayer();
            Debug.Log($"[CraftingTableDetector] Salió de mesa: {_currentTable.name}");
            _currentTable = null;
        }
    }

    private void OnDestroy()
    {
        if (_currentTable != null)
            _currentTable.UnregisterLocalPlayer();
    }
}