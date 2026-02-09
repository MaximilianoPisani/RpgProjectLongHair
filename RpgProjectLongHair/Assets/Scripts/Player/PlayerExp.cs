using Fusion;
using UnityEngine;

public class PlayerExp : NetworkBehaviour
{
    [Networked] public int TotalExp { get; private set; }

    private ChangeDetector _changeDetector;

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

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
            if (change == nameof(TotalExp))
            {
                OnExpChanged();
            }
        }
    }

    private void OnExpChanged()
    {
        if (!Object.HasInputAuthority) return;

        var hud = FindFirstObjectByType<PlayerExpHUD>();
        if (hud != null)
            hud.OnNetworkExpChanged(TotalExp);
    }

    public void AddExperience(int amount)
    {
        if (!Object.HasStateAuthority) return;

        TotalExp += amount;
        Debug.Log($"[PlayerExp] Added {amount}. Total={TotalExp}");
    }
}