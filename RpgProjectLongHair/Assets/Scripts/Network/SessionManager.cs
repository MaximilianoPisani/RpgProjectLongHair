using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;

public class SessionManager : MonoBehaviour
{
    private const string SESSION_KEY = "session_data";

    public static SessionManager Instance { get; private set; }
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

    public async Task<bool> IsAccountInUse()
    {
        try
        {
            var keys = new HashSet<string> { SESSION_KEY };

            var result = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

            if (result.TryGetValue(SESSION_KEY, out Item item))
            {
                string json = item.Value.GetAs<string>();

                SessionData data = JsonUtility.FromJson<SessionData>(json);

                return data != null && data.isOnline;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }

        return false;
    }

    public async Task SetOnline(bool online, string sessionId)
    {
        SessionData data = new SessionData
        {
            isOnline = online,
            sessionId = sessionId,
            lastSeenTicks = System.DateTime.UtcNow.Ticks
        };

        string json = JsonUtility.ToJson(data);

        var save = new Dictionary<string, object>
    {
        { SESSION_KEY, json }
    };

        await CloudSaveService.Instance.Data.Player.SaveAsync(save);
    }

    public async Task<SessionData> GetSessionData()
    {
        try
        {
            var keys = new HashSet<string> { SESSION_KEY };

            var result = await CloudSaveService.Instance
                .Data.Player.LoadAsync(keys);

            if (result.TryGetValue(SESSION_KEY, out Item item))
            {
                string json = item.Value.GetAs<string>();

                return JsonUtility.FromJson<SessionData>(json);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }

        return null;
    }
}