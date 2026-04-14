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

    private void Awake()
    {
        _btnAccept.onClick.AddListener(OnAccept);
        _btnCancel.onClick.AddListener(OnCancel);
        _panel.SetActive(false);
    }

    public void Show(QuestDataSO data, QuestController controller)
    {
        Debug.Log($"[QuestOfferUI] Show - panel={_panel != null} - txtName={_txtQuestName != null} - btnAccept={_btnAccept != null}");

        _questData = data;
        _questController = controller;
        _txtQuestName.text = data.questName;
        _txtQuestDescription.text = data.questDescription;
        _panel.SetActive(true);

        // Mostrar cursor y bloquear input de camara
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnAccept()
    {
        _questController.RPC_StartMission(_questData.questId, default);
        _panel.SetActive(false);

        // Ocultar cursor y devolver el control
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Forzar el foco al juego
        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);

    }

    //private void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.Escape))
    //    {
    //        _panel.SetActive(false);
    //        Cursor.lockState = CursorLockMode.Locked;
    //        Cursor.visible = false;
    //    }
    //}
    private void OnCancel()
    {
        _panel.SetActive(false);
    }
}