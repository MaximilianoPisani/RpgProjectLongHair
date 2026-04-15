using Fusion;
using UnityEngine;

public class PlayerExp : NetworkBehaviour
{
    [Networked] public int Level { get; private set; }
    [Networked] public int CurrentExp { get; private set; }
    [Networked] public int ExpToNextLevel { get; private set; }

    [SerializeField] private ExpConfigSO expConfig;

    private ChangeDetector _changeDetector;
    private PlayerCloudSave _cloud;

    public override async void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        _cloud = FindFirstObjectByType<PlayerCloudSave>();

        if (_cloud == null)
            Debug.LogWarning("[PlayerExp] PlayerCloudSave no encontrado en escena");

        if (Object.HasStateAuthority)
        {
            if (_cloud != null)
            {
                var data = await _cloud.LoadPlayerData();
                Level = data.level;
                CurrentExp = data.exp;
            }
            else
            {
                Level = 0;
                CurrentExp = 0;
            }

            ExpToNextLevel = expConfig.CalcExpToNext(Level);
        }

        if (Object.HasInputAuthority)
        {
            var hud = FindFirstObjectByType<PlayerExpHUD>();
            if (hud != null) hud.Bind(this);
        }
    }

    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            if (change == nameof(CurrentExp) || change == nameof(Level))
            {
                if (Object.HasInputAuthority)
                {
                    var hud = FindFirstObjectByType<PlayerExpHUD>();
                    if (hud != null) hud.OnExpUpdated(CurrentExp, ExpToNextLevel, Level);
                }
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

        if (_cloud != null)
            await _cloud.SavePlayerData(new PlayerSaveData { level = Level, exp = CurrentExp });
        else
            Debug.LogWarning("[PlayerExp] PlayerCloudSave no encontrado, no se guardó");
    }
}