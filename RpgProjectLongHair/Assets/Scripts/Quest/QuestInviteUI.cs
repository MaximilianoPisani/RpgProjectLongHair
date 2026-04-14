using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestInviteUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject _panel;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI _txtQuestName;

    [Header("Buttons")]
    [SerializeField] private Button _btnAccept;
    [SerializeField] private Button _btnDecline;

    private QuestController _questController;
    private string _missionId;

    private void Start()
    {
        _btnAccept.onClick.AddListener(OnAccept);
        _btnDecline.onClick.AddListener(OnDecline);
    }

    public void Show(QuestDataSO data, QuestController controller)
    {
        _questController = controller;
        _missionId = data.questId;
        _txtQuestName.text = $"Misión: {data.questName}";
        _panel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnAccept()
    {
        _questController.RPC_StartMission(_missionId, default);

        var questData = Resources.Load<QuestDataSO>($"Quest/{_missionId}");
        if (questData != null && questData.teleportDestination != Vector3.zero)
        {
            _questController.RPC_RequestTeleport(questData.teleportDestination);
            Debug.Log($"[QuestInviteUI] Teleport solicitado a {questData.teleportDestination}");
        }

        Hide();
    }

    private void OnDecline()
    {
        Hide();
    }

    private void Hide()
    {
        _panel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
    }
}
