using UnityEngine;
using TMPro;

public class QuestProgressUI : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private TextMeshProUGUI _txtQuestName;
    [SerializeField] private TextMeshProUGUI _txtProgress;

    public void Show(QuestDataSO data)
    {
        _panel.SetActive(true);
        UpdateProgress(data);
    }

    public void UpdateProgress(QuestDataSO data)
    {
        _txtQuestName.text = data.questName;
        int current = 0;
        int total = 0;
        foreach (var step in data.questSteps)
        {
            current += step.currentAmount;
            total += step.amount;
        }
        _txtProgress.text = $"Enemigos: {current}/{total}";
    }

    public void Hide()
    {
        _panel.SetActive(false);
    }
}
