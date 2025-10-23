using System;
using UnityEngine;

[System.Serializable] 
public class QuestSteps
{
    public string targetId; // ||Gain_XP|| Kill_Enemy, Kill_Player, Pick_Item (general), Pick_Item_chest_001(especifico), buy_item
    public int amount; // ||1500_XP||   20

    [NonSerialized] public bool isComplete;
    [NonSerialized] public int currentAmount;

    public void UpdateProgress(string id, int amountProgress)
    {
        if (isComplete) return;

        if (string.CompareOrdinal(id, targetId) != 0) return;

        currentAmount += amountProgress;
        if (currentAmount >= amount)
        {
            isComplete = true;
        }

    }
}
