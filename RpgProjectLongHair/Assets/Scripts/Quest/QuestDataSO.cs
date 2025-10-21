using UnityEngine;
using System.Collections.Generic;

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
}
