using UnityEngine;

public class QuestTrigger : MonoBehaviour
{
    [SerializeField] private string _missionId = QuestIds.QUEST_TEST;
    private bool _isPlayerInZone;
    private QuestController _questController;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _questController = other.GetComponent<QuestController>();
        _isPlayerInZone = true;
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
        if (_questController == null) return;      // no hay QuestController -> ignorar
        if (_questController.CurrentQuest != null) return; // ya tiene misión -> ignorar

        // Si pasó todos los filtros -> iniciar mision
        _questController.RPC_StartMission(_missionId, default);
    }
}
