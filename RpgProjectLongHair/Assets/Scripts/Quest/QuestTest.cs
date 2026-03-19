using UnityEngine;
using Fusion;

public class QuestTester : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            var questController = FindFirstObjectByType<QuestController>();
            if (questController != null)
                questController.RPC_StartMission("quest_test_001", default);
        }
    }
}