using UnityEngine;

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    [Header("Panels — en orden de flujo")]
    [SerializeField] private GameObject _loginPanel;
    [SerializeField] private GameObject _characterPanel;
    [SerializeField] private GameObject _lobbyPanel;
    [SerializeField] private GameObject _rageCanvas;

    private bool _isLoggedIn = false;

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
        ShowLogin();
    }

    public void OnLoginSuccess()
    {
        _isLoggedIn = true;
        SetAllInactive();
        _characterPanel.SetActive(true);
        Debug.Log("[GameFlowManager] Login OK ? CharacterSelection");
    }

    public void OnCharacterSelected()
    {
        SetAllInactive();
        _lobbyPanel.SetActive(true);

        NetworkController.Instance?.OnLobbyOpened();

        Debug.Log($"[GameFlowManager] Personaje {CharacterSelection.SelectedCharacter} elegido ? Lobby");
    }

    public void EnterGameplay()
    {
        SetAllInactive();
        _rageCanvas.SetActive(true);
        Debug.Log("[GameFlowManager] Entrando a gameplay");
    }

    public void ResetToLogin()
    {
        _isLoggedIn = false;
        CharacterSelection.SelectedCharacter = -1;
        ShowLogin();
        Debug.Log("[GameFlowManager] Reset ? Login");
    }

    private void ShowLogin()
    {
        SetAllInactive();
        _loginPanel.SetActive(true);
    }

    private void SetAllInactive()
    {
        _loginPanel.SetActive(false);
        _characterPanel.SetActive(false);
        _lobbyPanel.SetActive(false);
        _rageCanvas.SetActive(false);
    }

    public bool CanConnect() => _isLoggedIn && CharacterSelection.SelectedCharacter != -1;
    public bool IsLoggedIn => _isLoggedIn;
}