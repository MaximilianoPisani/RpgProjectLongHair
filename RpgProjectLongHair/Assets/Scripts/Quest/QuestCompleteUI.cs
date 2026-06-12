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
    private void Update()
    {
        if (!_panel.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.Escape))
            Hide();
    }
    public void Show(QuestDataSO data)
    {
        if (_panel.activeSelf) return;
        _panel.SetActive(true);
        UiStateManager.OpenBlockingUI();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        _txtTitle.text = "¡Misión completada!";
        _txtRewards.text = $"XP: {data.xp}";
    }

    private void OnContinue()
    {
        Hide();
    }

    private void Hide()
    {
        if (!_panel.activeSelf) return;
        _panel.SetActive(false);
        UiStateManager.CloseBlockingUI();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable()
    {
    }
}
