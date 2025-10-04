using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Representa los datos persistentes del jugador.
/// En esta primera versión sólo contiene nivel y experiencia.
/// Más adelante se ampliará con inventario, equipo, etc.
/// </summary>
[System.Serializable]
public class PlayerProfile
{
    public int level = 1;
    public int currentExp = 0;
    public int expToNext = 100;

    // Campos para expansión futura:
    public List<int> inventoryItemIds = new();     // IDs de ítems (opcional)
    public Dictionary<string, int> equipped = new(); // slots equipados
}
