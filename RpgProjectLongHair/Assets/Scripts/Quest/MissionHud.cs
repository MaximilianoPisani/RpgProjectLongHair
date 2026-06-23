using UnityEngine;

public class MissionHud : MonoBehaviour
{
    [SerializeField] private QuestProgressUI _questProgressUI;
    [SerializeField] private QuestFailedUI _questFailedUI;
    [SerializeField] private QuestCompleteUI _questCompletedUI;
    [SerializeField] private QuestInviteUI _questInviteUI;

    private QuestController _localQuestController;

    private void OnEnable()
    {
        MissionEvents.OnMissionStart += OnMissionStart;
        MissionEvents.OnMissionComplete += OnMissionComplete;
        MissionEvents.OnUpdateProgress += OnUpdateMissionData;
        MissionEvents.OnMissionFailed += OnMissionFailed;
        MissionEvents.OnQuestInviteReceived += OnQuestInviteReceived;
    }

    private void OnMissionStart(QuestDataSO data)
    {
        if (_questProgressUI == null) return;
        var lqc = GetLocalController();
        if (lqc == null) return;
        if (lqc.CurrentQuest == null) return;
        if (lqc.CurrentQuest.questId != data.questId) return;
        _questProgressUI.Show(data);
    }

    private void OnMissionComplete(QuestDataSO data)
    {
        if (_questCompletedUI == null) return;
        if (_questProgressUI != null) _questProgressUI.Hide();

        // NUEVO: Sonido de victoria
        AudioManager.Instance.PlayVictoryQuest();

        _questCompletedUI.Show(data);
    }

    private void OnUpdateMissionData(QuestDataSO data)
    {
        if (_questProgressUI == null) return;
        var lqc = GetLocalController();
        if (lqc == null) return;
        if (lqc.CurrentQuest == null) return;
        if (lqc.CurrentQuest.questId != data.questId) return;
        _questProgressUI.UpdateProgress(data);
    }

    private void OnMissionFailed(QuestDataSO data)
    {
        var lqc = GetLocalController();
        if (lqc == null) return;
        if (_questFailedUI == null) return;

        // NUEVO: Sonido de derrota
        AudioManager.Instance.PlayDeffeatQuest();

        _questFailedUI.Show();
        if (_questProgressUI != null) _questProgressUI.Hide();
    }

    private void OnQuestInviteReceived(QuestDataSO data)
    {
        if (_questInviteUI == null) return;
        var lqc = GetLocalController();
        if (lqc != null)
        {
            _questInviteUI.Show(data, lqc);
            return;
        }
        StartCoroutine(ShowInviteWhenReady(data));
    }

    private System.Collections.IEnumerator ShowInviteWhenReady(QuestDataSO data)
    {
        QuestController lqc = null;
        float timeout = 3f;
        while (lqc == null && timeout > 0f)
        {
            yield return null;
            timeout -= Time.deltaTime;
            lqc = GetLocalController();
        }
        if (lqc == null)
        {
            Debug.LogWarning("[MissionHud] No se encontró QuestController local para mostrar invitación");
            yield break;
        }
        _questInviteUI.Show(data, lqc);
    }

    private QuestController GetLocalController()
    {
        if (_localQuestController != null) return _localQuestController;
        foreach (var controller in FindObjectsByType<QuestController>(FindObjectsSortMode.None))
        {
            if (controller.HasInputAuthority)
            {
                _localQuestController = controller;
                break;
            }
        }
        return _localQuestController;
    }

    private void OnDisable()
    {
        MissionEvents.OnMissionStart -= OnMissionStart;
        MissionEvents.OnMissionComplete -= OnMissionComplete;
        MissionEvents.OnUpdateProgress -= OnUpdateMissionData;
        MissionEvents.OnMissionFailed -= OnMissionFailed;
        MissionEvents.OnQuestInviteReceived -= OnQuestInviteReceived;
    }
}