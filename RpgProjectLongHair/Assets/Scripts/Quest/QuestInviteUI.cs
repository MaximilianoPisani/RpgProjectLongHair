using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class QuestInviteUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject _panel;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI _txtQuestName;

    [Header("Buttons")]
    [SerializeField] private Button _btnAccept;
    [SerializeField] private Button _btnDecline;

    [Header("Timer")]
    [SerializeField] private float _inviteTimeout = 10f; // - ajustás en Inspector
    [SerializeField] private TextMeshProUGUI _txtTimer;

    private QuestController _questController;
    private string _missionId;
    private Coroutine _timerCoroutine;

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

        // Reiniciar timer si ya había uno corriendo
        if (_timerCoroutine != null)
            StopCoroutine(_timerCoroutine);
        _timerCoroutine = StartCoroutine(InviteTimer());
    }

    private IEnumerator InviteTimer()
    {
        float timeLeft = _inviteTimeout;

        while (timeLeft > 0)
        {
            _txtTimer.text = $"Expira en: {Mathf.CeilToInt(timeLeft)}s";
            timeLeft -= Time.deltaTime;
            yield return null;
        }

        _txtTimer.text = "";
        Debug.Log("[QuestInviteUI] Timer expirado, cerrando panel");
        Hide();
    }

    private void OnAccept()
    {
        if (_timerCoroutine != null)
            StopCoroutine(_timerCoroutine);

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
        if (_timerCoroutine != null)
            StopCoroutine(_timerCoroutine);
        Hide();
    }

    private void Hide()
    {
        _timerCoroutine = null;
        _panel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
    }
}
