using Fusion;
using UnityEngine;
using System.Threading.Tasks;

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

        _cloud = GetComponent<PlayerCloudSave>();
        if (_cloud == null)
            _cloud = gameObject.AddComponent<PlayerCloudSave>();

        if (Object.HasStateAuthority)
        {
            var data = await _cloud.LoadPlayerData();
            Level = data.level;
            CurrentExp = data.exp;
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

        var saveData = await _cloud.LoadPlayerData(); 
        saveData.level = Level;
        saveData.exp = CurrentExp;
        await _cloud.SavePlayerData(saveData);
    }
}