using System;
using Unity.VisualScripting;
using UnityEngine;


public class QuestTrigger : MonoBehaviour
{
    [SerializeField] private string _missionId = QuestIds.QUEST_TEST;
    [SerializeField] private QuestOfferUI _questOfferUI;
    [SerializeField] private GameObject _canvasNPC;
    [SerializeField] private float _interactRadius = 3f;

    private QuestController _questController;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[QuestTrigger] OnTriggerEnter: {other.name} tag={other.tag}");

        if (!other.CompareTag("Player")) return;
        _questController = other.GetComponent<QuestController>();

        Debug.Log($"[QuestTrigger] QuestController encontrado: {_questController != null}");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _questController = null;
        
    }

    private void Update()
    {
        if (_questController == null) return;

        float dist = Vector3.Distance(transform.position, _questController.transform.position);
        bool cerca = dist <= _interactRadius;

        // Si el player se alejó, limpiamos la referencia manualmente
        if (!cerca)
        {
            _questController = null;
            _canvasNPC.SetActive(false);
            return;
        }

        // Mostrar canvas NPC solo si está cerca, sin misión y sin UI abierta
        bool mostrar = _questController.CurrentQuest == null && !_questOfferUI.IsOpen;
        _canvasNPC.SetActive(mostrar);

        // Interacción con E
        if (!Input.GetKeyDown(KeyCode.E)) return;
        if (_questController.CurrentQuest != null) return;

        var questData = Resources.Load<QuestDataSO>($"Quest/{_missionId}");
        if (questData != null)
        {
            _canvasNPC.SetActive(false);
            _questOfferUI.Show(questData, _questController);
        }
    }
}
