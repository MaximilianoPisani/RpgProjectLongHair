using UnityEngine;
using System.Collections.Generic;
using System;

[CreateAssetMenu(fileName = "QuestDataSO", menuName = "Scriptable Objects/QuestDataSO")]
public class QuestDataSO : ScriptableObject
{
    public string questId; // quest_forest_001
    public string questName; //find the special item
    public string questDescription; //investigar el forest y encontrar el item escondido
    public List<QuestSteps> questSteps;
    public List<QuestSteps> failureSteps; 
    public bool allowTeleportParty;
    public Vector3 teleportDestination; // ó puede ser a una escena! public Scene teleportDestination;
    //para que el otro player vaya directo a la misión si es aceptada!
    public int xp;
    public int coins;

    public bool UpdateProgress(string id, int amount, out bool success)
    {
        success = false;
        var allComplete = true;
        foreach (var steps in questSteps)
        {
            steps.UpdateProgress(id, amount);
            if (!steps.isComplete)
            {
                allComplete = false;
            }
        }

        var allFailure = true;
        foreach (var steps in failureSteps)
        {
            steps.UpdateProgress(id, amount);
            if (!steps.isComplete)
            {
                allFailure = false;
            }
        }

        if (allFailure)
        {
            success = false;
            return true;
        }

        if (allComplete)
        {
            success = true;
            return true;
        }
        return false;
    }
}
