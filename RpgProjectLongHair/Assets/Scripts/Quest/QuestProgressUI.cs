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
        _txtProgress.text = BuildProgressText(data);
    }

    private string BuildProgressText(QuestDataSO data)
    {
        var lines = new System.Text.StringBuilder();

        foreach (var step in data.questSteps)
        {
            string label = GetStepLabel(step.targetId);
            int current = Mathf.Min(step.currentAmount, step.amount);
            lines.AppendLine($"{label}: {current}/{step.amount}");
        }

        return lines.ToString().TrimEnd();
    }

    private string GetStepLabel(string targetId)
    {
        return targetId switch
        {
            "Kill_Mission_Enemy" => "Enemigos",
            "Kill_Enemy" => "Enemigos",
            "Pick_WeaponPart" => "Partes recogidas",
            "Craft_Weapon" => "Arma crafteada",
            _ => targetId
        };
    }

    public void Hide()
    {
        _panel.SetActive(false);
    }
}