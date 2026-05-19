using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;

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

    [Header("Login - Errores UI")]
    [SerializeField] private TMP_Text loginUsernameError;
    [SerializeField] private TMP_Text loginPasswordError;
    [SerializeField] private TMP_Text loginGeneralError;   

    [Header("Register Panel")]
    [SerializeField] private TMP_InputField regUsernameInput;
    [SerializeField] private TMP_InputField regPasswordInput;
    [SerializeField] private TMP_InputField regAgeInput;
    [SerializeField] private Button confirmSignUpButton;
    [SerializeField] private Button backToLoginButton;

    [Header("Register - Errores UI")]
    [SerializeField] private TMP_Text regUsernameError;
    [SerializeField] private TMP_Text regPasswordError;
    [SerializeField] private TMP_Text regAgeError;

    [Header("Toggle Password")]
    [SerializeField] private Button loginTogglePasswordButton;
    [SerializeField] private Button registerTogglePasswordButton;

    [SerializeField] private TMP_InputField loginPasswordField;
    [SerializeField] private TMP_InputField registerPasswordField;

    [SerializeField] private TMP_Text loginToggleText;
    [SerializeField] private TMP_Text registerToggleText;

    private bool loginPasswordVisible = false;
    private bool registerPasswordVisible = false;

    private void Start()
    {
        ValidateReferences();

        signInButton.onClick.AddListener(OnSignInClicked);
        openRegisterButton.onClick.AddListener(OpenRegisterPanel);
        anonymousButton.onClick.AddListener(OnAnonymousClicked);

        confirmSignUpButton.onClick.AddListener(OnSignUpClicked);
        backToLoginButton.onClick.AddListener(OpenLoginPanel);

        ShowPanel(loginPanel);

        loginTogglePasswordButton.onClick.AddListener(() =>
        TogglePassword(loginPasswordField, loginToggleText, ref loginPasswordVisible)
        );

        registerTogglePasswordButton.onClick.AddListener(() =>
        TogglePassword(registerPasswordField, registerToggleText, ref registerPasswordVisible)
        );

    }
    private void TogglePassword(TMP_InputField input, TMP_Text buttonText, ref bool isVisible)
    {
        isVisible = !isVisible;

        input.contentType = isVisible
            ? TMP_InputField.ContentType.Standard
            : TMP_InputField.ContentType.Password;

        input.ForceLabelUpdate();

        if (buttonText != null)
            buttonText.text = isVisible ? "O" : "-";
    }

    private void ClearLoginErrors()
    {
        SetError(loginUsernameError, null);
        SetError(loginPasswordError, null);
        SetError(loginGeneralError, null);
    }

    private void ClearRegisterErrors()
    {
        SetError(regUsernameError, null);
        SetError(regPasswordError, null);
        SetError(regAgeError, null);
    }

    private void SetError(TMP_Text label, string msg)
    {
        if (label == null) return;
        label.text = msg ?? "";
        label.gameObject.SetActive(!string.IsNullOrEmpty(msg));
    }


    private bool ValidateSignInInputs(string username, string password)
    {
        bool valid = true;

        if (string.IsNullOrWhiteSpace(username))
        {
            SetError(loginUsernameError, "El nombre de usuario es obligatorio.");
            valid = false;
        }
        else if (username.Length < 3)
        {
            SetError(loginUsernameError, "Mínimo 3 caracteres.");
            valid = false;
        }
        if (string.IsNullOrWhiteSpace(password))
        {
            SetError(loginPasswordError, "La contraseña es obligatoria.");
            valid = false;
        }
        else if (password.Length < 8)
        {
            SetError(loginPasswordError, "Mínimo 8 caracteres.");
            valid = false;
        }

        return valid;
    }


    private bool ValidateSignUpInputs(string username, string password, string ageText)
    {
        bool valid = true;

        if (string.IsNullOrWhiteSpace(username))
        {
            SetError(regUsernameError, "El nombre de usuario es obligatorio.");
            valid = false;
        }
        else if (username.Length < 3)
        {
            SetError(regUsernameError, "Mínimo 3 caracteres.");
            valid = false;
        }
        else if (username.Length > 20)
        {
            SetError(regUsernameError, "Máximo 20 caracteres.");
            valid = false;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            SetError(regPasswordError, "La contraseña es obligatoria.");
            valid = false;
        }
        else
        {
            string pwErr = GetPasswordError(password);
            if (pwErr != null)
            {
                SetError(regPasswordError, pwErr);
                valid = false;
            }
        }

        if (string.IsNullOrWhiteSpace(ageText))
        {
            SetError(regAgeError, "La edad es obligatoria.");
            valid = false;
        }
        else if (!int.TryParse(ageText, out int age))
        {
            SetError(regAgeError, "Ingresá un número válido.");
            valid = false;
        }
        else if (age < 1 || age > 120)
        {
            SetError(regAgeError, "La edad debe estar entre 1 y 120.");
            valid = false;
        }

        return valid;
    }

    private string GetPasswordError(string password)
    {
        if (password.Length < 8)
            return "Mínimo 8 caracteres.";
        if (!Regex.IsMatch(password, @"[A-Z]"))
            return "Debe tener al menos una mayúscula.";
        if (!Regex.IsMatch(password, @"[^a-zA-Z0-9]"))
            return "Debe tener al menos un símbolo (!@#$...).";
        if (!Regex.IsMatch(password, @"[0-9]"))
            return "Debe tener al menos un número.";
        return null;
    }


    private async void OnSignUpClicked()
    {
        ClearRegisterErrors();

        string username = regUsernameInput.text.Trim();
        string password = regPasswordInput.text;
        string ageText = regAgeInput.text.Trim();

        if (!ValidateSignUpInputs(username, password, ageText)) return;

        SetRegisterButtons(false);
        int? errorCode = await AuthenticationManager.Instance.SignUp(username, password);
        SetRegisterButtons(true);

        if (AuthenticationManager.Instance.IsSessionValid)
        {
            OnAuthSuccess();
        }
        else
        {
            switch (errorCode)
            {
                case 10002:
                    SetError(regUsernameError, "El nombre de usuario ya está en uso.");
                    break;
                default:
                    SetError(regUsernameError, "No se pudo completar el registro. Intentá de nuevo.");
                    break;
            }
        }
    }

    private async void OnSignInClicked()
    {
        ClearLoginErrors();

        string username = loginUsernameInput.text.Trim();
        string password = loginPasswordInput.text;

        if (!ValidateSignInInputs(username, password)) return;

        SetLoginButtons(false);

        bool success =
            await AuthenticationManager.Instance
                .SignIn(username, password);

        SetLoginButtons(true);

        if (success)
        {
            OnAuthSuccess();
        }
        else
        {
            SetError(
                loginGeneralError,
                "Usuario/contraseña incorrectos o la cuenta ya está en uso."
            );
        }
    }

    private async void OnAnonymousClicked()
    {
        ClearLoginErrors();
        SetLoginButtons(false);
        await AuthenticationManager.Instance.SignInAnonymously();
        SetLoginButtons(true);

        if (!AuthenticationManager.Instance.IsSessionValid)
            SetError(loginGeneralError, "No se pudo iniciar sesión anónima. Intentá de nuevo.");
        else
            OnAuthSuccess();
    }

    private void OpenRegisterPanel()
    {
        regUsernameInput.text = "";
        regPasswordInput.text = "";
        regAgeInput.text = "";
        ClearRegisterErrors();
        ShowPanel(registerPanel);
    }

    private void OpenLoginPanel()
    {
        ClearLoginErrors();
        ShowPanel(loginPanel);
    }

    private void OnAuthSuccess()
    {
        loginUsernameInput.text = "";
        loginPasswordInput.text = "";
        GameFlowManager.Instance.OnLoginSuccess();
    }

    private void ShowPanel(GameObject panel)
    {
        loginPanel.SetActive(panel == loginPanel);
        registerPanel.SetActive(panel == registerPanel);
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

    private void OnEnable()
    {
        if (GameFlowManager.Instance != null)
        {
            string msg = GameFlowManager.Instance.LastDisconnectMessage;
            if (!string.IsNullOrEmpty(msg))
            {
                SetError(loginGeneralError, msg);
                GameFlowManager.Instance.LastDisconnectMessage = null;
            }
        }
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