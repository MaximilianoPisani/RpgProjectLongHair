using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class AuthenticationManager : MonoBehaviour
{
    public static AuthenticationManager Instance { get; private set; }

    private string _currentSessionId;

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
    public async Task<bool> SignIn(string username, string password)
    {
        if (AuthenticationService.Instance.IsSignedIn)
            return false;

        try
        {
            await AuthenticationService.Instance
                .SignInWithUsernamePasswordAsync(username, password);

            SessionData currentSession =
                await SessionManager.Instance.GetSessionData();

            if (currentSession != null &&
           currentSession.isOnline)
            {
                long now = System.DateTime.UtcNow.Ticks;

                long diffMinutes =
                    (now - currentSession.lastSeenTicks)
                    / System.TimeSpan.TicksPerMinute;

                bool sessionExpired = diffMinutes > 2;

                if (!sessionExpired)
                {
                    Debug.LogError("Cuenta ya en uso");

                    AuthenticationService.Instance.SignOut();

                    return false;
                }
            }

            _currentSessionId = System.Guid.NewGuid().ToString();

            await SessionManager.Instance.SetOnline(
                true,
                _currentSessionId
            );

            Debug.Log("Login OK");

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            return false;
        }
    }

    // Sign In Anónimo 
    public async Task SignInAnonymously()
    {
        try
        {
            if (AuthenticationService.Instance.IsSignedIn)
            {
                await SessionManager.Instance.SetOnline(
                    false,
                    _currentSessionId
                );

                AuthenticationService.Instance.SignOut(true);
            }

            AuthenticationService.Instance.ClearSessionToken();

            PlayerCloudSave[] saves =
                FindObjectsByType<PlayerCloudSave>(
                    FindObjectsSortMode.None);

            foreach (var save in saves)
            {
                save.ClearCache();
            }

            await AuthenticationService.Instance
                .SignInAnonymouslyAsync();

            _currentSessionId = System.Guid.NewGuid().ToString();

            await SessionManager.Instance.SetOnline(
                true,
                _currentSessionId
            );

            Debug.Log("[Auth] Login anónimo NUEVO");

            LogPlayerId();
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Auth] Error login anónimo: " + e.Message);
        }
    }

    public async void SignOut()
    {
        if (AuthenticationService.Instance.IsSignedIn)
        {
            await SessionManager.Instance.SetOnline(
                false,
                _currentSessionId
            );

            AuthenticationService.Instance.SignOut();

            _currentSessionId = null;
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

    private async void OnApplicationQuit()
    {
        try
        {
            if (AuthenticationService.Instance != null &&
                AuthenticationService.Instance.IsSignedIn)
            {
                await SessionManager.Instance.SetOnline(
                    false,
                    _currentSessionId
                );
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning(
                "[Auth] No se pudo cerrar sesión limpiamente: "
                + e.Message);
        }
    }

    public bool IsSessionValid =>
        AuthenticationService.Instance != null &&
        AuthenticationService.Instance.IsSignedIn &&
        !AuthenticationService.Instance.IsExpired;

    public string PlayerId =>
        IsSessionValid ? AuthenticationService.Instance.PlayerId : null;
}
