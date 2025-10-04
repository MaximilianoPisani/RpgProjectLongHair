using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Representa los datos persistentes del jugador.
/// En esta primera versi�n s�lo contiene nivel y experiencia.
/// M�s adelante se ampliar� con inventario, equipo, etc.
/// </summary>
[System.Serializable]
public class PlayerProfile
{
    public int level = 1;
    public int currentExp = 0;
    public int expToNext = 100;

    // Campos para expansi�n futura:
    public List<int> inventoryItemIds = new();     // IDs de �tems (opcional)
    public Dictionary<string, int> equipped = new(); // slots equipados
}
