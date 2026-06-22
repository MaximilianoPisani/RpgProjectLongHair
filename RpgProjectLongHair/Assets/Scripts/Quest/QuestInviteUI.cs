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
    [SerializeField] private float _inviteTimeout = 10f;
    [SerializeField] private TextMeshProUGUI _txtTimer;

    private QuestController _questController;
    private string _missionId;
    private Coroutine _timerCoroutine;

    private bool _isShowing = false;

    private void Start()
    {
        _btnAccept.onClick.AddListener(OnAccept);
        _btnDecline.onClick.AddListener(OnDecline);
    }

    public void Show(QuestDataSO data, QuestController controller)
    {
        if (_isShowing) return;

        _isShowing = true;
        _questController = controller;
        _missionId = data.questId;
        _txtQuestName.text = $"Misión: {data.questName}";
        _panel.SetActive(true);
        UiStateManager.OpenBlockingUI();

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
        StopTimer();
        var questData = Resources.Load<QuestDataSO>($"Quest/{_missionId}");
        if (questData == null) { Hide(); return; }
        _questController.RPC_RequestJoinMission(_missionId);
        Hide();
    }

    private void OnDecline()
    {
        StopTimer();
        Hide();
    }

    private void StopTimer()
    {
        if (_timerCoroutine != null)
        {
            StopCoroutine(_timerCoroutine);
            _timerCoroutine = null;
        }
    }

    private void Hide()
    {
        if (!_isShowing) return;

        _isShowing = false;
        _panel.SetActive(false);
        UiStateManager.CloseBlockingUI();
    }

    private void OnDisable()
    {
        if (_isShowing)
        {
            _isShowing = false;
            UiStateManager.CloseBlockingUI();
        }
    }

    private void OnDestroy()
    {
        StopTimer();
        if (_isShowing)
        {
            _isShowing = false;
            UiStateManager.CloseBlockingUI();
        }
    }
}