using UnityEngine;

public class MissionHud : MonoBehaviour
{
    private void OnEnable()
    {
        MissionEvents.OnMissionComplete += OnMissionComplete;
        MissionEvents.OnUpdateProgress += OnUpdateMissionData;
        MissionEvents.OnMissionFailed += OnMissionFailed;
    }

    private void OnMissionComplete(QuestDataSO data)
    {
        //Mostrar un popup de victoria
        //stg_victory 
    }

    private void OnUpdateMissionData(QuestDataSO data)
    {
        //utilizar los datos de la mision para poder mostrarlo en un hud
    }

    private void OnMissionFailed(QuestDataSO data)
    {
        //Mostrar popup de Mission fallida
        // stg_defeat
    }

    private void OnDisable()
    {
        MissionEvents.OnMissionComplete -= OnMissionComplete;
        MissionEvents.OnUpdateProgress -= OnUpdateMissionData;
        MissionEvents.OnMissionFailed -= OnMissionFailed;
    }
}
