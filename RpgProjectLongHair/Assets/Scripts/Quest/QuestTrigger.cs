using System;
using Unity.VisualScripting;
using UnityEngine;


public class QuestTrigger : MonoBehaviour
{
    [SerializeField] private string _missionId = QuestIds.QUEST_TEST;
    [SerializeField] private QuestOfferUI _questOfferUI;
    [SerializeField] private GameObject _canvasNPC;     

    private bool _isPlayerInZone;
    private QuestController _questController;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[QuestTrigger] OnTriggerEnter: {other.name} tag={other.tag}");

        if (!other.CompareTag("Player")) return;
        //_canvasNPC.SetActive(true);  
        _questController = other.GetComponent<QuestController>();
        _isPlayerInZone = true;

        Debug.Log($"[QuestTrigger] QuestController encontrado: {_questController != null}");
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("[QuestTrigger] Player sale del collider");
            _isPlayerInZone = false;
            _questController = null;
        }
    }

    private void Update()
    {
        // Mostrar panel NPC solo si el player está cerca Y no tiene misión activa
        if (_questController != null)
        {
            float dist = Vector3.Distance(transform.position, _questController.transform.position);
            bool mostrar = dist <= 3f
                        && _questController.CurrentQuest == null  // sin misión activa
                        && !_questOfferUI.IsOpen;                 // sin UI offer abierta
            _canvasNPC.SetActive(mostrar);
        }

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
            _canvasNPC.SetActive(false); // - forzar ocultado inmediato
            _questOfferUI.Show(questData, _questController);
        }

    }
}
