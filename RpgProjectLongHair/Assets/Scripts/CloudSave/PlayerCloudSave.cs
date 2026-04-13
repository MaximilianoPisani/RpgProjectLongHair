using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using Unity.Services.Authentication;

public class PlayerCloudSave : MonoBehaviour
{
    private const string LEVEL_KEY = "player_level";
    private const string EXP_KEY = "player_exp";

    public async Task SavePlayerData(int level, int currentExp)
    {
        try
        {
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                Debug.LogWarning("[CloudSave] No está logueado");
                return;
            }

            var data = new Dictionary<string, object>
            {
                { LEVEL_KEY, level },
                { EXP_KEY,   currentExp }
            };

            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
            Debug.Log($"[CloudSave] Guardado -> Level: {level} | Exp: {currentExp}");
        }
        catch (Exception e)
        {
            Debug.LogError("[CloudSave] Error al guardar: " + e.Message);
        }
    }

    public async Task<(int level, int exp)> LoadPlayerData()
    {
        int level = 0;
        int exp = 0;

        try
        {
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                Debug.LogWarning("[CloudSave] No está logueado");
                return (level, exp);
            }

            var keys = new HashSet<string> { LEVEL_KEY, EXP_KEY };
            var result = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

            if (result.TryGetValue(LEVEL_KEY, out Item levelItem))
                level = levelItem.Value.GetAs<int>();

            if (result.TryGetValue(EXP_KEY, out Item expItem))
                exp = expItem.Value.GetAs<int>();

            Debug.Log($"[CloudSave] Cargado -> Level: {level} | Exp: {exp}");
        }
        catch (Exception e)
        {
            Debug.LogError("[CloudSave] Error al cargar: " + e.Message);
        }

        return (level, exp);
    }
}