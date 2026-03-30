using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AuthUIController : MonoBehaviour
{
    [Header("Paneles")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject registerPanel;

    [Header("Login Panel")]
    [SerializeField] private Button signInButton;
    [SerializeField] private Button openRegisterButton;
    [SerializeField] private Button anonymousButton;
    [SerializeField] private TMP_InputField loginUsernameInput;
    [SerializeField] private TMP_InputField loginPasswordInput;

    [Header("Register Panel")]
    [SerializeField] private TMP_InputField regUsernameInput;
    [SerializeField] private TMP_InputField regPasswordInput;
    [SerializeField] private TMP_InputField regAgeInput;
    [SerializeField] private Button confirmSignUpButton;
    [SerializeField] private Button backToLoginButton;

    private void Start()
    {
        ValidateReferences();

        signInButton.onClick.AddListener(OnSignInClicked);
        openRegisterButton.onClick.AddListener(OpenRegisterPanel);
        anonymousButton.onClick.AddListener(OnAnonymousClicked);

        confirmSignUpButton.onClick.AddListener(OnSignUpClicked);
        backToLoginButton.onClick.AddListener(OpenLoginPanel);

        ShowPanel(loginPanel);
    }

    private void OpenRegisterPanel()
    {
        Debug.Log("[AuthUI] Abriendo registro");

        regUsernameInput.text = "";
        regPasswordInput.text = "";
        regAgeInput.text = "";

        ShowPanel(registerPanel);
    }

    private void OpenLoginPanel()
    {
        Debug.Log("[AuthUI] Volviendo a login");
        ShowPanel(loginPanel);
    }

    private async void OnSignUpClicked()
    {
        string username = regUsernameInput.text.Trim();
        string password = regPasswordInput.text;
        string ageText = regAgeInput.text.Trim();

        if (string.IsNullOrWhiteSpace(username))
        {
            Debug.LogError("[AuthUI] Falta el nombre de usuario");
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            Debug.LogError("[AuthUI] Falta la contraseña");
            return;
        }

        if (string.IsNullOrWhiteSpace(ageText))
        {
            Debug.LogError("[AuthUI] Falta la edad");
            return;
        }

        if (!ValidateAge(ageText))
            return;

        int age = int.Parse(ageText);

        Debug.Log($"[AuthUI] Registrando '{username}' (Edad: {age})");

        SetRegisterButtons(false);

        await AuthenticationManager.Instance.SignUp(username, password);

        SetRegisterButtons(true);

        if (AuthenticationManager.Instance.IsSessionValid)
        {
            Debug.Log("[AuthUI] Registro exitoso");

            OnAuthSuccess();
        }
        else
        {
            Debug.LogWarning("[AuthUI] Registro fallido");
        }
    }

    private async void OnSignInClicked()
    {
        string username = loginUsernameInput.text.Trim();
        string password = loginPasswordInput.text;

        if (string.IsNullOrWhiteSpace(username))
        {
            Debug.LogError("[AuthUI] Falta el usuario");
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            Debug.LogError("[AuthUI] Falta la contraseña");
            return;
        }

        SetLoginButtons(false);

        await AuthenticationManager.Instance.SignIn(username, password);

        SetLoginButtons(true);

        if (AuthenticationManager.Instance.IsSessionValid)
            OnAuthSuccess();
        else
            Debug.LogWarning("[AuthUI] Login fallido");
    }

    private async void OnAnonymousClicked()
    {
        SetLoginButtons(false);

        await AuthenticationManager.Instance.SignInAnonymously();

        SetLoginButtons(true);

        if (AuthenticationManager.Instance.IsSessionValid)
            OnAuthSuccess();
        else
            Debug.LogWarning("[AuthUI] Login anónimo fallido");
    }

    private void OnAuthSuccess()
    {
        Debug.Log("[AuthUI] Login OK ? GameFlow");

        loginUsernameInput.text = "";
        loginPasswordInput.text = "";

        GameFlowManager.Instance.OnLoginSuccess();
    }

    private void ShowPanel(GameObject panel)
    {
        loginPanel.SetActive(panel == loginPanel);
        registerPanel.SetActive(panel == registerPanel);
    }

    private bool ValidateAge(string ageText)
    {
        if (!int.TryParse(ageText, out int age))
        {
            Debug.LogError("[AuthUI] Edad inválida");
            return false;
        }

        if (age < 1 || age > 120)
        {
            Debug.LogError("[AuthUI] Edad fuera de rango (1-120)");
            return false;
        }

        return true;
    }

    private void SetLoginButtons(bool state)
    {
        signInButton.interactable = state;
        openRegisterButton.interactable = state;
        anonymousButton.interactable = state;
    }

    private void SetRegisterButtons(bool state)
    {
        confirmSignUpButton.interactable = state;
        backToLoginButton.interactable = state;
    }

    private void ValidateReferences()
    {
        if (loginPanel == null) Debug.LogError("[AuthUI] loginPanel no asignado");
        if (registerPanel == null) Debug.LogError("[AuthUI] registerPanel no asignado");
        if (signInButton == null) Debug.LogError("[AuthUI] signInButton no asignado");
        if (openRegisterButton == null) Debug.LogError("[AuthUI] openRegisterButton no asignado");
        if (anonymousButton == null) Debug.LogError("[AuthUI] anonymousButton no asignado");
        if (loginUsernameInput == null) Debug.LogError("[AuthUI] loginUsernameInput no asignado");
        if (loginPasswordInput == null) Debug.LogError("[AuthUI] loginPasswordInput no asignado");
        if (regUsernameInput == null) Debug.LogError("[AuthUI] regUsernameInput no asignado");
        if (regPasswordInput == null) Debug.LogError("[AuthUI] regPasswordInput no asignado");
        if (regAgeInput == null) Debug.LogError("[AuthUI] regAgeInput no asignado");
        if (confirmSignUpButton == null) Debug.LogError("[AuthUI] confirmSignUpButton no asignado");
        if (backToLoginButton == null) Debug.LogError("[AuthUI] backToLoginButton no asignado");
    }
}