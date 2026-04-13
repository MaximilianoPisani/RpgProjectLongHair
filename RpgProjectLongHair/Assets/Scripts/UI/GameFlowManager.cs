using UnityEngine;

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject _loginPanel;
    [SerializeField] private GameObject _lobbyPanel;
    [SerializeField] private GameObject _gameplayMainCanvas;
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

        _loginPanel.SetActive(false);
        _lobbyPanel.SetActive(true);

        _gameplayMainCanvas.SetActive(false); 
        _rageCanvas.SetActive(false);         
    }

    public void EnterGameplay()
    {
        _loginPanel.SetActive(false);
        _lobbyPanel.SetActive(false);

        _gameplayMainCanvas.SetActive(true); 
        _rageCanvas.SetActive(true);         
    }

    public void ResetToLogin()
    {
        _isLoggedIn = false;

        _loginPanel.SetActive(false);
        _lobbyPanel.SetActive(false);

        _gameplayMainCanvas.SetActive(false);
        _rageCanvas.SetActive(false);

        _loginPanel.SetActive(true);
    }
    private void ShowLogin()
    {
        _loginPanel.SetActive(true);
        _lobbyPanel.SetActive(false);

        _gameplayMainCanvas.SetActive(false);
        _rageCanvas.SetActive(false);
    }
    public bool CanConnect()
    {
        return _isLoggedIn;
    }

    public bool IsLoggedIn => _isLoggedIn;
}