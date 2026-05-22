using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestCompleteUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject _panel;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI _txtTitle;
    [SerializeField] private TextMeshProUGUI _txtRewards;

    [Header("Button")]
    [SerializeField] private Button _btnContinue;

    private void Start()
    {
        _btnContinue.onClick.AddListener(OnContinue);
    }

    public void Show(QuestDataSO data)
    {
        _panel.SetActive(true);

        UiStateManager.OpenBlockingUI();

        Debug.Log("[QuestCompleteUI] Show llamado");
        _txtTitle.text = "¡Misión completada!";
        _txtRewards.text = $"XP: {data.xp}";
        _panel.SetActive(true);
    }

    private void OnContinue()
    {
        _panel.SetActive(false);

        UiStateManager.CloseBlockingUI();
    }
    private void OnDisable()
    {
        if (_panel != null && _panel.activeSelf)
        {
            _panel.SetActive(false);
            UiStateManager.CloseBlockingUI();
        }
    }
}
