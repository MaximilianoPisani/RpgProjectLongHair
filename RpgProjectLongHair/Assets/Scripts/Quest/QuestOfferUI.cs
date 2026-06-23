using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestOfferUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject _panel;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI _txtQuestName;
    [SerializeField] private TextMeshProUGUI _txtQuestDescription;

    [Header("Buttons")]
    [SerializeField] private Button _btnAccept;
    [SerializeField] private Button _btnCancel;

    private QuestController _questController;
    private QuestDataSO _questData;

    public bool IsOpen => _panel.activeSelf;

    private void Awake()
    {
        _btnAccept.onClick.AddListener(OnAccept);
        _btnCancel.onClick.AddListener(OnCancel);
        _panel.SetActive(false);
    }

    public void Show(QuestDataSO data, QuestController controller)
    {
        UiStateManager.OpenBlockingUI();

        _questData = data;
        _questController = controller;

        _txtQuestName.text = data.questName;
        _txtQuestDescription.text = data.questDescription;

        _panel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnAccept()
    {
        _questController.RPC_StartMission(_questData.questId, default);

        // NUEVO: Sonido al aceptar quest
        AudioManager.Instance.PlayTakeQuest();

        _panel.SetActive(false);

        UiStateManager.CloseBlockingUI();
    }

    private void OnCancel()
    {
        _panel.SetActive(false);

        UiStateManager.CloseBlockingUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && _panel.activeSelf)
        {
            _panel.SetActive(false);

            UiStateManager.CloseBlockingUI();
        }
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