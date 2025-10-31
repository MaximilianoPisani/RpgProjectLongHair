using Unity.VisualScripting;
using UnityEngine;

public class MissionHud : MonoBehaviour
{
    private void OnEnable()
    {
        MissionEvents.OnMissionComplete += OnMissionComplete;
        MissionEvents.OnUpdateProgress += OnUpdateMissionData;
    }

    private void OnMissionComplete(QuestDataSO data)
    {
        //Mostrar un popup de victoria
    }

    private void OnUpdateMissionData(QuestDataSO data)
    {
        //utilizar los datos de la mision para poder mostrarlo en un hud
    }

    private void OnDisable()
    {
        MissionEvents.OnMissionComplete -= OnMissionComplete;
        MissionEvents.OnUpdateProgress -= OnUpdateMissionData;
    }
}
