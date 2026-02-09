using Fusion;
using UnityEngine;

public class PlayerExp : NetworkBehaviour
{
    [Networked] public int Level { get; private set; }
    [Networked] public int CurrentExp { get; private set; }
    [Networked] public int ExpToNextLevel { get; private set; }

    [SerializeField] private ExpConfigSO expConfig;

    private ChangeDetector _changeDetector;

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (Object.HasStateAuthority)
        {
            Level = 1;
            CurrentExp = 0;
            ExpToNextLevel = expConfig.CalcExpToNext(Level);
        }

        if (Object.HasInputAuthority)
        {
            var hud = FindFirstObjectByType<PlayerExpHUD>();
            if (hud != null)
                hud.Bind(this);
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
                    if (hud != null)
                        hud.OnExpUpdated(CurrentExp, ExpToNextLevel, Level);
                }
            }
        }
    }

    public void AddExperience(int amount)
    {
        if (!Object.HasStateAuthority || amount <= 0) return;

        CurrentExp += amount;

        while (CurrentExp >= ExpToNextLevel)
        {
            CurrentExp -= ExpToNextLevel;
            Level++;

            ExpToNextLevel = expConfig.CalcExpToNext(Level);

            Debug.Log($"[PlayerExp] LEVEL UP ? {Level}");
        }
    }
}