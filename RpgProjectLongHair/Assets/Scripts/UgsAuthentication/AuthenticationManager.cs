using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class AuthenticationManager : MonoBehaviour
{
    public static AuthenticationManager Instance { get; private set; }

    private async void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        await InitializeUGS();
    }

    private async Task InitializeUGS()
    {
        try
        {
            await UnityServices.InitializeAsync();
            Debug.Log("[Auth] UGS inicializado");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Auth] Error inicializando UGS: " + e.Message);
        }
    }

    // Sign Up 
    public async Task<int?> SignUp(string username, string password)
    {
        if (!ValidateInputs(username, password)) return null;

        try
        {
            await AuthenticationService.Instance
                .SignUpWithUsernamePasswordAsync(username, password);

            Debug.Log("[Auth] Registro exitoso");
            LogPlayerId();
            return null;
        }
        catch (AuthenticationException e)
        {
            Debug.LogError("[Auth] Error en registro: " + e.Message);

            if (e.Message.ToLower().Contains("username already exists"))
                return 10002;

            return e.ErrorCode;
        }
        catch (RequestFailedException e)
        {
            Debug.LogError("[Auth] Error servidor (registro): " + e.Message);

            if (e.Message.ToLower().Contains("username already exists"))
                return 10002;

            return e.ErrorCode;
        }
    }

    // Sign In
    public async Task SignIn(string username, string password)
    {
        if (AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogWarning("[Auth] Ya hay sesión activa");
            return;
        }
        if (!ValidateInputs(username, password)) return;

        try
        {
            await AuthenticationService.Instance
                .SignInWithUsernamePasswordAsync(username, password);
            Debug.Log("[Auth] Login exitoso");
            LogPlayerId();
        }
        catch (AuthenticationException e)
        {
            switch (e.ErrorCode)
            {
                case 10001:
                    Debug.LogError("[Auth] Usuario o contraseña con formato incorrecto");
                    break;
                case 10003:
                    Debug.LogError("[Auth] Sesión expirada. Intentá de nuevo.");
                    break;
                default:
                    Debug.LogError("[Auth] Error en login: " + e.Message);
                    break;
            }
        }
        catch (RequestFailedException e)
        {
            if (e.ErrorCode == 401)
                Debug.LogError("[Auth] Usuario o contraseña incorrectos");
            else
                Debug.LogError("[Auth] Error servidor (login): " + e.Message);
        }
    }

    // Sign In Anónimo 
    public async Task SignInAnonymously()
    {
        if (AuthenticationService.Instance.IsSignedIn)
        {
            AuthenticationService.Instance.SignOut(true);
        }

        AuthenticationService.Instance.ClearSessionToken();

        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("[Auth] Login anónimo NUEVO");
            LogPlayerId();
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Auth] Error login anónimo: " + e.Message);
        }
    }

    public void SignOut()
    {
        if (AuthenticationService.Instance.IsSignedIn)
        {
            AuthenticationService.Instance.SignOut();
            Debug.Log("[Auth] Sesión cerrada");
        }
    }

    private void LogPlayerId()
    {
        if (IsSessionValid)
            Debug.Log($"[Auth] PlayerID: {AuthenticationService.Instance.PlayerId}");
    }

    private bool ValidateInputs(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
        {
            Debug.LogError("[Auth] Usuario inválido (mínimo 3 caracteres)");
            return false;
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            Debug.LogError("[Auth] Contraseña inválida (mínimo 8 caracteres)");
            return false;
        }
        return true;
    }

    public bool IsSessionValid =>
        AuthenticationService.Instance != null &&
        AuthenticationService.Instance.IsSignedIn &&
        !AuthenticationService.Instance.IsExpired;

    public string PlayerId =>
        IsSessionValid ? AuthenticationService.Instance.PlayerId : null;
}
