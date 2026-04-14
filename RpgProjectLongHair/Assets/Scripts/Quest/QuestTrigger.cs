using Unity.VisualScripting;
using UnityEngine;

public class QuestTrigger : MonoBehaviour
{
    [SerializeField] private string _missionId = QuestIds.QUEST_TEST;
    [SerializeField]private QuestOfferUI _questOfferUI;

    private bool _isPlayerInZone;
    private QuestController _questController;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[QuestTrigger] OnTriggerEnter: {other.name} tag={other.tag}");

        if (!other.CompareTag("Player")) return;
        _questController = other.GetComponent<QuestController>();
        _isPlayerInZone = true;

        Debug.Log($"[QuestTrigger] QuestController encontrado: {_questController != null}");
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerInZone = false;
            _questController = null;
        }
    }

    private void Update()
    {
        if (!_isPlayerInZone) return;        // no hay player cerca -> ignorar
        if (!Input.GetKeyDown(KeyCode.E)) return;  // no presionó E -> ignorar

        Debug.Log($"[QuestTrigger] E presionado - controller={_questController != null} - questActiva={_questController?.CurrentQuest != null}");


        if (_questController == null) return;      // no hay QuestController -> ignorar
        if (_questController.CurrentQuest != null) return; // ya tiene misión -> ignorar

        // Si pasó todos los filtros -> iniciar mision , inicio directo sin UI
        //_questController.RPC_StartMission(_missionId, default);

        // Inicio de quest con UI
        var questData = Resources.Load<QuestDataSO>($"Quest/{_missionId}");

        Debug.Log($"[QuestTrigger] questData={questData != null} - missionId={_missionId}");
        Debug.Log($"[QuestTrigger] questOfferUI={_questOfferUI != null}");

        if (questData != null)
        {
            _questOfferUI.Show(questData, _questController);
        }

    }
}
