using System;
using UnityEngine;

[System.Serializable] 
public class QuestSteps
{
    public string targetId; // Kill_Enemy, Kill_Player, Pick_Item (general), Pick_Item_chest_001(especifico), buy_item
    public int amount; //20
    public string failureId;

    [NonSerialized] public bool isComplete;
    [NonSerialized] public int currentAmount;
}
