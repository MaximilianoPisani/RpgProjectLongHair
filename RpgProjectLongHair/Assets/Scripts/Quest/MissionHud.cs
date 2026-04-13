using UnityEngine;

public class MissionHud : MonoBehaviour
{
    [SerializeField] private QuestProgressUI _questProgressUI;
    [SerializeField] private QuestFailedUI _questFailedUI;
    [SerializeField] private QuestCompleteUI _questCompletedUI;

    private void OnEnable()
    {
        MissionEvents.OnMissionComplete += OnMissionComplete;
        MissionEvents.OnUpdateProgress += OnUpdateMissionData;
        MissionEvents.OnMissionFailed += OnMissionFailed;
        MissionEvents.OnMissionStart += OnMissionStart;
    }

    private void OnMissionStart(QuestDataSO data)
    {
        if (_questProgressUI == null) return; // protección
        _questProgressUI.Show(data);
    }

    private void OnMissionComplete(QuestDataSO data)
    {
        if (_questProgressUI == null) return;
        if (_questCompletedUI == null) return;

        _questProgressUI.Hide();

        //Mostrar un popup de victoria
        _questCompletedUI.Show(data);

        //stg_victory 
    }

    private void OnUpdateMissionData(QuestDataSO data)
    {
        //utilizar los datos de la mision para poder mostrarlo en un hud
        if (_questProgressUI == null) return;
        _questProgressUI.UpdateProgress(data);
    }

    private void OnMissionFailed(QuestDataSO data)
    {
        Debug.Log($"[MissionHud] OnMissionFailed llamado. _questProgressUI={_questProgressUI}, _questFailedUI={_questFailedUI}");
        if (_questProgressUI == null) return;
        if (_questFailedUI == null) return;

        _questProgressUI.Hide();

        _questFailedUI.Show();
        //TODO Mostrar popup de Mission fallida
        // stg_defeat
    }

    private void OnDisable()
    {
        MissionEvents.OnMissionComplete -= OnMissionComplete;
        MissionEvents.OnUpdateProgress -= OnUpdateMissionData;
        MissionEvents.OnMissionFailed -= OnMissionFailed;
        MissionEvents.OnMissionStart -= OnMissionStart;
    }
}
