using Unity.VisualScripting;
using UnityEngine;
using System;

public static class MissionEvents 
{   
    public static Action<QuestDataSO> OnMissionComplete;
    public static Action<QuestDataSO> OnMissionFailed;
    public static Action<QuestDataSO> OnUpdateProgress;
}
