using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;

public class RoomBrowser : MonoBehaviour
{
    public static RoomBrowser Instance { get; private set; }

    public event Action<string> OnRoomSelected;

    [Header("UI")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private Transform _listContainer;
    [SerializeField] private GameObject _roomEntryPrefab;
    [SerializeField] private TextMeshProUGUI _emptyLabel;
    [SerializeField] private Button _joinButton;
    [SerializeField] private Button _openCreatePanelButton;

    [Header("Referencias")]
    [SerializeField] private RoomCreator _roomCreator;

    private string _selectedSessionName;

    private readonly List<GameObject> _spawnedEntries = new();

    private readonly HashSet<string> _currentRoomNames = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        _joinButton.interactable = false;
        _joinButton.onClick.AddListener(OnJoinClicked);
        _openCreatePanelButton.onClick.AddListener(OnOpenCreatePanel);
    }

    public void RefreshList(List<SessionInfo> sessions)
    {
        Debug.Log($"[RoomBrowser] RefreshList — {sessions.Count} sesión(es)");

        _currentRoomNames.Clear();

        foreach (var entry in _spawnedEntries)
            Destroy(entry);

        _spawnedEntries.Clear();

        bool selectedStillExists = false;

        foreach (var session in sessions)
        {
            if (!session.IsOpen || !session.IsVisible)
            {
                Debug.Log($"[RoomBrowser] '{session.Name}' omitida (open:{session.IsOpen} visible:{session.IsVisible})");
                continue;
            }

            _currentRoomNames.Add(session.Name.ToLowerInvariant());

            var entryGO = Instantiate(_roomEntryPrefab, _listContainer);
            var entry = entryGO.GetComponent<RoomEntryItem>();

            entry.Setup(
                session.Name,
                session.PlayerCount,
                session.MaxPlayers,
                SelectRoom
            );

            if (session.Name == _selectedSessionName)
            {
                entry.SetSelected(true);
                selectedStillExists = true;
            }

            _spawnedEntries.Add(entryGO);

            Debug.Log($"[RoomBrowser] Entrada: '{session.Name}' {session.PlayerCount}/{session.MaxPlayers}");
        }

        if (!selectedStillExists)
        {
            _selectedSessionName = null;
            _joinButton.interactable = false;
        }

        _emptyLabel.gameObject.SetActive(_spawnedEntries.Count == 0);
    }

    public bool RoomExists(string roomName)
    {
        if (string.IsNullOrWhiteSpace(roomName))
            return false;

        return _currentRoomNames.Contains(
            roomName.Trim().ToLowerInvariant()
        );
    }

    private void SelectRoom(string sessionName)
    {
        _selectedSessionName = sessionName;
        _joinButton.interactable = true;

        foreach (var entryGO in _spawnedEntries)
        {
            var entry = entryGO.GetComponent<RoomEntryItem>();
            entry.SetSelected(entry.SessionName == sessionName);
        }

        Debug.Log($"[RoomBrowser] Sala seleccionada: '{sessionName}'");
    }

    private void OnJoinClicked()
    {
        if (string.IsNullOrEmpty(_selectedSessionName))
        {
            Debug.LogWarning("[RoomBrowser] Ninguna sala seleccionada");
            return;
        }

        Debug.Log($"[RoomBrowser] Uniéndose a: '{_selectedSessionName}'");

        Hide();

        OnRoomSelected?.Invoke(_selectedSessionName);
    }

    private void OnOpenCreatePanel()
    {
        Hide();
        _roomCreator.Show();
    }

    public void Show() => _panel.SetActive(true);
    public void Hide() => _panel.SetActive(false);
}
