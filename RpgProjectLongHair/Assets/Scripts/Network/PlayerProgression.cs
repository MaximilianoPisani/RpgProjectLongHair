using Fusion;
using UnityEngine;

public interface IOnLevelUpReceiver { void OnLevelUp(int newLevel); }

/*public class PlayerProgression : NetworkBehaviour
{
    [Header("Config")]
    [SerializeField] private ExpConfigSO _config;

    [Header("Tags (opcional)")]
    [SerializeField] private bool tagLocalPlayer = true;
    [SerializeField] private string localPlayerTag = "Player"; // <- ahora "Player"

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    [Networked] public int Level { get; private set; } = 1;
    [Networked] public int CurrentExp { get; private set; }
    [Networked] public int ExpToNext { get; private set; }

    private PlayerExpHUD _hud;
    private bool _hudDirty;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            Level = Mathf.Max(1, Level);
            CurrentExp = Mathf.Max(0, CurrentExp);
            ExpToNext = Mathf.Max(1, _config ? _config.CalcExpToNext(Level) : 100);
        }

        _hudDirty = true;

        // Taggeá al jugador LOCAL con "Player"
        if (tagLocalPlayer && HasInputAuthority)
        {
            gameObject.tag = localPlayerTag; // "Player"
            if (debugLogs) Debug.Log($"[PlayerProgression:{Object.Id}] Set tag '{localPlayerTag}' (local).");
        }
    }



    private void OnDisable()
    {
        if (tagLocalPlayer && HasInputAuthority && gameObject.tag == localPlayerTag)
            gameObject.tag = "Untagged";
    }

    public override void Render()
    {
        if (!HasInputAuthority) return;
        if (_hudDirty) { RefreshHUD(); _hudDirty = false; }
    }

    public void BindHUD(PlayerExpHUD hud)
    {
        _hud = hud;
        _hudDirty = true;
        if (_hud != null)
        {
            _hud.SetInstant(Level, CurrentExp, Mathf.Max(1, ExpToNext));
            if (debugLogs) Debug.Log($"[PlayerProgression:{Object.Id}] HUD bound (first paint).");
        }
    }

    public void AddExpServer(int amount)
    {
        if (!Object.HasStateAuthority || amount <= 0) return;
        try { checked { CurrentExp += amount; } } catch { CurrentExp = int.MaxValue; }

        int guard = 256;
        while (CurrentExp >= ExpToNext && ExpToNext > 0 && guard-- > 0)
        {
            CurrentExp -= ExpToNext;
            Level++;
            ExpToNext = Mathf.Max(1, _config ? _config.CalcExpToNext(Level) : Mathf.CeilToInt(ExpToNext * 1.35f));
        }
        _hudDirty = true;
    }

    private void RefreshHUD()
    {
        if (_hud == null) return;
        _hud.Set(Level, CurrentExp, Mathf.Max(1, ExpToNext));
    }

    /// RPC: el server avisa al cliente dueño que ganó EXP (solo a ese jugador)
    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority, Channel = RpcChannel.Reliable)]
    public void RPC_OnExpGained(int amount, Vector3 atWorld, RpcInfo info = default)
    {
        if (debugLogs)
            Debug.Log($"[PlayerProgression:{Object.Id}] Notificado +{amount} XP en {atWorld}");

        // Mostrar feedback en HUD, si existe
        if (_hud != null)
        {
            _hud.ShowXpGain(amount, atWorld);               // popup/texto (ver más abajo)
            _hud.Set(Level, CurrentExp, Mathf.Max(1, ExpToNext)); // pequeño “nudge” por si la réplica aún no llegó
        }
        else
        {
            // Si aún no hay HUD enlazado, la barra se actualizará igual
            // cuando la replicación de CurrentExp/Level llegue y tu Render/OnChanged refresque.
        }
    }
}*/

