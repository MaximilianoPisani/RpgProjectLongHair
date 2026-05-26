using UnityEngine;
using Fusion;

public class QuestTrigger : MonoBehaviour
{
    [SerializeField] private string _missionId = QuestIds.QUEST_TEST;
    [SerializeField] private QuestOfferUI _questOfferUI;
    [SerializeField] private GameObject _canvasNPC;
    [SerializeField] private float _interactRadius = 3f;

    private QuestController _localQuestController;

    private void Start()
    {
        _canvasNPC.SetActive(false);
    }

    private void Update()
    {
        if (_localQuestController == null)
        {
            FindLocalPlayer();
            if (_localQuestController == null)
            {
                _canvasNPC.SetActive(false);
                return;
            }
        }

        float distance = Vector3.Distance(
            transform.position,
            _localQuestController.transform.position);

        bool isNear = distance <= _interactRadius;

        QuestDataSO questData =
            Resources.Load<QuestDataSO>($"Quest/{_missionId}");

        if (questData == null)
        {
            _canvasNPC.SetActive(false);
            return;
        }

        bool alreadyCompleted =
            _localQuestController.HasCompletedQuest(questData.questId);

        bool canInteract =
            isNear &&
            !alreadyCompleted &&
            _localQuestController.CurrentQuest == null &&
            !_questOfferUI.IsOpen &&
            !UiStateManager.HasBlockingUI;

        _canvasNPC.SetActive(canInteract);

        if (!canInteract)
            return;

        if (!Input.GetKeyDown(KeyCode.E))
            return;

        _canvasNPC.SetActive(false);

        _questOfferUI.Show(
            questData,
            _localQuestController);
    }

    private void FindLocalPlayer()
    {
        foreach (var controller in FindObjectsByType<QuestController>(FindObjectsSortMode.None))
        {
            if (controller.HasInputAuthority)
            {
                _localQuestController = controller;
                return;
            }
        }
    }
}