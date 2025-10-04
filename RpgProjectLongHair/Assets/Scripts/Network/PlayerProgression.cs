using Fusion;
using UnityEngine;

/// Implementar en componentes que deban reaccionar a subir de nivel (stats, equipo, VFX, etc.)
public interface IOnLevelUpReceiver { void OnLevelUp(int newLevel); }

public class PlayerProgression : NetworkBehaviour
{
    [Header("Config")]
    [SerializeField] private ExpConfigSO _config;

    [Header("Opcional: receptores de LevelUp (en este Player)")]
    [SerializeField] private MonoBehaviour[] _levelUpReceiversRaw;

    // ======= Estado (autoridad del server) =======
    [Networked] public int Level { get; private set; } = 1;
    [Networked] public int CurrentExp { get; private set; }
    [Networked] public int ExpToNext { get; private set; }

    // ======= HUD =======
    private PlayerExpHUD _hud;
    private bool _hudDirty;

    // Cache de receptores (para no castear cada frame)
    private IOnLevelUpReceiver[] _levelUpReceivers = System.Array.Empty<IOnLevelUpReceiver>();

    // Cache para diffs locales (evita trabajo en Render)
    private int _lastLevel = int.MinValue;
    private int _lastExp = int.MinValue;
    private int _lastTo = int.MinValue;

    private void Awake()
    {
        if (_levelUpReceiversRaw != null && _levelUpReceiversRaw.Length > 0)
        {
            var list = new System.Collections.Generic.List<IOnLevelUpReceiver>(_levelUpReceiversRaw.Length);
            foreach (var mb in _levelUpReceiversRaw)
                if (mb is IOnLevelUpReceiver r) list.Add(r);
            _levelUpReceivers = list.ToArray();
        }
    }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            Level = Mathf.Max(1, Level);
            CurrentExp = Mathf.Max(0, CurrentExp);
            ExpToNext = CalcExpToNextSafe(Level);
        }

        _hudDirty = true;
        _lastLevel = int.MinValue; // fuerza primer pintado
        _lastExp = int.MinValue;
        _lastTo = int.MinValue;
    }

    public override void Render()
    {
        if (!HasInputAuthority) return;

        // ¿cambió algo desde el último frame?
        if (Level != _lastLevel || CurrentExp != _lastExp || ExpToNext != _lastTo)
        {
            // Hooks de level-up locales
            if (Level != _lastLevel && _levelUpReceivers.Length > 0)
            {
                for (int i = 0; i < _levelUpReceivers.Length; i++)
                    _levelUpReceivers[i]?.OnLevelUp(Level);
            }

            // Actualizar HUD
            RefreshHUD();

            _lastLevel = Level;
            _lastExp = CurrentExp;
            _lastTo = ExpToNext;
        }
        else if (_hudDirty)
        {
            RefreshHUD();
            _hudDirty = false;
        }
    }

    public void BindHUD(PlayerExpHUD hud)
    {
        _hud = hud;
        _hudDirty = true; // fuerza un refresh próximo Render
    }

    // ======= API Server =======
    public void AddExpServer(int amount)
    {
        if (!Object.HasStateAuthority || amount <= 0) return;

        try { checked { CurrentExp += amount; } }
        catch { CurrentExp = int.MaxValue; }

        int guard = 256; // evita loops si config está mal
        while (CurrentExp >= ExpToNext && ExpToNext > 0 && guard-- > 0)
        {
            CurrentExp -= ExpToNext;
            Level++;
            ExpToNext = CalcExpToNextSafe(Level);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    public void RPC_RequestAward(ExpEvent evt, int hint = 0)
    {
        if (!Object.HasStateAuthority || _config == null) return;
        int amount = Mathf.Max(0, _config.GetExp(evt, hint));
        if (amount > 0) AddExpServer(amount);
    }

    // Persistencia
    public void LoadFromProfileServer(PlayerProfile p)
    {
        if (!Object.HasStateAuthority) return;
        Level = Mathf.Max(1, p.level);
        CurrentExp = Mathf.Max(0, p.currentExp);
        ExpToNext = Mathf.Max(1, p.expToNext > 0 ? p.expToNext : CalcExpToNextSafe(Level));
    }

    public PlayerProfile ToProfileServerSnapshot()
    {
        return new PlayerProfile { level = Level, currentExp = CurrentExp, expToNext = ExpToNext };
    }

    // ======= Helpers =======
    private int CalcExpToNextSafe(int lvl)
    {
        int v = (_config != null) ? _config.CalcExpToNext(Mathf.Max(1, lvl)) : 100;
        return Mathf.Max(1, v);
    }

    private int SafeToNext() => Mathf.Max(1, ExpToNext);

    private void RefreshHUD()
    {
        if (!HasInputAuthority || _hud == null) return;
        _hud.Set(Level, CurrentExp, SafeToNext());
        _hudDirty = false;
    }
}
