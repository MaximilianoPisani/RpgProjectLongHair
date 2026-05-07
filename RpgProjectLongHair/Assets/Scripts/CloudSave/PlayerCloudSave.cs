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

    private PlayerSaveData _cache = null;
    private bool _isLoading = false;
    private TaskCompletionSource<PlayerSaveData> _loadingTcs = null;

    private bool _isSaving = false;
    private bool _pendingSave = false;

    public async Task<PlayerSaveData> LoadPlayerData()
    {
        if (_cache != null) return _cache;

        if (_isLoading)
            return await _loadingTcs.Task;

        _isLoading = true;
        _loadingTcs = new TaskCompletionSource<PlayerSaveData>();

        try
        {
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                Debug.LogWarning("[CloudSave] No está logueado");
                _cache = new PlayerSaveData();
            }
            else
            {
                var keys = new HashSet<string> { SAVE_KEY };
                var result = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

                if (result.TryGetValue(SAVE_KEY, out Item item))
                {
                    string json = item.Value.GetAs<string>();
                    _cache = JsonUtility.FromJson<PlayerSaveData>(json) ?? new PlayerSaveData();
                    Debug.Log($"[CloudSave] Cargado -> Level:{_cache.level} Exp:{_cache.exp} Items:{_cache.inventoryItemIds?.Length ?? 0}");
                }
                else
                {
                    _cache = new PlayerSaveData();
                    Debug.Log("[CloudSave] Sin datos previos");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[CloudSave] Error al cargar: " + e.Message);
            _cache = new PlayerSaveData();
        }
        finally
        {
            _isLoading = false;
            _loadingTcs.SetResult(_cache);
        }

        return _cache;
    }

    public async Task SavePlayerData(PlayerSaveData saveData)
    {
        _cache = saveData;

        if (_isSaving)
        {
            _pendingSave = true;
            return;
        }

        _isSaving = true;

        do
        {
            _pendingSave = false;

            try
            {
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    Debug.LogWarning("[CloudSave] No está logueado");
                    break;
                }

                string json = JsonUtility.ToJson(_cache);
                var data = new Dictionary<string, object> { { SAVE_KEY, json } };
                await CloudSaveService.Instance.Data.Player.SaveAsync(data);
                Debug.Log($"[CloudSave] Guardado -> Level:{_cache.level} Exp:{_cache.exp} Items:{_cache.inventoryItemIds?.Length ?? 0}");
            }
            catch (Exception e)
            {
                Debug.LogError("[CloudSave] Error al guardar: " + e.Message);
            }

        } while (_pendingSave);

        _isSaving = false;
    }

    public void ClearCache() => _cache = null;
}