using UnityEngine;

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    [Header("Canvas")]
    [SerializeField] private GameObject loginCanvas;
    [SerializeField] private GameObject characterCanvas;
    [SerializeField] private GameObject connectionCanvas;
    [SerializeField] private GameObject gameplayHUD;

    public bool IsLoggedIn { get; private set; }
    public bool HasSelectedCharacter { get; private set; }

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        ShowLogin();
    }

    public void OnLoginSuccess()
    {
        if (!AuthenticationManager.Instance.IsSessionValid)
        {
            Debug.LogError("[Flow] Login inválido detectado");
            return;
        }
        IsLoggedIn = true;
        Debug.Log("[Flow] Login OK ? Ir a selección de personaje");
        ShowCharacterSelection();
    }

    public void OnCharacterSelected(int index)
    {
        if (!IsLoggedIn)
        {
            Debug.LogError("[Flow] Intento de seleccionar personaje sin login");
            return;
        }
        HasSelectedCharacter = true;
        Debug.Log("[Flow] Personaje seleccionado ? Ir a conexión");
        ShowConnection();
    }

    public bool CanConnect() => IsLoggedIn && HasSelectedCharacter;

    private void ShowLogin()
    {
        loginCanvas.SetActive(true);
        characterCanvas.SetActive(false);
        connectionCanvas.SetActive(false);
    }

    private void ShowCharacterSelection()
    {
        loginCanvas.SetActive(false);
        characterCanvas.SetActive(true);
        connectionCanvas.SetActive(false);
    }

    private void ShowConnection()
    {
        loginCanvas.SetActive(false);
        characterCanvas.SetActive(false);
        connectionCanvas.SetActive(true);
    }
    public void EnterGameplay()
    {
        Debug.Log("[Flow] Gameplay iniciado");

        loginCanvas.SetActive(false);
        characterCanvas.SetActive(false);
        connectionCanvas.SetActive(false);

        if (gameplayHUD != null)
            gameplayHUD.SetActive(true);
    }
    public void ResetToLogin()
    {
        Debug.Log("[Flow] Reset completo a login");

        IsLoggedIn = false;
        HasSelectedCharacter = false;

        CharacterSelection.SelectedPlayer = -1;

        ShowLogin();
    }

    public void Logout()
    {
        Debug.Log("[Flow] Cerrando sesión...");

        if (AuthenticationManager.Instance != null)
        {
            AuthenticationManager.Instance.SignOut();
        }

        ResetToLogin();
    }
}
