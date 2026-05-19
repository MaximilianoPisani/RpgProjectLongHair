using Fusion;
using UnityEngine;

public class PlayerExp : NetworkBehaviour
{
    [Networked] public int Level { get; private set; }
    [Networked] public int CurrentExp { get; private set; }
    [Networked] public int ExpToNextLevel { get; private set; }

    [SerializeField] private ExpConfigSO expConfig;
    [SerializeField] private GameObject _expCanvas;

    private ChangeDetector _changeDetector;
    private PlayerCloudSave _cloud;
    private PlayerExpHUD _hud;

    public override async void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        _cloud = GetComponent<PlayerCloudSave>();
        if (_cloud == null)
            _cloud = gameObject.AddComponent<PlayerCloudSave>();

        if (Object.HasInputAuthority)
        {
            var data = await _cloud.LoadPlayerData();

            RPC_SendInitialExp(data.level, data.exp);
        }

        if (Object.HasInputAuthority)
        {
            if (_expCanvas != null)
                _expCanvas.SetActive(true);

            _hud = GetComponentInChildren<PlayerExpHUD>(true);

            if (_hud != null)
                _hud.Bind(this);
        }
        else
        {
            if (_expCanvas != null)
                _expCanvas.SetActive(false);
        }
    }

    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            if (change == nameof(CurrentExp) || change == nameof(Level))
            {
                if (Object.HasInputAuthority && _hud != null)
                    _hud.OnExpUpdated(CurrentExp, ExpToNextLevel, Level);
            }
        }
    }

    public async void AddExperience(int amount)
    {
        if (!Object.HasStateAuthority || amount <= 0) return;

        CurrentExp += amount;

        while (CurrentExp >= ExpToNextLevel)
        {
            CurrentExp -= ExpToNextLevel;
            Level++;
            ExpToNextLevel = expConfig.CalcExpToNext(Level);
        }

        var saveData = await _cloud.LoadPlayerData();
        saveData.level = Level;
        saveData.exp = CurrentExp;
        await _cloud.SavePlayerData(saveData);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    public void RPC_RequestAddExp(int amount)
    {
        AddExperience(amount);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SendInitialExp(int level, int exp)
    {
        Level = level;
        CurrentExp = exp;
        ExpToNextLevel = expConfig.CalcExpToNext(Level);
    }
}