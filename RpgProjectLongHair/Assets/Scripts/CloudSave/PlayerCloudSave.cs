using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using Unity.Services.Authentication;

public class PlayerCloudSave : MonoBehaviour
{
    private const string SAVE_KEY = "player_data";

    public async Task SavePlayerData(PlayerSaveData saveData)
    {
        try
        {
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                Debug.LogWarning("[CloudSave] No está logueado");
                return;
            }

            if (saveData == null)
            {
                Debug.LogWarning("[CloudSave] saveData es null");
                return;
            }

            string json = JsonUtility.ToJson(saveData);

            var data = new Dictionary<string, object>
            {
                { SAVE_KEY, json }
            };

            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
            Debug.Log($"[CloudSave] Guardado -> Level: {saveData.level} | Exp: {saveData.exp}");
        }
        catch (Exception e)
        {
            Debug.LogError("[CloudSave] Error al guardar: " + e.Message);
        }
    }

    public async Task<PlayerSaveData> LoadPlayerData()
    {
        try
        {
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                Debug.LogWarning("[CloudSave] No está logueado");
                return new PlayerSaveData();
            }

            var keys = new HashSet<string> { SAVE_KEY };
            var result = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

            if (result.TryGetValue(SAVE_KEY, out Item item))
            {
                string json = item.Value.GetAs<string>();
                var playerData = JsonUtility.FromJson<PlayerSaveData>(json);

                if (playerData == null)
                {
                    Debug.LogWarning("[CloudSave] JSON inválido, devolviendo datos por defecto");
                    return new PlayerSaveData();
                }

                Debug.Log($"[CloudSave] Cargado -> Level: {playerData.level} | Exp: {playerData.exp}");
                return playerData;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[CloudSave] Error al cargar: " + e.Message);
        }

        Debug.Log("[CloudSave] Sin datos previos, iniciando desde cero");
        return new PlayerSaveData();
    }
}