using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestCancelMenuUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject _panel;

    [Header("Buttons")]
    [SerializeField] private Button _btnCancelQuest;
    [SerializeField] private Button _btnClose;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI _txtTitle;

    private QuestController _questController;
    private bool _isOpen = false;

    public bool IsOpen => _isOpen;

    private void Awake()
    {
        if (_panel != null)
            _panel.SetActive(false);

        if (_btnCancelQuest != null)
            _btnCancelQuest.onClick.AddListener(OnCancelQuest);

        if (_btnClose != null)
            _btnClose.onClick.AddListener(OnClose);
    }

    public void Show(QuestController controller)
    {
        if (_isOpen) return;

        _questController = controller;
        _isOpen = true;

        if (_panel != null)
            _panel.SetActive(true);

        UiStateManager.OpenBlockingUI();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (_txtTitle != null)
            _txtTitle.text = "¿Abandonar misión?";
    }

    public void Hide()
    {
        if (!_isOpen) return;

        _isOpen = false;

        if (_panel != null)
            _panel.SetActive(false);

        _questController = null;

        UiStateManager.CloseBlockingUI();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnCancelQuest()
    {
        if (_questController != null)
        {
            _questController.RPC_LeaveQuest();
        }
        Hide();
    }

    private void OnClose()
    {
        Hide();
    }

    private void Update()
    {
        if (!_isOpen) return;

        if (Input.GetKeyDown(KeyCode.Escape))
            Hide();
    }

    private void OnDisable()
    {
        if (_isOpen)
            Hide();
    }

    private void OnDestroy()
    {
        if (_btnCancelQuest != null)
            _btnCancelQuest.onClick.RemoveListener(OnCancelQuest);
        if (_btnClose != null)
            _btnClose.onClick.RemoveListener(OnClose);
    }
}
