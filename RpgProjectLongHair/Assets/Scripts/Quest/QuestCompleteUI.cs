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
        Debug.Log("[QuestCompleteUI] Show llamado");
        _txtTitle.text = "¡Misión completada!";
        _txtRewards.text = $"XP: {data.xp}";
        _panel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnContinue()
    {
        _panel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
    }
}
