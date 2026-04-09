using UnityEngine;

public class QuestTester : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            var questController = FindFirstObjectByType<QuestController>();
            if (questController != null)
            {
                Debug.Log("[QuestTester] Iniciando misión quest_test_001.");
                questController.RPC_StartMission(QuestIds.QUEST_TEST, default);
            }
            else
            {
                Debug.Log("[QuestTester] ERROR: No se encontró QuestController en escena.");
            }
                
        }
    }
}