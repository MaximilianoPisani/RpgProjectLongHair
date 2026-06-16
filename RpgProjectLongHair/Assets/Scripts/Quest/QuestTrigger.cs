using UnityEngine;
using Fusion;

public class QuestTrigger : MonoBehaviour
{
    [SerializeField] private string _missionId = QuestIds.QUEST_TEST;
    [SerializeField] private QuestOfferUI _questOfferUI;
    [SerializeField] private GameObject _canvasNPC;
    [SerializeField] private float _interactRadius = 3f;

    private QuestController _localQuestController;
    private PlayerCharacterData _localCharacterData;

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

        // Chequeo de character requerido
        bool characterAllowed = true;
        if (questData.requiredCharacter != CharacterType.None)
        {
            characterAllowed = _localCharacterData != null
                && _localCharacterData.characterType == questData.requiredCharacter;
        }

        bool canInteract =
            isNear &&
            characterAllowed &&
            !QuestController.HasActiveMission() &&
            !_questOfferUI.IsOpen &&
            !UiStateManager.HasBlockingUI;

        _canvasNPC.SetActive(canInteract);

        if (!canInteract)
            return;

        if (!Input.GetKeyDown(KeyCode.E))
            return;

        _canvasNPC.SetActive(false);

        _questOfferUI.Show(questData, _localQuestController);
    }

    private void FindLocalPlayer()
    {
        foreach (var controller in FindObjectsByType<QuestController>(FindObjectsSortMode.None))
        {
            if (controller.HasInputAuthority)
            {
                _localQuestController = controller;
                // Cachear el CharacterData del mismo GameObject
                _localCharacterData = controller.GetComponent<PlayerCharacterData>();
                return;
            }
        }
    }
}